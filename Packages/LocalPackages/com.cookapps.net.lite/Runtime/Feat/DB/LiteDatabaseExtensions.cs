/*
* Copyright (c) CookApps.
*/

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using LiteDB;

namespace CookApps.NetLite.Feat.DB
{
    internal static class LiteDatabaseExtensions
    {
        // 추가는 거의 없지만 읽기위주로 사용되므로 ImmutableDictionary와 Volatile, Interlocked를 사용하여 스레드 안전성을 보장
        private static ImmutableDictionary<Type, string> _collectionNames = ImmutableDictionary<Type, string>.Empty;
        public static ILiteCollection<T> GetAutoMappedCollection<T>(this ILiteDatabase db)
        {
            Type type = typeof(T);
            ImmutableDictionary<Type, string> snap = Volatile.Read(ref _collectionNames);
            // 있으면 바로 반환
            if (snap.TryGetValue(type, out string collectionName))
            {
                return db.GetCollection<T>(collectionName);
            }

            // 없으면 추가
            var attr = type.GetCustomAttributes(typeof(BsonCollectionAttribute), false)
                .FirstOrDefault() as BsonCollectionAttribute;
            collectionName = attr?.Name ?? type.Name;
            AddOrReplace(type, collectionName);
            return db.GetCollection<T>(collectionName);
        }

        // 스레드 안전하게 컬렉션 이름을 추가 또는 교체
        private static void AddOrReplace(Type key, string value)
        {
            while (true)
            {
                var snapshot = Volatile.Read(ref _collectionNames);
                var updated = snapshot.SetItem(key, value);
                if (Interlocked.CompareExchange(ref _collectionNames, updated, snapshot) == snapshot)
                    break; // 성공적으로 교체
            }
        }
    }
}

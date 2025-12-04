/*
* Copyright (c) CookApps.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using CookApps.NetLite.Feat.Logger;
using CookApps.NetLite.Utils;
using JetBrains.Annotations;
using LiteDB;
using UnityEngine;

namespace CookApps.NetLite.Feat.DB
{
    public class InternalLiteDB : IDisposable
    {
        private bool Initialized => !_isDisposed && _liteDB != null;
        private bool _isDisposed;
        private readonly string _fileName;
        private readonly ILiteDatabase _liteDB;
        private readonly NetLogger.TaggedLogger _logger;

        private static class DbFields
        {
            public const string Id = "_id";
        }

        internal InternalLiteDB(string tag, string fileName)
        {
            _logger = NetLogger.WithTag(tag);

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            try
            {
                var connectionString = new ConnectionString($"filename={GetDBFullPath(fileName)};password={GetDBPassWord(fileName)}");
                var mapper = new BsonMapper
                {
                    EnumAsInteger = true,
                };
                _liteDB = new LiteDatabase(connectionString, mapper);

                _logger.Log($"DB initialized: {connectionString.Filename}, {connectionString.Password}");
            }
            catch (Exception e)
            {
                _logger.LogError(e);
            }
        }

        public int Count<T>() where T : new()
        {
            return ExecuteDbOperationLiteDB(db =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                int result = collection.Count();
                return result;
            });
        }

        /// 비어있는가?
        public bool IsEmpty<T>() where T : new()
        {
            return ExecuteDbOperationLiteDB(db =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                bool result = collection.Find(LiteDB.Query.All(), limit: 1).FirstOrDefault() == null;
                return result;
            });
        }

        /// id 기준으로 Ascending 정렬된 첫번째 데이터 얻기 (없으면 null)
        public T FirstOrDefault<T>() where T : new()
        {
            return ExecuteDbOperationLiteDB(db =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                T result = collection.Find(LiteDB.Query.All(DbFields.Id, LiteDB.Query.Ascending), limit: 1).FirstOrDefault();
                return result;
            });
        }

        /// id 기준으로 Descending 정렬된 첫번째 데이터 얻기 (없으면 null)
        public T LastOrDefault<T>() where T : new()
        {
            return ExecuteDbOperationLiteDB(db =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                T result = collection.Find(LiteDB.Query.All(DbFields.Id, LiteDB.Query.Descending), limit: 1).FirstOrDefault();
                return result;
            });
        }

        /// 데이터 삽입 (이미 존재하면 취소)
        public bool Insert<T>(T obj) where T : new()
        {
            return ExecuteDbOperationLiteDB(obj, (db, o) =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                collection.Insert(o); // 중복시 예외 발생으로 false 반환
                return true;
            });
        }

        /// 데이터 삽입 또는 업데이트
        public bool InsertOrReplace<T>(T obj) where T : new()
        {
            return ExecuteDbOperationLiteDB(obj, (db, o) =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                collection.Upsert(o); // upsert은 신규는 true, 업데이트는 false 반환
                return true; // 예외 발생의 경우 false 반환
            });
        }

        /// 데이터 삽입 (추가 중 이미 존재하면 모두 롤백)
        public bool InsertAll<T>(IEnumerable<T> objects) where T : new()
        {
            return ExecuteDbOperationLiteDB(objects, (db, o) =>
            {
                if (!db.BeginTrans())
                {
                    return false;
                }

                try
                {
                    ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                    collection.InsertBulk(o);
                    db.Commit();
                    return true;
                }
                catch (Exception)
                {
                    db.Rollback();
                    throw;
                }
            });
        }

        /// 데이터 삽입 또는 업데이트
        public bool InsertAllOrReplace<T>(IEnumerable<T> objects) where T : new()
        {
            return ExecuteDbOperationLiteDB(objects, (db, o) =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                collection.Upsert(o);
                return true; // 예외 발생의 경우 false 반환
            });
        }

        /// 데이터 삭제
        public bool DeleteById<T>(object id)
        {
            return ExecuteDbOperationLiteDB(id, (db, k) =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                return collection.Delete(new BsonValue(k));
            });
        }

        /// 모든 데이터 삭제
        public int DeleteAll<T>()
        {
            return ExecuteDbOperationLiteDB(db => db.GetAutoMappedCollection<T>().DeleteAll());
        }

        /// 전체 테이블 얻기
        public IEnumerable<T> Table<T>() where T : new()
        {
            return ExecuteDbOperationLiteDB(db => db.GetAutoMappedCollection<T>().FindAll());
        }

        /// id 값으로 데이터 얻기 (없으면 null)
        public T FindById<T>(object id) where T : new()
        {
            return ExecuteDbOperationLiteDB(id, (db, o) =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                BsonValue bsonValue = o is Enum ? new BsonValue(Convert.ToInt32(o)) : new BsonValue(o);
                return collection.FindById(bsonValue);
            });
        }

        /// 조건에 맞는 데이터 얻기 (없으면 null)
        public T Find<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            return ExecuteDbOperationLiteDB(predicate, (db, o) =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                return collection.FindOne(o);
            });
        }

        /// 조건에 맞는 데이터 얻기 (없으면 empty)
        public IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            return ExecuteDbOperationLiteDB(predicate, (db, o) =>
            {
                ILiteCollection<T> collection = db.GetAutoMappedCollection<T>();
                return collection.Find(o);
            });
        }

        /// <summary>
        /// 쿼리 요청
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>쿼리</returns>
        /// <example>
        /// Player에서 Score가 7000 이상인 플레이어를 내림차순으로 정렬하여 상위 10명을 가져오는 예시
        /// <code language="cs">
        /// var topPlayers = Query()
        ///     .Where(x => x.Score > 7000)
        ///     .OrderByDescending(x => x.Score)
        ///     .Limit(10)
        ///     .ToList();
        /// </code>
        /// </example>
        public ILiteQueryable<T> Query<T>() where T : new()
        {
            return _liteDB.GetAutoMappedCollection<T>().Query();
        }

        /// 컬렉션 얻기
        [CanBeNull]
        public ILiteCollection<T> GetCollection<T>() where T : new()
        {
            if (!Initialized)
                return null;
            return _liteDB.GetAutoMappedCollection<T>();
        }

        [CanBeNull]
        public ILiteCollection<BsonDocument> GetCollection(string collectionName)
        {
            if (!Initialized)
                return null;
            return _liteDB.GetCollection(collectionName);
        }

        /// Checkpoint
        public void Checkpoint()
        {
            if (!Initialized)
                return;
            _liteDB.Checkpoint();
        }

        /// 닫기
        public void Dispose()
        {
            if (!Initialized)
                return;

            try
            {
                _isDisposed = true;
                _liteDB.Dispose();
                _logger.Log("DB Disposed");
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        private void OnPause(bool pause)
        {
            if(pause)
            {
                Checkpoint();
            }
        }

        //----------------------------------------------------------------------

        // 파라미터가 있는 DB 작업을 위한 헬퍼 메서드
        private TResult ExecuteDbOperationLiteDB<TObj, TResult>(TObj obj, Func<ILiteDatabase, TObj, TResult> operation)
        {
            if (!Initialized)
                return default;

            try
            {
                return operation(_liteDB, obj);
            }
            catch (Exception e)
            {
                HandleException(e);
                return default;
            }
        }

        // 파라미터가 없는 DB 작업을 위한 헬퍼 메서드
        private TResult ExecuteDbOperationLiteDB<TResult>(Func<ILiteDatabase, TResult> operation)
        {
            if (!Initialized)
                return default;

            try
            {
                return operation(_liteDB);
            }
            catch (Exception e)
            {
                HandleException(e);
                return default;
            }
        }

        private TResult ExecuteDbQueryOperationLite<TResult>(
            string query,
            object[] args,
            Func<ILiteDatabase, string, object[], TResult> operation)
        {
            if (!Initialized)
                return default;

            try
            {
                return operation(_liteDB, query, args);
            }
            catch (Exception e)
            {
                HandleException(e);
                return default;
            }
        }

        private void HandleException(Exception e)
        {
            if (e is LiteException {InnerException: IOException} liteException)
            {
            }
            _logger.LogError($"DB Exception: {e}");
        }

        /// SQLite 전체 경로
        private static string GetSqLiteFullPath(string fileName)
        {
            var dbFileName = Hash.FNV1aHash(Application.identifier + $"{fileName}.db").ToString("x16");
            string fileFullPath = Path.Combine(Application.persistentDataPath, dbFileName);
            return fileFullPath;
        }

        /// LiteDB 전체 경로
        private static string GetDBFullPath(string fileName)
        {
            var dbFileName = Hash.FNV1aHash(Application.identifier + $"{fileName}.ldb").ToString("x16");
            string fileFullPath = Path.Combine(Application.persistentDataPath, dbFileName);
            return fileFullPath;
        }

        /// LiteDB 비밀번호
        private static string GetDBPassWord(string fileName)
        {
            var hash = Hash.FNV1aHash(Application.identifier + $"{fileName}.pwd").ToString("x16");
            return hash;
        }
    }
}

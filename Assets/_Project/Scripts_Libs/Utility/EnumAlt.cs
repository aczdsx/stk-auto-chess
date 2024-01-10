using System;

namespace CookApps.TeamBattle.Utility
{
    /// <summary>
    /// int에서 EnumAlt<T>로 형변환시 ToString은 하지마세요.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class EnumAlt<T> : IEquatable<EnumAlt<T>>, IEquatable<int>, IComparable {
        readonly protected int val;
        readonly protected string name;

        protected EnumAlt(int val, string name) {
            this.val = val; this.name = name;
        }

        int IComparable.CompareTo(object obj) {
            if (obj is EnumAlt<T>) {
                return val.CompareTo((obj as EnumAlt<T>).val);
            } else if (obj is int) {
                return val.CompareTo((int)obj);
            } else {
                throw new System.ArgumentException();
            }
        }

        public override bool Equals(object obj) {
            if (obj is EnumAlt<T>) {
                return val == (obj as EnumAlt<T>).val;
            } else if (obj is int) {
                return val == (int)obj;
            } else {
                throw new System.ArgumentException();
            }
        }
        bool IEquatable<int>.Equals(int other) => val == other;
        bool IEquatable<EnumAlt<T>>.Equals(EnumAlt<T> other) => val == other.val;
        public static bool operator==(EnumAlt<T> a, EnumAlt<T> b) => a.val == b.val;
        public static bool operator!=(EnumAlt<T> a, EnumAlt<T> b) => a.val != b.val;
        public static bool operator==(EnumAlt<T> a, int b) => a.val == b;
        public static bool operator!=(EnumAlt<T> a, int b) => a.val != b;
        public override int GetHashCode() => val;
        public static explicit operator int(EnumAlt<T> a) => a.val;
        public static explicit operator EnumAlt<T>(int a) => new EnumAlt<T>(a, String.Empty);
        public override string ToString() => name;
    }
}

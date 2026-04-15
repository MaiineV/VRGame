using System.Collections.Generic;

namespace Utilities.Pool
{
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new Stack<List<T>>();
        private const int MaxPoolSize = 32;
        private const int DefaultCapacity = 16;

        public static List<T> Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            return new List<T>(DefaultCapacity);
        }

        public static List<T> Get(int capacity)
        {
            var list = Get();
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
            return list;
        }

        public static void Return(List<T> list)
        {
            if (list == null) return;
            if (_pool.Count >= MaxPoolSize) return;

            list.Clear();
            _pool.Push(list);
        }
    }

    public static class HashSetPool<T>
    {
        private static readonly Stack<HashSet<T>> _pool = new Stack<HashSet<T>>();
        private const int MaxPoolSize = 16;

        public static HashSet<T> Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            return new HashSet<T>();
        }

        public static void Return(HashSet<T> set)
        {
            if (set == null) return;
            if (_pool.Count >= MaxPoolSize) return;

            set.Clear();
            _pool.Push(set);
        }
    }
}

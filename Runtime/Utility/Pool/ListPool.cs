using System.Collections.Generic;

namespace ProvisGames.Core.Utility
{
    internal sealed class ListPool<T>
    {
        private static Pool<List<T>> pool = new Pool<List<T>>(OnCreate, OnReset, IsEqual);
        private static List<T> OnCreate()
        {
            return new List<T>();
        }
        private static void OnReset(List<T> list)
        {
            list.Clear();
        }
        private static bool IsEqual(List<T> a, List<T> b)
        {
            return object.ReferenceEquals(a, b);
        }

        public static List<T> Get()
        {
            return pool.Get();
        }
        public static void Release(List<T> item)
        {
            pool.Release(item);
        }
    }
}

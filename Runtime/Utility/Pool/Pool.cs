using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProvisGames.Core.Utility
{
    /// <summary>
    /// 단순한 Pool 시스템
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Pool <T>
    {
        public delegate T Create();
        public delegate void Reset(T item);
        public delegate bool IsEqual(T a, T b);

        public List<T> m_Pool;
        private Create onCreate;
        private Reset onReset;
        private IsEqual equalityCheck;

        // Pool Count
        private int totalCount = 0;
        public int Total => totalCount;
        public int Active => Total - InActive;
        public int InActive => m_Pool.Count;

        public Pool(Create onCreate, Reset onReset, IsEqual equalityCheck)
        {
            this.m_Pool = new List<T>(0);
            this.onCreate = onCreate;
            this.onReset = onReset;
            this.equalityCheck = equalityCheck;
        }

        public T Get()
        {
            T item;
            if (InActive == 0)
            {
                item = this.onCreate();
                totalCount++;
            }
            else
            {
                item = m_Pool[m_Pool.Count - 1];
                m_Pool.RemoveAt(m_Pool.Count - 1);
            }

            return item;
        }

        public void Release(T item)
        {
            if (InActive > 0)
            {
                for (int i = 0; i < m_Pool.Count; i++)
                {
                    if (this.equalityCheck(m_Pool[i], item))
                    {
                        throw new ArgumentException("Same Instance of {nameof(T)} Exist in Pool.");
                    }
                }
            }

            this.onReset(item);
            m_Pool.Add(item);
        }
    }
}

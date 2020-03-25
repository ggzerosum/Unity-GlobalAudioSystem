using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProvisGames.Core.Utility
{
    public abstract class ComponentPool <T> where T : Component
    {
        private Pool<T> m_ComponentPool { get; set; }
        protected GameObject master { get; private set; }

        public ComponentPool(GameObject master)
        {
            this.master = master;
            this.m_ComponentPool = new Pool<T>(CreateComponent, ResetComponent, ComponentEqualityCheck);
        }

        protected T Get()
        {
            return m_ComponentPool.Get();
        }
        protected bool Release(T item)
        {
            try
            {
                m_ComponentPool.Release(item);
                return true;

            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                return false;
            }
        }

        protected abstract T CreateComponent();
        protected abstract void ResetComponent(T component);
        protected abstract bool ComponentEqualityCheck(T a, T b);
    }
}

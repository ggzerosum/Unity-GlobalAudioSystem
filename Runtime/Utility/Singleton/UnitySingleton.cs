using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProvisGames.Core.Utility
{
    public abstract class UnitySingleton<TDerived> : MonoBehaviour where TDerived : UnitySingleton<TDerived>
    {
        private static TDerived m_Singleton = null;
        public static TDerived Instance
        {
            get
            {
                if (m_Singleton == null)
                {
                    var go = new GameObject($"{typeof(TDerived)}_Singleton");
                    GameObject.DontDestroyOnLoad(go.gameObject);

                    TDerived component = go.AddComponent<TDerived>();
                    component.InitializeSingleton();

                    m_Singleton = component;
                }

                return m_Singleton;
            }
        }
        protected abstract void InitializeSingleton();
    }
}
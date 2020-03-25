using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProvisGames.Core.Utility;
using UnityEngine;

namespace ProvisGames.Core.AudioSystem
{
    /// <summary>
    /// ServiceLocator of AudioSystem
    /// </summary>
    public class ServiceLocator : UnitySingleton<ServiceLocator>
    {
        private GameObject m_ServiceLoactorGameObject;
        private GlobalAudioService m_AudioService;

        protected override void InitializeSingleton()
        {
            // Audio Service Create
            m_ServiceLoactorGameObject = new GameObject("AudioSystem ServiceLoactor");
            m_ServiceLoactorGameObject.transform.parent = this.transform;
            m_AudioService = new GlobalAudioService(m_ServiceLoactorGameObject);
        }

        public GlobalAudioService GetGlobalAudioService() => this.m_AudioService;

        private void Update()
        {
            m_AudioService.OnUpdate(Time.deltaTime);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using ProvisGames.Core.Utility;

namespace ProvisGames.Core.AudioSystem
{
    public interface IComponentGet<T> where T : Component
    {
        T Get();
    }
    public interface IComponentRelease<T> where T : Component
    {
        bool Release(T item);
    }

    /// <summary>
    /// Pooling Audiosource for memeory Efficency.
    /// </summary>
    public sealed class AudioSourcePool : ComponentPool<AudioSource>, IComponentGet<AudioSource>, IComponentRelease<AudioSource>
    {
        public AudioSourcePool(GameObject master) : base(master)
        {}

        public new AudioSource Get() // Hiding Base Get Keyword
        {
            return base.Get(); // But, use Get of Base. This is 'Intended'.
        }

        public new bool Release(AudioSource audioSource) // Hiding Base Release Keyword
        {
            return base.Release(audioSource); // But, use Release of Base. This is 'Intended'.
        }

        protected override AudioSource CreateComponent()
        {
            AudioSource audioSource = this.master.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = null;
            audioSource.loop = false;

            return audioSource;
        }
        protected override void ResetComponent(AudioSource component)
        {
            component.Stop();
            component.playOnAwake = false;
            component.clip = null;
            component.loop = false;
            component.volume = 0;
        }
        protected override bool ComponentEqualityCheck(AudioSource a, AudioSource b)
        {
            return object.ReferenceEquals(a, b);
        }
    }
}
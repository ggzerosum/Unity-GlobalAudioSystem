using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using ProvisGames.Core.Utility;

namespace ProvisGames.Core.AudioSystem
{
    public class GlobalAudioService
    {
        public enum PlayMode
        {
            Single,
            Additive
        }
        public enum MixMode
        {
            Transition,
            FadeIn,
            FadeOut,
        }

        private static AudioSetting Default = AudioSetting.NullSetting;
        public static AudioSetting DefaultSetting(AudioClip clip)
        {
            return new AudioSetting(clip, Default.IsLoop);
        }

        private Dictionary<int, AudioTrack> tracks;
        private AudioSourcePool sharedPool;

        public GlobalAudioService(GameObject master)
        {
            if (tracks == null)
                tracks = new Dictionary<int, AudioTrack>();

            if (sharedPool == null)
                sharedPool = new AudioSourcePool(master);
        }

        public void Play(int track, AudioSetting setting, PlayMode playMode)
        {
            TryCreateTrack(track);
            this.tracks[track].Play(setting, playMode, false);
        }
        public void Play(int track, AudioSetting setting, PlayMode playMode, MixMode mixmode)
        {
            TryCreateTrack(track);
            this.tracks[track].SetTrackMixer(SelectMixer(mixmode));
            this.tracks[track].Play(setting, playMode, true);
        }
        public void Stop(int track)
        {
            if (HasTrack(track))
            {
                this.tracks[track].Stop();
            }
        }
        /// <summary>
        /// set volume(0-1)
        /// </summary>
        /// <param name="track"></param>
        /// <param name="volume"></param>
        public void SetVolume(float volume, int track = -1)
        {
            volume = Mathf.Clamp01(volume);

            if (track < 0)
            {
                var trackKeys = tracks.Keys.ToArray();
                foreach (int trackKey in trackKeys)
                {
                    this.tracks[trackKey].SetVolume(volume);
                }
            }
            else if (HasTrack(track))
            {
                this.tracks[track].SetVolume(volume);
            }
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (KeyValuePair<int, AudioTrack> pair in tracks)
            {
                pair.Value.OnUpdate(deltaTime);
            }
        }

        public bool HasTrack(int track)
        {
            return this.tracks.ContainsKey(track);
        }
        public bool TryCreateTrack(int track)
        {
            if (!HasTrack(track))
            {
                tracks.Add(track, new AudioTrack(sharedPool));
                return true;
            }

            return false;
        }

        private Mixer<AudioTrack.AudioPlayer> SelectMixer(MixMode mixMode)
        {
            switch (mixMode)
            {
                case MixMode.Transition:
                    return AudioMixerFactory.CreateVolumeTransitionMixer();

                case MixMode.FadeIn:
                    return AudioMixerFactory.CreateVolumeFadeInMixer();

                case MixMode.FadeOut:
                    return AudioMixerFactory.CreateVolumeFadeOutMixer();

                default:
                    return AudioMixerFactory.CreateNullMixer();
            }
        }

        public struct AudioSetting
        {
            public static AudioSetting NullSetting = new AudioSetting(null, false);

            public AudioSetting(AudioClip clip, bool isLoop)
            {
                this.Clip = clip;
                this.IsLoop = isLoop;
            }

            public AudioClip Clip { get; private set; }
            public bool IsLoop;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using ProvisGames.Core.Utility;

namespace ProvisGames.Core.AudioSystem
{
    public class GlobalAudioService
    {
        /*
         * GloablAudioService 작동 개요
         *
         * 1. Mixing 중이 아닐 때
         *  Single
         *      Track의 다른 오디오를 전부 Stop시키고 자신만 실행됩니다.
         *
         *  Additive:
         *      Track의 다른 오디오는 그대로 둔 채 자신을 마지막에 추가합니다.
         *
         * 2. Mixing 중 일 때
         *  Single
         *      Track의 다른 오디오 마지막에 추가되며, Mixing Pivot을 자신에게 당겨옵니다.
         *      따라서, Mixing Pivot의 우측에 자신만 존재하게됩니다.
         *
         *  Additive:
         *      Track의 다른 오디오 마지막에 추가되며, Mixing Pivot을 그대로 둡니다.
         *      따라서, Mixing Pivot의 우측에 여러개의 오디오가 존재할 수 있으며 자기 자신을 추가하기만 합니다.
         */
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
            this.tracks[track].PlayAudio(setting, playMode);
        }
        public void Play(int track, AudioSetting setting, PlayMode playMode, MixMode mixmode)
        {
            TryCreateTrack(track);
            this.tracks[track].PlayAudio(setting, playMode);
            this.tracks[track].SetTrackMixer(SelectMixer(mixmode));
            this.tracks[track].DoMixing();
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

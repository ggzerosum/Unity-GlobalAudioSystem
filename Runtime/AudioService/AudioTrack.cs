using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using ProvisGames.Core.Utility;
using UnityEngine.AddressableAssets;

namespace ProvisGames.Core.AudioSystem
{
    public class AudioTrack
    {
        [Flags]
        public enum State : int
        {
            None = 0b0000,
            Play = 0b0001,
            Lerp = 0b0010
        }

        // Shared Pool
        private readonly AudioSourcePool pool;
        private Mixer<AudioPlayer> trackMixer;

        // These two Audio List need to be unordered.
        private List<AudioPlayer> audioSources;
        private int TotalCount => audioSources.Count;
        private int PlayingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < audioSources.Count; i++)
                {
                    if (audioSources.Count > i && audioSources[i].Audio != null)
                    {
                        if (HasFlag(i, State.Play))
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        // Mixing
        private int pivot = -1;
        private bool allowMix = false;
        private bool isMixing = false;

        // Dirty Flag
        private bool isOptionDirty = false;

        // Options
        private float trackVolume = 1.0f;

        public AudioTrack(AudioSourcePool pool)
        {
            this.pool = pool;
            this.audioSources = new List<AudioPlayer>();
            this.trackMixer = AudioMixerFactory.CreateNullMixer();
        }

        public void OnUpdate(float deltaTime)
        {
            // Update Mixing
            if (!UpdateMixing(deltaTime))
            {
                // 여기서만 Inspect 해야하는 지 생각해보기.
                InspectAudioList(); // Update Audio State
            }

            UpdateOption();
        }

        private void InspectAudioList()
        {
            var removableList = ListPool<int>.Get();

            for (int i = 0; i < audioSources.Count; i++) // front to end
            {
                Debug.Log($"{i}번째 오디오 : {audioSources[i].IsAudioPlaying()}");
                if (!audioSources[i].IsAudioPlaying())
                {
                    RemoveFlag(i, State.Play);
                }

                bool hasAny = HasFlag(i, State.Play) || HasFlag(i, State.Lerp);

                if (!hasAny)
                {
                    removableList.Add(i);
                    Debug.Log($"반환되어야할 오디오 인덱스 : {i}");
                }
            }

            for (int i = removableList.Count - 1; i >= 0; i--) // end to front, for avoid index change
            {
                this.pivot = ReMapPivotWhenElementRemoved(audioSources.Count, removableList[i]);
                Debug.Log($"Moved Pivot: {this.pivot}");
                AudioSource audiosource = this.audioSources[removableList[i]].Audio;
                audioSources.RemoveAt(i); // Remove Inverse. This algorithm doesn't change index of list when remove
                // Audio Release to Pool
                pool.Release(audiosource);
            }

            ListPool<int>.Release(removableList);
        }
        private int ReMapPivotWhenElementRemoved(int arrayLength, int removeIndex)
        {
            if (arrayLength == 0)
                throw new ArgumentOutOfRangeException("arrayLength must bigger than 0");

            if (arrayLength == 1) // removable Index is just one (0 index)
            {
                return -1;
            }

            if (removeIndex < this.pivot)
            {
                return this.pivot - 1;
            }
            else if (removeIndex == this.pivot)
            {
                if (arrayLength - 1 == removeIndex) // if remove Index is last index of array
                {
                    return this.pivot - 1;
                }
            }

            return this.pivot;
        }
        /// <summary>
        /// Update Mixing Algorithm
        /// </summary>
        /// <returns>when Mixing, return true else return false</returns>
        private bool UpdateMixing(float deltaTime)
        {
            if (allowMix && !isMixing)
            {
                if (pivot < 0)
                {
                    EndMixing();
                }

                // Notify Begin Mixing
                BeginMixing();
            }

            // Mix Audio
            if (isMixing)
            {
                // Get List from Pool
                var left = ListPool<AudioPlayer>.Get();
                var right = ListPool<AudioPlayer>.Get();

                // there's one or no audio available
                if (pivot <= 0)
                {
                    EndMixing();
                    ListPool<AudioPlayer>.Release(left);
                    ListPool<AudioPlayer>.Release(right);
                    return false;
                }

                GetDividedListsByPivot(left, right, ApplLerpFlag, ApplLerpFlag);
                if (!trackMixer.Mix(left, right, deltaTime)) // Mix On Every Frame.
                {
                    // When Mix Done
                    EndMixing();
                }

                ListPool<AudioPlayer>.Release(left);
                ListPool<AudioPlayer>.Release(right);

                return true;
            }

            return false;

            void ApplLerpFlag(int i)
            {
                AddFlag(i, State.Lerp);
            }
        }
        private void UpdateOption()
        {
            if (isOptionDirty)
            {
                for (int i = 0; i < audioSources.Count; i++)
                {
                    audioSources[i] = new AudioPlayer(audioSources[i], new AudioPlayer.AudioVolume(0.0f, this.trackVolume));
                }

                isOptionDirty = false;
            }
        }

        private void BeginMixing()
        {
            trackMixer.BeginMix();
            isMixing = true;
        }
        private void EndMixing()
        {
            if (isMixing)
            {
                trackMixer.EndMix();

                if (pivot >= 0)
                {
                    for (int i = 0; i < pivot + 1; i++)
                    {
                        RemoveFlag(i, State.Lerp);
                    }
                }
            }

            allowMix = false;
            isMixing = false;
        }

        private void GetDividedListsByPivot(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right, Action<int> onLeft, Action<int> onRight)
        {
            // Seperate All Audio By Pivot
            for (int i = 0; i < audioSources.Count; i++)
            {
                if (i < pivot)
                {
                    left.Add(audioSources[i]);
                    onLeft(i);
                }
                else
                {
                    right.Add(audioSources[i]);
                    onRight(i);
                }
            }
        }
        public void SetTrackMixer(Mixer<AudioPlayer> mixer)
        {
            if (this.trackMixer.GetType().IsAssignableFrom((mixer.GetType())))
            {
                Debug.Log("Same type of Mixer Ignored");
                return;
            }

            this.trackMixer = mixer;
        }

        public void Play(GlobalAudioService.AudioSetting setting, GlobalAudioService.PlayMode mode, bool mix)
        {
            AudioSource source = this.pool.Get();
            source.clip = setting.Clip;
            source.loop = setting.IsLoop;
            source.volume = this.trackVolume;
            AudioPlayer audioPlayer = new AudioPlayer(source, new AudioPlayer.AudioVolume(0.0f, this.trackVolume), State.Play);

            if (mix)
            {
                // If Mode is Single, Pivot always move to lastest player
                // If Mode is Additive, Pivot stay while mixing
                if (!isMixing || mode == GlobalAudioService.PlayMode.Single)
                    pivot = audioSources.Count; // Update should be on main thread.

                trackMixer.SettingTarget(audioPlayer);
                allowMix = true;
            }
            else
            {
                // Not Mix Mode
                if (mode == GlobalAudioService.PlayMode.Single) // And Just Sigle Clip should be play
                {
                    Stop(); // Stop all audio in track that has been playing
                }
                else if (mode == GlobalAudioService.PlayMode.Additive)// Mode == Additive
                {

                }
            }

            audioSources.Add(audioPlayer);
            source.Play();
        }
        public void Stop()
        {
            // Always Stop Immediately now
            EndMixing();
            for (int i = 0; i < audioSources.Count; i++)
            {
                audioSources[i].StopAndEjectClip();
                RemoveFlag(i, State.Play);
            }
        }

        public void SetVolume(float volume)
        {
            this.trackVolume = Mathf.Clamp01(volume);
            this.isOptionDirty = true;
        }

        private bool HasFlag(int index, State check)
        {
            if (index >= this.TotalCount)
            {
                throw new IndexOutOfRangeException("Index is out of Track's Capacity");
            }

            return (audioSources[index].State & check) == check;
        }
        private void AddFlag(int index, State state)
        {
            if (index >= this.TotalCount)
            {
                throw new IndexOutOfRangeException("Index is out of Track's Capacity");
            }

            State audioState = audioSources[index].State | state;
            audioSources[index] = new AudioPlayer(audioSources[index], audioState);
        }
        private void RemoveFlag(int index, State state)
        {
            if (index >= this.TotalCount)
            {
                throw new IndexOutOfRangeException("Index is out of Track's Capacity");
            }

            State audioState = ~((~audioSources[index].State) | state);
            audioSources[index] = new AudioPlayer(audioSources[index], audioState);
        }
        private void ClearFlag(int index)
        {
            if (index >= this.TotalCount)
            {
                throw new IndexOutOfRangeException("Index is out of Track's Capacity");
            }

            audioSources[index] = new AudioPlayer(audioSources[index], State.None);
        }

        public struct AudioPlayer
        {
            public AudioPlayer(AudioPlayer other):
                this(other.Audio, other.Volume, other.State)
            {}
            public AudioPlayer(AudioPlayer other, AudioVolume volume):
                this(other.Audio, volume, other.State)
            {}
            public AudioPlayer(AudioPlayer other, State state) :
                this(other.Audio, other.Volume, state)
            {}
            public AudioPlayer(AudioSource audio, AudioVolume volume)
            {
                this.Audio = audio;
                this.Volume = volume;
                this.State = State.None;
            }
            public AudioPlayer(AudioSource audio, AudioVolume volume, State state)
            {
                this.Audio = audio;
                this.Volume = volume;
                this.State = state;
            }

            public AudioSource Audio { get; private set; }
            public AudioVolume Volume { get; private set; }
            public State State { get; private set; }

            public void SetState(State state)
            {
                this.State = state;
            }
            public void StopAndEjectClip()
            {
                this.Audio.Stop();
                this.Audio.clip = null;
            }
            public bool IsAudioPlaying()
            {
                return this.Audio?.isPlaying ?? false;
            }

            public struct AudioVolume
            {
                public AudioVolume(float min, float max)
                {
                    this.Min = min;
                    this.Max = max;
                }

                public float Min { get; }
                public float Max { get; }
            }
        }
    }
}
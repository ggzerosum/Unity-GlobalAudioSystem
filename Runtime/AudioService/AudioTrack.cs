using System;
using System.Collections.Generic;
using UnityEngine;

using ProvisGames.Core.Utility;

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
        private readonly AudioSourcePool sharedAudioSourcePool;
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
        private int _mixingPivot = -1;
        private bool _allowMix = false;
        private bool _isMixing = false;
        // 현재 Track이 Mixing 기능을 실행중인 지 확인합니다.
        public bool IsMixing => this._isMixing;

        // Dirty Flag
        private bool shouldApplyOptionWhenPossible = false;
        private bool isOptionDataDirty = false;

        // Options
        private float trackVolume = 1.0f;

        public AudioTrack(AudioSourcePool pool)
        {
            this.sharedAudioSourcePool = pool;
            this.audioSources = new List<AudioPlayer>();
            this.trackMixer = AudioMixerFactory.CreateNullMixer();
        }

        public void OnUpdate(float deltaTime)
        {
            // Mixing이 실행되면 오디오에 다양한 상태변화가 일어나므로 옵션이 변경되더라도 Mixing이 완료된 후에 적용해야한다.
            // 이 구문에서는 옵션의 데이터만을 갱신한다.
            // 1프레임 뒤에 옵션이 적용되는 것을 방지하기위해, 데이터를 먼저 갱신한다.
            UpdateOptionDataOnly();

            // Update Mixing
            if (!UpdateMixing(deltaTime))
            {
                // 여기서만 Inspect 해야하는 지 생각해보기.
                InspectAudioList(); // Update Audio State

                // 갱신되었던 옵션 데이터를 Mixing이 일어나지 않을 때 즉시 적용한다.
                ApplyOption();
            }
        }


        // AudioPlayer를 실행 목록에 올립니다. 추가된 AudioPlayer는 자동으로 실행됩니다.
        public void PlayAudio(GlobalAudioService.AudioSetting setting, GlobalAudioService.PlayMode mode/*, bool mix*/)
        {
            // Mix와 Audio Play의 알고리즘은 철저히 분리되어야하며, Pivot을 통해서만 서로 제약해야한다.
            AudioSource source = this.sharedAudioSourcePool.Get();
            source.clip = setting.Clip;
            source.loop = setting.IsLoop;
            source.volume = this.trackVolume;
            AudioPlayer audioPlayer = new AudioPlayer(source, new AudioPlayer.AudioVolume(0.0f, this.trackVolume), State.Play);

            if (_isMixing)
            {
                // 믹싱 중일 때 Single을 사용하면 Pivot을 갱신시켜야한다. Additive는 Pivot을 건드리면 안된다.
                if (mode == GlobalAudioService.PlayMode.Single)
                {
                    // Pivot을 추가할 오디오 플레이어의 위치로 미리 옮깁니다.
                    MoveMixingPivot(audioSources.Count - _mixingPivot);
                }

                // Mixing 중일 때 새로 추가된 Audio는 Mixer가 필요로하는 AudioPlayer의 초기 세팅을 적용해준다.
                trackMixer.AttuneAudioPlayerToMixer(ref audioPlayer);
            }
            else // Mixing 중이 아닐 때
            {
                // Pivot을 추가할 오디오 플레이어의 위치로 미리 옮깁니다.
                MoveMixingPivot(audioSources.Count - _mixingPivot);

                if (mode == GlobalAudioService.PlayMode.Single) // And Just Sigle Clip should be play
                {
                    Stop();
                }
                else if (mode == GlobalAudioService.PlayMode.Additive)// Mode == Additive
                {
                    // 반드시 아무것도 하지 말아야한다.
                }
            }

            // 오디오플레이어를 추가함으로써 미리 옮겨둔 Pivot과 위치가 동일해진다.
            audioSources.Add(audioPlayer);
            source.Play();
        }

        // Track의 모든 오디오를 강제 종료시킵니다.
        public void Stop()
        {
            // Always Stop Immediately now
            StopMixing(true);

            for (int i = 0; i < audioSources.Count; i++)
            {
                audioSources[i].StopAndEjectClip();
                RemoveFlag(i, State.Play);
            }
        }

        // Track에서 사용할 Mixer를 교체합니다.
        public void SetTrackMixer(Mixer<AudioPlayer> mixer)
        {
            // 믹싱이 진행중 일 때, 같은 타입의 Mixer가 투입될 경우 무시해야함.
            if (_isMixing && this.trackMixer.GetType() == mixer.GetType())
            {
                Debug.Log("Same type of Mixer Ignored while mixing");
                return;
            }

            this.trackMixer = mixer;
        }

        // Mix 기능을 실행할 수 있으면 현재 트랙에서 Mix를 실행합니다.
        public void DoMixing()
        {
            if (CanMix())
            {
                DoMixBehaviour();
            }
        }

        // Mix를 강제로 종료합니다.
        // immediate true: mixer를 즉시 종료 시킵니다.
        // immediate false: 다음 업데이트때 mixer 종료가 반영됩니다.
        public void StopMixing(bool immediate = true)
        {
            EndMixing();

            if (immediate)
                UpdateMixing(0);
        }

        public void SetVolume(float volume)
        {
            this.trackVolume = Mathf.Clamp01(volume);
            this.isOptionDataDirty = true;
        }


        private void InspectAudioList()
        {
            var removableList = ListPool<int>.Get();

            for (int i = 0; i < audioSources.Count; i++) // front to end
            {
                //Debug.Log($"{i}번째 오디오 : {audioSources[i].IsAudioPlaying()}");
                if (!audioSources[i].IsAudioPlaying())
                {
                    RemoveFlag(i, State.Play);
                }

                bool hasAny = HasFlag(i, State.Play) || HasFlag(i, State.Lerp);

                if (!hasAny)
                {
                    removableList.Add(i);
                    //Debug.Log($"반환되어야할 오디오 인덱스 : {i}");
                }
            }

            // 가장 뒤부터 차례대로 삭제함으로써 RemoveAt을 For루프 안에서 사용할 수 있게한다.
            for (int i = removableList.Count - 1; i >= 0; i--) // end to front, for avoid index change
            {
                int indexOfAudioSource = removableList[i];
                ReMapMixingPivotWhenElementRemoved(audioSources.Count, indexOfAudioSource);

                AudioSource audiosource = this.audioSources[indexOfAudioSource].Audio;
                audioSources.RemoveAt(indexOfAudioSource); // Remove Inverse. This algorithm doesn't change index of list when remove
                
                // Audio Release to Pool
                sharedAudioSourcePool.Release(audiosource);
            }

            ListPool<int>.Release(removableList);
        }

        /// <summary>
        /// Update Mixing Algorithm
        /// </summary>
        /// <returns>when Mixing, return true else return false</returns>
        private bool UpdateMixing(float deltaTime)
        {
            if (_allowMix && !_isMixing)
            {
                if (!CanMix())
                {
                    EndMixing();
                    return false;
                }

                // Notify Begin Mixing
                BeginMixing();
            }

            // Mix Audio
            if (_isMixing)
            {
                // Get List from Pool
                var left = ListPool<AudioPlayer>.Get();
                var right = ListPool<AudioPlayer>.Get();

                // there's one or no audio available
                if (!CanMix())
                {
                    EndMixing();
                    ListPool<AudioPlayer>.Release(left);
                    ListPool<AudioPlayer>.Release(right);
                    return false;
                }

                GetDividedListsByPivot(left, right, MarkLerpFlag, MarkLerpFlag);
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

            void MarkLerpFlag(int i)
            {
                AddFlag(i, State.Lerp);
            }
        }

        // 옵션의 '데이터'만을 갱신합니다.
        private void UpdateOptionDataOnly()
        {
            if (isOptionDataDirty)
            {
                for (int i = 0; i < audioSources.Count; i++)
                {
                    audioSources[i] = new AudioPlayer(audioSources[i], new AudioPlayer.AudioVolume(0.0f, this.trackVolume));
                }

                isOptionDataDirty = false;
                shouldApplyOptionWhenPossible = true;
            }
        }

        // 갱신된 옵션을 '적용'합니다.
        private void ApplyOption()
        {
            if (shouldApplyOptionWhenPossible)
            {
                for (int i = 0; i < audioSources.Count; i++)
                {
                    audioSources[i].ApplyOptionDataImmediately();
                }

                shouldApplyOptionWhenPossible = false;
            }
        }


        private void BeginMixing()
        {
            trackMixer.BeginMix();
            _isMixing = true;
        }
        
        // EndMixing과 Play Audio입력이 경쟁상태에 빠져서는 안된다. 동기함수로 작성할 것을 강제한다.
        private void EndMixing()
        {
            if (_isMixing)
            {
                trackMixer.EndMix();

                // Mixing은 Track내 전체 오디오를 반으로 나눠서 계산하므로
                // 끝나고나면 AudioPlayer에 Lerp가 입력된 상태이다. 따라서 Mixing을 끝낼 때
                // 트랙 내 모든 AudioPlayer의 Lerp 상태를 해제해야한다.
                for (int i = 0; i < audioSources.Count; i++)
                {
                    RemoveFlag(i, State.Lerp);
                }
            }

            _allowMix = false;
            _isMixing = false;
        }

        private void ReMapMixingPivotWhenElementRemoved(int arrayLength, int removeIndex)
        {
            // 배열의 길이가 0일 경우에는 Pivot을 움직일 공간 자체가 없으므로 예외처리한다.
            if (arrayLength == 0)
                throw new ArgumentOutOfRangeException("arrayLength must bigger than 0");

            if (arrayLength == 1) // removable Index is just one (0 index)
            {
                ResetMixingPivot();
            }

            // Pivot 좌측의 Index를 삭제할 경우, Pivot을 좌측으로 당겨야한다.
            if (removeIndex < this._mixingPivot)
            {
                DecrementMixingPivot();
            }
            // Pivot의 위치와 동일한 Index를 삭제할 경우, Index가 배열의 마지막 요소였을 경우 Pivot을 배열의 길이에맞게 한칸 좌측으로 당겨야한다.
            else if (removeIndex == this._mixingPivot)
            {
                if (arrayLength - 1 == removeIndex) // if remove Index is last index of array
                {
                    DecrementMixingPivot();
                }
            }

            // Pivot을 건드리지않고 그대로 둔다.
        }
        private void IncrementMixingPivot()
        {
            MoveMixingPivot(1);
        }
        private void DecrementMixingPivot()
        {
            MoveMixingPivot(-1);
        }
        private void ResetMixingPivot()
        {
            // 언제나 Pivot을 -1로 설정하기위함.
            MoveMixingPivot(-(_mixingPivot + 1));
        }
        private void MoveMixingPivot(int delta)
        {
            _mixingPivot = Mathf.Clamp(_mixingPivot + delta, -1, int.MaxValue);
        }


        private void GetDividedListsByPivot(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right, Action<int> onLeft, Action<int> onRight)
        {
            // Divide All Audio Two Parts
            // Left : mix from, Right : mix to
            for (int i = 0; i < audioSources.Count; i++)
            {
                if (i < _mixingPivot)
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


        private bool CanMix()
        {
            // Pivot은 Index이므로 갯수 비교를 위해 +1 을 실시하였다.
            // Pivot always (except 0) same as total count - 1
            return _mixingPivot + 1 >= trackMixer.MinimumRequirementsCount;
        }
        private void DoMixBehaviour()
        {
            _allowMix = true;
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
                if (this.Audio == null)
                    return;

                this.Audio.Stop();
                this.Audio.clip = null;
            }
            public bool IsAudioPlaying()
            {
                if (this.Audio == null)
                    return false;

                return this.Audio.isPlaying;
            }

            public void ApplyOptionDataImmediately()
            {
                if (this.Audio != null)
                {
                    this.Audio.volume = this.Volume.Max;
                }
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
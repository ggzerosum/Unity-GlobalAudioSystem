using System.Collections.Generic;

using ProvisGames.Core.AudioSystem;
using ProvisGames.Core.Utility;
using ProvisGames.Core.Utility.MathUtility;

using UnityEngine;


namespace ProvisGames.Core.AudioService
{
    /// <summary>
    /// Track의 전체 볼륨을 주어진 Curve에 따라 Fade 합니다.
    /// </summary>
    public sealed class VolumeControl : Mixer<AudioTrack.AudioPlayer>
    {
        // 볼륨 컨트롤은 트랙내 모든 오디오에 적용되므로, 오디오의 갯수가 1개 이상만 있으면된다.
        public override int MinimumRequirementsCount => 1;

        private ReusableCurve m_FadeOutCurve;
        private ReusableCurve.CurveValue curveValue;
        private float duration;

        private readonly bool stopAudioPlayersWhenMixEnd;

        private bool isEnd = false;

        private List<AudioTrack.AudioPlayer> cachedLeft;
        private List<AudioTrack.AudioPlayer> cachedRight;


        public VolumeControl(CurveAsset fadeout, float duration, bool stopAudioPlayersWhenMixEnd)
        {
            this.m_FadeOutCurve = fadeout.GetReusableCurve();

            this.duration = duration;
            this.stopAudioPlayersWhenMixEnd = stopAudioPlayersWhenMixEnd;
        }

        public override void AttuneAudioPlayerToMixer(ref AudioTrack.AudioPlayer target)
        {
            base.AttuneAudioPlayerToMixer(ref target);

            target.Audio.volume = 0;
        }

        public override void BeginMix()
        {
            base.BeginMix();

            this.m_FadeOutCurve.BeginEvaluate();
        }

        public override void EndMix()
        {
            base.EndMix();

            if (stopAudioPlayersWhenMixEnd)
            {
                DestroyAudioPlayer(cachedLeft);
                DestroyAudioPlayer(cachedRight);

                cachedLeft = null;
                cachedRight = null;
            }

            this.m_FadeOutCurve.EndEvaluate();
        }

        protected override void BeforeMixUpdate(float deltaTime)
        {
            // duration이 0일 경우 마지막 값에 바로 도달할 수 있는 Delta값을 계산
            float dt = duration >= float.Epsilon ? deltaTime / duration : m_FadeOutCurve.EndTime - m_FadeOutCurve.CurveTime;
            curveValue = m_FadeOutCurve.Evaluate(dt);
        }

        protected override void PrepareMix(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {}

        protected override bool Mixing(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {
            if (!isEnd)
            {
                SetVolumeAll(left, curveValue.Normalized);
                SetVolumeAll(right, curveValue.Normalized);
                
                isEnd = m_FadeOutCurve.GetNormalizedElapsedTime() >= 1.0f;

                return true;
            }
            else // Mixing 끝났음을 반드시 false로 알려야한다.
            {
                return false;
            }
        }

        protected override void AfterMixUpdate(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {
            if (stopAudioPlayersWhenMixEnd)
            {
                cachedLeft = left;
                cachedRight = right;
            }
        }


        private void SetVolumeAll(List<AudioTrack.AudioPlayer> audioPlayers, float lerpRatio)
        {
            foreach (AudioTrack.AudioPlayer player in audioPlayers)
            {
                float max = player.Volume.Max;
                float min = player.Volume.Min;

                player.Audio.volume = Mathf.Clamp01(LerpUtility.Lerp(min, max, lerpRatio));
            }
        }
        private void DestroyAudioPlayer(List<AudioTrack.AudioPlayer> audioPlayers)
        {
            foreach (AudioTrack.AudioPlayer player in audioPlayers)
            {
                player.StopAndEjectClip();
            }
        }
    }
}

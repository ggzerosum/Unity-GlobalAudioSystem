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
        private ReusableCurve m_FadeOutCurve;
        private ReusableCurve.CurveValue fadeOutFactor, fadeInFactor;
        private float duration;

        private readonly bool stopAudioPlayersWhenMixEnd;
        private List<AudioTrack.AudioPlayer> cachedLeft;
        private List<AudioTrack.AudioPlayer> cachedRight;

        public VolumeControl(CurveAsset fadeout, bool stopAudioPlayersWhenMixEnd)
        {
            this.m_FadeOutCurve = fadeout.GetReusableCurve();
            this.stopAudioPlayersWhenMixEnd = stopAudioPlayersWhenMixEnd;
        }

        public override void SettingTarget(AudioTrack.AudioPlayer target)
        {
            base.SettingTarget(target);

            target.Audio.volume = 0;
        }

        public override void BeginMix()
        {
            base.BeginMix();

            this.m_FadeOutCurve.BeginEvaluate();
            duration = m_FadeOutCurve.EndTime - m_FadeOutCurve.BeginTime;
        }

        public override void EndMix()
        {
            base.EndMix();

            if (stopAudioPlayersWhenMixEnd)
            {
                // Mixing이 끝났을 때 한번만 초기화를 실시해준다.
                DestroyAudioPlayer(cachedLeft);
                DestroyAudioPlayer(cachedRight);

                cachedLeft = null;
                cachedRight = null;
            }
        }

        protected override void BeforeMixUpdate(float deltaTime)
        {}

        protected override void PrepareMix(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {}

        protected override bool Mixing(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {
            if (this.MixTime < duration)
            {
                ReusableCurve.CurveValue curveValue = m_FadeOutCurve.Evaluate(this.MixTime);
                SetVolumeAll(left, curveValue.Normalized);
                SetVolumeAll(right, curveValue.Normalized);

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

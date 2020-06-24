using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using ProvisGames.Core.Utility;
using ProvisGames.Core.Utility.MathUtility;

namespace ProvisGames.Core.AudioSystem
{
    /// <summary>
    /// Mix Multiple Audio in same Track.
    /// </summary>
    public class VolumeTransition : Mixer<AudioTrack.AudioPlayer>
    {
        // 볼륨 전환은 트랙내 오디오의 갯수가 최소 2개 이상이어야한다.
        public override int MinimumRequirementsCount => 2;

        private ReusableCurve m_FadeOutCurve, m_FadeInCurve;
        private float transitionDelay;
        private bool isFadingOut;

        private ReusableCurve.CurveValue fadeOutFactor, fadeInFactor;

        private float fadeInBeginTime;
        private float fadeInEndTime;

        private List<AudioTrack.AudioPlayer> cachedLeftPlayers;

        public VolumeTransition(CurveAsset fadeout, CurveAsset fadein, float transitionDelay)
        {
            this.m_FadeOutCurve = fadeout.GetReusableCurve();
            this.m_FadeInCurve = fadein.GetReusableCurve();

            this.transitionDelay = transitionDelay;
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
            this.m_FadeInCurve.BeginEvaluate();

            fadeInBeginTime = m_FadeOutCurve.EndTime + transitionDelay + m_FadeInCurve.BeginTime;
            fadeInEndTime = m_FadeOutCurve.EndTime + transitionDelay + m_FadeInCurve.EndTime;
        }

        public override void EndMix()
        {
            base.EndMix();

            if (this.cachedLeftPlayers != null)
            {
                // Stop Left AudioSources
                for (int i = 0; i < this.cachedLeftPlayers.Count; i++)
                {
                    this.cachedLeftPlayers[i].StopAndEjectClip();
                }
                this.cachedLeftPlayers = null;
            }

            this.m_FadeOutCurve.EndEvaluate();
            this.m_FadeInCurve.EndEvaluate();

            fadeInBeginTime = 0;
            fadeInEndTime = 0;
        }

        protected sealed override void BeforeMixUpdate(float deltaTime)
        {
            float fadeInBeginTime = m_FadeOutCurve.EndTime + transitionDelay + m_FadeInCurve.BeginTime;
            if (this.MixTime < fadeInBeginTime)
            {
                isFadingOut = true;
                fadeOutFactor = this.m_FadeOutCurve.Evaluate(deltaTime);
            }
            else
            {
                isFadingOut = false;
                fadeInFactor = this.m_FadeInCurve.Evaluate(deltaTime);
            }
        }
        protected override void PrepareMix(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {
            SetVolumeAll(right, 0);
        }
        protected override bool Mixing(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {
            if (isFadingOut) // Fade Out Left Audio
            {
                if (left.Count == 0)
                    this.MixTime = Mathf.Max(this.MixTime, fadeInBeginTime);

                SetVolumeAll(left, fadeOutFactor.Normalized);
                return true;
            }
            else // Fade In Left Audio
            {
                if (right.Count == 0)
                    this.MixTime = Mathf.Min(this.MixTime, fadeInEndTime);

                SetVolumeAll(right, fadeInFactor.Normalized);

                if (this.MixTime >= fadeInEndTime)
                {
                    return false;
                }

                return true;
            }
        }
        protected sealed override void AfterMixUpdate(List<AudioTrack.AudioPlayer> left, List<AudioTrack.AudioPlayer> right)
        {
            this.cachedLeftPlayers = left;
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
    }
}

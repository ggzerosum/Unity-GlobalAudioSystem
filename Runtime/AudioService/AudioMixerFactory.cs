using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ProvisGames.AudioService.Mixer;
using ProvisGames.Core.AudioService;
using UnityEngine;
using ProvisGames.Core.Utility;

namespace ProvisGames.Core.AudioSystem
{
    public class AudioMixerFactory
    {
        private static readonly string mixerAssetPath = "AudioMixer";
        private static readonly string fadeInAsset = "FadeIn";
        private static readonly string fadeOutAsset = "FadeOut";

        private static CurveAsset fadeInData, fadeOutData;

        public static VolumeTransition CreateVolumeTransitionMixer(float transitionTime = 0.0f)
        {
            return new VolumeTransition(LoadFadeOutCurveAsset(), LoadFadeInCurveAsset(), transitionTime);
        }

        public static VolumeControl CreateVolumeFadeInMixer()
        {
            return new VolumeControl(LoadFadeInCurveAsset(), false);
        }
        public static VolumeControl CreateVolumeFadeOutMixer()
        {
            return new VolumeControl(LoadFadeOutCurveAsset(), true);
        }

        private static AudioNullMixer mixerCache = new AudioNullMixer();
        public static AudioNullMixer CreateNullMixer()
        {
            return mixerCache;
        }


        private static CurveAsset LoadFadeInCurveAsset()
        {
            if (fadeInData == null)
                fadeInData = Resources.Load<CurveAsset>($"{mixerAssetPath}/{fadeInAsset}");

            return fadeInData;
        }
        private static CurveAsset LoadFadeOutCurveAsset()
        {
            if (fadeOutData == null)
                fadeOutData = Resources.Load<CurveAsset>($"{mixerAssetPath}/{fadeOutAsset}");

            return fadeOutData;
        }
    }
}
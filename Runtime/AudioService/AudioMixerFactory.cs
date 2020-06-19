using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ProvisGames.AudioService.Mixer;
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
        public static VolumeMixer CreateVolumeMixer(float transitionTime = 0.0f)
        {
            //Debug.Log($"Path:{mixerAssetPath}/{fadeInAsset}");

            if (fadeInData == null)
                fadeInData = Resources.Load<CurveAsset>($"{mixerAssetPath}/{fadeInAsset}");
            if (fadeOutData == null)
                fadeOutData = Resources.Load<CurveAsset>($"{mixerAssetPath}/{fadeOutAsset}");

            return new VolumeMixer(fadeOutData, fadeInData, transitionTime);
        }

        private static AudioNullMixer mixerCache = new AudioNullMixer();
        public static AudioNullMixer CreateNullMixer()
        {
            return mixerCache;
        }
    }
}
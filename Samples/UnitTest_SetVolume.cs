using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProvisGames.Core.AudioSystem.UnitTest
{
    internal class UnitTest_SetVolume : MonoBehaviour
    {
        public Slider slider;

        public int track;
        public bool applyAllTrack;

        public void SetVolume()
        {
            if (applyAllTrack)
            {
                ServiceLocator.Instance
                    ?.GetGlobalAudioService()
                    ?.SetVolume(slider.normalizedValue);
            }
            else
            {
                ServiceLocator.Instance
                    ?.GetGlobalAudioService()
                    ?.SetVolume(slider.normalizedValue, track);
            }
        }
    }
}

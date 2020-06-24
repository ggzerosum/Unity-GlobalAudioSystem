using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProvisGames.Core.AudioSystem.UnitTest
{
    internal class UnitTest_AudioClipPlayer : MonoBehaviour
    {
        public AudioClip clip;
        public int track;
        public bool isLoop;


        public void PlayAdditive()
        {
            ServiceLocator.Instance
                ?.GetGlobalAudioService()
                ?.Play(track, new GlobalAudioService.AudioSetting(clip, isLoop), GlobalAudioService.PlayMode.Additive);
        }
        public void PlaySingle()
        {
            ServiceLocator.Instance
                ?.GetGlobalAudioService()
                ?.Play(track, new GlobalAudioService.AudioSetting(clip, isLoop), GlobalAudioService.PlayMode.Single);
        }

        public void PlayFadeOutMix()
        {
            ServiceLocator.Instance
                ?.GetGlobalAudioService()
                ?.Play(track, new GlobalAudioService.AudioSetting(clip, isLoop), GlobalAudioService.PlayMode.Additive, GlobalAudioService.MixMode.FadeOut);
        }
        public void PlayFadeInMix()
        {
            ServiceLocator.Instance
                ?.GetGlobalAudioService()
                ?.Play(track, new GlobalAudioService.AudioSetting(clip, isLoop), GlobalAudioService.PlayMode.Additive, GlobalAudioService.MixMode.FadeIn);
        }
        public void PlayTransitionMix()
        {
            ServiceLocator.Instance
                ?.GetGlobalAudioService()
                ?.Play(track, new GlobalAudioService.AudioSetting(clip, isLoop), GlobalAudioService.PlayMode.Additive, GlobalAudioService.MixMode.Transition);
        }
    }
}
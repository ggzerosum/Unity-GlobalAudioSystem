using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProvisGames.Core.AudioSystem;
using ProvisGames.Core.Utility;
using ProvisGames.Core.Utility.ResourcesManaging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace ProvisGames.Core.AudioSystem
{
    /// <summary>
    /// Audio Player that using Global Audio Service
    /// </summary>
    public class GlobalAddressableAudioPlayer : MonoBehaviour
    {
        public enum Track : int
        {
            BGM,
            EFFECT
        }
        [SerializeField] private Track track;
        [SerializeField] private GlobalAudioService.PlayMode playMode;
        [SerializeField] private AudioClipReference audioReference;
        private ReferenceOfInstance<AudioClip> audioClip;
        [SerializeField] private bool isLoop = false;
        [SerializeField] private bool isMix = false;
        private bool isInitialized = false;

        void Awake()
        {
            Initialize(false);
        }

        void OnDestroy()
        {
            UnLoad();
        }

        // Preparing Manual Initialize by manager.. etc
        public void Initialize(bool overwrite)
        {
            if (isInitialized && !overwrite)
                return;

            LoadResource();
        }
        private async void LoadResource()
        {
            try
            {
                if (audioClip.IsValid())
                {
                    return;
                }
                else // 레퍼런스가 유효하지않다면, 혹시모를 오류를 방지하기위해 Release를 실시해주고 진입
                {
                    audioClip.Release();
                }

                var task = audioReference.LoadAsync();
                await task;

                if (!task.IsCompleted)
                {
                    task.Result.Release();
                    throw new Exception("Audio Clip Load Failed");
                }

                audioClip = task.Result;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        private void UnLoad()
        {
            audioClip.Release();
        }

        public void RequestPlay()
        {
            this.StartErrorHandlableCoroutine(PlayAsync(10.0f));
        }
        IEnumerator PlayAsync(float timeoutInSeconds)
        {
            if (audioClip.LoadStatus == ReferenceOfInstance<AudioClip>.Status.Failed)
            {
                yield break;
            }

            float t = 0.0f;
            while (audioClip.LoadStatus != ReferenceOfInstance<AudioClip>.Status.Succeed)
            {
                yield return null;
                if (t >= timeoutInSeconds)
                {
                    Debug.LogError("AudioRequest-Play Time Out");
                    yield break;
                }
                t += Time.deltaTime;
            }

            if (!audioClip.IsValid())
            {
                Debug.LogError("AudoClip Instance not vaild");
                yield break;
            }

            if (!isMix)
                ServiceLocator.Instance.GetGlobalAudioService().Play((int)track, new GlobalAudioService.AudioSetting(audioClip.GetResult, isLoop), playMode);
            else
                ServiceLocator.Instance.GetGlobalAudioService().Play((int)track, new GlobalAudioService.AudioSetting(audioClip.GetResult, isLoop), playMode, GlobalAudioService.MixMode.Transition);
        }
    }
}
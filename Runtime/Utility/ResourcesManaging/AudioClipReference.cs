using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProvisGames.Core.Utility.ResourcesManaging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ProvisGames.Core.AudioSystem
{
    [CreateAssetMenu(fileName = "Audio Clip Locator", menuName = "ProvisGames/AudioSystem/AudioClipReference")]
    public class AudioClipReference : ResourceAssetReferenceT<AssetReferenceAudioClip, AudioClip>
    {
        [Header("Audio Clip")]
        [SerializeField] private AssetReferenceAudioClip m_assetReference;
        protected override AssetReferenceAudioClip GetReference => this.m_assetReference;
    }
}

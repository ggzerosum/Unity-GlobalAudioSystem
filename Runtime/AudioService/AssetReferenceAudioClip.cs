using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProvisGames.Core.AudioSystem
{
    /// <summary>
    /// Audio Clip Asset
    /// </summary>
    [Serializable]
    public class AssetReferenceAudioClip : AssetReferenceT<AudioClip>
    {
        public AssetReferenceAudioClip(string guid) : base(guid)
        {}

        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(AudioClip))
                return true;

            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            return typeof(AudioClip).IsAssignableFrom(type);

#endif
            return false;
        }
    }
}

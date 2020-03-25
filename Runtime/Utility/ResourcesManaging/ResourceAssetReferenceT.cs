using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ProvisGames.Core.Utility.ResourcesManaging
{
    /// <summary>
    /// Resource Load / UnLoad Through AssetReference API
    /// </summary>
    /// <typeparam name="TReference"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public abstract class ResourceAssetReferenceT<TReference, TResult> : ScriptableObject 
        where TReference : AssetReferenceT<TResult>
        where TResult : UnityEngine.Object
    {
        protected abstract TReference GetReference { get; }

        public virtual async Task<ReferenceOfInstance<TResult>> LoadAsync()
        {
            try
            {
                AsyncOperationHandle<TResult> taskHandler = GetReference.LoadAssetAsync();
                await taskHandler.Task; // Do Loading Task
                return new ReferenceOfInstance<TResult>(taskHandler);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return default;
            }
        }
    }

    public struct ReferenceOfInstance<T>
    {
        public enum Status
        {
            None,
            Loading,
            Succeed,
            Failed
        }
        public Status LoadStatus { get; private set; }

        private AsyncOperationHandle<T> m_Handler;
        public T GetResult => m_Handler.Result;

        public ReferenceOfInstance(AsyncOperationHandle<T> handler)
        {
            m_Handler = handler;

            if (!handler.IsDone)
            {
                this.LoadStatus = Status.Failed;
            }
            else if (handler.Status == AsyncOperationStatus.Succeeded)
            {
                this.LoadStatus = Status.Succeed;
            }
            else // m_Handler.Status == AsyncOperationStatus.Failed And Other Cases
            {
                this.LoadStatus = Status.Failed;
            }
        }

        public bool IsValid()
        {
            if (EqualityComparer<AsyncOperationHandle<T>>.Default.Equals(m_Handler, default(AsyncOperationHandle<T>)))
            {
                return false;
            }

            return m_Handler.IsValid();
        }
        public void Release()
        {
            if (IsValid())
            {
                Addressables.Release(m_Handler);
                Debug.Log("Handler Released");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProvisGames.Core.Utility.ResourcesManaging
{
    public abstract class ResourceReferenceT<T> : ScriptableObject
    {
        public enum Status
        {
            None,
            Succeed,
            Failed,
            Canceled
        }
        public bool IsDone { get; protected set; } = false;
        public Status LoadStatus { get; protected set; } = Status.None;
        public T Result { get; protected set; } = default;

        public abstract void LoadAsync();
        public abstract void UnLoad();
    }
}
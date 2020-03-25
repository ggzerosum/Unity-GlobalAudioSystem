using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProvisGames.Core.AudioSystem
{
    public abstract class Mixer<T>
    {
        public float MixTime { get; protected set; } = 0.0f;
        private bool isExecutedFirst = true;

        public virtual void BeginMix()
        {}

        public bool Mix(List<T> left, List<T> right, float deltaTime)
        {
            if (isExecutedFirst)
            {
                PrepareMix(left, right);
                isExecutedFirst = false;
            }

            BeforeMixUpdate(deltaTime);

            bool ret = Mixing(left, right);
            MixTime += deltaTime;

            // AfterMixUpdate
            AfterMixUpdate(left, right);

            return ret;
        }
        public virtual void EndMix()
        {
            Reset();
        }

        public void Reset()
        {
            this.MixTime = 0.0f;
            this.isExecutedFirst = true;
        }

        protected abstract void PrepareMix(List<T> left, List<T> right);
        protected abstract void BeforeMixUpdate(float deltaTime);
        protected abstract bool Mixing(List<T> left, List<T> right);
        protected abstract void AfterMixUpdate(List<T> left, List<T> right);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProvisGames.Core.Utility
{
    /// <summary>
    /// Curve Data for Lerp Sound
    /// </summary>
    [CreateAssetMenu(fileName = "Curve Asset", menuName = "ProvisGames/Utility/Curve/Curve Asset")]
    public class CurveAsset : ScriptableObject
    {
        [Header("Curve Data. Curve will be normalized to 0 - 1")]
        [SerializeField] private AnimationCurve curve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        public ReusableCurve GetReusableCurve()
        {
            return curve.GetReusableCurve();
        }
    }

    /// <summary>
    /// Reusable Evaluator
    /// </summary>
    public struct ReusableCurve
    {
        public ReusableCurve(AnimationCurve curve)
        {
            this.source = curve;
            this.copy = null;
            this.simulatedTime = 0.0f;
        }

        private AnimationCurve source;
        private AnimationCurve copy;
        private float simulatedTime;

        public float BeginTime
        {
            get
            {
                if (copy == null)
                    return 0.0f;

                return copy.keys[0].time;
            }
        }
        public float CurveTime => simulatedTime;
        public float EndTime
        {
            get
            {
                if (copy == null)
                    return 0.0f;

                return copy.keys[copy.length - 1].time;
            }
        }

        public void BeginEvaluate()
        {
            this.copy = CreateCopy();
            this.simulatedTime = 0.0f;
        }
        public bool IsCurveSimulationEnd()
        {
            if (this.copy == null)
                return true;

            if (this.simulatedTime > EndTime)
            {
                return true;
            }

            return false;
        }
        public void EndEvaluate()
        {
            this.copy = null;
            this.simulatedTime = 0.0f;
        }
        public CurveValue Evaluate(float deltaTime)
        {
            float time = simulatedTime;
            float normalizedTime = NormalizeTime(time);
            float value = this.copy.Evaluate(time);
            float normalizedValue = time != 0 ? (value * normalizedTime) / time : 0;

            simulatedTime += deltaTime;
            return new CurveValue(value, normalizedValue);
        }

        public float GetNormalizedElapsedTime()
        {
            return Mathf.Clamp01(NormalizeTime(CurveTime));
        }

        private AnimationCurve CreateCopy()
        {
            return new AnimationCurve(source.keys);
        }
        private float NormalizeTime(float time)
        {
            var minFrame = this.source.keys[0];
            var maxFrame = this.source.keys[this.source.length - 1];

            float numerator = time - minFrame.time;
            float denominator = maxFrame.time - minFrame.time;

            if (denominator == 0)
                throw new ArgumentOutOfRangeException("time of Curve is '0'");

            return numerator / denominator;
        }

        public struct CurveValue
        {
            public CurveValue(float value, float normalized)
            {
                this.Value = value;
                this.Normalized = normalized;
            }

            public float Value { get; private set; }
            public float Normalized { get; private set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProvisGames.Core.Utility.MathUtility
{
    internal static class LerpUtility
    {
        public static float Lerp(float min, float max, float t)
        {
            t = Mathf.Clamp01(t);
            return (min * (1.0f - t)) + (max * t);
        }
    }
}

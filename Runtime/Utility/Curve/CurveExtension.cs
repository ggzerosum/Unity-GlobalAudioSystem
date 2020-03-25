using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProvisGames.Core.Utility
{
    public static class CurveExtension
    {
        /// <summary>
        /// Create reusable Evaluator of Animation Curve
        /// </summary>
        public static ReusableCurve GetReusableCurve(this AnimationCurve @this)
        {
            return new ReusableCurve(@this);
        }
    }
}

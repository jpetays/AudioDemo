using UnityEngine;
using UnityEngine.Assertions;

namespace Prg.Util
{
    public class AnimationCurveUtil
    {
        public static float TimeFraction(float currentDuration, float totalDuration)
        {
            Assert.IsTrue(currentDuration >= 0 && currentDuration <= totalDuration, "invalid duration");
            return currentDuration == 0 ? 0
                : currentDuration < totalDuration ? currentDuration / totalDuration
                : 1f;
        }

        public static Color EaseOnCurve(AnimationCurve curve, Color from, Color to, float timeFraction)
        {
            var distance = to - from;
            return from + curve.Evaluate(timeFraction) * distance;
        }

        public static float EaseOnCurve(AnimationCurve curve, float from, float to, float timeFraction)
        {
            var distance = to - from;
            return from + curve.Evaluate(timeFraction) * distance;
        }
    }
}

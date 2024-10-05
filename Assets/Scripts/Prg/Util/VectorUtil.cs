using UnityEngine;

namespace Prg.Util
{
    public static class VectorUtil
    {
        /// <summary>
        /// Getting 0-360 (clockwise) angle between two vectors, 0/360 being upwards to north and 180 down to south.
        /// 90 is right/east and 270 is left/west.<br />
        /// https://forum.unity.com/threads/how-to-get-a-360-degree-vector3-angle.42145/
        /// https://gist.github.com/shiwano/0f236469cd2ce2f4f585
        /// </summary>
        public static float AngleTo(this Vector3 source, Vector3 target)
        {
            var direction = target - source;
            return 360f - Quaternion.FromToRotation(Vector3.up, direction).eulerAngles.z;
        }

        /// <summary>
        /// Direction and Distance from One Object to Another
        /// https://docs.unity3d.com/2019.3/Documentation/Manual/DirectionDistanceFromOneObjectToAnother.html
        /// </summary>
        public static Vector3 Direction(this Transform from, Transform to)
        {
            return to.position - from.position;
        }

        public static Vector3 Direction(this Vector3 from, Vector3 to)
        {
            return to - from;
        }
    }
}

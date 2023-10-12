using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Prg.Util
{
    /// <summary>
    /// Formatting utilities mainly for debugging.
    /// </summary>
    /// <remarks>
    /// Note that JSON methods require "com.unity.nuget.newtonsoft-json@3.2.1" package or later
    /// to be installed via Package Manager.<br />
    /// https://github.com/jilleJr/Newtonsoft.Json-for-Unity/wiki/Install-official-via-UPM
    /// </remarks>
    public static class FormatUtil
    {
        public static string JsonToLog(JObject jObject)
        {
            return JsonToLog(jObject?.ToString() ?? "");

        }
        public static string JsonToLog(string json)
        {
            return json?.Replace("\r\n", "") ?? "";
        }

        public static string FormatInt(Vector2 vector)
        {
            return $"{vector.x:0},{vector.y:0}";
        }

        public static string FormatInt(Vector3 vector)
        {
            return $"{vector.x:0},{vector.y:0},{vector.z:0}";
        }

        public static string FormatD1(Vector3 vector)
        {
            return $"{vector.x:0.0},{vector.y:0.0},{vector.z:0.0}";
        }

        public static string FormatD1(Vector3 vector, bool isVector3)
        {
            return isVector3
                ? $"{vector.x:0.0},{vector.y:0.0},{vector.z:0.0}"
                : $"{vector.x:0.0},{vector.y:0.0}";
        }

        public static string FormatD2(Vector3 vector, bool isVector3)
        {
            return isVector3
                ? $"{vector.x:0.00},{vector.y:0.00},{vector.z:0.00}"
                : $"{vector.x:0.00},{vector.y:0.00}";
        }
    }
}

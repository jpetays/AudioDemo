using System;
using System.Collections.Generic;
using System.Reflection;
using Prg;
using UnityEditor;
using UnityEngine;
using Debug = Prg.Debug;
#if USE_GA
using GameAnalyticsSDK.Setup;
#endif

namespace Editor.Prg.Data
{
    public static class AnalyticsSettings
    {
        public static bool CreateForPlatform(BuildTarget buildTarget, Tuple<string, string> tuple)
        {
#if USE_GA
            var settings = Resources.Load<Settings>("GameAnalytics/Settings");
            var platforms = settings.Platforms;
            var platformIndex = buildTarget switch
            {
                BuildTarget.Android => platforms.FindIndex(x => x == RuntimePlatform.Android),
                BuildTarget.WebGL => platforms.FindIndex(x => x == RuntimePlatform.WebGLPlayer),
                BuildTarget.StandaloneWindows64 => platforms.FindIndex(x => x == RuntimePlatform.WindowsPlayer),
                _ => -1
            };
            if (platformIndex == -1)
            {
                Debug.Log(
                    $"{RichText.Red("Did not find Platform settings")} for BuildTarget {RichText.Yellow(buildTarget)}");
                return false;
            }
            Debug.Log($"BuildTarget {RichText.Yellow(buildTarget)}");
            return SetKeys(settings, platformIndex, tuple);
#else
            Debug.LogError($"#define {RichText.Magenta("USE_GA")} is not defined");
            return false;
#endif
        }

#if USE_GA
        private static bool SetKeys(Settings settings, int index, Tuple<string, string> tuple)
        {
            // ReSharper disable EntityNameCapturedOnly.Local
            const string gameKey = "";
            const string secretKey = "";
            // ReSharper restore EntityNameCapturedOnly.Local

            var type = settings.GetType();
            var field1 = type.GetField(nameof(gameKey), BindingFlags.Instance | BindingFlags.NonPublic);
            var value1 = field1?.GetValue(settings);
            if (value1 is not List<string> gameKeys)
            {
                return false;
            }
            var field2 = type.GetField(nameof(secretKey), BindingFlags.Instance | BindingFlags.NonPublic);
            var value2 = field2?.GetValue(settings);
            if (value2 is not List<string> secretKeys)
            {
                return false;
            }
            var isDirty = false;
            var gameKeyValue = gameKeys[index];
            var secretKeyValue = secretKeys[index];
            if (gameKeyValue != tuple.Item1)
            {
                gameKeys[index] = tuple.Item1;
                isDirty = true;
            }
            if (secretKeyValue != tuple.Item2)
            {
                secretKeys[index] = tuple.Item2;
                isDirty = true;
            }
            if (!isDirty)
            {
                return false;
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return true;
        }
#endif
    }
}

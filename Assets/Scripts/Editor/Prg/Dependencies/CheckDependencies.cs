using System;
using System.IO;
using System.Linq;
using Prg;
using Prg.Util;
using UnityEditor;
using UnityEngine;
using Debug = Prg.Debug;

namespace Editor.Prg.Dependencies
{
    /// <summary>
    /// Utility script to check dependencies of selected objects in UNITY <c>Editor</c> based on their <c>GUID</c>.
    /// </summary>
    /// <remarks>
    /// List of supported object types (in selection) is limited to some "well known" types used in UNITY.
    /// </remarks>
    internal static class CheckDependencies
    {
        private const string AssetRootName = "Assets";

        public static void CheckAssetUsage(string[] selectedGuids)
        {
            Debug.Log("*");
            if (selectedGuids.Length == 0)
            {
                Debug.Log("Nothing is selected");
                return;
            }
            // Keep extensions lowercase!
            var validExtensions = new[]
            {
                ".anim",
                ".asset",
                ".blend",
                ".controller",
                ".cs",
                ".cubemap",
                ".flare",
                ".gif",
                ".inputactions",
                ".mat",
                ".mp3",
                ".otf",
                ".physicMaterial",
                ".physicsmaterial2d",
                ".png",
                ".prefab",
                ".psd",
                ".shader",
                ".tga",
                ".tif",
                ".ttf",
                ".wav",
            };
            var hasShaders = false;
            foreach (var guid in selectedGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var extension = Path.HasExtension(path) ? Path.GetExtension(path).ToLower() : string.Empty;
                if (!hasShaders && extension == ".shader")
                {
                    hasShaders = true;
                }
                if (validExtensions.Contains(extension))
                {
                    continue;
                }
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                Debug.LogWarning($"Selected object is not supported asset: {path}", asset);
                return;
            }
            Debug.Log($"Search dependencies for {selectedGuids.Length} assets");
            var searchFolders = new[] { AssetRootName };
            var foundCount = new int[selectedGuids.Length];
            Array.Clear(foundCount, 0, foundCount.Length);

            var assetFilters = new[] { "t:Scene", "t:Prefab", "t:ScriptableObject", "t:AnimatorController" };
            var totalCount = 0;
            foreach (var assetFilter in assetFilters)
            {
                var foundAssets = AssetDatabase.FindAssets(assetFilter, searchFolders);
                var searchCount = CheckForGuidInAssets(selectedGuids, ref foundCount, foundAssets);
                totalCount += searchCount;
                Debug.Log($"search {assetFilter}:{foundAssets.Length} found={searchCount}");
            }
            Debug.Log(">");
            var noDepCount = 0;
            for (var i = 0; i < selectedGuids.Length; ++i)
            {
                if (foundCount[i] == 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(selectedGuids[i]);
                    var asset = AssetDatabase.LoadMainAssetAtPath(path);
                    Debug.LogWarning(
                        $"{ColorSource(path)} has <b>{RichText.Brown("NO dependencies")}</b> in this search",
                        asset);
                    noDepCount += 1;
                }
            }
            if (totalCount > 0)
            {
                if (noDepCount > 0)
                {
                    Debug.Log(">");
                }
                for (var i = 0; i < selectedGuids.Length; ++i)
                {
                    var path = AssetDatabase.GUIDToAssetPath(selectedGuids[i]);
                    var depCount = foundCount[i];
                    var message = depCount > 0
                        ? $"has <b>{depCount} dependencies</b>"
                        : $"does not have <i>any dependencies</i> and <b>can be safely deleted</b>";
                    var asset = AssetDatabase.LoadMainAssetAtPath(path);
                    Debug.LogWarning($"{ColorSource(path)} {message}", asset);
                }
            }
            if (hasShaders)
            {
                Debug.LogWarning(
                    $"{RichText.Yellow("Shaders are referenced by name and can not be detected with this script")}");
            }
        }

        private static int CheckForGuidInAssets(string[] selectedGuids, ref int[] foundCount, string[] assetGuids)
        {
            var count = 0;
            foreach (var assetGuid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuid);
                var assetContent = File.ReadAllText(path, PlatformUtil.Encoding);
                for (var guidIndex = 0; guidIndex < selectedGuids.Length; ++guidIndex)
                {
                    var guid = selectedGuids[guidIndex];
                    if (assetContent.Contains(guid))
                    {
                        var source = AssetDatabase.GUIDToAssetPath(guid);
                        var go = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
                        Debug.LogWarning($"{ColorSource(source)} found in {ColorTarget(path)}",
                            go);
                        foundCount[guidIndex] += 1;
                        count += 1;
                    }
                }
            }
            return count;
        }

        private static string ColorSource(string filepath) => RichText.Yellow(Path.GetFileName(filepath));

        private static string ColorTarget(string filepath)
        {
            var filename = Path.GetFileName(filepath);
            var dirname = filepath[..^filename.Length];
            dirname = dirname.Replace("Assets/", "");
            return $"{dirname}{RichText.Blue(filename)}";
        }
    }
}

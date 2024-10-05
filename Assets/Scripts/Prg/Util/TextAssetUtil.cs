using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Prg.Util
{
    public static class TextAssetUtil
    {
        /// <summary>
        /// Loads <c>TextAsset</c> by name from <c>Resources</c> folder.
        /// </summary>
        /// <param name="assetName">the text asset name to load</param>
        /// <returns>text asset or null if not found</returns>
        public static TextAsset Load(string assetName) => Resources.Load<TextAsset>(assetName);

        /// <summary>
        /// Creates empty <c>TextAsset</c> in <c>Resources</c> folder.
        /// </summary>
        /// <remarks>
        /// This method works only in UNITY Editor environment - not at runtime!
        /// </remarks>
        /// <param name="assetName">the text asset name to create</param>
        [Conditional("UNITY_EDITOR")]
        public static void Create(string assetName)
        {
#if UNITY_EDITOR
            const string folder = "Assets/Resources";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var path = $"{folder}/{assetName}.txt";
            FileUtil.WriteAllText(path, "");
            AssetDatabase.ImportAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            MyAssert.IsNotNull(textAsset, $"failed to create TextAsset {path}", null);
#endif
        }

        public static string GetAssetPathFrom(Object textAsset)
        {
#if UNITY_EDITOR
            var assetFilter = $"{textAsset.name} t:TextAsset";
            var foundAssets = AssetDatabase.FindAssets(assetFilter, new[] { "Assets" });
            return foundAssets.Length switch
            {
                0 => string.Empty,
                1 => AssetDatabase.GUIDToAssetPath(foundAssets[0]),
                _ => throw new UnityException($"found {foundAssets.Length} assets with name {textAsset.name}"),
            };
#else
            throw new UnityException("not supported");
#endif
        }
    }
}

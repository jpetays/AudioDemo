using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Prg.Localization
{
    /// <summary>
    /// Saves and loads binary data from <c>TextAsset</c> to localization dictionary.<br />
    /// Saving can only be done in Editor.
    /// </summary>
    public static class BinFile
    {
        public static Dictionary<string, Dictionary<string, string>> Load(
            TextAsset binAsset, out List<string> localeCodes)
        {
            localeCodes = new List<string>();
            var dictionary = new Dictionary<string, Dictionary<string, string>>();

            if (binAsset.dataSize < sizeof(int))
            {
                Debug.LogError($"binAsset invalid length {binAsset.dataSize} for {binAsset.name}");
                return dictionary;
            }
            var bytes = binAsset.bytes;
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            // Dictionary header.
            var localeCount = reader.ReadInt32();
            for (var i = 0; i < localeCount; ++i)
            {
                var localeCode = reader.ReadString();
                localeCodes.Add(localeCode);
                dictionary.Add(localeCode, new Dictionary<string, string>());
            }
            // Locales one by one, as many as localeCount.
            for (var i = 0; i < localeCount; ++i)
            {
                var localeCode = reader.ReadString();
                var locale = dictionary[localeCode];
                var wordCount = reader.ReadInt32();
                for (var j = 0; j < wordCount; ++j)
                {
                    var key = reader.ReadString();
                    var value = reader.ReadString();
                    locale.Add(key, value);
                }
            }
            return dictionary;
        }

#if UNITY_EDITOR
        public static void Save(Dictionary<string, Dictionary<string, string>> dictionary, TextAsset binAsset)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Dictionary header, keys sorted.
            var localeKeys = dictionary.Keys.ToList();
            localeKeys.Sort();
            writer.Write(localeKeys.Count);
            foreach (var localeKey in localeKeys)
            {
                if (string.IsNullOrWhiteSpace(localeKey))
                {
                    throw new UnityException($"invalid locale key: '{localeKey}'");
                }
                writer.Write(localeKey);
            }
            // Dictionary values in key order.
            foreach (var localeKey in localeKeys)
            {
                writer.Write(localeKey);
                var locale = dictionary[localeKey];
                writer.Write(locale.Count);
                foreach (var (key, phrase) in locale)
                {
                    writer.Write(key);
                    if (phrase.Contains('\n'))
                    {
                        var pos1 = phrase.IndexOf('\r');
                        var pos2 = phrase.IndexOf('\n');
                        if (pos2 - pos1 != 1)
                        {
                            throw new UnityException(
                                $"dictionary key {key} has invalid CR-LF values: " +
                                $"'{phrase.Replace('\r', '$').Replace('\n', '$')}'");
                        }
                    }
                    writer.Write(phrase);
                }
            }

            var bytes = stream.ToArray();
            var path = GetAssetPath(binAsset.name);
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            return;

            string GetAssetPath(string assetName)
            {
                var assetFilter = $"{assetName} t:TextAsset";
                var foundAssets = AssetDatabase.FindAssets(assetFilter, new[] { "Assets" });
                Assert.IsTrue(foundAssets.Length == 1, "foundAssets.Length == 1");
                return AssetDatabase.GUIDToAssetPath(foundAssets[0]);
            }
        }
#endif
    }
}

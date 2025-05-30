using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;

namespace Editor.Prg.Util
{
    internal static class GameConstantsMenu
    {
        private const string Filename = "GameConstants.cs";

        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Game/";

        [MenuItem(MenuItem + "Generate C# Game Constants", false, 10)]
        private static void GenerateGameConstants() =>
            GameConstantsGenerator.GenerateUnityConstants(Filename);
    }

    internal static class GameConstantsGenerator
    {
        public static void GenerateUnityConstants(string filename)
        {
            Debug.Log("*");
            var path = InternalGenerate(filename);
            Debug.Log($"Wrote {path}");
        }

        private static string InternalGenerate(string filename)
        {
            // Try to find an existing file in the project by its filename.
            var filePath = string.Empty;
            var files = Directory.GetFiles(Application.dataPath, filename, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(filename))
                {
                    filePath = AppPlatform.ConvertToWindowsPath(file);
                    break;
                }
            }

            // If no such file exists already, use the save panel to get a folder in which the file will be placed.
            if (string.IsNullOrEmpty(filePath))
            {
                EditorUtility.DisplayDialog("Create Game Constants file",
                    $"Create a C# file with name '{filename}' in correct location " +
                    $"and proper namespace so it can be updated.", "Understood");
                return "<b>nothing</b>";
            }
            Assert.IsTrue(File.Exists(filePath));
            var namespaceName = GetNamespace();

            // Write out our file
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("// This file is auto-generated by {0}.", nameof(GameConstantsGenerator));
                writer.WriteLine();
                writer.WriteLine($"namespace {namespaceName}");
                writer.WriteLine("{");

                // Write out the tags - https://docs.unity3d.com/Manual/Tags.html
                writer.WriteLine("    /// <summary>");
                writer.WriteLine("    /// Convenience class for UNITY Tags.");
                writer.WriteLine("    /// </summary>");
                writer.WriteLine("    public static class Tags");
                writer.WriteLine("    {");
                writer.WriteLine("        // UNITY Builtin tags.");
                foreach (var tag in InternalEditorUtility.tags)
                {
                    // EditorOnly
                    // A GameObject tagged with EditorOnly in a Scene is destroyed when the game builds.
                    // Any child GameObjects of a GameObject tagged with EditorOnly are also destroyed when the game builds.
                    writer.WriteLine("        public const string {0} = \"{1}\";", MakeSafeForCode(tag), tag);
                    if (tag.Equals("GameController"))
                    {
                        writer.WriteLine("        // Project specific values.");
                    }
                }
                writer.WriteLine("    }");
                writer.WriteLine();

                // Write out layers
                writer.WriteLine("    /// <summary>");
                writer.WriteLine("    /// Convenience class for UNITY Layers.");
                writer.WriteLine("    /// </summary>");
                writer.WriteLine("    /// <remarks>");
                writer.WriteLine(
                    "    /// If using <c>LayerMask</c> in Editor compiler can not check 'used layers' by name");
                writer.WriteLine("    /// but it is easier to select correct layer(s).");
                writer.WriteLine("    /// </remarks>");
                writer.WriteLine("    public static class Layers");
                writer.WriteLine("    {");
                var first = true;
                var layerNumbers = new[]
                {
                    0, 1, 2, 4, 5, 3, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                    17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31
                };
                for (var index = 0; index < 32; index++)
                {
                    var layer = layerNumbers[index];
                    var layerName = InternalEditorUtility.GetLayerName(layer);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        if (first)
                        {
                            writer.WriteLine("        // UNITY Builtin layers");
                            first = false;
                        }
                        writer.WriteLine("        public const int {0} = {1};", MakeSafeForCode(layerName), layer);
                        if (layer == 5)
                        {
                            writer.WriteLine("        // Project specific values.");
                        }
                    }
                }
                writer.WriteLine();
                first = true;
                for (var index = 1; index < 32; index++) // Skip default mask!
                {
                    var layer = layerNumbers[index];
                    var layerName = InternalEditorUtility.GetLayerName(layer);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        if (first)
                        {
                            writer.WriteLine("        // Bitmask of UNITY Builtin layers");
                            first = false;
                        }
                        writer.WriteLine("        public const int {0}Mask = 1 << {1};",
                            MakeSafeForCode(layerName), layer);
                        if (layer == 5)
                        {
                            writer.WriteLine("        // Project specific values.");
                        }
                    }
                }
                writer.WriteLine("    }");
                writer.WriteLine();

                // Write out sorting layers
                writer.WriteLine("    /// <summary>");
                writer.WriteLine("    /// Convenience class for UNITY SortingLayers");
                writer.WriteLine("    /// </summary>");
                writer.WriteLine("    /// <remarks>");
                writer.WriteLine("    /// Typically 'sorting layers' are not used at all in the code.");
                writer.WriteLine("    /// </remarks>");
                writer.WriteLine("    public static class SortingLayers");
                writer.WriteLine("    {");
                foreach (var layer in SortingLayer.layers)
                {
                    writer.WriteLine("        /// <summary>");
                    writer.WriteLine("        /// ID of sorting layer '{0}'.", layer.name);
                    writer.WriteLine("        /// </summary>");
                    writer.WriteLine("        public const int {0} = {1};", MakeSafeForCode(layer.name), layer.id);
                }
                writer.WriteLine("    }");

                // End of namespace UnityConstants
                writer.Write("}");
                writer.WriteLine();
            }

            // Refresh
            AssetDatabase.Refresh();
            return filePath;

            string GetNamespace()
            {
                const string namespacePrefix = "namespace ";
                var lines = File.ReadLines(filePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith(namespacePrefix))
                    {
                        return line[namespacePrefix.Length..];
                    }
                }
                return "Prg";
            }

            string MakeSafeForCode(string name)
            {
                // Code style might not allow underscores so we just remove everything suspicious
                var str = Regex.Replace(name, "[^a-zA-Z0-9_]", string.Empty, RegexOptions.Compiled);
                Assert.IsTrue(str.Length > 0);
                return str;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Prg.Util;
using UnityEditor;
using UnityEngine;

namespace Prg
{
    /// <summary>
    /// Machine generated code below!<br />
    /// Current Android BundleVersionCode, Patch (number as in Sem Ver) and CompiledOnDate.
    /// </summary>
    /// <remarks>Patch value is reset to zero when BundleVersionCode is changed</remarks>
    public static class BuildInfo
    {
        private const string BuildPropertiesFilename = @"Assets\Scripts\Prg\BuildInfo.cs";
        private static readonly Encoding Encoding = PlatformUtil.Encoding;

        private const string BundleVersionCodeValue = "1";
        private const string PatchValue = "1";
        private const string CompiledOnDateValue = "2023-01-10 15:44";

        public static string Version => $"{Application.version}.{BundleVersionCodeValue}.{PatchValue}";

        public static int BundleVersionCode => int.Parse(BundleVersionCodeValue);
        public static int Patch => int.Parse(PatchValue);
#if UNITY_EDITOR
        public static string CompiledOnDate => DateTime.Now.FormatMinutes();
#else
        public static string CompiledOnDate => CompiledOnDateValue;
#endif

#if false
    [MenuItem("Prg/Write SourceCode Changes", false, 10)]
    public static void WriteSourceCodeChanges()
    {
        UpdateSourceCode(PlayerSettings.Android.bundleVersionCode);
    }
#endif

        [Conditional("UNITY_EDITOR")]
        public static void UpdateFile(int bundleVersionCode)
        {
#if UNITY_EDITOR
            const string tagBundleVersionCodeValue = "BundleVersionCodeValue = \"";
            const string tagPatchValue = "PatchValue = \"";
            const string tagCompiledOnDateValue = "CompiledOnDateValue = \"";
            const string endTag = "\";";

            var bundleVersionText = bundleVersionCode.ToString();
            var oldSource = File.ReadAllText(BuildPropertiesFilename, Encoding);
            var index1 = 0;
            var index2 = 0;
            var newSource = oldSource;

            void UpdateIndexesFor(string startTag)
            {
                index1 = newSource.IndexOf(startTag, index1, StringComparison.Ordinal);
                if (index1 == -1)
                {
                    index2 = -1;
                    return;
                }
                index2 = newSource.IndexOf(endTag, index1, StringComparison.Ordinal);
                if (index2 == -1)
                {
                    return;
                }
                index1 += startTag.Length;
            }

            string GetCurrentText()
            {
                return newSource.Substring(index1, index2 - index1);
            }

            void ReplaceCurrentTextWith(string newText)
            {
                var part3 = GetCurrentText();
                if (part3 == newText)
                {
                    return;
                }
                var part1 = newSource[..index1];
                var part2 = newSource[index2..];
                newSource = part1 + newText + part2;
            }

            // Update BundleVersionCode
            var isResetPatch = false;
            UpdateIndexesFor(tagBundleVersionCodeValue);
            if (index2 > index1 && index1 >= 0)
            {
                var bundleText = GetCurrentText();
                if (bundleText != bundleVersionText)
                {
                    isResetPatch = true;
                    ReplaceCurrentTextWith(int.Parse(bundleVersionText).ToString());
                }
            }
            // Update Patch
            UpdateIndexesFor(tagPatchValue);
            if (index2 > index1 && index1 >= 0)
            {
                if (isResetPatch)
                {
                    ReplaceCurrentTextWith("0");
                }
                else
                {
                    var patchText = GetCurrentText();
                    if (int.TryParse(patchText, out var patchValue))
                    {
                        patchValue += 1;
                        ReplaceCurrentTextWith(patchValue.ToString());
                    }
                }
            }
            // Update CompiledOnDate
            UpdateIndexesFor(tagCompiledOnDateValue);
            if (index2 > index1 && index1 >= 0)
            {
                ReplaceCurrentTextWith(DateTime.Now.FormatMinutes());
            }

            if (newSource == oldSource)
            {
                return;
            }
            File.WriteAllText(BuildPropertiesFilename, newSource, Encoding);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}

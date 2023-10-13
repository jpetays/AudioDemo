using System;
using System.Text;
using Prg.Util;
using UnityEngine;

namespace Demo
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
    }
}

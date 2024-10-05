using System;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Editor.Prg.Secrets
{
    public static class SecretStrings
    {
        /// <summary>
        /// Creates MD5 hash 'fingerprint' for given target and (build) date.
        /// </summary>
        public static string GetFingerPrint(BuildTarget buildTarget, string date)
        {
            var secretKeys = BatchBuild.BatchBuild.LoadSecretKeys(@".\etc\secretKeys", buildTarget);
            var gameKey = secretKeys[$"{buildTarget}_gameKey"];
            var secretKey = secretKeys[$"{buildTarget}_secretKey"];
            var input = $"{date}{Application.productName}.{Application.version}.{gameKey}.{secretKey}";
            var fingerPrint = MD5.Create().Md5(input);
            return fingerPrint;
        }


        private static string Md5(this MD5 md5, string input)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(input), "input string is required for MD5");
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hexString = BitConverter.ToString(bytes);
            return hexString;
        }
    }
}

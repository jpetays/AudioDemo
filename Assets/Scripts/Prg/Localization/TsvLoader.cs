using System;
using System.Collections.Generic;
using System.IO;
using Prg.Util;
using UnityEngine;

namespace Prg.Localization
{
    /// <summary>
    /// Loads tab separated files into key-value dictionaries by locale.
    /// </summary>
    public static class TsvLoader
    {
        private const char Tab = '\t';

        public static Dictionary<string, Dictionary<string, string>> Load(TextAsset textAsset,
            out List<string> localeCodes)
        {
            try
            {
                var content = textAsset.text;
                return Read(content, out localeCodes);
            }
            catch (Exception x)
            {
                Debug.LogError($"textAsset {textAsset.name} {x.GetType().Name}: {x.Message}");
                return Empty(out localeCodes);
            }
        }

        public static Dictionary<string, Dictionary<string, string>> Load(string filename,
            out List<string> localeCodes)
        {
            try
            {
                var content = FileUtil.ReadAllText(filename);
                return Read(content, out localeCodes);
            }
            catch (IOException x)
            {
                Debug.LogError($"file {filename} {x.GetType().Name}: {x.Message}");
                return Empty(out localeCodes);
            }
        }

        private static Dictionary<string, Dictionary<string, string>> Empty(out List<string> localeCodes)
        {
            localeCodes = new List<string>();
            return new Dictionary<string, Dictionary<string, string>>();
        }

        private static Dictionary<string, Dictionary<string, string>> Read(string content, out List<string> localeCodes)
        {
            // Content has both CR-LF and plain LF 'line end' characters to format multiline columns properly.
            /*
                key	en	fi
                lang.en	English	Finnish
                lang.fi	Englanti	Suomi
             */
            localeCodes = new List<string>();
            var localeWords = new Dictionary<string, Dictionary<string, string>>();
            var lines = content.Split("\r\n");
            var localeColumns = lines[0].Split(Tab);
            var isSkipping = false;
            // Read locales from left to right until empty colum or eol.
            for (var i = 1; i < localeColumns.Length; ++i)
            {
                var localeCode = localeColumns[i];
                if (string.IsNullOrEmpty(localeCode))
                {
                    isSkipping = true;
                    continue;
                }
                if (isSkipping)
                {
                    throw new UnityException($"invalid columns in first line: '{lines[0]}'");
                }
                localeCodes.Add(localeCode);
                localeWords.Add(localeCode, new Dictionary<string, string>());
            }
            var localeCount = localeCodes.Count;
            var errorCount = 0;
            for (var i = 1; i < lines.Length; ++i)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    // Allow empty lines and comment lines.
                    continue;
                }
                var words = line.Split(Tab);
                if (words.Length < 1 + localeCount)
                {
                    throw new UnityException($"invalid line: '{line}'");
                }
                try
                {
                    var key = words[0];
                    if (key.EndsWith('#'))
                    {
                        // Allow comment char after key so that lines can be sorted by the key.
                        continue;
                    }
                    // Read only those columns that have a locale code.
                    for (var index = 1; index <= localeCount; ++index)
                    {
                        // words is 1 based
                        var localizedWord = words[index];
                        if (string.IsNullOrEmpty(localizedWord))
                        {
                            continue;
                        }
                        if (localizedWord.StartsWith('"') && localizedWord.EndsWith('"'))
                        {
                            localizedWord = localizedWord.Length == 2
                                ? ""
                                : localizedWord.Substring(1, localizedWord.Length - 2);
                        }
                        if (localizedWord.Contains('\n'))
                        {
                            // Add proper CR-LF.
                            localizedWord = localizedWord.Replace("\n", "\r\n");
                        }
                        // localeCodes is zero based
                        var localeCode = localeCodes[index - 1];
                        localeWords[localeCode].Add(key, localizedWord);
                    }
                }
                catch (Exception x)
                {
                    Debug.LogWarning($"line {i} {x.GetType().Name}: {x.Message}");
                    errorCount += 1;
                }
            }
            if (errorCount > 0)
            {
                Debug.LogWarning($"errors {errorCount}");
                for (var i = 1; i < lines.Length; ++i)
                {
                    var line = lines[i];
                    Debug.Log($"{i,-4} {line}");
                }
            }
            return localeWords;
        }
    }
}

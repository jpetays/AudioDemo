using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace Prg.Util
{
    /// <summary>
    /// Simple file logger that catches all log messages from UNITY and writes them to a file.
    /// </summary>
    public class LogFileWriter
    {
        private const string LogFileSuffix = "game.log";

        private static readonly Encoding Encoding = PlatformUtil.Encoding;

        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public static void CreateLogFileWriter()
        {
            // Safeguard to close previous log file before creating a new one.
            if (AppPlatform.IsWebGL)
            {
                UnityEngine.Debug.Log("LogFileWriter is not created on WebGL");
                return;
            }
            _instance = new LogFileWriter(_instance);
        }

        private static LogFileWriter _instance;

        private StreamWriter _writer;
        private readonly object _lock = new();
        private readonly StringBuilder _builder = new(500);
#if PRG_DEBUG
        private int _prevLogLineCount;
        private string _prevLogString = string.Empty;
#endif

        private LogFileWriter(LogFileWriter previous)
        {
            var hadPrevious = previous != null;
            previous?.Close();
            var baseName = GetLogName();
            var baseFileName = Path.Combine(Application.persistentDataPath, baseName);
            var filename = baseFileName;
            try
            {
                // We can only write safely directly under Application.persistentDataPath on all platforms!
                // WebGL is very limited for file operations inside a browser sandbox.
                var retry = 1;
                for (;;)
                {
                    try
                    {
                        // Open for overwrite!
                        _writer = new StreamWriter(filename, false, Encoding) { AutoFlush = true };
                        break;
                    }
                    catch (IOException)
                    {
                        // Sharing violation if more than one instance at the same time
                        if (++retry > 10)
                        {
                            throw new UnityException("Unable to allocate log file");
                        }
                        var newSuffix = $"{retry:D2}_{LogFileSuffix}";
                        filename = baseFileName.Replace(LogFileSuffix, newSuffix);
                    }
                }
                // Show effective log filename.
                filename = AppPlatform.ConvertToWindowsPath(filename);
            }
            catch (Exception x)
            {
                _writer = null;
                UnityEngine.Debug.LogWarning($"unable to create log file {filename}");
                UnityEngine.Debug.LogException(x);
                throw;
            }
            if (!hadPrevious)
            {
                UnityEngine.Debug.Log($"log file {filename}");
                WriteLog($"console aka Editor.log {AppPlatform.ConvertToWindowsPath(Application.consoleLogPath)}");
            }
            _instance = this;
            Application.logMessageReceivedThreaded += UnityLogCallback;
            Application.quitting += Close;
        }

        private void Close()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
            }
            // Release instance when file has been closed.
            _instance = null;
            Application.logMessageReceivedThreaded -= UnityLogCallback;
            Application.quitting -= Close;
        }

        private void WriteLog(string message)
        {
            if (_writer == null)
            {
                return;
            }
            _writer.WriteLine(message);
            _writer.Flush();
        }

        /// <summary>
        /// Thread safe callback to listen UNITY Debug messages and write them to a file.
        /// </summary>
        /// <remarks>
        /// This is thread safe because Debug.Log can be called from background threads as well.
        /// </remarks>
        private void UnityLogCallback(string logString, string stackTrace, LogType type)
        {
            lock (_lock)
            {
#if PRG_DEBUG
                if (type != LogType.Error && logString.Equals(_prevLogString, StringComparison.Ordinal))
                {
                    // Filter away messages that comes in every frame like:
                    // There are no audio listeners in the scene. Please ensure there is always one audio listener in the scene
                    // Warning	Mesh has more materials (2) than subsets (1)
                    _prevLogLineCount += 1;
                    return;
                }

                if (_prevLogLineCount > 2)
                {
                    _instance.WriteLog($"duplicate_lines {_prevLogLineCount}");
                    _prevLogLineCount = 0;
                }
                _prevLogString = logString;
#if UNITY_EDITOR
                // Remove UNITY Console log tag (color) decorations.
                // - actually this removes all text between angle brackets for any tag etc.
                logString = RegexUtil.RemoveAllTags(logString);
#endif
#endif
                // Reset builder
                _builder.Length = 0;

                // File log has timestamp (and optionally category) before message.
                _builder.AppendFormat("{0:HH:mm:ss.fff} ", DateTime.Now);
                if (type != LogType.Log)
                {
                    _builder.Append(type).Append(' ');
                }

                _builder.Append(logString);
                _instance.WriteLog(_builder.ToString());
                if (type != LogType.Error && type != LogType.Exception)
                {
                    return;
                }
                // Show stack trace only for real errors with proper call stack.
                if (stackTrace.Length > 5)
                {
                    _builder.Length = 0;
                    _builder.AppendFormat("{0:HH:mm:ss.fff}\t{1}\t{2}", DateTime.Now, "STACK", stackTrace);
                    _instance.WriteLog(_builder.ToString());
                }
            }
        }

        /// <summary>
        /// Gets log file name for current platform.
        /// </summary>
        /// <returns></returns>
        public static string GetLogName(string baseNameOverride = null)
        {
            void DeleteOldProductionFiles()
            {
                var oldFiles = Directory.GetFiles(Application.persistentDataPath, $"*_{LogFileSuffix}");
                var today = DateTime.Now.Day;
                foreach (var oldFile in oldFiles)
                {
                    if (oldFile.Contains("editor_"))
                    {
                        continue;
                    }
                    if (File.GetCreationTime(oldFile).Day != today)
                    {
                        try
                        {
                            File.Delete(oldFile);
                        }
                        catch (IOException)
                        {
                            // NOP - we just swallow it
                        }
                    }
                }
            }

            string prefix;
            if (AppPlatform.IsEditor)
            {
                prefix = "editor";
            }
            else
            {
                prefix = Application.platform.ToString().ToLower().Replace("player", string.Empty);
                DeleteOldProductionFiles();
            }
            var baseName = Application.productName.Replace(" ", "_").ToLower();
            return $"{prefix}_{baseName}_{LogFileSuffix}";
        }
    }
}

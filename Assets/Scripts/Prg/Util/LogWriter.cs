using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Prg.Util
{
    /// <summary>
    /// UNITY wrapper for <c>LogFileWriter</c> that catches all log messages from UNITY and writes them to a file.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class LogWriter : MonoBehaviour
    {
        private static LogWriter _instance;

        // ReSharper disable once NotAccessedField.Local
        [Header("Live Data"), SerializeField] private string _fileName;
        private LogFileWriter _logFileWriter;

        private void Awake()
        {
            if (_instance != null)
            {
                throw new UnityException("LogWriter already created");
            }
            // Register us as the singleton!
            _instance = this;
        }

        private void OnEnable()
        {
            _logFileWriter = LogFileWriter.CreateLogFileWriter();
            _fileName = _logFileWriter.Filename;
        }

        private void OnDestroy()
        {
            // OnApplicationQuit() comes before OnDestroy() so we are *not* interested to listen it.

            _logFileWriter?.Close();
            _instance = null;
        }
    }

    /// <summary>
    /// Simple file logger that catches all log messages from UNITY and writes them to a file.
    /// </summary>
    public class LogFileWriter
    {
        private const string LogFileSuffix = "game.log";

        private static readonly Encoding Encoding = PlatformUtil.Encoding;
        private static readonly object Lock = new();
        private static readonly StringBuilder Builder = new(500);

        private static int _prevLogLineCount;
        private static string _prevLogString = string.Empty;

        private static LogFileWriter _instance;

        public string Filename { get; }

        public static LogFileWriter CreateLogFileWriter() => new();

        private StreamWriter _writer;

        private LogFileWriter()
        {
            try
            {
                // We can only write safely directly under Application.persistentDataPath on all platforms!
                // WebGL is very limited for file operations inside a browser sandbox.
                var baseName = GetLogName();
                var baseFileName = Path.Combine(Application.persistentDataPath, baseName);
                Filename = baseFileName;
                var retry = 1;
                for (;;)
                {
                    try
                    {
                        // Open for overwrite!
                        _writer = new StreamWriter(Filename, false, Encoding) { AutoFlush = true };
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
                        Filename = baseFileName.Replace(LogFileSuffix, newSuffix);
                    }
                }
                // Show effective log filename.
                if (AppPlatform.IsWindows)
                {
                    Filename = AppPlatform.ConvertToWindowsPath(Filename);
                }
            }
            catch (Exception x)
            {
                _writer = null;
                UnityEngine.Debug.LogWarning($"unable to create log file '{Filename}'");
                UnityEngine.Debug.LogException(x);
                throw;
            }
            //UnityEngine.Debug.Log($"LogWriter Open file {Filename}");
            _instance = this;
            Application.logMessageReceivedThreaded += UnityLogCallback;
        }

        public void Close()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
            }
            Application.logMessageReceivedThreaded -= UnityLogCallback;
            //UnityEngine.Debug.Log($"LogWriter Close file {Filename}");
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
        private static void UnityLogCallback(string logString, string stackTrace, LogType type)
        {
            lock (Lock)
            {
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
                // Remove UNITY Console log tag (color) decorations.
                logString = Debug.FilterFormattedMessage(logString);
                // Reset builder
                Builder.Length = 0;

                // File log has timestamp (and optionally category) before message.
                Builder.AppendFormat("{0:HH:mm:ss.fff} ", DateTime.Now);
                if (type != LogType.Log)
                {
                    Builder.Append(type).Append(' ');
                }

                Builder.Append(logString);
                _instance.WriteLog(Builder.ToString());
                if (type != LogType.Error && type != LogType.Exception)
                {
                    return;
                }
                // Show stack trace only for real errors with proper call stack.
                if (stackTrace.Length > 5)
                {
                    Builder.Length = 0;
                    Builder.AppendFormat("{0:HH:mm:ss.fff}\t{1}\t{2}", DateTime.Now, "STACK", stackTrace);
                    _instance.WriteLog(Builder.ToString());
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

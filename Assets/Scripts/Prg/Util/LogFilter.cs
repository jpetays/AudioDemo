using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Prg.Util
{
    /// <summary>
    /// Convenience class to filter duplicate log messages from <c>Debug</c> log.
    /// </summary>
    public static class LogFilter
    {
        private static string _prevMessage;

        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public static void Log(string message, Object context = null, [CallerMemberName] string memberName = null)
        {
            if (message == _prevMessage)
            {
                return;
            }
            _prevMessage = message;
            // Actual caller is one more level up!
            Debug.FormatMessage(LogType.Log, message, context, memberName, new StackFrame(2).GetMethod());
        }
    }
}

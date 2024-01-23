#if PRG_DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Prg.Util
{
    /// <summary>
    /// Convenience class to filter duplicate log messages from <c>Debug</c> log.
    /// </summary>
    public class LogFilter
    {
        private string _prevMessage;

        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public void Log(string message, Object context = null, [CallerMemberName] string memberName = null,
            int skipFrames = 1)
        {
            if (message == _prevMessage)
            {
                return;
            }
            _prevMessage = message;
            // Actual caller is one+ more level(s) up!
            Debug.FormatMessage(LogType.Log, message, context, memberName, new StackFrame(skipFrames).GetMethod());
        }
    }
}
#else
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

namespace Prg.Util
{
    public class LogFilter
    {
        public void Log(string message, Object context = null, [CallerMemberName] string memberName = null,
            int skipFrames = 1)
        {
            UnityEngine.Debug.LogFormat(context, message);
        }
    }
}
#endif

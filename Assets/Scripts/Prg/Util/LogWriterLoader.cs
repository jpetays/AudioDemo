using UnityEditor;
using UnityEngine;

namespace Prg.Util
{
    public static class LogWriterLoader
    {
        public static void LoadLogWriter()
        {
            var parent = new GameObject(nameof(LogWriter));
            parent.AddComponent<LogWriter>();
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                // DontDestroyOnLoad will fail with 'InvalidOperationException' during EditMode tests etc.
                return;
            }
#endif
            Object.DontDestroyOnLoad(parent);
        }
    }
}

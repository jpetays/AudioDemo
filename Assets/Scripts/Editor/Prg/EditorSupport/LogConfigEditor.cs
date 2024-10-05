using System.Linq;
using System.Reflection;
using System.Text;
using Prg.Util;
using UnityEditor;
using UnityEngine;
using Debug = Prg.Debug;

namespace Editor.Prg.EditorSupport
{
    [CustomEditor(typeof(LogConfig))]
    public class LogConfigEditor : UnityEditor.Editor
    {
        private const string DefaultLoggingState = "01";
        private const string FolderPrefix = "Assets/Scripts";

        private static readonly string[] RulesForReset =
        {
            "^Prg\\.MyAssert=01",
            "^Editor\\..*=01",
            ".*SceneLoader.*=01",
            "^Prg\\..*=01",
        };

        private static readonly string[] ExcludedFoldersForReset =
        {
            FolderPrefix + "/Editor",
            FolderPrefix + "/Prg",
        };

        private static bool _isNaughtyChecked;
        private static bool _isNaughty;
        private static string _buttonLabel;

        public override void OnInspectorGUI()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                DrawDefaultInspector();
                return;
            }
            GUILayout.Space(20);
            if (!_isNaughtyChecked)
            {
                _isNaughtyChecked = true;
                var propertyInfo = typeof(LogConfig).GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .First(x => x.Name == nameof(LogConfig._loggerRules));
                _isNaughty = propertyInfo.GetCustomAttributes(false)
                    .Any(x => x.GetType().FullName == "NaughtyAttributes.ResizableTextAreaAttribute");
                _buttonLabel = !_isNaughty
                    ? "Reset 'Class Names Filter' to defaults"
                    : "Copy 'Default Names Filter' to Clipboard";
            }
            if (GUILayout.Button(_buttonLabel))
            {
                Debug.Log("*");
                serializedObject.Update();
                if (UpdateState(serializedObject, _isNaughty))
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
            if (GUILayout.Button("Set 'Class Names Filter' to empty"))
            {
                Debug.Log("*");
                serializedObject.Update();
                SetEmpty(serializedObject);
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.Space(20);
            DrawDefaultInspector();
        }

        private static void SetEmpty(SerializedObject serializedObject)
        {
            var loggerRules = serializedObject.FindProperty(nameof(LogConfig._loggerRules));
            loggerRules.stringValue = "";
        }

        private static bool UpdateState(SerializedObject serializedObject, bool isNaughty)
        {
            var loggerRules = serializedObject.FindProperty(nameof(LogConfig._loggerRules));
            var value = LoadAssetFolders();
            if (isNaughty)
            {
                EditorGUIUtility.systemCopyBuffer = value;
                return false;
            }
            loggerRules.stringValue = value;
            return true;
        }

        private static string LoadAssetFolders()
        {
            var folders = AssetDatabase.GetSubFolders(FolderPrefix);
            var builder = new StringBuilder();
            foreach (var defaultRule in RulesForReset)
            {
                builder.Append(defaultRule).AppendLine();
            }
            foreach (var folder in folders)
            {
                if (ExcludedFoldersForReset.Contains(folder))
                {
                    continue;
                }
                var line = folder.Replace($"{FolderPrefix}/", "^");
                line += $"\\..*={DefaultLoggingState}";
                builder.Append(line).AppendLine();
            }
            while (builder[^1] == '\r' || builder[^1] == '\n')
            {
                builder.Length -= 1;
            }
            return builder.ToString();
        }
    }
}

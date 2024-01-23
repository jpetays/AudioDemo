using System.Linq;
using System.Text;
using Prg.Util;
using UnityEditor;
using UnityEngine;
using Debug = Prg.Debug;
namespace Editor.Prg.EditorSupport
{
    [CustomEditor(typeof(LogConfig))]
    public class LogConfigEditor: UnityEditor.Editor
    {
        private const string DefaultLoggingState = "01";

        private static readonly string[] RulesForReset =
        {
            ".*SceneLoader.*=1",
            ".*ScoreFlash.*=0",
            ".*Window.*=0",
        };

        private static readonly string[] ExcludedFoldersForReset =
        {
            "Assets/Photon",
            "Assets/Plugins",
            "Assets/TextMesh Pro",
            "Assets/Prototype Textures",
        };

        public override void OnInspectorGUI()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                DrawDefaultInspector();
                return;
            }
            GUILayout.Space(20);
            if (GUILayout.Button("Reset 'Class Names Filter'"))
            {
                Debug.Log("*");
                serializedObject.Update();
                UpdateState(serializedObject);
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.Space(20);
            DrawDefaultInspector();
        }

        private static void UpdateState(SerializedObject serializedObject)
        {
            var loggerRules = serializedObject.FindProperty(nameof(LogConfig._loggerRules));
            loggerRules.stringValue = LoadAssetFolders();
        }

        private static string LoadAssetFolders()
        {
            const string folderPrefix = "Assets/Scripts";
            var folders = AssetDatabase.GetSubFolders("Assets/Scripts");
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
                var line = folder.Replace($"{folderPrefix}/", "^");
                line += $".*={DefaultLoggingState}";
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

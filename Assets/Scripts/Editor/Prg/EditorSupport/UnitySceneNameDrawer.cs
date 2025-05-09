using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Prg.Window.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Editor.Prg.EditorSupport
{
    [CustomPropertyDrawer(typeof(UnitySceneName), true)]
    public class UnitySceneNameDrawer : PropertyDrawer
    {
        private const string BadSceneNameMarker = "<<<";

        private class MyEditorScene
        {
            public readonly string SceneName;
            public readonly string SceneGuid;
            public readonly int SceneIndex;

            public MyEditorScene(EditorBuildSettingsScene scene, int sceneIndex)
            {
                var tokens = scene.path.Split('/');
                SceneName = Path.GetFileNameWithoutExtension(tokens[^1]);
                SceneGuid = scene.guid.ToString();
                SceneIndex = sceneIndex;
            }
        }

        private static List<MyEditorScene> _sceneList;
        private static string[] _sceneDisplayNames;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _sceneList = new List<MyEditorScene>();
            var scenes = EditorBuildSettings.scenes;
            var usedSceneCounter = -1;
            for (var sceneIndex = 0; sceneIndex < scenes.Length; ++sceneIndex)
            {
                var scene = scenes[sceneIndex];
                var buildSettingsIndex = scene.enabled ? ++usedSceneCounter : -1;
                _sceneList.Add(new MyEditorScene(scene, buildSettingsIndex));
            }
            _sceneList.Sort((a, b) => string.Compare(a.SceneName, b.SceneName, StringComparison.Ordinal));
            _sceneDisplayNames = new string[_sceneList.Count];
            for (var i = 0; i < _sceneDisplayNames.Length; ++i)
            {
                var scene = _sceneList[i];
                _sceneDisplayNames[i] = $"{scene.SceneName} [{scene.SceneIndex}]";
            }

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            {
                // Draw label
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                // Don't make child fields be indented
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                {
                    DrawProperty(position, property);
                }
                // Set indent back to what it was
                EditorGUI.indentLevel = indent;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // This is one line property
            return 1f * (base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing);
        }

        private static void DrawProperty(Rect position, SerializedProperty property)
        {
            // Calculate rects
            var lineWidth = position.width;
            var lineHeight = position.height;
            var line1 = new Rect(position.x, position.y, lineWidth, lineHeight);

            var sceneNameProp = property.FindPropertyRelative(UnitySceneName.SceneNameName);
            var sceneName = sceneNameProp.stringValue;

            var itemIndex = _sceneList.FindIndex(x => x.SceneName == sceneName);
            if (itemIndex == -1)
            {
                var levelName = $"{BadSceneNameMarker} {sceneName} {BadSceneNameMarker}";
                _sceneDisplayNames = AddItem(_sceneDisplayNames, levelName);
                itemIndex = _sceneDisplayNames.Length - 1;
            }
            var newItemIndex = EditorGUI.Popup(line1, itemIndex, _sceneDisplayNames, EditorStyles.popup);
            if (newItemIndex != itemIndex)
            {
                // Property was changed by user.
                sceneNameProp.stringValue = _sceneList[newItemIndex].SceneName;
                // Scene GUID is just saved for later use.
                var sceneGuidProp = property.FindPropertyRelative(UnitySceneName.SceneGuidName);
                sceneGuidProp.stringValue = _sceneList[newItemIndex].SceneGuid;
            }
        }

        private static T[] AddItem<T>(T[] array, T item)
        {
            var list = array.ToList();
            list.Add(item);
            return list.ToArray();
        }
    }
}

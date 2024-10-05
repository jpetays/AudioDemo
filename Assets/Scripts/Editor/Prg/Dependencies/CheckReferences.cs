using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Prg;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = Prg.Debug;

namespace Editor.Prg.Dependencies
{
    /// <summary>
    /// A helper editor script for finding missing references to objects.
    /// </summary>
    /// <remarks>
    /// See: https://github.com/liortal53/MissingReferencesUnity
    /// See: http://www.li0rtal.com/find-missing-references-unity/
    /// </remarks>
    public static class CheckReferences
    {
        public static void CheckReferencesInPrefabs()
        {
            var allPrefabs = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/") && path.EndsWith(".prefab"))
                .OrderBy(x => x)
                .ToList();
            var gameObjects = allPrefabs.Select(a =>
                    AssetDatabase.LoadAssetAtPath(a, typeof(GameObject)) as GameObject).Where(a => a != null)
                .ToList();
            FindMissingReferences("Prefab", gameObjects);
        }

        public static void CheckReferencesInScenes()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(x => x.enabled && !string.IsNullOrEmpty(x.path))
                .OrderBy(x => x.path)
                .ToList();
            foreach (var scene in scenes)
            {
                Debug.Log($"OpenScene {scene.path}");
                EditorSceneManager.OpenScene(scene.path);
                var sceneObjects = GetSceneObjects();
                FindMissingReferences("Scene", sceneObjects);
            }
            return;

            List<GameObject> GetSceneObjects()
            {
                // Use this method since GameObject.FindObjectsOfType will not return disabled objects.
                return Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(go => string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go))
                                 && go.hideFlags == HideFlags.None)
                    .ToList();
            }
        }

        public static void CheckTextMeshProUsage(string[] selectedGuids)
        {
            var gameObjects = new List<GameObject>();
            foreach (var guid in selectedGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                if (asset == null)
                {
                    continue;
                }
                gameObjects.Add(asset);
            }
            FindReferences("Text", gameObjects, typeof(TMP_Text));
        }

        public static void CheckComponentsInPrefabs<T>(Action<T> callback) where T : MonoBehaviour
        {
            var allPrefabs = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/") && path.EndsWith(".prefab"))
                .OrderBy(x => x);
            var gameObjects = allPrefabs.Select(a =>
                AssetDatabase.LoadAssetAtPath(a, typeof(GameObject)) as GameObject).Where(a => a != null);
            foreach (var gameObject in gameObjects)
            {
                var components = gameObject.GetComponentsInChildren<T>(includeInactive: true);
                foreach (var component in components)
                {
                    callback(component);
                }
            }
        }

        private static void FindReferences(string context, List<GameObject> gameObjects, Type type)
        {
            Debug.Log($"{context} FindReferences for {gameObjects.Count} entries, type {type.Name}");
            var foundCount = 0;
            foreach (var gameObject in gameObjects)
            {
                foundCount += FindReferences(context, gameObject, type);
            }
            Debug.Log($"{context} foundCount {foundCount}");
        }

        private static void FindMissingReferences(string context, List<GameObject> gameObjects)
        {
            Debug.Log($"{context} FindMissingReferences for {gameObjects.Count} entries");
            var missingCount = 0;
            foreach (var gameObject in gameObjects)
            {
                missingCount += FindMissingReferences(context, gameObject);
            }
            Debug.Log($"{context} missingCount {missingCount}");
        }

        private static readonly PropertyInfo ObjRefValueMethod =
            typeof(SerializedProperty).GetProperty("objectReferenceStringValue",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static int FindReferences(string context, GameObject gameObject, Type type)
        {
            var foundCount = 0;
            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                // Missing components will be null, we can't find their type, etc.
                if (!component)
                {
                    Report.NotFound(context, gameObject, "Is component deleted?");
                    continue;
                }
                if (!type.IsInstanceOfType(component))
                {
                    continue;
                }
                foundCount += 1;
                Report.IsFound(context, gameObject, $"{type.Name}");
            }
            var transform = gameObject.transform;
            var childCount = transform.childCount;
            if (childCount > 0)
            {
                for (var i = 0; i < childCount; ++i)
                {
                    var child = transform.GetChild(i);
                    foundCount += FindReferences(context, child.gameObject, type);
                }
            }
            return foundCount;
        }

        private static int FindMissingReferences(string context, GameObject gameObject)
        {
            var missingCount = 0;
            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                // Missing components will be null, we can't find their type, etc.
                if (!component)
                {
                    Report.NotFound(context, gameObject, "Is component deleted?");
                    missingCount += 1;
                    continue;
                }
                var so = new SerializedObject(component);
                var sp = so.GetIterator();
                // Iterate over the components' properties.
                while (sp.NextVisible(true))
                {
                    // This should find arrays of object - at least if first item is missing
                    if (sp.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }
                    var objectReferenceStringValue = string.Empty;
                    if (ObjRefValueMethod != null)
                    {
                        objectReferenceStringValue =
                            (string)ObjRefValueMethod.GetGetMethod(true).Invoke(sp, new object[] { });
                    }
                    if (sp.objectReferenceValue != null)
                    {
                        continue;
                    }
                    if (sp.objectReferenceInstanceIDValue == 0 && !objectReferenceStringValue.StartsWith("Missing"))
                    {
                        // objectReferenceStringValue is None (Xxx), e.g. 'None (Camera)'.
                        continue;
                    }
                    var componentName = component.GetType().Name;
                    var propName1 = ObjectNames.NicifyVariableName(sp.name);
                    var propName2 = sp.propertyPath;
                    var propName = string.Equals(propName1, propName2, StringComparison.CurrentCultureIgnoreCase)
                        ? propName1
                        : $"{propName1} ({propName2})";
                    Report.Missing(context, gameObject, componentName, propName, objectReferenceStringValue);
                    missingCount += 1;
                }
            }
            var transform = gameObject.transform;
            var childCount = transform.childCount;
            if (childCount > 0)
            {
                for (var i = 0; i < childCount; ++i)
                {
                    var child = transform.GetChild(i);
                    missingCount += FindMissingReferences(context, child.gameObject);
                }
            }
            return missingCount;
        }
    }

    public static class Report
    {
        public static void IsFound(string context, GameObject gameObject, string message)
        {
            Debug.Log($"{RichText.Green("FOUND")}: [{context}] {GetFullPath(gameObject)}: " +
                      $"{message}", gameObject);
        }

        public static void NotFound(string context, GameObject gameObject, string message)
        {
            Debug.Log($"{RichText.Red("NOT FOUND")}: [{context}] {GetFullPath(gameObject)}: " +
                      $"{message}", gameObject);
        }

        public static void Missing(string context, GameObject gameObject,
            string componentName, string propertyName, string propMessage)
        {
            Debug.Log($"{RichText.Yellow("MISSING")}: [{context}] {GetFullPath(gameObject)}: " +
                      $"component: {componentName}, property: {propertyName} : {propMessage}", gameObject);
        }

        private static string GetFullPath(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return string.Empty;
            }
            var path = new StringBuilder(gameObject.name);
            while (gameObject.transform.parent != null)
            {
                gameObject = gameObject.transform.parent.gameObject;
                path.Insert(0, '/').Insert(0, gameObject.name);
            }
            return path.ToString();
        }
    }
}

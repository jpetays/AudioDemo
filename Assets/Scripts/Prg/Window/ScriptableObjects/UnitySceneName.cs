using System;
using UnityEngine;

namespace Prg.Window.ScriptableObjects
{
    /// <summary>
    /// Convenience class to set UNITY scene name in Editor without typos using custom <c>PropertyDrawer</c>.
    /// </summary>
    [Serializable]
    public class UnitySceneName
    {
        public const string SceneNameName = nameof(_sceneName);
        public const string SceneGuidName = nameof(_sceneGuid);

        /// <summary>
        /// UNITY scene name without path.
        /// </summary>
        [SerializeField] private string _sceneName;

        /// <summary>
        /// Scene GUID is currently not used, but just saved if need to track actual scene by its GUID arises.
        /// </summary>
        [SerializeField] private string _sceneGuid;

        public string SceneName => _sceneName;

        public void SetSceneName(string sceneName) => _sceneName = sceneName;
    }
}

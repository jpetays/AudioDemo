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
        public const string SceneName = nameof(_sceneName);
        public const string SceneGuid = nameof(_sceneGuid);

        /// <summary>
        /// UNITY scene name without path.
        /// </summary>
        public string _sceneName;

        /// <summary>
        /// Scene GUID is currently not used, but just saved if need to track actual scene by its GUID arises.
        /// </summary>
        [SerializeField] private string _sceneGuid;
    }
}

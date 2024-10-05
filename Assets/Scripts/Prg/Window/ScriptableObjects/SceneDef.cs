using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prg.Window.ScriptableObjects
{
    /// <summary>
    /// Scene definition for <c>WindowManager</c> and <c>SceneLoader</c>.<br />
    /// It contains UNITY scene name and flag to indicate if it is a networked game scene.
    /// </summary>
    [CreateAssetMenu(menuName = "Prg/Prg/SceneDef", fileName = "scene NAME")]
    public class SceneDef : ScriptableObject
    {
        [SerializeField] private UnitySceneName _sceneName;
        [SerializeField] private bool _isNetworkScene;

        public string SceneName => _sceneName.SceneName;
        public bool IsNetworkScene => _isNetworkScene;

        public void SetSceneName(string sceneName)
        {
            _sceneName = new UnitySceneName();
            _sceneName.SetSceneName(sceneName);
        }

        public override string ToString()
        {
            return $"SceneDef: {SceneName}, network: {_isNetworkScene}";
        }
    }
}

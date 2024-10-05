using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace Prg.Window
{
    /// <summary>
    /// These <c>PlatformNames</c> are exactly same as UNITY <c>RuntimePlatform</c> but the numbering is our own.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum PlatformNames
    {
        OSXPlayer = 2,
        WindowsPlayer = 3,
        IPhonePlayer = 5,
        Android = 6,
        LinuxPlayer = 7,
        WebGLPlayer = 9,
    }

    /// <summary>
    /// Allow selected component(s) on given platform(s).<br />
    /// </summary>
    /// <remarks>
    /// Selected component(s) are activated or disabled based on selection criteria success.
    /// </remarks>
    public class PlatformSelector : MonoBehaviour
    {
        private const string Tp1 = "Applicable when running in UNITY Editor";
        private const string Tp2 = "Applicable when 'Development Build' in Build Settings is set";
        private const string Tp3 = "Applicable for given production platforms below";
        private const string Tp4 = "GameObject(s) to enable on allowed platform(s), otherwise they are disabled";

        [SerializeField, Tooltip(Tp1)] private bool _isAllowInEditor;
        [SerializeField, Tooltip(Tp2)] private bool _isAllowInDevelopmentBuild;
        [SerializeField, Tooltip(Tp3)] private bool _isAllowInProductionPlatforms;
        [SerializeField] private PlatformNames[] _productionPlatforms;
        [SerializeField, Tooltip(Tp4)] private GameObject[] _gameObjectsToWatch;

        private bool IsAllowedPlatform(string platformName) => _productionPlatforms.Any(x =>
            string.Compare(platformName, x.ToString(), StringComparison.Ordinal) == 0);

        private void OnEnable()
        {
            MyAssert.IsTrue(_gameObjectsToWatch.Length > 0,
                "PlatformSelector gameObjectsToWatch has nothing to manage", this);
            var isAllowed = (_isAllowInEditor && AppPlatform.IsEditor) ||
                            (_isAllowInDevelopmentBuild && AppPlatform.IsDevelopmentBuild) ||
                            (_isAllowInProductionPlatforms && IsAllowedPlatform(Application.platform.ToString()));
            Debug.Log(
                $"{name} {Application.platform} isAllowed {isAllowed}: editor {AppPlatform.IsEditor}, development {AppPlatform.IsDevelopmentBuild}" +
                $", platforms {string.Join(',', _productionPlatforms)}",
                this);
            HandleComponents(isAllowed);
        }

        private void HandleComponents(bool state)
        {
            foreach (var gameObjectToDisable in _gameObjectsToWatch)
            {
                gameObjectToDisable.SetActive(state);
            }
        }
    }
}

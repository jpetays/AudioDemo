using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Prg.Window
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum PlatformNames
    {
        OSXEditor = 1,
        OSXPlayer = 2,
        WindowsPlayer = 3,
        WindowsEditor = 4,
        IPhonePlayer = 5,
        Android = 6,
        LinuxPlayer = 7,
        LinuxEditor = 8,
        WebGLPlayer = 9,
        WSAPlayerX86 = 10,
        WSAPlayerX64 = 11,
        WSAPlayerARM = 20,
        PS4 = 13,
        XboxOne = 14,
        tvOS = 15,
        Switch = 16,
        Stadia = 17,
        EmbeddedLinuxArm32 = 18,
        EmbeddedLinuxX64 = 19,
        EmbeddedLinuxX86 = 20,
        LinuxServer = 21,
        WindowsServer = 22,
        OSXServer = 23,
        QNXArm32 = 24,
        QNXArm64 = 25,
        QNXX64 = 26,
        QNXX86 = 27
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
        private const string Tp3 = "Applicable for given build platforms below";
        private const string Tp4 = "GameObject(s) to enable on allowed platform(s), otherwise they are disabled";

        [SerializeField, Tooltip(Tp1)] private bool _isAllowInEditor;
        [SerializeField, Tooltip(Tp2)] private bool _isAllowDevelopmentBuild;
        [SerializeField, Tooltip(Tp3)] private bool _isAllowInProductionPlatforms;
        [SerializeField] private PlatformNames[] _platformNames;
        [SerializeField, Tooltip(Tp4)] private GameObject[] _gameObjectsToWatch;

        private void OnEnable()
        {
            Debug.Log(
                $"{name} on {Application.platform} is: editor {AppPlatform.IsEditor}, dev build {AppPlatform.IsDevelopmentBuild}, platforms {string.Join(',', _platformNames)}",
                this);
            if (AppPlatform.IsEditor && _isAllowInEditor)
            {
                HandleComponents(true);
                return;
            }
            if (AppPlatform.IsDevelopmentBuild && _isAllowDevelopmentBuild)
            {
                HandleComponents(true);
                return;
            }
            if (IsAllowedPlatform(Application.platform.ToString()) && _isAllowInProductionPlatforms)
            {
                HandleComponents(true);
                return;
            }
            HandleComponents(false);
        }

        private void HandleComponents(bool state)
        {
            foreach (var gameObjectToDisable in _gameObjectsToWatch)
            {
                gameObjectToDisable.SetActive(state);
            }
        }

        private bool IsAllowedPlatform(string platformName)
        {
            foreach (var platform in _platformNames)
            {
                var allowedPlatform = platform.ToString();
                if (platformName.Equals(allowedPlatform))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

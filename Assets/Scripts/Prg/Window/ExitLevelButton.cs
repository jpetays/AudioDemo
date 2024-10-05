using System;
using System.Collections;
using Prg.Window.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prg.Window
{
    public interface IExitLevelMessage
    {
        void OnExitLevel();
    }

    /// <summary>
    /// Enhanced <c>NaviButton</c> that broadcasts a message before opening the next window.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ExitLevelButton : MonoBehaviour
    {
        private const string Tooltip = "Pop out and hide current window before showing target window";

        [Header("Settings"), SerializeField] private WindowDef _naviTarget;
        [Tooltip(Tooltip), SerializeField] private bool _isCurrentPopOutWindow;

        private void Start()
        {
            Debug.Log($"{name}", this);
            var button = GetComponent<Button>();
            if (_naviTarget == null)
            {
                button.interactable = false;
                return;
            }
            var windowManager = WindowManager.Get();
            var isCurrentWindow = windowManager.FindIndex(_naviTarget) == 0;
            if (isCurrentWindow)
            {
                button.interactable = false;
                return;
            }
            button.onClick.AddListener(() =>
            {
                button.interactable = false;
                StartCoroutine(DoNaviButtonClick());
            });
        }

        private void OnExitLevel()
        {
            // Method name for the IExitLevelMessage interface
        }

        private IEnumerator DoNaviButtonClick()
        {
            yield return new WaitForEndOfFrame();
            GetRootGameObjects(rootGameObject =>
            {
                rootGameObject.BroadcastMessage(nameof(OnExitLevel), SendMessageOptions.DontRequireReceiver);
            });
            yield return null;
            Debug.Log($"naviTarget {_naviTarget} isCurrentPopOutWindow {_isCurrentPopOutWindow}", _naviTarget);
            var windowManager = WindowManager.Get();
            if (_isCurrentPopOutWindow)
            {
                windowManager.PopCurrentWindow();
            }
            windowManager.UnwindNaviHelper(_naviTarget);
            windowManager.ShowWindow(_naviTarget);
        }

        private static void GetRootGameObjects(Action<GameObject> onRootGameObject)
        {
            var currentScene = SceneManager.GetActiveScene();
            var rootGameObjects = currentScene.GetRootGameObjects();
            foreach (var rootGameObject in rootGameObjects)
            {
                onRootGameObject(rootGameObject);
            }
        }
    }
}

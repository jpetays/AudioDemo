using System;
using System.Collections;
using Prg.Window.ScriptableObjects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prg.Window
{
    /// <summary>
    /// OnExitLevel custom event listener.
    /// </summary>
    public interface IExitLevelMessage: IEventSystemHandler
    {
        void OnExitLevel();
    }

    /// <summary>
    /// Enhanced <c>NaviButton</c> that sends OnExitLevel custom event before opening the next window.<br />
    /// https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/MessagingSystem.html
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

        private IEnumerator DoNaviButtonClick()
        {
            yield return new WaitForEndOfFrame();
            GetRootGameObjects(rootGameObject =>
            {
                ExecuteEvents.Execute<IExitLevelMessage>(rootGameObject, null,
                    (x, _) => x.OnExitLevel());
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

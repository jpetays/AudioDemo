using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prg.Window
{
    /// <summary>
    /// Pops out current window and then reloads current scene.
    /// </summary>
    /// <remarks>
    /// Click handler has one frame delay to let other button listeners execute
    /// before actually closing the current window and going back.
    /// </remarks>
    [RequireComponent(typeof(Button))]
    public class ReloadLevelButton : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"{name}", this);
            var button = GetComponent<Button>();
            button.onClick.AddListener(() => StartCoroutine(OnClick()));
        }

        private static IEnumerator OnClick()
        {
            yield return null;
            var windowManager = WindowManager.Get();
            windowManager.PopCurrentWindow();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

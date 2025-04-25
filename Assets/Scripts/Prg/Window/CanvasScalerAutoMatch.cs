using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace Prg.Window
{
    /// <summary>
    /// Sets <c>CanvasScaler</c> matchWidthOrHeight value based on screen width and height in order to force portrait or landscape match value.
    /// </summary>
    /// <remarks>
    /// Requires <c>ScaleMode.ScaleWithScreenSize</c> to work!
    /// </remarks>
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasScalerAutoMatch : MonoBehaviour
    {
        private const float DefaultLandscapeMatch = 0f;
        private const float DefaultPortraitMatch = 1f;

        [Header("Settings"), SerializeField] private float _landscapeMatch = DefaultLandscapeMatch;
        [SerializeField] private float _portraitMatch = DefaultPortraitMatch;
#if UNITY_EDITOR
        [SerializeField, Header("For Editor")] private float _pollingInterval = 0.5f;
#endif

        private void OnEnable()
        {
            var canvasScaler = GetComponent<CanvasScaler>();
            if (canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                enabled = false;
                return;
            }
            StartEditorPoller(canvasScaler);
            FixCanvasScaler(canvasScaler, _landscapeMatch, _portraitMatch);
        }

        private static void FixCanvasScaler(CanvasScaler canvasScaler, float landscapeMatch, float portraitMatch)
        {
            var match = Screen.width > Screen.height ? landscapeMatch : portraitMatch;
            if (Mathf.Approximately(canvasScaler.matchWidthOrHeight, match))
            {
                return;
            }
            Debug.Log(
                $"screen w={Screen.width} h={Screen.height} matchWidthOrHeight {canvasScaler.matchWidthOrHeight:0.0} <- {match:0.0}",
                canvasScaler.gameObject);
            canvasScaler.matchWidthOrHeight = match;
        }

        [Conditional("UNITY_EDITOR")]
        private void StartEditorPoller(CanvasScaler canvasScaler)
        {
#if UNITY_EDITOR
            InternalStartEditorPoller(canvasScaler);
#endif
        }

#if UNITY_EDITOR
        [Conditional("UNITY_EDITOR")]
        private void InternalStartEditorPoller(CanvasScaler canvasScaler)
        {
            if (!AppPlatform.IsEditor && !AppPlatform.IsSimulator)
            {
                return;
            }
            // No other way than to poll for changes in Screen size or its orientation.
            StartCoroutine(ScreenChangedPoller(canvasScaler));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator ScreenChangedPoller(CanvasScaler canvasScaler)
        {
            var width = 0;
            var height = 0;
            var orientation = Screen.orientation;
            YieldInstruction delay = _pollingInterval > 0 ? new WaitForSeconds(_pollingInterval) : null;
            for (; enabled;)
            {
                yield return delay;
                if (height == Screen.height && width == Screen.width && Screen.orientation == orientation)
                {
                    continue;
                }
                width = Screen.width;
                height = Screen.height;
                FixCanvasScaler(canvasScaler, _landscapeMatch, _portraitMatch);
            }
        }
#endif
    }
}

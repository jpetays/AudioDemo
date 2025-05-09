using System.Collections;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Prg
{
    /// <summary>
    /// Helper methods to create UNITY singleton <c>Component</c> with hosting <c>GameObject</c>.
    /// </summary>
    /// <remarks>
    /// <c>Component</c> lifetime is either forever (<c>Object.DontDestroyOnLoad()</c>) or current scene.
    /// </remarks>
    public static class UnitySingleton
    {
        public static T CreateStaticSingleton<T>() where T : Component
        {
            var name = typeof(T).Name;
            var parent = new GameObject(name);
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                // DontDestroyOnLoad will fail with 'InvalidOperationException' during EditMode tests etc. and we just skip it with error message.
                Debug.LogError($"You are creating a STATIC SINGLETON outside PLAY mode: {name}");
                return parent.AddComponent<T>();
            }
#endif
            Object.DontDestroyOnLoad(parent);
            return parent.AddComponent<T>();
        }

        public static T CreateGameObjectAndComponent<T>(string name = null) where T : Component
        {
            var parent = new GameObject(name ?? typeof(T).Name);
            return parent.AddComponent<T>();
        }
    }

    /// <summary>
    /// Extension for working with UNITY <c>GameObject</c>s and <c>Component</c>s.
    /// </summary>
    public static class UnityExtensions
    {
        #region GameObjects and Components

        public static bool IsSceneObject(this GameObject instance)
        {
            return instance.scene.handle != 0;
        }

        public static bool IsSceneObject(this Component component)
        {
            return component.gameObject.scene.handle != 0;
        }

        public static bool HasParent(this GameObject gameObject)
        {
            return gameObject.transform.parent != null;
        }

        public static bool HasChild(this GameObject parent, GameObject other)
        {
            return parent.transform.HasChild(other.transform);
        }

        public static T GetOrAddComponent<T>(this GameObject parent) where T : Component
        {
            var component = parent.GetComponent<T>();
            return component != null ? component : parent.AddComponent<T>();
        }

        #endregion

        #region Transform

        private static readonly Quaternion NormalRotation = Quaternion.Euler(0f, 0f, 0f);
        private static readonly Quaternion UpsideDown = Quaternion.Euler(0f, 0f, 180f);

        public static void Rotate(this Transform transform, bool isUpsideDown)
        {
            var rotation = isUpsideDown ? UpsideDown : NormalRotation;
            transform.rotation = rotation;
        }

        public static bool HasChild(this Transform parent, Transform other)
        {
            Assert.AreNotEqual(parent, other);
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child == other || child.HasChild(other))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Collision2D

        public static ContactPoint2D GetFirstContactPoint(this Collision2D collision)
        {
            if (collision == null || collision.contactCount == 0)
            {
                return new ContactPoint2D();
            }
            var contactPoint = collision.GetContact(0);
            return contactPoint;
        }

        #endregion

        #region Button

        public static void SetCaptionColor(this Button button, Color color)
        {
            var textMeshProUGUI = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.color = color;
                return;
            }
            throw new UnityException($"Button {button.GetFullPath()} does not have TextMeshProUGUI component");
        }

        public static void SetCaption(this Button button, string caption)
        {
            var textMeshProUGUI = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = caption;
                return;
            }
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                Debug.LogWarning(
                    $"Using deprecating 'UI/Legacy/Text' instead of 'TextMeshProUGUI' in {button.GetFullPath()}");
                text.text = caption;
                return;
            }
            var tmpText = button.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                Debug.LogWarning($"Using old 'TMP_Text' instead of new 'TextMeshProUGUI' in {button.GetFullPath()}");
                tmpText.text = caption;
                return;
            }
            Assert.IsTrue(false, "button does not have a text component");
        }

        public static string GetCaption(this Button button)
        {
            var textMeshProUGUI = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
            {
                return textMeshProUGUI.text;
            }
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                Debug.LogWarning(
                    $"Using deprecating 'UI/Legacy/Text' instead of 'TextMeshProUGUI' in {button.GetFullPath()}");
                return text.text;
            }
            var tmpText = button.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                Debug.LogWarning($"Using old 'TMP_Text' instead of new 'TextMeshProUGUI' in {button.GetFullPath()}");
                return tmpText.text;
            }
            Assert.IsTrue(false, "button does not have a text component");
            return null;
        }

        #endregion

        #region TrailRenderer

        public static void ResetTrailRendererAfterTeleport(this TrailRenderer trailRenderer, MonoBehaviour host,
            int skipFrames = 2)
        {
            IEnumerator DelayedExecute()
            {
                var trailRendererTime = trailRenderer.time;
                trailRenderer.time = 0;
                trailRenderer.emitting = false;
                yield return null;
                // It seems that at least two frames are required for physics engine to catch up after Rigidbody has been teleported.
                // - note that this was tested using WaitForFixedUpdate which was *totally wrong* in this context!
                var delay = new WaitForEndOfFrame();
                while (--skipFrames >= 0)
                {
                    yield return delay;
                }
                trailRenderer.time = trailRendererTime;
                trailRenderer.emitting = true;
            }

            host.StartCoroutine(DelayedExecute());
        }

        #endregion

        #region Rect

        public static Rect Inflate(this Rect rect, Vector2 size)
        {
            return new Rect
            {
                xMin = rect.xMin - size.x,
                yMin = rect.yMin - size.y,
                xMax = rect.xMax + size.x,
                yMax = rect.yMax + size.y
            };
        }

        public static Rect Inflate(this Rect rect, float left, float top, float right, float bottom)
        {
            return new Rect
            {
                xMin = rect.xMin - left,
                yMin = rect.yMin - top,
                xMax = rect.xMax + right,
                yMax = rect.yMax + bottom
            };
        }

        #endregion

        #region Debugging

        public static string GetFullPath(this Component component)
        {
            return component == null ? string.Empty : GetFullPath(component.gameObject);
        }

        public static string GetFullPath(this GameObject gameObject)
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

        public static string GetFullName(this Scene scene)
        {
            return $"[{scene.name}](#{scene.buildIndex})";
        }

        public static string GetInfo(this Collider2D other, bool enter)
        {
            var otherObject = other.gameObject;
            var direction = enter ? "ENTER" : "EXIT";
            var trigger = other.isTrigger ? " trig" : "norm";
            return
                $"{direction} {otherObject.name} C={trigger} T={otherObject.tag} L={LayerMask.LayerToName(otherObject.layer)}";
        }

        public static string GetInfo(this Collision2D collision, bool enter)
        {
            return GetInfo(collision.collider, enter);
        }

        #endregion
    }
}

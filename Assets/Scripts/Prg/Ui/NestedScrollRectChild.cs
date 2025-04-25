using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Prg.Ui
{
    /// <summary>
    /// Helper to allow nested <c>ScrollRect</c> children to bubble pointer drag events to its <c>ScrollRect</c> parent.<br />
    /// Video: https://www.youtube.com/watch?v=eGcW7xYVurM<br />
    /// Asset Store: https://assetstore.unity.com/packages/tools/gui/nested-ui-scroll-view-with-snapping-scrollrect-extension-134131<br />
    /// No support for nesting but newer and maintained:<br />
    /// Magnetic Scroll View: https://assetstore.unity.com/packages/tools/gui/magnetic-scroll-view-93042
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class NestedScrollRectChild : MonoBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string Ib =
            "Helper to allow nested ScrollRect children to bubble pointer drag events to its ScrollRect parent.";

        [SerializeField, InfoBox(Ib)] private ScrollRect _parent;
        [SerializeField] private bool _findParent;

        private void Awake()
        {
            if (_parent == null && _findParent)
            {
                _parent = FindParentScrollRect(transform);
            }
            if (_parent == null)
            {
                enabled = false;
                return;
            }
            Debug.Log($"{name} parent {_parent.name}", this);
        }

        private static ScrollRect FindParentScrollRect(Transform transform)
        {
            for (;;)
            {
                transform = transform.parent;
                if (transform == null)
                {
                    return null;
                }
                var scrollRect = transform.GetComponent<ScrollRect>();
                if (scrollRect != null)
                {
                    return scrollRect;
                }
            }
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            Debug.Log($"{enabled}", this);
            _parent.OnInitializePotentialDrag(eventData);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            _parent.OnBeginDrag(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            _parent.OnDrag(eventData);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            _parent.OnEndDrag(eventData);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Prg.Util
{
    public class RaycastAll
    {
        private readonly bool _isDebugLog;

        private readonly PointerEventData _uiPointerEventData = new(EventSystem.current);
        private readonly List<RaycastResult> _uiRaycastResultList = new();

        public RaycastAll(bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;
        }

        public bool IsPointerOnActiveUiElement(Vector2 screenPosition)
        {
            _uiPointerEventData.position = screenPosition;
            EventSystem.current.RaycastAll(_uiPointerEventData, _uiRaycastResultList);
            if (_uiRaycastResultList.Count <= 0)
            {
                return false;
            }
            if (_isDebugLog)
                Debug.Log($"RaycastAll {_uiRaycastResultList.Count} @ {screenPosition.x:0},{screenPosition.y:0}");
            foreach (var raycastResult in _uiRaycastResultList)
            {
                var uiObject = raycastResult.gameObject;
                var selectable = uiObject.GetComponentInChildren<Selectable>();
                if (selectable == null || !selectable.interactable)
                {
                    continue;
                }
                if (_isDebugLog)
                    Debug.Log($"BLOCKED {selectable.name} {screenPosition.x:0},{screenPosition.y:0}", selectable);
                return true;
            }
            return false;
        }
    }
}

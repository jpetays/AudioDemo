using System;
using UnityEngine;

namespace Prg.Util
{
    [RequireComponent(typeof(Collider2D))]
    public class Collider2DGizmo : MonoBehaviour
    {
        [SerializeField, Header("Settings")] private float _gizmoSizeFactor = 0.9f;
        [SerializeField] private Color _gizmoColor = Color.magenta;

        [SerializeField, Header("Live Data")] private Collider2D _collider;

        private bool _isAwake;
        private bool _isValid;
        private Vector3 _gizmoCubeSize;
        private float _gizmoSphereSize;

        private void Awake()
        {
            if (!AppPlatform.IsEditor)
            {
                return;
            }
            _collider = GetComponent<Collider2D>();
            _isValid = _collider != null;
            if (!_isValid)
            {
                return;
            }
            var size = _collider.bounds.size;
            if (_collider is BoxCollider2D)
            {
                _gizmoCubeSize = size * _gizmoSizeFactor;
                return;
            }
            _gizmoSphereSize = Mathf.Min(size.x / 2f, size.y / 2f) * _gizmoSizeFactor;
            _isAwake = true;
        }

        private void OnDrawGizmos()
        {
            if (!_isAwake)
            {
                Awake();
            }
            if (!_isValid)
            {
                return;
            }
            Gizmos.color = _gizmoColor;
            var position = transform.position;
            var offset = _collider.offset;
            position.x += offset.x;
            position.y += offset.y;
            if (_collider is BoxCollider2D)
            {
                Gizmos.DrawWireCube(position, _gizmoCubeSize);
            }
            Gizmos.DrawWireSphere(position, _gizmoSphereSize);
        }
    }
}

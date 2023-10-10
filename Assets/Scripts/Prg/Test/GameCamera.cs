using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Prg.Test
{
    public class GameCamera : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            _instance = null;
        }

        public static GameCamera Get() => _instance;

        private static GameCamera _instance;

        // We use full viewport size for all viewport related calculations.
        private static readonly Vector3 ViewportTopRight = new(1f, 1f, 1f);
        private static readonly Vector3 ViewportBottomLeft = new(0, 0, 1f);

        private const string Tp1 = "Transform for camera runtime position calculations";

        private const string Tp2 = "BoxCollider2D for camera runtime resize, repositon and collision detection\r\n" +
                                   "Must be <b>BoxCollider2D</b>";
        private const string Tp3 = "Tracker offset to move it outside camera collider\r\n" +
                                   "Trackers are used to detect when camera is about to move outside of allowed gameplay area\r\n" +
                                   "Trackers are are positioned automatically based on camera (collider) size and position";
        private const string Tp4 = "Right Side Tracker transform";
        private const string Tp5 = "Left Side Tracker transform";

        [SerializeField, Tooltip(Tp1), Header("Settings")] private Transform _cameraTransform;

        [SerializeField, Tooltip(Tp2), Header("Optional Settings")] private BoxCollider2D _cameraCollider2D;
        [SerializeField, Tooltip(Tp3)] private float _trackerOffsetX = -0.5f;
        [SerializeField, Tooltip(Tp3)] private float _trackerOffsetY = 0.5f;
        [SerializeField, Tooltip(Tp4)] private Transform _trackerTransformR;
        [SerializeField, Tooltip(Tp5)] private Transform _trackerTransformL;

        [SerializeField, Header("Live Data")] private Camera _camera;
        [SerializeField] private float _orthographicSize;
        [SerializeField] private Vector3 _cameraPosition;
        [SerializeField] private bool _hasCollider2D;
        [SerializeField] private bool _hasTrackers;

        [SerializeField, Header("Debug")] private Vector3 _worldTopRight;
        [SerializeField] private Vector3 _worldBottomLeft;
        [SerializeField] private float _cameraViewHalfWidth;
        [SerializeField] private float _cameraViewHalfHeight;

        public Camera Camera => _camera;

        public float CameraViewHalfWidth => _cameraViewHalfWidth;
        public float CameraViewHalfHeight => _cameraViewHalfHeight;

        private Vector3 _tempPosition;
        private Vector3 _tempSize;

        private void Awake()
        {
            _instance = this;
            Assert.IsNotNull(_cameraTransform);
            _camera = _cameraTransform.GetComponent<Camera>();
            Assert.IsNotNull(_camera);
            Assert.IsTrue(_camera.orthographic);
            _cameraPosition = _camera.transform.position;
            _orthographicSize = _camera.orthographicSize;
            if (_cameraCollider2D == null)
            {
                _cameraCollider2D = GetComponentInChildren<BoxCollider2D>();
            }
            _hasCollider2D = _cameraCollider2D != null;
            _hasTrackers = _hasCollider2D && _trackerTransformR != null && _trackerTransformL != null;
        }

        private void OnEnable()
        {
            StartCoroutine(FixCameraSize());
        }

        private IEnumerator FixCameraSize()
        {
            while (Time.frameCount < 10)
            {
                yield return null;
            }
            CalculateCameraSize();
        }

        private void OnDrawGizmos()
        {
            if (!_hasCollider2D)
            {
                return;
            }
            Gizmos.color = Color.magenta;
            _tempPosition = _cameraCollider2D.transform.position;
            _tempPosition.y += _cameraCollider2D.offset.y;
            var bounds = _cameraCollider2D.bounds;
            _tempSize.x = bounds.size.x;
            _tempSize.y = bounds.size.y;
            Gizmos.DrawWireCube(_tempPosition, _tempSize);
            if (!_hasTrackers)
            {
                return;
            }
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_trackerTransformR.position, .25f);
            Gizmos.DrawWireSphere(_trackerTransformL.position, .25f);
        }

        private void CalculateCameraSize()
        {
            if (Mathf.Approximately(_orthographicSize, _camera.orthographicSize))
            {
                return;
            }
            Debug.Log($"orthographicSize {_orthographicSize} <- {_camera.orthographicSize}");
            _orthographicSize = _camera.orthographicSize;
            _worldTopRight = _camera.ViewportToWorldPoint(ViewportTopRight);
            _worldBottomLeft = _camera.ViewportToWorldPoint(ViewportBottomLeft);
            _cameraViewHalfWidth = (_worldTopRight.x - _worldBottomLeft.x) / 2f;
            _cameraViewHalfHeight = (_worldTopRight.y - _worldBottomLeft.y) / 2f;

            // Reposition camera collider.
            if (!_hasCollider2D)
            {
                return;
            }
            var size = _cameraCollider2D.size;
            var offset = _cameraCollider2D.offset;
            offset.y = -(_cameraViewHalfHeight + size.y / 2f);
            size.x = _cameraViewHalfWidth * 2f;
            _cameraCollider2D.size = size;
            _cameraCollider2D.offset = offset;

            // Reposition trackers - they must be outside of any camera collider to work!
            if (!_hasTrackers)
            {
                return;
            }
            var positionR = _cameraCollider2D.transform.position;
            positionR.y -= _cameraViewHalfHeight;
            var positionL = positionR;
            positionR.x += _cameraViewHalfWidth + _trackerOffsetX;
            positionR.y += _trackerOffsetY;
            positionL.x -= _cameraViewHalfWidth + _trackerOffsetX;
            positionL.y += _trackerOffsetY;
            _trackerTransformR.position = positionR;
            _trackerTransformL.position = positionL;
        }

        public Vector3 ScreenToViewportPoint(Vector2 screenPosition)
        {
            _tempPosition.x = screenPosition.x;
            _tempPosition.y = screenPosition.y;
            _tempPosition.z = _cameraPosition.z;
            return _camera.ScreenToViewportPoint(_tempPosition);
        }

        public Vector3 ScreenToWorldPoint(Vector2 screenPosition)
        {
            _tempPosition.x = screenPosition.x;
            _tempPosition.y = screenPosition.y;
            _tempPosition.z = _cameraPosition.z;
            return _camera.ScreenToWorldPoint(_tempPosition);
        }

        public Ray ScreenPointToRay(Vector2 screenPosition)
        {
            _tempPosition.x = screenPosition.x;
            _tempPosition.y = screenPosition.y;
            _tempPosition.z = _cameraPosition.z;
            return _camera.ScreenPointToRay(_tempPosition);
        }

        public bool IsInsideViewport(Vector3 worldPosition)
        {
            if (!Mathf.Approximately(_orthographicSize, _camera.orthographicSize))
            {
                CalculateCameraSize();
            }
            _cameraPosition = _cameraTransform.position;
            if (worldPosition.x < _cameraPosition.x - _cameraViewHalfWidth)
            {
                return false;
            }
            if (worldPosition.x > _cameraPosition.x + _cameraViewHalfWidth)
            {
                return false;
            }
            if (worldPosition.y < _cameraPosition.y - _cameraViewHalfHeight)
            {
                return false;
            }
            if (worldPosition.y > _cameraPosition.y + _cameraViewHalfHeight)
            {
                return false;
            }
            return true;
        }

        public void ClampMovement(Vector3 worldPosition, ref Vector3 targetPosition)
        {
            // Clamp player x-axis movement so that it just slides on screen sides when it tries to move trough them.
            _cameraPosition = _cameraTransform.position;
            if (worldPosition.x < _cameraPosition.x - _cameraViewHalfWidth)
            {
                targetPosition.x = worldPosition.x;
            }
            else if (worldPosition.x > _cameraPosition.x + _cameraViewHalfWidth)
            {
                targetPosition.x = worldPosition.x;
            }
            if (worldPosition.y < _cameraPosition.y - _cameraViewHalfHeight)
            {
                targetPosition.y = worldPosition.y;
            }
            else if (worldPosition.y > _cameraPosition.y + _cameraViewHalfHeight)
            {
                targetPosition.y = worldPosition.y;
            }
        }
    }
}

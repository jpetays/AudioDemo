using System.Collections;
using Prg.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Prg.Test
{
    public class MouseTrackerTest : MonoBehaviour
    {
        private const int MaxRaycastTargets = 5;

        [SerializeField, Header("Player to test")] private Transform _player;

#if UNITY_EDITOR
        [SerializeField, Header("Pause Key")] private Key _pauseKey = Key.A;
#endif
        [SerializeField, Header("Position Data")] private bool _isIdle;
        [SerializeField] private Vector2 _screenPosition;
        [SerializeField] private Vector3 _worldPosition;
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private Vector3 _playerPosition;

        [SerializeField, Header("RayCast Data")] private bool _canMove;
        [SerializeField] private bool _isHit;
        [SerializeField] private bool _isTrigger;
        [SerializeField] private string _gameObjectName;
        [SerializeField] private string _gameObjectTag;
        [SerializeField] private string _colliderLayer;
        [SerializeField] private string _colliderType;

        [SerializeField, Header("Live Data")] private GameCamera _gameCamera;
        [SerializeField] private LineRendererUtil _lineRenderer;

        private bool _isLineRenderer;
        private Vector2 _prevScreenPosition;
        private RaycastHit2D[] _raycastResults = new RaycastHit2D[MaxRaycastTargets];

        private void Awake()
        {
            Assert.IsNotNull(_player);
            _playerPosition = _player.position;
            _isLineRenderer = true;
            _lineRenderer = gameObject.AddComponent<LineRendererUtil>();
            _lineRenderer.SetLineWidth(0.10f, 0.05f);
            DeviceUtil.DoNotRequireSimulator();
        }

        private IEnumerator Start()
        {
            Debug.Log("find camera");
            yield return new WaitUntil(() => (_gameCamera = GameCamera.Get()) != null);
            Debug.Log($"found camera {_gameCamera.name}");
#if UNITY_EDITOR
            Keyboard.current.onTextInput += _ =>
            {
                if (Keyboard.current[_pauseKey].isPressed)
                {
                    EditorApplication.isPaused = true;
                }
            };
#endif
        }

        private void Update()
        {
            _screenPosition = Mouse.current.position.ReadValue();
            _isIdle = _prevScreenPosition == _screenPosition && _playerPosition == _targetPosition;
            _prevScreenPosition = _screenPosition;
            if (_isIdle)
            {
                HandlePlayerMovement();
                if (_isLineRenderer)
                {
                    DrawLine(_playerPosition, _targetPosition);
                }
                return;
            }
            _worldPosition = _gameCamera.ScreenToWorldPoint(_screenPosition);
            if (_gameCamera.IsInsideViewport(_worldPosition))
            {
                _targetPosition = _worldPosition;
            }
            var ray = _gameCamera.ScreenPointToRay(_screenPosition);
            var hit2D = Physics2D.GetRayIntersection(ray);
            if (!hit2D)
            {
                var hitCount = Physics2D.LinecastNonAlloc(_playerPosition, _targetPosition, _raycastResults);
                while (hitCount >= _raycastResults.Length)
                {
                    _raycastResults = new RaycastHit2D[hitCount + MaxRaycastTargets];
                    hitCount = Physics2D.LinecastNonAlloc(_playerPosition, _targetPosition, _raycastResults);
                }
                for (var i = 0; i < hitCount; ++i)
                {
                    if (!_raycastResults[i].collider.isTrigger)
                    {
                        hit2D = _raycastResults[i];
                        break;
                    }
                }
            }
            HandleRaycastHit(hit2D);
            HandlePlayerMovement();
            if (_isLineRenderer)
            {
                SetLineColor(_isHit, _isTrigger);
                DrawLine(_playerPosition, _targetPosition);
            }
        }

        private void HandleRaycastHit(RaycastHit2D hit2D)
        {
            _isHit = hit2D;
            if (!_isHit)
            {
                _gameObjectName = $"nothing @ {_targetPosition.x:0.00},{_targetPosition.y:0.00}";
                _gameObjectTag = string.Empty;
                _colliderLayer = string.Empty;
                _colliderType = string.Empty;
                _isTrigger = false;
                _canMove = true;
                return;
            }
            var hitCollider = hit2D.collider;
            var hitGameObject = hitCollider.gameObject;
            _gameObjectName = $"{hitGameObject.name} @ {_targetPosition.x:0.00},{_targetPosition.y:0.00}";
            _gameObjectTag = hitGameObject.tag;
            var layer = hitGameObject.layer;
            _colliderLayer = LayerMask.LayerToName(layer);
            _colliderType = hitCollider.GetType().Name;
            _isTrigger = hitCollider.isTrigger;
            _canMove = _isTrigger;
        }

        private void HandlePlayerMovement()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }
            if (!_canMove)
            {
                Debug.Log(
                    $"refuse {_playerPosition.x:0.00},{_playerPosition.y:0.00} -> {_targetPosition.x:0.00},{_targetPosition.y:0.00} " +
                    $"{_gameObjectName} {_gameObjectTag} {_colliderLayer} {_colliderType}");
                return;
            }
            if (_isTrigger)
            {
                Debug.Log(
                    $"eat {_playerPosition.x:0.00},{_playerPosition.y:0.00} -> {_targetPosition.x:0.00},{_targetPosition.y:0.00}");
            }
            else
            {
                Debug.Log(
                    $"move {_playerPosition.x:0.00},{_playerPosition.y:0.00} -> {_targetPosition.x:0.00},{_targetPosition.y:0.00}");
            }
            _playerPosition.x = _targetPosition.x;
            _playerPosition.y = _targetPosition.y;
            _player.position = _playerPosition;
        }

        private void SetLineColor(bool isHit, bool isTrigger)
        {
            if (isHit)
            {
                if (isTrigger)
                {
                    _lineRenderer.SetLineColor(Color.green, Color.green);
                }
                else
                {
                    _lineRenderer.SetLineColor(Color.magenta, Color.magenta);
                }
            }
            else
            {
                _lineRenderer.SetLineColor(Color.yellow, Color.yellow);
            }
        }

        private void DrawLine(Vector3 start, Vector3 end)
        {
            start.z = _playerPosition.z;
            end.z = _playerPosition.z;
            _lineRenderer.ShowLine(start, end);
        }
    }
}

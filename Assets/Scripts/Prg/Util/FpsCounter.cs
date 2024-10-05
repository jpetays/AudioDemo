using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prg.Util
{
    /// <summary>
    /// FPS counter with private <c>Canvas</c> and cached counter values for zero garbage collection.
    /// </summary>
    /// <remarks>
    /// FPS counter is calculated using sampling and rounded to even number
    /// to prevent small fluctuation int FPS value.
    /// </remarks>
    public class FpsCounter : MonoBehaviour
    {
        private enum LabelPosition
        {
            TopLeft = 0,
            TopRight = 1
        }

        private const string Tp1 = "Font size relative to Screen dimensions (formula: screen / ratio)";
        private const string Tp2 = "Duration (sec) to collect FPS data for sampling";
        private const string Tp3 = "Sample length for FPS, these are averaged orver sampling duration";

        [SerializeField, Header("Settings")] private LabelPosition _labelPosition;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private int _fontSize;
        [SerializeField] private int _pixelOffsetY;
        [SerializeField] private string _labelFormat = "{0} fps";
        [SerializeField] private Color _warmUpColor = Color.gray;
        [SerializeField] private Color _labelColor = Color.yellow;
        [SerializeField, Tooltip(Tp1)] private float _fontRatio = 25f;
        [SerializeField, Tooltip(Tp2), Min(1f)] private float _sampleLength = 3.0f;
        [SerializeField, Tooltip(Tp3), Min(0.1f)] private float _samplingRate = 0.5f;

        private TextMeshProUGUI _fpsLabel;

        // Cache for FPS number string values - we allocate them once and never free when UI is visible.
        private readonly Dictionary<int, string> _labels = new();

        private int _fpsSampleCount;
        private int[] _frameCount;
        private float[] _sampleDuration;
        private int _startFrame;
        private float _startTime;
        private float _fpsSum;
        private int _fpsValue;

        private int _startFrameTotal;
        private float _startTimeTotal;
        private YieldInstruction _delay;

        private void Awake()
        {
            _fpsSampleCount = (int)Math.Round(_sampleLength / _samplingRate, MidpointRounding.AwayFromZero);
            _frameCount = new int[_fpsSampleCount];
            _sampleDuration = new float[_fpsSampleCount];
            _startFrameTotal = Time.frameCount;
            _startTimeTotal = Time.time;
            _delay = new WaitForSeconds(_samplingRate);
        }

        private void OnEnable()
        {
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                CreateCanvas();
            }
            StartCoroutine(CalculateFps());
            return;

            void CreateCanvas()
            {
                var canvasParent = new GameObject("Canvas");
                canvasParent.transform.SetParent(transform);

                canvas = canvasParent.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasParent.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                var width = Screen.width;
                var height = Screen.height;
                scaler.referenceResolution = new Vector2(width, height);
                scaler.scaleFactor = width > height ? 0 : 1f;

                var textParent = new GameObject("FPS Label");
                textParent.transform.SetParent(canvas.transform);

                var rectTransform = textParent.AddComponent<RectTransform>();
                _fpsLabel = textParent.AddComponent<TextMeshProUGUI>();
                if (_font != null)
                {
                    _fpsLabel.font = _font;
                    _fpsLabel.fontSize = _fontSize;
                }
                else
                {
                    _fpsLabel.fontSize = (int)(Mathf.Min(Screen.height, Screen.width) / _fontRatio);
                }
                switch (_labelPosition)
                {
                    case LabelPosition.TopLeft:
                        rectTransform.anchorMin = new Vector2(0f, 1f);
                        rectTransform.anchorMax = new Vector2(0f, 1f);
                        rectTransform.pivot = new Vector2(0f, 1f);
                        rectTransform.anchoredPosition = new Vector2(20f, -10f + _pixelOffsetY);
                        _fpsLabel.horizontalAlignment = HorizontalAlignmentOptions.Left;
                        break;
                    case LabelPosition.TopRight:
                        rectTransform.anchorMin = new Vector2(1f, 1f);
                        rectTransform.anchorMax = new Vector2(1f, 1f);
                        rectTransform.pivot = new Vector2(1f, 1f);
                        rectTransform.anchoredPosition = new Vector2(-20f, -10f + _pixelOffsetY);
                        _fpsLabel.horizontalAlignment = HorizontalAlignmentOptions.Right;
                        break;
                }
                rectTransform.sizeDelta = new Vector2(200f, 50f);
                _fpsLabel.enableWordWrapping = false;
                _fpsLabel.color = _warmUpColor;
                _fpsLabel.text = string.Format(_labelFormat, 123);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator CalculateFps()
        {
            // Show data collection warm up phase on UI.
            for (var i = 0; i < _fpsSampleCount; ++i)
            {
                _startFrame = Time.frameCount;
                _startTime = Time.time;
                yield return _delay;
                _sampleDuration[i] = Time.time - _startTime;
                _frameCount[i] = Time.frameCount - _startFrame;
                _fpsLabel.text = string.Format(_labelFormat,
                    Math.Round((Time.frameCount - _startFrameTotal) / (Time.time - _startTimeTotal)));
            }
            _fpsLabel.color = _labelColor;
            // Start updating sampling UI.
            for (var i = 0; i < _fpsSampleCount; ++i)
            {
                _startFrame = Time.frameCount;
                _startTime = Time.time;
                yield return _delay;
                _sampleDuration[i] = Time.time - _startTime;
                _frameCount[i] = Time.frameCount - _startFrame;
                UpdateFpsCounter();
            }
            yield break;

            void UpdateFpsCounter()
            {
                _fpsSum = 0;
                for (var i = 0; i < _fpsSampleCount; ++i)
                {
                    _fpsSum += _frameCount[i] / _sampleDuration[i];
                }
                _fpsValue = (int)Math.Round(_fpsSum / _fpsSampleCount, MidpointRounding.AwayFromZero);
                _fpsValue -= _fpsValue % 2;
                if (!_labels.TryGetValue(_fpsValue, out var label))
                {
                    label = string.Format(_labelFormat, _fpsValue);
                    _labels.Add(_fpsValue, label);
                }
                _fpsLabel.text = label;
            }
        }
    }
}

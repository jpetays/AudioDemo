using UnityEngine;

namespace Prg.Test
{
    /// <summary>
    /// Simple <c>LineRenderer</c> helper to draw line between two points in 3D/2D space.
    /// </summary>
    public class LineRendererUtil : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startWidth = 0.5f;
            _lineRenderer.endWidth = 0.2f;
            _lineRenderer.positionCount = 2;
            // Default color is white!
            _lineRenderer.SetPosition(0, Vector3.zero);
            _lineRenderer.SetPosition(1, Vector3.zero);
        }

        private void OnEnable()
        {
            _lineRenderer.enabled = true;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _lineRenderer.enabled = false;
        }

        public void SetLineWidth(float start, float end)
        {
            _lineRenderer.startWidth = start;
            _lineRenderer.endWidth = end;
        }

        public void SetLineColor(Color start, Color end)
        {
            _lineRenderer.startColor = start;
            _lineRenderer.endColor = end;
        }

        public void ShowLine(Vector3 from, Vector3 to)
        {
            // Set Z position to same on both ends before calling this for 2D game.
            _lineRenderer.SetPosition(0, from);
            _lineRenderer.SetPosition(1, to);
        }
    }
}

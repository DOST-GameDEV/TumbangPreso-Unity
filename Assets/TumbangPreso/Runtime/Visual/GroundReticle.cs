using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Smart cartoon ground targeting decal / reticle that projects onto the pavement
    /// to clearly indicate where AOE abilities, zones, and ground slams will land.
    /// </summary>
    public sealed class GroundReticle : MonoBehaviour
    {
        private GameObject _ringGo;
        private GameObject _fillGo;
        private GameObject _centerDotGo;
        private Renderer _ringRenderer;
        private Renderer _fillRenderer;
        private Renderer _centerDotRenderer;

        private float _targetRadius = 3.0f;
        private Color _color = UiTheme.HeroEarth;
        private bool _active;

        public static GroundReticle Create(Transform parent)
        {
            var go = new GameObject("~GroundReticle");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<GroundReticle>();
        }

        private void Awake()
        {
            // Outer Ring Disc
            _ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _ringGo.name = "ReticleOuterRing";
            _ringGo.transform.SetParent(transform, false);
            _ringGo.transform.localScale = new Vector3(_targetRadius * 2.0f, 0.02f, _targetRadius * 2.0f);
            _ringGo.transform.localPosition = new Vector3(0, 0.02f, 0);

            _ringRenderer = _ringGo.GetComponent<Renderer>();
            if (_ringRenderer != null) _ringRenderer.material.color = new Color(_color.r, _color.g, _color.b, 0.65f);
            Destroy(_ringGo.GetComponent<Collider>());

            // Inner Translucent Fill
            _fillGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _fillGo.name = "ReticleInnerFill";
            _fillGo.transform.SetParent(transform, false);
            _fillGo.transform.localScale = new Vector3(_targetRadius * 1.8f, 0.015f, _targetRadius * 1.8f);
            _fillGo.transform.localPosition = new Vector3(0, 0.015f, 0);

            _fillRenderer = _fillGo.GetComponent<Renderer>();
            if (_fillRenderer != null) _fillRenderer.material.color = new Color(_color.r, _color.g, _color.b, 0.25f);
            Destroy(_fillGo.GetComponent<Collider>());

            // Center Bullseye Dot
            _centerDotGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _centerDotGo.name = "ReticleCenterDot";
            _centerDotGo.transform.SetParent(transform, false);
            _centerDotGo.transform.localScale = new Vector3(0.4f, 0.03f, 0.4f);
            _centerDotGo.transform.localPosition = new Vector3(0, 0.03f, 0);

            _centerDotRenderer = _centerDotGo.GetComponent<Renderer>();
            if (_centerDotRenderer != null) _centerDotRenderer.material.color = Color.white;
            Destroy(_centerDotGo.GetComponent<Collider>());

            gameObject.SetActive(false);
        }

        public void Show(Vector3 worldPos, float radius, Color color)
        {
            _targetRadius = radius;
            _color = color;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            // Project down to ground
            Vector3 groundPos = worldPos;
            if (Physics.Raycast(worldPos + Vector3.up * 2.0f, Vector3.down, out var hit, 10.0f))
            {
                groundPos = hit.point + Vector3.up * 0.02f;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                groundPos.y = 0.05f;
                transform.rotation = Quaternion.identity;
            }

            transform.position = groundPos;

            // Scale geometry
            if (_ringGo != null)
                _ringGo.transform.localScale = new Vector3(_targetRadius * 2.0f, 0.02f, _targetRadius * 2.0f);
            if (_fillGo != null)
                _fillGo.transform.localScale = new Vector3(_targetRadius * 1.8f, 0.015f, _targetRadius * 1.8f);

            // Set colors with gentle pulse
            float pulse = 0.8f + Mathf.Sin(Time.time * 8.0f) * 0.2f;
            if (_ringRenderer != null)
                _ringRenderer.material.color = new Color(_color.r, _color.g, _color.b, 0.75f * pulse);
            if (_fillRenderer != null)
                _fillRenderer.material.color = new Color(_color.r, _color.g, _color.b, 0.30f * pulse);
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}

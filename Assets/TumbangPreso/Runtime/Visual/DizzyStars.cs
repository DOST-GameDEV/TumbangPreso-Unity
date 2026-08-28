using System.Collections.Generic;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Spawns funny cartoon dizzy stars orbiting over a character's head when stunned, frozen, or heavily staggered.
    /// </summary>
    public sealed class DizzyStars : MonoBehaviour
    {
        private const float OrbitRadius = 0.42f;
        private const float OrbitSpeed = 320.0f; // degrees/sec
        private const float HeadHeight = 1.75f;

        private readonly List<Transform> _starTransforms = new List<Transform>();
        private float _duration;
        private float _elapsed;
        private Transform _target;

        public static DizzyStars Attach(Transform target, float duration, Color? starColor = null)
        {
            if (target == null) return null;

            var existing = target.GetComponentInChildren<DizzyStars>();
            if (existing != null)
            {
                existing.Extend(duration);
                return existing;
            }

            var go = new GameObject("~DizzyStars");
            go.transform.SetParent(target, false);
            go.transform.localPosition = new Vector3(0, HeadHeight, 0);

            var dizzy = go.AddComponent<DizzyStars>();
            dizzy._target = target;
            dizzy.Init(duration, starColor ?? UiTheme.Highlight);
            return dizzy;
        }

        public void Extend(float duration)
        {
            _duration = Mathf.Max(_duration, duration);
            _elapsed = 0.0f;
        }

        private void Init(float duration, Color starColor)
        {
            _duration = duration;
            _elapsed = 0.0f;

            // Spawn 3 orbiting cartoon star points
            int starCount = 3;
            for (int i = 0; i < starCount; i++)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Cube);
                star.name = $"Star_{i}";
                star.transform.SetParent(transform, false);
                star.transform.localScale = Vector3.one * 0.12f;

                // Flatten/diamond rotate to read as a cute cartoon star
                star.transform.localRotation = Quaternion.Euler(45, 45, 45);

                var r = star.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = starColor;
                }

                var col = star.GetComponent<Collider>();
                if (col != null) Destroy(col);

                _starTransforms.Add(star.transform);
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            float angleBase = _elapsed * OrbitSpeed;
            float bob = Mathf.Sin(_elapsed * 8.0f) * 0.06f;

            for (int i = 0; i < _starTransforms.Count; i++)
            {
                var st = _starTransforms[i];
                if (st == null) continue;

                float angle = (angleBase + (i * 360.0f / _starTransforms.Count)) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * OrbitRadius;
                float z = Mathf.Sin(angle) * OrbitRadius;
                float y = bob * ((i % 2 == 0) ? 1.0f : -1.0f);

                st.localPosition = new Vector3(x, y, z);
                st.localRotation = Quaternion.Euler(45 + _elapsed * 180f, 45 + i * 60f, 45 + _elapsed * 90f);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Instances the picked model on a unit, tints it, and flashes it when it is hit.
    ///
    /// ⚠️⚠️ IT MEASURES THE INSTANCED MODEL RATHER THAN ASSUMING ITS HEIGHT, and that is the
    /// property that must survive the art replacement. Because a roster character is a rig plus
    /// a palette, every character has to sit on the capsule floor without per-character setup;
    /// the moment this file assumes a height, a new model floats or sinks and it reads as an
    /// art bug rather than a code one. Measure, always.
    ///
    /// ⚠️⚠️ THE FLASH AND THE TINT ARE SEPARATE CHANNELS, DELIBERATELY. The tint writes base
    /// colour, because that is what "what colour am I" means. The hit flash drives its own
    /// property. If the flash were implemented by writing base colour, then tinting a prop
    /// would break the flash, and worse, a flash mid-tint would leave the prop the wrong colour
    /// permanently. Two concerns, two channels, and they must not be merged for tidiness.
    ///
    /// ⚠️ THE ART HERE IS ALL PLACEHOLDER (docs/Port_Plan.md section 8). Invest in the
    /// MECHANISM, which survives the swap, not in matching the current look.
    /// </summary>
    public sealed class CharacterVisual : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Transform _modelRoot;
        [SerializeField] private float _flashTime = 0.12f;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _block;

        private Color _tint = Color.white;
        private float _flashLeft;
        private GameObject _instance;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (_modelRoot == null) _modelRoot = transform;
        }

        /// <summary>Swap in the model for a roster pick.</summary>
        public void ApplyModel(GameObject prefab, Color tint)
        {
            if (_instance != null) Destroy(_instance);

            _tint = tint;

            if (prefab != null)
            {
                _instance = Instantiate(prefab, _modelRoot);
                _instance.transform.localPosition = Vector3.zero;
                _instance.transform.localRotation = Quaternion.identity;
            }

            CacheRenderers();
            AlignToCapsuleFloor();
            PushColour();

            // ⚠️ BOUND HERE, NOT IN Awake. The model is instanced at this moment, and an
            // Animator bound before its rig exists silently drives nothing at all: the
            // character simply stands still and no error is ever logged.
            var anim = GetComponent<CharacterAnimator>();
            if (anim == null) anim = gameObject.AddComponent<CharacterAnimator>();
            if (_instance != null) anim.Bind(_instance);
        }

        private void CacheRenderers()
        {
            _renderers.Clear();
            _renderers.AddRange(_modelRoot.GetComponentsInChildren<Renderer>(includeInactive: true));
        }

        /// <summary>
        /// ⚠️ MEASURED, NOT ASSUMED. Combine the instanced renderers' bounds and drop the model
        /// so its lowest point sits on the capsule's base. A model authored with its origin at
        /// the hips, or at the head, or scaled differently, all land correctly.
        ///
        /// ⚠️ IT USES bounds, WHICH ARE WORLD SPACE, so this must run AFTER the instance is
        /// parented and positioned. Running it during instantiation reads bounds from the
        /// prefab's own transform and silently offsets every character by the same wrong
        /// amount, which looks like a deliberate art choice rather than a bug.
        /// </summary>
        public void AlignToCapsuleFloor()
        {
            if (_renderers.Count == 0) return;

            bool any = false;
            Bounds combined = default;

            foreach (var r in _renderers)
            {
                if (r == null) continue;

                if (!any) { combined = r.bounds; any = true; }
                else combined.Encapsulate(r.bounds);
            }

            if (!any) return;

            float capsuleBase = transform.position.y;
            float drop = combined.min.y - capsuleBase;

            if (Mathf.Abs(drop) > 0.0005f)
                _modelRoot.position -= new Vector3(0.0f, drop, 0.0f);
        }

        /// <summary>
        /// ⚠️ THE HIT FLASH IS A SEPARATE CHANNEL FROM THE TINT. See the class note: merging
        /// them means a tint breaks the flash, and a flash interrupted mid-tint leaves the
        /// wrong colour permanently.
        /// </summary>
        public void FlashHit()
        {
            _flashLeft = _flashTime;
            PushColour();
        }

        public void SetTint(Color tint)
        {
            _tint = tint;
            PushColour();
        }

        private void Update()
        {
            if (_flashLeft <= 0.0f) return;

            _flashLeft = Mathf.Max(0.0f, _flashLeft - Time.deltaTime);
            PushColour();
        }

        /// <summary>
        /// ⚠️ A MaterialPropertyBlock, NOT `renderer.material`. Touching `.material` on a shared
        /// material CLONES it per renderer, which quietly multiplies draw calls and leaks a
        /// material instance for every character in every round. A property block writes
        /// per-renderer overrides without instancing anything.
        /// </summary>
        private void PushColour()
        {
            float flash = _flashTime <= 0.0f ? 0.0f : Mathf.Clamp01(_flashLeft / _flashTime);
            Color shown = Color.Lerp(_tint, Color.white, flash);

            foreach (var r in _renderers)
            {
                if (r == null) continue;

                r.GetPropertyBlock(_block);

                // URP uses _BaseColor; the built-in pipeline and some placeholder shaders use
                // _Color. Writing both is harmless and means the flash works before the real
                // toon shader lands.
                _block.SetColor(BaseColorId, shown);
                _block.SetColor(ColorId, shown);

                r.SetPropertyBlock(_block);
            }
        }
    }
}

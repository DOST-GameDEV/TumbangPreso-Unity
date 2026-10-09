using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// ⚠️⚠️ THE FIRST-PERSON HANDS ARE SHADED BY THE WORLD, NOT BY THEIR OWN BODY.
    ///
    /// Owner, 2026-10-06: *"can you fix the hand rendering? they only react to the players shadows and not world
    /// lighting"*. The hands sit a hand's breadth in front of the eye, which is INSIDE the player's own body, and that
    /// body is still there casting shadows while it is hidden from its own camera (`ShadowCastingMode.ShadowsOnly`, so
    /// the player keeps a shadow on the ground). So the shadow the hands caught was mostly their owner's: they went dark
    /// and light as the player turned against the sun, in a way that had nothing to do with where the player stood, and
    /// that flicker drowned what the place was doing to them.
    ///
    /// So the hands no longer take cast shadows at all, and are shaded as ONE THING by where the player stands: a line
    /// is tested from the eye toward the sun, past every character, and when the world is in the way the hands (and
    /// whatever rides on them) go to the tone the cast's shader gives a shadowed surface, over a quarter of a second.
    /// Walking under a bridge or into a building's shade now darkens the hands; turning round in the open does not.
    /// The sun's own colour and the map's ambient reach them as they always did.
    ///
    /// ⚠️ IT IS A TEST OF COLLIDERS, NOT OF THE SHADOW MAP: a thing that casts a shadow and cannot be touched (a cloth
    /// awning with no collider) does not shade the hands. It runs ten times a second, not every frame.
    /// </summary>
    public sealed partial class ViewmodelArms
    {
        /// <summary>How bright a shaded hand is against a lit one (the cast's smooth shading puts a shadowed, sun-facing surface about here).</summary>
        public const float ShadedTone = .66f;
        private const float ShadeEvery = .1f, ShadeBlend = .25f, ShadeReach = 90f;

        private static readonly RaycastHit[] ShadeHits = new RaycastHit[12];
        private readonly List<Renderer> _shadeRenderers = new List<Renderer>();
        private readonly List<Color> _shadeBase = new List<Color>();
        private MaterialPropertyBlock _shadeBlock;
        private Light _shadeSun;
        private float _shade, _shadeTarget, _shadeTested, _shadeGathered = -9f, _shadeSunSought = -9f, _shadeApplied = -1f;
        private int _shadeCount = -1;

        /// <summary>How far into the world's shade the hands are, 0 in the sun to 1 (diagnostics and probes).</summary>
        public float WorldShade => _shade;

        private void StepWorldShade(float dt)
        {
            float now = Time.unscaledTime;
            // WHO IS SHADED: every renderer under the arms (the arms, the held slipper, a hand companion), found again
            // twice a second because pieces come and go.
            if (now - _shadeGathered > .5f || now < _shadeGathered)
            {
                _shadeGathered = now;
                GetComponentsInChildren(true, _shadeRenderers);
                // Put on again each time: a change of hero clears the arms' property blocks.
                _shadeCount = _shadeRenderers.Count; _shadeApplied = -1f;
                _shadeBase.Clear();
                foreach (var r in _shadeRenderers)
                {
                    if (r != null) r.receiveShadows = false;
                    var m = r != null ? r.sharedMaterial : null;
                    _shadeBase.Add(m != null && m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
                }
            }

            // WHETHER THE WORLD IS BETWEEN THE EYE AND THE SUN.
            if (_characterMotor != null && now - _shadeTested >= ShadeEvery)
            {
                _shadeTested = now;
                if (_shadeSun == null && now - _shadeSunSought > 2f)
                {
                    _shadeSunSought = now;
                    _shadeSun = RenderSettings.sun;
                    if (_shadeSun == null)
                        foreach (var light in FindObjectsByType<Light>())
                            if (light.type == LightType.Directional && light.isActiveAndEnabled && (_shadeSun == null || light.intensity > _shadeSun.intensity)) _shadeSun = light;
                }
                bool shaded = false;
                if (_shadeSun != null && _shadeSun.isActiveAndEnabled && _shadeSun.shadows != LightShadows.None)
                {
                    Vector3 eye = transform.position, toSun = -_shadeSun.transform.forward;
                    int hits = Physics.RaycastNonAlloc(eye + toSun * .35f, toSun, ShadeHits, ShadeReach, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < hits && !shaded; i++)
                    {
                        var collider = ShadeHits[i].collider;
                        // Not a body (hers or anyone's), and not something being carried or thrown.
                        if (collider == null || collider.attachedRigidbody != null || collider.GetComponentInParent<CharacterMotor>() != null) continue;
                        shaded = true;
                    }
                }
                _shadeTarget = shaded ? 1f : 0f;
            }
            _shade = Mathf.MoveTowards(_shade, _shadeTarget, Mathf.Max(0f, dt) / ShadeBlend);

            if (Mathf.Abs(_shade - _shadeApplied) < .004f) return;
            _shadeApplied = _shade;
            _shadeBlock ??= new MaterialPropertyBlock();
            float tone = Mathf.Lerp(1f, ShadedTone, _shade);
            for (int i = 0; i < _shadeRenderers.Count && i < _shadeBase.Count; i++)
            {
                var r = _shadeRenderers[i];
                if (r == null || r is LineRenderer) continue;
                r.GetPropertyBlock(_shadeBlock);
                Color c = _shadeBase[i];
                _shadeBlock.SetColor("_Color", new Color(c.r * tone, c.g * tone, c.b * tone, c.a));
                r.SetPropertyBlock(_shadeBlock);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE CIRCLE (HERO-10 v3, plan 9.7 and 9.9 row 17). The owner, 2026-09-29: *"to make it cooler cast like a big magic circle in
    /// the sky or smth when she ults"*. When her VOODOO DOLL wakes, a huge STITCHED magic circle opens in the sky over it, and the doll's
    /// string hangs from its centre:
    ///
    /// | Time | What | Direction |
    /// |---|---|---|
    /// | 0.00 to 0.50 | the outer rim (her crimson) sews itself round, X stitches following it | clockwise |
    /// | 0.10 to 0.60 | the inner rim (her violet) sews round the other way | anticlockwise |
    /// | 0.50 to 0.98 | eight pins stab in across both rims at the compass points, one after another | in, from above |
    /// | 0.90 to 1.20 | her sigil blooms in the middle: the X she stitches, and a button | out from the centre |
    /// | then | it turns slowly; a thin glowing string runs from the doll's crown up to its centre | 8 degrees a second |
    /// | 3.0 to 3.8 | it draws in to a small ring high over the doll that follows it, the top of its string, for the round | in, and down |
    ///
    /// Owned by the doll's body (`Abilities.VoodooDollBody` adds it on every peer, the replica included), so everyone sees it and it
    /// goes when the doll goes. ⚠️ ONE GRAPHIC, THE STITCH (plan 9.3): every piece is a `LineRenderer` in `Shaders/VoodooThread` (a
    /// dark smoke cord with a hot core and stitches), which reads against the sky where a soft glow would not. A body that joins late
    /// (a rejoiner) skips straight to the small ring.
    /// </summary>
    public sealed class VoodooSkyCircle : MonoBehaviour
    {
        public const float Height = 7.0f, Radius = 5.6f, InnerRadius = 4.3f;
        public const float SmallRadius = 0.8f, SmallHeight = 4.2f;
        public const float OpenSeconds = 3.0f, DrawInSeconds = 0.8f;
        private const int RimPoints = 96, Stitches = 24, Pins = 8;
        private const float TurnDegreesPerSecond = 8.0f;

        private static readonly Color Crimson = new Color(1.00f, 0.22f, 0.30f, 1f);
        private static readonly Color Violet = new Color(0.74f, 0.40f, 1.00f, 1f);

        private static Material _material;
        private MaterialPropertyBlock _block;
        private LineRenderer _outer, _inner, _string, _sigilA, _sigilB, _button;
        private readonly List<LineRenderer> _stitchA = new List<LineRenderer>(), _stitchB = new List<LineRenderer>(), _pins = new List<LineRenderer>();
        private readonly List<GameObject> _owned = new List<GameObject>();
        private float _age;
        private Vector3 _centre;
        private float _turn;

        /// <summary>Opens the circle over <paramref name="doll"/>; <paramref name="late"/> skips to the small ring (a rejoiner's view).</summary>
        public static VoodooSkyCircle Open(GameObject doll, bool late)
        {
            if (doll == null) return null;
            var circle = doll.AddComponent<VoodooSkyCircle>();
            circle._age = late ? OpenSeconds + DrawInSeconds : 0f;
            circle._centre = doll.transform.position + Vector3.up * Height;
            return circle;
        }

        private static Material ThreadMaterial
        {
            get
            {
                if (_material != null) return _material;
                var shader = Resources.Load<Shader>("Shaders/VoodooThread");
                if (shader == null) return null;
                _material = new Material(shader) { name = "VoodooSkyCircle" };
                return _material;
            }
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _outer = Line("SkyCircleOuter", RimPoints + 1);
            _inner = Line("SkyCircleInner", RimPoints + 1);
            _string = Line("DollString", 12);
            _sigilA = Line("SkyCircleSigilA", 2);
            _sigilB = Line("SkyCircleSigilB", 2);
            _button = Line("SkyCircleButton", 33);
            for (int i = 0; i < Stitches; i++) { _stitchA.Add(Line("SkyStitchA", 2)); _stitchB.Add(Line("SkyStitchB", 2)); }
            for (int i = 0; i < Pins; i++) _pins.Add(Line("SkyPin", 2));
        }

        private LineRenderer Line(string name, int points)
        {
            var go = new GameObject(name);
            _owned.Add(go);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = points;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = ThreadMaterial;
            line.enabled = false;
            return line;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _age += dt;
            // Open: full size over where the doll woke. Drawn in: small, high over the doll, following it.
            float drawIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_age - OpenSeconds) / DrawInSeconds));
            Vector3 over = transform.position + Vector3.up * SmallHeight;
            Vector3 centre = Vector3.Lerp(_centre, over, drawIn);
            float scale = Mathf.Lerp(1f, SmallRadius / Radius, drawIn);
            if (_age > 1.2f) _turn += TurnDegreesPerSecond * dt * Mathf.Lerp(1f, 3f, drawIn);
            float spin = _turn * Mathf.Deg2Rad;

            // The rims, sewn round.
            float outerSewn = Mathf.Clamp01(_age / 0.5f);
            float innerSewn = Mathf.Clamp01((_age - 0.1f) / 0.5f);
            Rim(_outer, centre, Radius * scale, spin, outerSewn, clockwise: true, Crimson, 0.30f * Mathf.Lerp(1f, 0.45f, drawIn));
            Rim(_inner, centre, InnerRadius * scale, -spin, innerSewn, clockwise: false, Violet, 0.20f * Mathf.Lerp(1f, 0.45f, drawIn));

            // The X stitches between the rims, following the outer sewing; hidden once it is small (too fine to read).
            float mid = (Radius + InnerRadius) * 0.5f * scale, half = (Radius - InnerRadius) * 0.42f * scale;
            for (int i = 0; i < Stitches; i++)
            {
                float u = i / (float)Stitches;
                bool on = u < outerSewn && drawIn < 0.6f;
                SetEnabled(_stitchA[i], on); SetEnabled(_stitchB[i], on);
                if (!on) continue;
                float a = -u * Mathf.PI * 2f + spin;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), along = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                Vector3 c = centre + radial * mid;
                Segment(_stitchA[i], c - radial * half - along * half, c + radial * half + along * half, Crimson, 0.12f, 1f - drawIn);
                Segment(_stitchB[i], c - radial * half + along * half, c + radial * half - along * half, Crimson, 0.12f, 1f - drawIn);
            }

            // Eight pins stab in across both rims, one after another, from above.
            for (int i = 0; i < Pins; i++)
            {
                float stab = Mathf.Clamp01((_age - 0.5f - i * 0.06f) / 0.12f);
                bool on = stab > 0f;
                SetEnabled(_pins[i], on);
                if (!on) continue;
                float a = i / (float)Pins * Mathf.PI * 2f + spin;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 inner = centre + radial * (InnerRadius - 0.5f) * scale, outer = centre + radial * (Radius + 0.9f) * scale;
                Vector3 drop = Vector3.up * (1f - stab) * 3f * scale;
                Segment(_pins[i], inner + drop, outer + drop, i % 2 == 0 ? Violet : Crimson, 0.16f * Mathf.Lerp(1f, 0.5f, drawIn), 1f);
            }

            // Her sigil blooms: the X she stitches, and a button.
            float bloom = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_age - 0.9f) / 0.3f));
            bool sigil = bloom > 0f;
            SetEnabled(_sigilA, sigil); SetEnabled(_sigilB, sigil); SetEnabled(_button, sigil);
            if (sigil)
            {
                float r = InnerRadius * 0.62f * scale * bloom;
                Vector3 d1 = new Vector3(Mathf.Cos(spin + 0.785f), 0f, Mathf.Sin(spin + 0.785f)) * r;
                Vector3 d2 = new Vector3(Mathf.Cos(spin - 0.785f), 0f, Mathf.Sin(spin - 0.785f)) * r;
                Segment(_sigilA, centre - d1, centre + d1, Violet, 0.22f * Mathf.Lerp(1f, 0.5f, drawIn), 1f);
                Segment(_sigilB, centre - d2, centre + d2, Violet, 0.22f * Mathf.Lerp(1f, 0.5f, drawIn), 1f);
                Rim(_button, centre, 1.1f * scale * bloom, spin, 1f, clockwise: true, Crimson, 0.16f * Mathf.Lerp(1f, 0.5f, drawIn), points: 33);
            }

            // The doll's string: from its tied crown up to the circle's centre, swaying a little.
            bool strung = _age > 1.0f;
            SetEnabled(_string, strung);
            if (strung)
            {
                var capsule = GetComponent<CharacterController>();
                float tall = capsule != null ? capsule.height * transform.lossyScale.y : 1.6f;
                Vector3 crown = transform.position + Vector3.up * (tall + 0.25f);
                float t = Time.time;
                for (int i = 0; i < _string.positionCount; i++)
                {
                    float u = i / (float)(_string.positionCount - 1);
                    Vector3 p = Vector3.Lerp(crown, centre, u);
                    float envelope = Mathf.Sin(Mathf.PI * u);
                    p += new Vector3(Mathf.Sin(t * 1.3f + u * 5f), 0f, Mathf.Cos(t * 1.1f + u * 4f)) * 0.18f * envelope;
                    _string.SetPosition(i, p);
                }
                _string.widthMultiplier = 0.06f;
                Paint(_string, Violet, Mathf.Clamp01((_age - 1.0f) / 0.2f), 1f, 1f);
            }
        }

        private void Rim(LineRenderer line, Vector3 centre, float radius, float spin, float sewn, bool clockwise, Color hue, float width, int points = RimPoints + 1)
        {
            bool on = sewn > 0f;
            SetEnabled(line, on);
            if (!on) return;
            int count = Mathf.Max(2, Mathf.CeilToInt((points - 1) * sewn) + 1);
            line.positionCount = count;
            float sign = clockwise ? -1f : 1f;
            for (int i = 0; i < count; i++)
            {
                float a = sign * (i / (float)(points - 1)) * Mathf.PI * 2f + spin;
                line.SetPosition(i, centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
            }
            line.widthMultiplier = width;
            Paint(line, hue, 1f, 1f, 1f);
        }

        private void Segment(LineRenderer line, Vector3 from, Vector3 to, Color hue, float width, float alpha)
        {
            line.SetPosition(0, from); line.SetPosition(1, to);
            line.widthMultiplier = width;
            Paint(line, hue, alpha, 1f, 0f);
        }

        private void Paint(LineRenderer line, Color hue, float alpha, float tight, float stitches)
        {
            var c = new Color(hue.r, hue.g, hue.b, Mathf.Clamp01(alpha));
            line.startColor = c; line.endColor = c;
            line.GetPropertyBlock(_block);
            _block.SetFloat("_Tight", tight);
            _block.SetFloat("_StitchOn", stitches);
            line.SetPropertyBlock(_block);
        }

        private static void SetEnabled(LineRenderer line, bool on)
        {
            if (line != null && line.enabled != on) line.enabled = on;
        }

        private void OnDestroy()
        {
            foreach (var go in _owned) if (go != null) Destroy(go);
            _owned.Clear();
        }
    }
}

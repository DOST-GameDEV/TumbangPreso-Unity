using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE CIRCLE (HERO-10 v3, plan 9.7, 9.8b and 9.9). The owner, 2026-09-29: *"a big magic circle in the sky or smth when she
    /// ults"*, *"i want her to cast like a really scary magic circle in teh sky for her cutscene and then this monster comes out of it and
    /// looks like its controlled by strings and scary"*; then on the first one, stitched lines against the sky: *"improve magic circle it
    /// looks underwhelming af it doesnt feel like an ult"*, *"pls use genshin reference and other ult cutscenes"*.
    ///
    /// What the references do (research.md section 5: Castorice's summoning, Nahida's shrine, Raiden's emblem, Neuvillette's pillars):
    /// the circle is a SOURCE OF LIGHT with many layers turning against each other, it OPENS with a flash, the sky TEARS as it does, and
    /// the thing it summons comes out of its middle. So it is:
    ///
    /// | Age | What | Direction |
    /// |---|---|---|
    /// | 0.00 to 0.55 | the rim is SEWN round, hot crimson with a bloom halo and a ring of fangs (`Shaders/VoodooCircle`) | clockwise |
    /// | 0.15 to 0.90 | violet LIGHTNING tears down out of the rim to the ground, flickering | down |
    /// | 0.50 to 0.98 | eight long PINS stab in through the rim from above, one after another (3D, so they have depth) | in, from above |
    /// | 0.55 | the FLASH: the whole circle blazes as it closes | out |
    /// | 0.45 to 0.95 | the rune band, the star {8/3} and the stitches light up inside, crawling against the rim | anticlockwise |
    /// | 0.50 to 0.95 | the EYE in the dark void at its centre opens, nearly the void's width, an ember slit twitching | opening |
    /// | then | the rim, the runes and the star turn against each other; the void swirls | slow |
    ///
    /// One object, posed from an age, used by the cutscene (`HeroIntroductionScene.Phaister.cs`, from the scene clock) and by play
    /// (`VoodooSkyCircle` below, from its own clock), so the circle she tears open is the one hanging over the court after.
    /// </summary>
    public sealed class SkyCircle
    {
        public const float Radius = 5.6f, EyeHalfWidth = 0.46f * Radius, EyeHalfHeight = 0.27f * Radius;
        public const float FullyOpenAge = 1.4f;
        private const int Pins = 8, Bolts = 6, BoltPoints = 9;

        public static readonly Color Crimson = new Color(1.00f, 0.22f, 0.30f, 1f);
        public static readonly Color Violet = new Color(0.74f, 0.40f, 1.00f, 1f);

        /// <summary>
        /// The cutscene's hand on it (v7, plan 9.8c): the eye's opening (0 shut to 1 open; negative leaves it to the age), where the
        /// pupil looks (x along the eye, y across; NaN leaves it to its own darting), the stitched SEAM that shows before it opens and
        /// how many of its stitches have snapped, and the age of THE BURST as the eye opens (negative for none). Play leaves them.
        /// </summary>
        public float EyeOverride = -1f, LookX = float.NaN, LookY = 0f, Seam = 0f, Tear = 0f, BurstAge = -1f;

        private static Material _thread;
        private static Mesh _disc;
        private readonly bool _world;
        private readonly Material _circle;
        private readonly Transform _discT;
        private readonly Renderer _discR;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly List<GameObject> _owned = new List<GameObject>();
        private readonly List<LineRenderer> _pins = new List<LineRenderer>(Pins), _bolts = new List<LineRenderer>(Bolts);

        public static Material ThreadMaterial
        {
            get
            {
                if (_thread != null) return _thread;
                var shader = Resources.Load<Shader>("Shaders/VoodooThread");
                if (shader == null) return null;
                _thread = new Material(shader) { name = "VoodooSkyThread" };
                return _thread;
            }
        }

        private static Mesh Disc
        {
            get
            {
                if (_disc != null) return _disc;
                _disc = new Mesh { name = "VoodooCircleDisc" };
                const float e = 1.12f;
                _disc.vertices = new[] { new Vector3(-e, 0f, -e), new Vector3(e, 0f, -e), new Vector3(e, 0f, e), new Vector3(-e, 0f, e) };
                float lo = 0.5f - e * 0.5f, hi = 0.5f + e * 0.5f;
                _disc.uv = new[] { new Vector2(lo, lo), new Vector2(hi, lo), new Vector2(hi, hi), new Vector2(lo, hi) };
                _disc.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                _disc.RecalculateNormals();
                _disc.bounds = new Bounds(Vector3.zero, new Vector3(2.4f, 0.2f, 2.4f));
                return _disc;
            }
        }

        /// <param name="parent">Null for scene objects (play); the cutscene's stage root in the cutscene.</param>
        /// <param name="world">Positions are world space (play) or the parent's local space (the cutscene).</param>
        public SkyCircle(Transform parent, bool world, int layer)
        {
            _world = world;
            var go = new GameObject("VoodooCircle");
            if (parent != null) go.transform.SetParent(parent, false);
            go.layer = layer;
            _owned.Add(go);
            go.AddComponent<MeshFilter>().sharedMesh = Disc;
            var r = go.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var shader = Resources.Load<Shader>("Shaders/VoodooCircle");
            if (shader != null) { _circle = new Material(shader) { name = "VoodooCircle" }; r.sharedMaterial = _circle; }
            r.enabled = false;
            _discT = go.transform; _discR = r;
            for (int i = 0; i < Pins; i++) _pins.Add(Line("SkyPin", 2, parent, layer));
            for (int i = 0; i < Bolts; i++) _bolts.Add(Line("SkyBolt", BoltPoints, parent, layer));
        }

        private LineRenderer Line(string name, int points, Transform parent, int layer)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.layer = layer;
            _owned.Add(go);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = _world;
            line.positionCount = points;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = ThreadMaterial;
            line.enabled = false;
            return line;
        }

        /// <summary>Where a string hangs from: round the eye in the void, spread a little per string.</summary>
        public static Vector3 StringAnchor(Vector3 centre, float scale, int index, int count)
        {
            if (count <= 1) return centre;
            float a = index / (float)count * Mathf.PI * 2f + 0.4f;
            return centre + new Vector3(Mathf.Cos(a) * EyeHalfWidth * 0.7f, 0f, Mathf.Sin(a) * EyeHalfHeight * 0.9f) * scale;
        }

        /// <summary>
        /// Poses it at <paramref name="age"/> seconds since it began to open, at <paramref name="centre"/> (world or parent space as
        /// built), scaled (1 full size), turned by <paramref name="spin"/> radians, faded by <paramref name="alpha"/>. The lightning and
        /// the pins show at full size only (<paramref name="detail"/>).
        /// </summary>
        public void Pose(float age, Vector3 centre, float scale, float spin, float alpha, float detail = 1f)
        {
            bool on = age > 0f && alpha > 0.01f;
            _discR.enabled = on;
            if (on)
            {
                if (_world) { _discT.position = centre; _discT.localScale = Vector3.one * Radius * scale; }
                else { _discT.localPosition = centre; _discT.localScale = Vector3.one * Radius * scale; }
                float reveal = Mathf.Clamp01(age / 0.55f);
                float flash = Mathf.Clamp01(1f - Mathf.Abs(age - 0.58f) / 0.18f);
                if (_circle != null)
                {
                    _circle.SetFloat("_Reveal", reveal * reveal * (3f - 2f * reveal));
                    _circle.SetFloat("_Inner", Mathf.Clamp01((age - 0.45f) / 0.5f));
                    _circle.SetFloat("_Eye", EyeOverride >= 0f ? EyeOverride : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - 0.5f) / 0.45f)));
                    _circle.SetFloat("_Look", float.IsNaN(LookX) ? Mathf.Sin(age * 3.1f) * 0.7f + Mathf.Sin(age * 7.7f) * 0.2f : LookX);
                    _circle.SetFloat("_LookY", float.IsNaN(LookX) ? 0f : LookY);
                    _circle.SetFloat("_Seam", Seam);
                    _circle.SetFloat("_Tear", Tear);
                    _circle.SetFloat("_Spin", spin);
                    _circle.SetFloat("_Phase", age);
                    float burst = BurstAge >= 0f ? Mathf.Exp(-BurstAge * 5f) : 0f;
                    _circle.SetFloat("_Glow", 2.6f + 4.5f * flash + 7f * burst + 0.25f * Mathf.Sin(age * 6f));
                    _circle.SetFloat("_Alpha", alpha);
                }
            }

            // The pins stab in through the rim from above, one after another.
            for (int i = 0; i < Pins; i++)
            {
                float stab = Mathf.Clamp01((age - 0.5f - i * 0.06f) / 0.12f);
                bool pin = on && stab > 0f && detail > 0.05f;
                SetEnabled(_pins[i], pin);
                if (!pin) continue;
                float a = i / (float)Pins * Mathf.PI * 2f + spin;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 tip = centre + radial * Radius * 0.86f * scale + Vector3.down * 0.6f * scale;
                Vector3 head = centre + radial * Radius * 1.18f * scale + Vector3.up * 2.4f * scale;
                Vector3 drop = Vector3.up * (1f - stab) * 4f * scale;
                _pins[i].SetPosition(0, tip + drop); _pins[i].SetPosition(1, head + drop);
                _pins[i].widthMultiplier = 0.22f * Mathf.Lerp(0.5f, 1f, scale);
                Paint(_pins[i], i % 2 == 0 ? Violet : Crimson, alpha * detail, 1f, 0f);
            }

            // The sky tears: jagged violet lightning out of the rim, down toward the ground, flickering while it opens.
            for (int b = 0; b < Bolts; b++)
            {
                float life = Mathf.Clamp01((age - 0.15f - b * 0.09f) / 0.35f);
                // THE BURST fires them all again, down round the court (v7).
                if (BurstAge >= 0f && BurstAge < 0.8f) life = Mathf.Clamp01((BurstAge - b * 0.05f) / 0.45f);
                float flicker = Mathf.Repeat(age * 17f + b * 0.37f, 1f) < 0.55f ? 1f : 0.25f;
                bool bolt = on && life > 0f && life < 1f && detail > 0.05f;
                SetEnabled(_bolts[b], bolt);
                if (!bolt) continue;
                float a = b / (float)Bolts * Mathf.PI * 2f + 0.3f + spin * 0.5f;
                Vector3 from = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Radius * 0.95f * scale;
                Vector3 to = from + Vector3.down * (centre.y - 0.2f) * Mathf.Min(1f, life * 3f) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.5f;
                for (int k = 0; k < BoltPoints; k++)
                {
                    float u = k / (float)(BoltPoints - 1);
                    float jag = (Mathf.Repeat(Mathf.Sin((k + 1) * 12.9898f + b * 78.233f + Mathf.Floor(age * 17f)) * 43758.5453f, 1f) - 0.5f) * 1.1f;
                    Vector3 side = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                    _bolts[b].SetPosition(k, Vector3.Lerp(from, to, u) + side * jag * Mathf.Sin(Mathf.PI * u));
                }
                _bolts[b].widthMultiplier = 0.16f;
                Paint(_bolts[b], Violet, alpha * flicker * (1f - life * life), 1f, 0f);
            }
        }

        public void Paint(LineRenderer line, Color hue, float alpha, float tight, float stitches)
        {
            var c = new Color(hue.r, hue.g, hue.b, Mathf.Clamp01(alpha));
            line.startColor = c; line.endColor = c;
            line.GetPropertyBlock(_block);
            _block.SetFloat("_Tight", tight);
            _block.SetFloat("_StitchOn", stitches);
            line.SetPropertyBlock(_block);
        }

        private static void SetEnabled(Renderer line, bool on)
        {
            if (line != null && line.enabled != on) line.enabled = on;
        }

        public void Hide()
        {
            foreach (var go in _owned) if (go != null) { var r = go.GetComponent<Renderer>(); if (r != null) r.enabled = false; }
        }

        public void Destroy()
        {
            foreach (var go in _owned) if (go != null) Object.Destroy(go);
            _owned.Clear();
            if (_circle != null) Object.Destroy(_circle);
        }
    }

    /// <summary>
    /// ⚠️⚠️ THE PORTAL AND THE CONTROL OVER THE DOLL IN PLAY (HERO-10 v3, plan 9.7, 9.8c and 9.9 in-play rows 1 and 12). Owned by the
    /// doll's body on every peer (`Abilities.VoodooDollBody` adds it, the replica included), so everyone sees it and it goes with the doll.
    /// The owner, 2026-09-29: *"I want the portal (eye) to be diff from the thing that controls it too"*, *"i want the wires on its head
    /// to look like this"* (a photo of a marionette control).
    ///
    /// | Time | What |
    /// |---|---|
    /// | the hand-back | the circle hangs open 7 m over where the doll landed, its eye staring, exactly as the cutscene left it (never sewn twice) |
    /// | 3.0 to 3.8 s | the PORTAL SHUTS: the eye closes and the circle shrinks away into the dark and is gone |
    /// | always | the MARIONETTE CONTROL (`MarionetteControl`) hangs over its head, swaying behind its moves and leaning into them, WIRES from it to its crown and both mitten hands; slack while it is tagged |
    /// </summary>
    public sealed class VoodooSkyCircle : MonoBehaviour
    {
        public const float Height = 7.0f;
        public const float OpenSeconds = 3.0f, DrawInSeconds = 0.8f;
        private const float TurnDegreesPerSecond = 8.0f;

        private SkyCircle _circle;
        private Transform _control;
        private readonly List<LineRenderer> _strings = new List<LineRenderer>(3);
        private readonly List<GameObject> _owned = new List<GameObject>();
        private Transform _crown, _leftArm, _rightArm;
        private Vector3 _leftPalm, _rightPalm;
        private float _age, _turn, _slack;
        private Vector3 _centre, _controlAt, _controlVelocity;
        private bool _placed;
        private CharacterMotor _motor;

        /// <summary>Opens the portal over <paramref name="doll"/>, already fully open (the cutscene tore it open); <paramref name="late"/>
        /// (a rejoiner's view) starts with it already shut.</summary>
        public static VoodooSkyCircle Open(GameObject doll, bool late)
        {
            if (doll == null) return null;
            var circle = doll.AddComponent<VoodooSkyCircle>();
            circle._age = late ? OpenSeconds + DrawInSeconds : SkyCircle.FullyOpenAge;
            circle._centre = doll.transform.position + Vector3.up * Height;
            return circle;
        }

        private void Awake()
        {
            _circle = new SkyCircle(null, world: true, layer: 0);
            _motor = GetComponent<CharacterMotor>();
            _control = MarionetteControl.BuildControl(null, 0);
            _control.localScale = Vector3.one * MarionetteControl.DollScale;
            _owned.Add(_control.gameObject);
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("DollWire" + i);
                _owned.Add(go);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 10;
                line.alignment = LineAlignment.View;
                line.textureMode = LineTextureMode.Tile;
                line.numCapVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sharedMaterial = SkyCircle.ThreadMaterial;
                line.enabled = false;
                _strings.Add(line);
            }
        }

        private void FindJoints()
        {
            var visual = GetComponent<CharacterVisual>();
            var skinned = visual != null && visual.Model != null ? visual.Model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (skinned == null) return;
            for (int i = 0; i < skinned.bones.Length; i++)
            {
                var b = skinned.bones[i];
                if (b == null) continue;
                if (b.name == "head") _crown = b;
                else if (b.name == "arm-left" && CharacterVisual.PalmCentre(skinned, i, out var lp)) { _leftArm = b; _leftPalm = lp; }
                else if (b.name == "arm-right" && CharacterVisual.PalmCentre(skinned, i, out var rp)) { _rightArm = b; _rightPalm = rp; }
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_crown == null) FindJoints();

            // THE PORTAL: open and staring, then the eye shuts and it shrinks away into the dark.
            float shut = Mathf.Clamp01((_age - OpenSeconds) / DrawInSeconds);
            _turn += TurnDegreesPerSecond * dt;
            if (shut < 1f)
            {
                _circle.EyeOverride = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shut / 0.35f));
                float scale = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((shut - 0.25f) / 0.75f));
                _circle.Pose(_age, _centre, Mathf.Max(0.01f, scale), _turn * Mathf.Deg2Rad, Mathf.Clamp01(scale * 1.5f), detail: 1f - shut);
            }
            else _circle.Hide();

            // THE CONTROL over its head: it follows on a spring (so it swings behind the doll's moves), leans into them, and turns with
            // the doll so its bar's ends stay over the matching hands.
            var capsule = GetComponent<CharacterController>();
            float tall = capsule != null ? capsule.height * transform.lossyScale.y : 1.6f;
            Vector3 crown = _crown != null ? _crown.position + Vector3.up * 0.55f : transform.position + Vector3.up * (tall + 0.25f);
            Vector3 target = crown + Vector3.up * MarionetteControl.AboveCrown;
            if (!_placed) { _controlAt = target; _placed = true; }
            float step = Mathf.Min(dt, 0.05f);
            Vector3 pull = (target - _controlAt) * 60f - _controlVelocity * 11f;
            _controlVelocity += pull * step;
            _controlAt += _controlVelocity * step;
            Vector3 lean = transform.InverseTransformDirection(_controlVelocity);
            float yaw = transform.eulerAngles.y;
            _control.SetPositionAndRotation(_controlAt,
                Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(Mathf.Clamp(lean.z * 9f, -25f, 25f), 0f, Mathf.Clamp(-lean.x * 9f, -25f, 25f)));

            // THE WIRES: slack while it is tagged (plan 9.9 row 8: the string goes slack), taut again after.
            bool tagged = _motor != null && _motor.IsTagged;
            _slack = Mathf.MoveTowards(_slack, tagged ? 1f : 0f, dt * (tagged ? 3f : 8f));
            Vector3 left = _leftArm != null ? _leftArm.TransformPoint(_leftPalm) : transform.position + transform.right * -0.6f + Vector3.up * 0.9f;
            Vector3 right = _rightArm != null ? _rightArm.TransformPoint(_rightPalm) : transform.position + transform.right * 0.6f + Vector3.up * 0.9f;
            var ends = new[] { crown, left, right };
            float t = Time.time;
            for (int s = 0; s < _strings.Count; s++)
            {
                var line = _strings[s];
                Vector3 top = _control.TransformPoint(MarionetteControl.Anchor(s));
                for (int i = 0; i < line.positionCount; i++)
                {
                    float u = i / (float)(line.positionCount - 1);
                    Vector3 p = Vector3.Lerp(ends[s], top, u);
                    float envelope = Mathf.Sin(Mathf.PI * u);
                    p += Vector3.down * _slack * 0.45f * envelope;
                    p += new Vector3(Mathf.Sin(t * 1.3f + u * 5f + s), 0f, Mathf.Cos(t * 1.1f + u * 4f + s * 2f)) * 0.03f * envelope;
                    line.SetPosition(i, p);
                }
                line.widthMultiplier = s == 0 ? 0.05f : 0.04f;
                _circle.Paint(line, SkyCircle.Violet, 1f, 1f, 1f);
                line.enabled = true;
            }
        }

        private void OnDestroy()
        {
            _circle?.Destroy();
            foreach (var go in _owned) if (go != null) Destroy(go);
            _owned.Clear();
        }
    }
}

using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ PHAISTER'S CURSES, SEEN ON EVERY SCREEN (HERO-10 v3, plan 9.3 and 9.5). Owner: *"whiels he's holding towards them it
    /// shows like an eerie vfx connecitng the two"*. Body-owned like `PhaisterStatusPresenter`: it reads the body's REPLICATED
    /// reach and mark (`CharacterMotor.Voodoo.cs`), so the owner, the host, every observer and a rejoiner draw the same thing and
    /// no cast is replayed to draw it.
    ///
    /// | Moment | What | Direction |
    /// |---|---|---|
    /// | the lock | the thread whips from her palm to them in 0.12 s and pierces with a small X (DRAIN at the chest, HEX at the eyes) | out from her hand |
    /// | the hold (2 s) | a wavering cord of dark smoke with a bright core and dashed stitches crawling from them TO her; it tightens as it fills: less sway, a thicker brighter core | stitches toward her |
    /// | the mark | the thread snaps taut and zips back into her hand; a flash where it held; the MARK appears over them | back to her |
    /// | broken | the thread frays, its sway growing, and snaps back to her, fading | back to her |
    /// | the mark's life | DRAIN: a twisted crimson knot turning over them (1.5 s). HEX: a violet button, a stitch through it, that fills with light over the 10 s fuse and throbs once armed | turning, filling |
    ///
    /// ⚠️ ONE GRAPHIC, THE STITCH (plan 9.3): every piece is a `LineRenderer` in `Shaders/VoodooThread`, a dark smoke cord with a
    /// hot core and dashed stitches, in the curse's colour (DRAIN crimson, HEX violet); shape carries it too for colour-blind
    /// players (DRAIN is a twist, HEX an eye with a button). Nothing appears from empty air: the thread starts at her palm, and
    /// the mark is stitched on where the thread held.
    /// </summary>
    public sealed class VoodooCursePresenter : MonoBehaviour
    {
        /// <summary>The curses' colours (her crimson and her lilac, lifted so they read as light).</summary>
        public static readonly Color DrainHue = new Color(1.00f, 0.22f, 0.30f, 1f);
        public static readonly Color HexHue = new Color(0.74f, 0.40f, 1.00f, 1f);

        /// <summary>Where the thread pierces: DRAIN at the chest, HEX at the eyes (metres above the feet, the cast's big heads), on
        /// the side facing her: the chest's front is about 0.4 m out from the body's axis, the face's about 0.75 m.</summary>
        private const float ChestHeight = 0.62f, EyeHeight = 1.38f, ChestFront = 0.40f, FaceFront = 0.75f;

        private const int Points = 26;
        private const float WhipSeconds = 0.12f, ZipSeconds = 0.16f, FraySeconds = 0.32f, PierceSeconds = 0.5f;

        private CharacterMotor _body;
        private CharacterController _capsule;
        private MaterialPropertyBlock _block;
        private static Material _material;

        // The thread, drawn on the CASTER's body.
        private LineRenderer _thread;
        private LineRenderer _pierceA, _pierceB;
        private int _target = -1;
        private VoodooMarkKind _kind;
        private float _age;
        private float _pierceAge = -1.0f;
        private Vector3 _pierceAt;
        private enum Ending { None, Zip, Fray }
        private Ending _ending;
        private float _endAge;
        private Vector3 _endFar;
        private readonly Vector3[] _points = new Vector3[Points];

        // The mark, drawn over the MARKED body.
        private LineRenderer _mark, _stitchA, _stitchB;
        private VoodooMarkKind _markShown;
        private float _markBorn;

        private static Material ThreadMaterial
        {
            get
            {
                if (_material != null) return _material;
                var shader = Resources.Load<Shader>("Shaders/VoodooThread");
                if (shader == null) return null;
                _material = new Material(shader) { name = "VoodooThread" };
                return _material;
            }
        }

        private void Awake()
        {
            _body = GetComponent<CharacterMotor>();
            _capsule = GetComponent<CharacterController>();
            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_body == null) return;
            float dt = Time.deltaTime;
            StepThread(dt);
            StepMark(dt);
        }

        // ------------------------------------------------------------------ the thread

        private void StepThread(float dt)
        {
            bool reaching = _body.IsVoodooReaching && _body.isActiveAndEnabled;
            var round = GameServices.Round;

            if (reaching)
            {
                if (_target != _body.VoodooReachTarget || _kind != _body.VoodooReachKind || _ending != Ending.None)
                {
                    // A fresh reach (or a new target): the whip starts again from her palm.
                    _target = _body.VoodooReachTarget; _kind = _body.VoodooReachKind;
                    _age = Mathf.Clamp(_body.VoodooReachElapsed, 0.0f, WhipSeconds * 0.5f);
                    _ending = Ending.None; _pierceAge = -1.0f;
                }
                _age += dt;
            }
            else if (_target >= 0 && _ending == Ending.None)
            {
                // The reach just ended: the body's own result says how (`VoodooReachSucceeded`, set with the end on every peer).
                var victim = round != null ? round.PlayerAt(_target) : null;
                _endFar = victim != null ? Pierce(victim, _kind, Hand()) : _endFar;
                _ending = _body.VoodooReachSucceeded ? Ending.Zip : Ending.Fray;
                _endAge = 0.0f;
                if (_ending == Ending.Zip)
                {
                    Flash(_endFar);
                    GameServices.Audio?.PlayAt("sfx_phaister_mark", _endFar);
                }
                else GameServices.Audio?.PlayAt("sfx_phaister_reach_snap", Hand());
            }

            if (_target < 0) { Show(false); return; }

            Vector3 from = Hand();
            Color hue = _kind == VoodooMarkKind.Drain ? DrainHue : HexHue;
            float tight = reaching ? _body.VoodooReachProgress : 1.0f;

            Vector3 far;
            float reachFraction = 1.0f, alpha = 1.0f, sway = Mathf.Lerp(0.16f, 0.02f, tight);
            if (_ending == Ending.None)
            {
                var victim = round != null ? round.PlayerAt(_target) : null;
                if (victim == null) { Clear(); return; }
                far = Pierce(victim, _kind, from);
                reachFraction = Mathf.Clamp01(_age / WhipSeconds);
                if (reachFraction >= 1.0f && _pierceAge < 0.0f) { _pierceAge = 0.0f; _pierceAt = far; }
                _endFar = far;
            }
            else
            {
                _endAge += dt;
                far = _endFar;
                if (_ending == Ending.Zip)
                {
                    // Snaps taut and zips back into her hand, the far end racing home.
                    float u = Mathf.Clamp01(_endAge / ZipSeconds);
                    reachFraction = 1.0f - u * u;
                    sway = 0.0f;
                    tight = 1.0f;
                    if (u >= 1.0f) { Clear(); return; }
                }
                else
                {
                    // Frays: the sway grows as it comes apart, and it falls back toward her, fading.
                    float u = Mathf.Clamp01(_endAge / FraySeconds);
                    reachFraction = 1.0f - 0.7f * u;
                    sway = Mathf.Lerp(sway, 0.34f, u);
                    alpha = 1.0f - u;
                    tight = 0.0f;
                    if (u >= 1.0f) { Clear(); return; }
                }
            }

            EnsureThread();
            Lay(from, far, reachFraction, sway);
            _thread.positionCount = Points;
            _thread.SetPositions(_points);
            float width = Mathf.Lerp(0.07f, 0.11f, tight);
            _thread.widthMultiplier = width;
            Paint(_thread, hue, alpha, tight, stitches: 1.0f);
            _thread.enabled = true;

            StepPierce(dt, hue, alpha);
        }

        /// <summary>The cord from her palm toward the far point, `fraction` of the way, waving across its length (still at both ends).</summary>
        private void Lay(Vector3 from, Vector3 to, float fraction, float sway)
        {
            Vector3 span = to - from;
            Vector3 dir = span.sqrMagnitude > 1e-4f ? span.normalized : transform.forward;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = transform.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(side, dir).normalized;
            float t = Time.time;
            for (int i = 0; i < Points; i++)
            {
                float u = i / (float)(Points - 1) * fraction;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / Mathf.Max(0.05f, fraction)));
                // Two travelling waves, from her toward them, a little out of step on the two axes: a cord, not a spring.
                float a = Mathf.Sin(u * 9.0f - t * 7.0f) * 0.7f + Mathf.Sin(u * 17.0f - t * 11.0f + 1.3f) * 0.3f;
                float b = Mathf.Sin(u * 7.0f - t * 5.3f + 2.1f) * 0.6f;
                _points[i] = from + span * u + (side * a + up * b) * sway * envelope;
            }
        }

        /// <summary>The small X the thread pierces them with, stitched where it landed; it fades in half a second.</summary>
        private void StepPierce(float dt, Color hue, float alpha)
        {
            if (_pierceAge < 0.0f) { SetEnabled(_pierceA, false); SetEnabled(_pierceB, false); return; }
            _pierceAge += dt;
            float u = Mathf.Clamp01(_pierceAge / PierceSeconds);
            if (u >= 1.0f) { SetEnabled(_pierceA, false); SetEnabled(_pierceB, false); return; }
            if (_pierceA == null) _pierceA = MakeLine("VoodooPierceA", 2);
            if (_pierceB == null) _pierceB = MakeLine("VoodooPierceB", 2);
            var view = Camera.main;
            Vector3 right = view != null ? view.transform.right : transform.right;
            Vector3 up = view != null ? view.transform.up : Vector3.up;
            float size = Mathf.Lerp(0.26f, 0.16f, u);
            _pierceA.SetPosition(0, _pierceAt + (-right + up) * size * 0.5f); _pierceA.SetPosition(1, _pierceAt + (right - up) * size * 0.5f);
            _pierceB.SetPosition(0, _pierceAt + (right + up) * size * 0.5f); _pierceB.SetPosition(1, _pierceAt + (-right - up) * size * 0.5f);
            _pierceA.widthMultiplier = _pierceB.widthMultiplier = 0.06f;
            Paint(_pierceA, hue, alpha * (1.0f - u), 1.0f, stitches: 0.0f);
            Paint(_pierceB, hue, alpha * (1.0f - u), 1.0f, stitches: 0.0f);
            _pierceA.enabled = _pierceB.enabled = true;
        }

        /// <summary>The flash where the thread held when it snaps taut into a mark: the X stitched once more, bright.</summary>
        private void Flash(Vector3 at)
        {
            _pierceAt = at;
            _pierceAge = PierceSeconds * 0.1f;
        }

        /// <summary>
        /// Her palm. ⚠️ On HER OWN screen it is the first-person hand she sees (her body is hidden from her lens and its hand is
        /// below it), so the thread leaves the hand she is holding out; everyone else sees it leave her body's hand.
        /// </summary>
        private Vector3 Hand()
        {
            var arms = FirstPersonArms(_body);
            if (arms != null && arms.TryRightPalmWorld(out var palm)) return palm;
            var visual = GetComponent<CharacterVisual>();
            var hand = visual != null ? visual.HandAnchor : null;
            return hand != null ? hand.position : transform.position + Vector3.up * 1.2f + transform.forward * 0.45f;
        }

        /// <summary>
        /// Where the thread goes into them: the chest (DRAIN) or the eyes (HEX), on their side facing her. ⚠️ On the VICTIM'S OWN
        /// screen their eyes are the lens, so a thread to them would stand as a column through the frame (film v12); there it
        /// arrives just below the middle of their view, low for DRAIN and higher for HEX, so they watch it come at them.
        /// </summary>
        private static Vector3 Pierce(CharacterMotor victim, VoodooMarkKind kind, Vector3 from)
        {
            var view = Camera.main;
            if (view != null && FirstPersonArms(victim) != null)
                return view.transform.position + view.transform.forward * 0.8f
                       - view.transform.up * (kind == VoodooMarkKind.Drain ? 0.42f : 0.2f);
            Vector3 toward = from - victim.transform.position; toward.y = 0.0f;
            toward = toward.sqrMagnitude > 0.01f ? toward.normalized : victim.transform.forward;
            return victim.transform.position + Vector3.up * (kind == VoodooMarkKind.Drain ? ChestHeight : EyeHeight)
                   + toward * (kind == VoodooMarkKind.Drain ? ChestFront : FaceFront);
        }

        /// <summary>The first-person arms drawing this body's own view, or null when nobody is looking through its eyes.</summary>
        private static CameraSystem.ViewmodelArms FirstPersonArms(CharacterMotor who)
        {
            if (who == null) return null;
            foreach (var arms in Object.FindObjectsByType<CameraSystem.ViewmodelArms>(FindObjectsSortMode.None))
                if (arms != null && arms.isActiveAndEnabled && arms.BoundCharacter == who) return arms;
            return null;
        }

        private void EnsureThread()
        {
            if (_thread == null) _thread = MakeLine("VoodooThread", Points);
        }

        private void Show(bool on)
        {
            SetEnabled(_thread, on);
            if (!on) { SetEnabled(_pierceA, false); SetEnabled(_pierceB, false); }
        }

        private void Clear()
        {
            _target = -1;
            _ending = Ending.None;
            _pierceAge = -1.0f;
            Show(false);
        }

        // ------------------------------------------------------------------ the mark

        /// <summary>
        /// HEXED, SEEN BY EVERYONE ELSE (plan 9.5: *"a stitched band snaps across their eyes (seen by all for the 7.5 s)"*, fraying
        /// at the end): a violet stitched band across the face, on the side facing the viewer. Not drawn on the victim's own
        /// screen, whose lens it would cover; their screen has the phantoms (`HexedPhantomSlippers`).
        /// </summary>
        private LineRenderer _band;
        private void StepBand()
        {
            bool show = _body.IsHexed && _body.isActiveAndEnabled && FirstPersonArms(_body) == null;
            if (!show) { SetEnabled(_band, false); return; }
            if (_band == null) _band = MakeLine("VoodooHexBand", 2);
            var view = Camera.main;
            Vector3 toward = view != null ? view.transform.position - transform.position : transform.forward;
            toward.y = 0.0f;
            toward = toward.sqrMagnitude > 0.01f ? toward.normalized : transform.forward;
            Vector3 across = Vector3.Cross(Vector3.up, toward).normalized;
            Vector3 eyes = transform.position + Vector3.up * EyeHeight + toward * (FaceFront + 0.04f);
            float fray = Mathf.Clamp01(_body.HexedLeft / 0.6f);
            _band.SetPosition(0, eyes - across * 0.62f * fray);
            _band.SetPosition(1, eyes + across * 0.62f * fray);
            _band.widthMultiplier = 0.16f;
            Paint(_band, HexHue, fray, 0.8f, stitches: 1.0f);
            _band.enabled = true;
        }

        private void StepMark(float dt)
        {
            StepBand();
            var kind = _body.VoodooMark;
            if (kind == VoodooMarkKind.None || !_body.isActiveAndEnabled)
            {
                if (_markShown != VoodooMarkKind.None) { SetEnabled(_mark, false); SetEnabled(_stitchA, false); SetEnabled(_stitchB, false); }
                _markShown = VoodooMarkKind.None;
                return;
            }
            if (_markShown != kind) { _markShown = kind; _markBorn = Time.time; }
            float grow = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01((Time.time - _markBorn) / 0.18f));

            if (_mark == null) _mark = MakeLine("VoodooMark", 48);
            float top = (_capsule != null ? _capsule.height * transform.lossyScale.y : 1.8f) + 0.95f;
            Vector3 centre = transform.position + Vector3.up * top;
            var view = Camera.main;
            Vector3 right = view != null ? view.transform.right : transform.right;
            Vector3 up = view != null ? view.transform.up : Vector3.up;
            Vector3 toward = view != null ? -view.transform.forward : transform.forward;

            if (kind == VoodooMarkKind.Drain)
            {
                // A twisted knot: a (2,3) torus knot, turning, tightening as the 1.5 s run out.
                float left = Mathf.Clamp01(1.0f - _body.VoodooMarkAge / VoodooRules.DrainDelaySeconds);
                float r = 0.46f * grow * Mathf.Lerp(0.72f, 1.0f, left);
                float spin = Time.time * 4.0f;
                _mark.positionCount = 48;
                for (int i = 0; i < 48; i++)
                {
                    float a = i / 47.0f * Mathf.PI * 2.0f;
                    float ring = 1.0f + 0.42f * Mathf.Cos(3.0f * a);
                    float x = ring * Mathf.Cos(2.0f * a + spin), y = ring * Mathf.Sin(2.0f * a + spin), z = 0.42f * Mathf.Sin(3.0f * a);
                    _mark.SetPosition(i, centre + (right * x + up * y + toward * z) * r * 0.62f);
                }
                _mark.loop = false;
                _mark.widthMultiplier = 0.08f;
                Paint(_mark, DrainHue, 1.0f, 1.0f - left * 0.6f, stitches: 1.0f);
                _mark.enabled = true;
                SetEnabled(_stitchA, false); SetEnabled(_stitchB, false);
            }
            else
            {
                // A button with a stitch through it, filling with light as the fuse burns and throbbing once armed.
                float fuse = Mathf.Clamp01(_body.VoodooMarkAge / VoodooRules.HexArmSeconds);
                bool armed = _body.VoodooHexArmed;
                float throb = armed ? 0.5f + 0.5f * Mathf.Sin(Time.time * 7.0f) : 0.0f;
                float r = 0.24f * grow * (1.0f + 0.08f * throb);
                _mark.positionCount = 48;
                for (int i = 0; i < 48; i++)
                {
                    float a = i / 47.0f * Mathf.PI * 2.0f;
                    _mark.SetPosition(i, centre + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * r);
                }
                _mark.widthMultiplier = 0.09f;
                float light = armed ? 0.85f + 0.15f * throb : Mathf.Lerp(0.15f, 0.8f, fuse);
                Paint(_mark, HexHue, 1.0f, light, stitches: 1.0f);
                _mark.enabled = true;

                if (_stitchA == null) _stitchA = MakeLine("VoodooMarkStitchA", 2);
                if (_stitchB == null) _stitchB = MakeLine("VoodooMarkStitchB", 2);
                float s = r * 0.62f;
                _stitchA.SetPosition(0, centre + (-right + up) * s); _stitchA.SetPosition(1, centre + (right - up) * s);
                _stitchB.SetPosition(0, centre + (right + up) * s); _stitchB.SetPosition(1, centre + (-right - up) * s);
                _stitchA.widthMultiplier = _stitchB.widthMultiplier = 0.07f;
                Paint(_stitchA, HexHue, 1.0f, light, stitches: 0.0f);
                Paint(_stitchB, HexHue, 1.0f, light, stitches: 0.0f);
                _stitchA.enabled = _stitchB.enabled = true;
            }
        }

        // ------------------------------------------------------------------ plumbing

        private LineRenderer MakeLine(string name, int points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
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

        private void Paint(LineRenderer line, Color hue, float alpha, float tight, float stitches)
        {
            var c = new Color(hue.r, hue.g, hue.b, Mathf.Clamp01(alpha));
            line.startColor = c; line.endColor = c;
            line.GetPropertyBlock(_block);
            _block.SetFloat("_Tight", Mathf.Clamp01(tight));
            _block.SetFloat("_StitchOn", stitches);
            line.SetPropertyBlock(_block);
        }

        private static void SetEnabled(LineRenderer line, bool on)
        {
            if (line != null && line.enabled != on) line.enabled = on;
        }

        private void OnDisable()
        {
            _target = -1; _ending = Ending.None; _pierceAge = -1.0f; _markShown = VoodooMarkKind.None;
            SetEnabled(_thread, false); SetEnabled(_pierceA, false); SetEnabled(_pierceB, false);
            SetEnabled(_mark, false); SetEnabled(_stitchA, false); SetEnabled(_stitchB, false); SetEnabled(_band, false);
        }
    }
}

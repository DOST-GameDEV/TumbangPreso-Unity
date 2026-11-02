using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ PHAISTER'S CURSES, SEEN ON EVERY SCREEN (HERO-10 v3, plan 9.3 and 9.5). Body-owned like `PhaisterStatusPresenter`: it
    /// reads the body's REPLICATED reach and mark (`CharacterMotor.Voodoo.cs`), so the owner, the host, every observer and a
    /// rejoiner draw the same thing and no cast is replayed to draw it.
    ///
    /// ⚠️⚠️ THE REACH IS `VoodooSoulDraw`, WHICH THIS ADDS. Films v12 to v16 drew it as a thread from her palm to them, and the owner
    /// turned it down: *"dont make the pulling thing look like a physical line i want it to look like sucking aura or smth"*. It is
    /// now their aura sucked out of them into the doll she holds up at them. This class keeps what stays ON the victim after it:
    ///
    /// | Moment | What | Direction |
    /// |---|---|---|
    /// | the mark's life | DRAIN: a twisted crimson knot turning over them (1.5 s). HEX: a violet button, a stitch through it, that fills with light over the 10 s fuse and throbs once armed | turning, filling |
    /// | HEXED | a violet stitched band across their eyes, seen by everyone else for the 7.5 s | frays away |
    ///
    /// Colour and shape both carry the curse for colour-blind players: DRAIN crimson and a twist, HEX violet and an eye.
    /// </summary>
    public sealed class VoodooCursePresenter : MonoBehaviour
    {
        /// <summary>The curses' colours (her crimson and her lilac, lifted so they read as light).</summary>
        public static readonly Color DrainHue = new Color(1.00f, 0.22f, 0.30f, 1f);
        public static readonly Color HexHue = new Color(0.74f, 0.40f, 1.00f, 1f);

        /// <summary>The eyes, metres above the feet (the cast's big heads), and the face's front, about 0.75 m out from the body's axis.</summary>
        private const float EyeHeight = 1.38f, FaceFront = 0.75f;

        private CharacterMotor _body;
        private CharacterController _capsule;
        private MaterialPropertyBlock _block;
        private static Material _material;

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
            if (GetComponent<VoodooSoulDraw>() == null) gameObject.AddComponent<VoodooSoulDraw>();
            if (GetComponent<PhaisterDrainPresenter>() == null) gameObject.AddComponent<PhaisterDrainPresenter>();
        }

        private void LateUpdate()
        {
            if (_body == null) return;
            StepMark(Time.deltaTime);
        }

        /// <summary>The first-person arms drawing this body's own view, or null when nobody is looking through its eyes.</summary>
        private static CameraSystem.ViewmodelArms FirstPersonArms(CharacterMotor who)
        {
            if (who == null) return null;
            foreach (var arms in Object.FindObjectsByType<CameraSystem.ViewmodelArms>(FindObjectsSortMode.None))
                if (arms != null && arms.isActiveAndEnabled && arms.BoundCharacter == who) return arms;
            return null;
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
            bool show = _body.IsHexed && _body.isActiveAndEnabled && FirstPersonArms(_body) == null && PhaisterLook.Direction == PhaisterLook.Style.Current;
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

        /// <summary>
        /// ⚠️ THE REBUILT HEX (`PhaisterHexSigil`) READS ONLY WHAT IS REPLICATED, so every peer and a rejoiner draws it: who is
        /// reaching at this body, this body's mark and its age, and whether it is HEXED. The stab and the fray are nothing on the
        /// wire: they are how the mark ended, read on the frame it ends (HEXED came on, or it ran out its full life).
        /// </summary>
        private void StepSigil()
        {
            if (PhaisterLook.Direction == PhaisterLook.Style.Current || !_body.isActiveAndEnabled)
            {
                _hexSigil?.Hide(); _sigilWasMarked = false; _sigilEndedAt = -1f; return;
            }
            float reach = -1f, mark = _body.VoodooMark == VoodooMarkKind.Hex ? _body.VoodooMarkAge : -1f, stab = -1f, hexed = -1f, fray = -1f;
            Vector3 hand = transform.position;
            var round = GameServices.Round;
            if (mark < 0f && round != null)
                foreach (var p in round.Players)
                    if (p != null && p != _body && p.IsVoodooReaching && p.VoodooReachKind == VoodooMarkKind.Hex && p.VoodooReachTarget == _body.PlayerSlot)
                    {
                        reach = p.VoodooReachElapsed;
                        Vector3 to = transform.position - p.transform.position; to.y = 0f;
                        hand = p.transform.position + Vector3.up * 1.0f + (to.sqrMagnitude > 0.01f ? to.normalized : p.transform.forward) * 0.45f;
                        break;
                    }
            if (mark >= 0f) { _sigilWasMarked = true; _sigilLastAge = mark; _sigilEndedAt = -1f; _sigilStabbed = false; }
            else if (_sigilWasMarked)
            {
                _sigilWasMarked = false;
                _sigilStabbed = _body.IsHexed;
                // A mark that ended early any other way (the round, a cleanse) simply goes.
                _sigilEndedAt = _sigilStabbed || _sigilLastAge >= VoodooRules.HexMarkLifeSeconds - 0.25f ? Time.time : -1f;
            }
            if (_sigilEndedAt >= 0f)
            {
                float since = Time.time - _sigilEndedAt;
                if (_sigilStabbed) stab = since; else fray = since;
            }
            if (_body.IsHexed) hexed = StatusRules.HexedSeconds - _body.HexedLeft;
            bool live = reach >= 0f || mark >= 0f || hexed >= 0f || (stab >= 0f && stab < PhaisterHexSigil.StabSeconds) || (fray >= 0f && fray < PhaisterHexSigil.FraySeconds + PhaisterHexSigil.SmokeOut);
            if (!live) { _hexSigil?.Hide(); _sigilEndedAt = -1f; return; }
            if (_hexSigil == null) _hexSigil = new PhaisterHexSigil(transform);
            _hexSigil.Bind(PhaisterLook.ModelOf(transform));
            var view = Camera.main;
            _hexSigil.Pose(reach, mark, stab, hexed, fray, transform.position, BodyMarkHeight, hand, Time.time, view != null ? view.transform.position : (Vector3?)null, FirstPersonArms(_body) != null);
        }

        // The cast's bodies are big-headed: what the sigil treats as the body's height, so its 0.6 falls on the torso.
        private const float BodyMarkHeight = 1.35f;
        private PhaisterHexSigil _hexSigil;
        private bool _sigilWasMarked, _sigilStabbed;
        private float _sigilLastAge, _sigilEndedAt = -1f;

        private void StepMark(float dt)
        {
            StepBand();
            StepSigil();
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

            if (kind == VoodooMarkKind.Drain && PhaisterDrainPresenter.DrawsMark)
            {
                // The candle and its moths draw this mark (`PhaisterDrainLook`).
                SetEnabled(_mark, false); SetEnabled(_stitchA, false); SetEnabled(_stitchB, false);
            }
            else if (kind == VoodooMarkKind.Drain)
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
            else if (PhaisterLook.Direction != PhaisterLook.Style.Current)
            {
                // ⚠️ The owner's pending pick (`PhaisterLook`, 2026-10-29): the same mark age and armed flag, another look.
                SetEnabled(_mark, false); SetEnabled(_stitchA, false); SetEnabled(_stitchB, false);
                return;
            }
            else
            {
                if (_stitchA == null) _stitchA = MakeLine("VoodooMarkStitchA", 2);
                if (_stitchB == null) _stitchB = MakeLine("VoodooMarkStitchB", 2);
                PoseHexLines(_mark, _stitchA, _stitchB, _block, centre, right, up, grow,
                    Mathf.Clamp01(_body.VoodooMarkAge / VoodooRules.HexArmSeconds), _body.VoodooHexArmed, Time.time);
            }
        }

        /// <summary>
        /// Today's HEX mark (`PhaisterLook.Style.Current`), lifted out unchanged so the kit film can draw the same lines beside
        /// the new looks: a button with a stitch through it, filling with light as the fuse burns and throbbing once armed.
        /// </summary>
        public static void PoseHexLines(LineRenderer mark, LineRenderer stitchA, LineRenderer stitchB, MaterialPropertyBlock block,
                                        Vector3 centre, Vector3 right, Vector3 up, float grow, float fuse, bool armed, float time)
        {
            float throb = armed ? 0.5f + 0.5f * Mathf.Sin(time * 7.0f) : 0.0f;
            float r = 0.24f * grow * (1.0f + 0.08f * throb);
            mark.positionCount = 48;
            for (int i = 0; i < 48; i++)
            {
                float a = i / 47.0f * Mathf.PI * 2.0f;
                mark.SetPosition(i, centre + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * r);
            }
            mark.widthMultiplier = 0.09f;
            float light = armed ? 0.85f + 0.15f * throb : Mathf.Lerp(0.15f, 0.8f, fuse);
            Paint(mark, block, HexHue, 1.0f, light, stitches: 1.0f);
            mark.enabled = true;

            float s = r * 0.62f;
            stitchA.SetPosition(0, centre + (-right + up) * s); stitchA.SetPosition(1, centre + (right - up) * s);
            stitchB.SetPosition(0, centre + (right + up) * s); stitchB.SetPosition(1, centre + (-right - up) * s);
            stitchA.widthMultiplier = stitchB.widthMultiplier = 0.07f;
            Paint(stitchA, block, HexHue, 1.0f, light, stitches: 0.0f);
            Paint(stitchB, block, HexHue, 1.0f, light, stitches: 0.0f);
            stitchA.enabled = stitchB.enabled = true;
        }

        // ------------------------------------------------------------------ plumbing

        private LineRenderer MakeLine(string name, int points) => NewThread(transform, name, points);

        /// <summary>A thread line as this presenter draws them (public for the kit film's side-by-side of today's look).</summary>
        public static LineRenderer NewThread(Transform parent, string name, int points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
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
            => Paint(line, _block, hue, alpha, tight, stitches);

        private static void Paint(LineRenderer line, MaterialPropertyBlock block, Color hue, float alpha, float tight, float stitches)
        {
            var c = new Color(hue.r, hue.g, hue.b, Mathf.Clamp01(alpha));
            line.startColor = c; line.endColor = c;
            line.GetPropertyBlock(block);
            block.SetFloat("_Tight", Mathf.Clamp01(tight));
            block.SetFloat("_StitchOn", stitches);
            line.SetPropertyBlock(block);
        }

        private static void SetEnabled(LineRenderer line, bool on)
        {
            if (line != null && line.enabled != on) line.enabled = on;
        }

        private void OnDisable()
        {
            _markShown = VoodooMarkKind.None;
            SetEnabled(_mark, false); SetEnabled(_stitchA, false); SetEnabled(_stitchB, false); SetEnabled(_band, false);
            _hexSigil?.Hide();
            _sigilWasMarked = false; _sigilEndedAt = -1f;
        }

        private void OnDestroy() => _hexSigil?.Free();
    }
}

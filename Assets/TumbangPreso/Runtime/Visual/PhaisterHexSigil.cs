using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ CURSE: HEX, REBUILT ON THE OWNER'S REFERENCE (2026-10-30). Of the button-and-moth mark: *"no this genuinely needs
    /// some rework. the execution of the effects are horrible"*. His main reference ("this") is a bright sigil floating over
    /// the cursed player's chest, with thin smoky rings sweeping round the body. The sigil is her own
    /// (`tools/paint_phaister_sigil.py`: a crescent cupping an eye on a stitched spine, moth antennae; a fictional lunar rune
    /// grammar, nothing of the reference's symbol), drawn by `PhaisterSigil.shader`.
    ///
    /// Every beat is the ability's own (`VoodooRules`):
    ///
    /// | Beat | When | What a player sees |
    /// |---|---|---|
    /// | REACH | her 2 s reach | the circle is written clockwise on the ground under them while the sigil writes itself on their chest; wisps from her hand |
    /// | MARKED | 0 to 10 s | sigil dim and unfinished, smoke rising from the circle round the body, arcs sweeping round them; the circle's rim fills clockwise as the fuse |
    /// | ARMED | 10 to 25 s | the last strokes write in with a star flash and a shockwave along the ground; the sigil burns white-hot, the runes magenta |
    /// | STAB | her recast | the sigil punches into the chest with a blast of light; the circle bursts and an eye opens in the burst |
    /// | HEXED | 7.5 s | the sigil stays burning on the chest, the only mark of it, and burns away as the status ends |
    /// | FRAY | unused at 25 s | circle and sigil crack, part and drift away with the smoke |
    ///
    /// Cost: at most eighteen renderers: four quads and 96 motes in one mesh (`PhaisterSigil`), six clouds of 9 balls (`FxCloud`,
    /// two passes each), two wisps, three sweeps and one thread (`FxStudyRibbon`, built once, moved by transforms). No mesh is rebuilt after it is made.
    /// </summary>
    public sealed class PhaisterHexSigil
    {
        /// <summary>
        /// Metres: the sigil's half size (the torso's), the circle's radius, how far up the body the chest is (a share) and how
        /// far out from the body's axis the flashes stand. `SigilPull` is how far toward the eye the sigil's DEPTH is written (see the shader): it sits on the body's axis on screen, clears it, and a wall still hides it. ⚠️ The layout is the owner's: *"maybe put the magic circle on the
        /// ground and the sigil on the torso"*. `ChestFront` is measured on the bench's capsule, not on the cast's models.
        /// </summary>
        public const float SigilSize = 0.54f, CircleSize = 1.40f, ChestShare = 0.60f, ChestFront = 0.42f, SigilPull = 0.95f;
        private const float FrayBurst = 0.55f;
        public const float StabSeconds = 0.95f, FraySeconds = 1.0f, SmokeOut = 1.8f, StabCircle = 0.7f, BlastSeconds = 1.25f;

        private readonly GameObject _root;
        private readonly Material _sigil, _runes, _burst;
        private readonly GameObject _sigilGo, _runesGo, _burstGo;
        private readonly FxPiece[] _sweeps = new FxPiece[3];
        private readonly FxPiece _thread;
        private readonly List<Object> _owned = new List<Object>();

        public PhaisterHexSigil(Transform parent)
        {
            _root = new GameObject("PhaisterHexSigil");
            _root.transform.SetParent(parent, false);
            _sigil = Quad("Sigil", 0f, out _sigilGo);
            _runes = Quad("Runes", 1f, out _runesGo);
            _burst = Quad("Burst", 2f, out _burstGo);
            // Nothing here writes depth, so without this the circle's dark pool paints straight over the eye and the ring.
            if (_burst != null) _burst.renderQueue += 10;
            // Motes: one static mesh of 48 small cards, each orbiting and rising on its own seed (the shader's mode 4).
            if (_moteMesh == null) _moteMesh = MoteMesh(96);
            _motesGo = PhaisterLook.Draw("Motes", _root.transform, _moteMesh);
            _motes = PhaisterLook.Paint(_motesGo, "PhaisterSigil");
            _motes?.SetFloat("_Mode", 4f);
            // ⚠️ THE GROUND FOG IS THE APPROVED CLOUD, CALLED AND NOT COPIED (the owner's standing rule; `FxCloud`, the one on F12):
            // six low emitters round the circle's rim, breathing outward over the ground, its two tones close and no line so
            // the balls melt into one mass. Columns of it read as black sausages in front of her body.
            for (int i = 0; i < Fog.Length; i++)
            {
                _fogAt[i] = new GameObject("Fog" + i).transform;
                _fogAt[i].SetParent(_root.transform, false);
                _fog[i] = FxCloud.Build(_fogAt[i], "cloud", 11 + i, 9, new Color(0.31f, 0.10f, 0.46f), new Color(0.24f, 0.07f, 0.37f), new Color(0.24f, 0.07f, 0.37f), 0f, 0.10f, 0.25f);
            }
            // Two smoky rings round the body: one ribbon each, laid on a circle, turned and tilted by its transform.
            var ring = new List<FxStudyKit.Knot>();
            for (int i = 0; i <= 16; i++)
            {
                float a = i / 16f * 330f * Mathf.Deg2Rad;
                ring.Add(new FxStudyKit.Knot(Mathf.Cos(a) * 0.85f, 0f, Mathf.Sin(a) * 0.85f, Mathf.Lerp(0.07f, 0.17f, Mathf.Sin(i / 16f * Mathf.PI)), 0f));
            }
            for (int s = 0; s < 3; s++)
            {
                var mesh = FxStudyKit.Sweep(ring, Vector3.up, 6, 4, 0.1f);
                _sweeps[s] = new FxPiece("Sweep" + s, _root.transform, mesh, FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 30 + s);
                _sweeps[s].Colours(new Color(0.30f, 0.05f, 0.45f), new Color(0.80f, 0.25f, 0.95f), new Color(1f, 0.92f, 1f), new Color(0.85f, 0.3f, 1f))
                          .Set("_Scroll", -1.8f, 0f).Set("_Tile", 2.5f, 1f).Set("_Cut", 0.12f, 0.45f).Set("_Stops", 0.35f, 0.62f, 0.92f)
                          .Set("_Ends", 0.08f, 0.35f, 0.45f).Set("_Add", 0.7f).Set("_Halo", 0.6f).Set("_Soft", 0.15f)
                          .Set("_ErodeTile", 2f, 1f).Set("_ErodeScroll", -0.5f, 0f).WindowSoft(0.12f);
                _owned.Add(mesh); _owned.Add(_sweeps[s].Material);
            }

            // The thread of her reach: a unit ribbon along +z with a slight sag, stretched from her hand to the sigil.
            var line = new List<FxStudyKit.Knot>();
            for (int i = 0; i <= 8; i++)
            {
                float u = i / 8f;
                line.Add(new FxStudyKit.Knot(0f, -0.22f * Mathf.Sin(u * Mathf.PI), u, Mathf.Lerp(0.06f, 0.12f, Mathf.Sin(u * Mathf.PI)), u * 200f));
            }
            var threadMesh = FxStudyKit.Sweep(line, Vector3.up, 8, 3, 0f);
            _thread = new FxPiece("Thread", _root.transform, threadMesh, FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 29);
            _thread.Colours(new Color(0.22f, 0.04f, 0.34f), new Color(0.82f, 0.25f, 0.95f), new Color(1f, 0.92f, 1f), new Color(0.85f, 0.3f, 1f))
                   .Set("_Scroll", -3.0f, 0f).Set("_Tile", 3f, 1f).Set("_Cut", 0.28f, 0.6f).Set("_Ends", 0.05f, 0.05f, 0.4f).Set("_Add", 0.8f)
                   .Set("_Halo", 0.6f).Set("_Soft", 0.12f).Set("_ErodeTile", 6f, 1.5f).Set("_ErodeScroll", -1.8f, 0f).WindowSoft(0.08f);
            _owned.Add(threadMesh); _owned.Add(_thread.Material);
            // The slam's impact frame: a burst of light at their chest (nothing blooms for us).
            _flash = PhaisterLook.Glow(_root.transform, "Flash", "glow_soft", new Color(1f, 0.95f, 1f), new Color(0.90f, 0.30f, 1f), 40, false, _owned);
            _armFlash = PhaisterLook.Glow(_root.transform, "ArmFlash", "flare_star", new Color(1f, 0.96f, 1f), new Color(0.90f, 0.35f, 1f), 41, false, _owned);
            Hide();
        }

        // Typed: where round the rim each patch of fog lies (degrees), unevenly, so it is not a neat ring.
        private static readonly float[] Fog = { 12f, 71f, 138f, 196f, 249f, 318f };
        private const float FogLife = 2.2f, WispHeight = 0.72f;
        /// <summary>
        /// True draws the ground fog with `FxCloud`, the owner's standing rule for smoke, which is solid balls and shows them
        /// (film nobloomg9). False, the default for now, paints a soft fog in the circle's own shader (nobloomg10). His call.
        /// </summary>
        public static bool CloudFog = false;
        private readonly FxCloud[] _fog = new FxCloud[6];
        private readonly Transform[] _fogAt = new Transform[6];
        private const float GlowOut = 1.1f;
        private PhaisterCurseGlow _glow;

        /// <summary>Puts the body's light out without touching anything else (the film's other columns).</summary>
        public void DarkenBody() => _glow?.Pose(0f, 0f, Vector3.zero, 1f);

        /// <summary>The body the curse's light goes inside. Built once per model, nothing rebuilt per frame.</summary>
        public void Bind(Transform model)
        {
            if (_glow != null && _glow.Model == model) return;
            _glow?.Free();
            _glow = model != null ? new PhaisterCurseGlow(model) : null;
        }

        private float _smokeFrom = -1f, _smokeTo = 1e6f;
        private readonly FxPiece _flash, _armFlash;
        private static Mesh _moteMesh;
        private readonly GameObject _motesGo;
        private readonly Material _motes;

        /// <summary>Cards with their corner in x and z and a fixed seed each (typed by hash, the same every run).</summary>
        private static Mesh MoteMesh(int count)
        {
            var v = new List<Vector3>(); var seed = new List<Vector4>(); var t = new List<int>();
            for (int i = 0; i < count; i++)
            {
                var s = new Vector4(FxStudyKit.Hash(i, 1), FxStudyKit.Hash(i, 2), FxStudyKit.Hash(i, 3), FxStudyKit.Hash(i, 4));
                int at = v.Count;
                v.Add(new Vector3(-1, 0, -1)); v.Add(new Vector3(1, 0, -1)); v.Add(new Vector3(1, 0, 1)); v.Add(new Vector3(-1, 0, 1));
                for (int c = 0; c < 4; c++) seed.Add(s);
                t.Add(at); t.Add(at + 2); t.Add(at + 1); t.Add(at); t.Add(at + 3); t.Add(at + 2);
            }
            var m = new Mesh { name = "PhaisterHexMotes" };
            m.SetVertices(v); m.SetUVs(1, seed); m.SetTriangles(t, 0);
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            return m;
        }
        private Vector3 _eyeHint;

        private Material Quad(string name, float mode, out GameObject go)
        {
            go = PhaisterLook.Draw(name, _root.transform, PhaisterLook.Quad);
            var m = PhaisterLook.Paint(go, "PhaisterSigil");
            if (m != null)
            {
                m.SetFloat("_Mode", mode);
                m.SetTexture("_SigilTex", FxStudyKit.Mask("phaister_sigil"));
                m.SetTexture("_RuneTex", FxStudyKit.Mask("phaister_runes"));
                m.SetTexture("_CloudTex", FxStudyKit.Mask("erode_cloud"));
                m.SetTexture("_SmokeTex", FxStudyKit.Mask("trail_smoke"));
                m.SetTexture("_RayTex", FxStudyKit.Mask("erode_radial"));
                m.SetTexture("_RingTex", FxStudyKit.Mask("phaister_blast_burn"));
                m.SetTexture("_BurnTex", FxStudyKit.Mask("phaister_sigil_burn"));
                m.SetTexture("_OrderTex", FxStudyKit.Mask("phaister_sigil_order"));
                m.SetTexture("_EyeTex", FxStudyKit.Mask("phaister_eye_burn"));
            }
            return m;
        }

        public void Hide() { _glow?.Pose(0f, 0f, Vector3.zero, 1f); if (_root.activeSelf) _root.SetActive(false); _smokeFrom = -1f; _smokeTo = 1e6f; }
        public void Free() { _glow?.Free(); foreach (var w in _fog) w?.Dispose(); foreach (var thing in _owned) PhaisterLook.Free(thing); PhaisterLook.Free(_root); }

        private static void On(GameObject go, bool on) { if (go != null && go.activeSelf != on) go.SetActive(on); }

        /// <summary>
        /// One frame. Each phase is the seconds since it began, or below 0 when it is not happening: <paramref name="reach"/>
        /// her 2 s reach, <paramref name="mark"/> the mark's age, <paramref name="stab"/> since her recast,
        /// <paramref name="hexed"/> into the 7.5 s HEXED, <paramref name="fray"/> since it expired unused. <paramref name="feet"/>
        /// and <paramref name="height"/> are the victim's; <paramref name="hand"/> is her reaching hand.
        /// </summary>
        public void Pose(float reach, float mark, float stab, float hexed, float fray, Vector3 feet, float height, Vector3 hand, float clock, Vector3? eye = null, bool ownView = false)
        {
            _eyeHint = PhaisterLook.EyeOr(eye);
            if (!_root.activeSelf) _root.SetActive(true);
            Vector3 head = feet + Vector3.up * height, chest = feet + Vector3.up * (height * ChestShare);
            Vector3 toEye = _eyeHint - chest; toEye.y = 0f;
            toEye = toEye.sqrMagnitude > 1e-4f ? toEye.normalized : Vector3.back;
            bool armed = mark >= VoodooRules.HexArmSeconds;
            float since = armed ? mark - VoodooRules.HexArmSeconds : -1f;
            float flare = armed ? FxStudyKit.Envelope(since, 0.10f, 0.05f, 0.18f, 0.25f) : 0f;

            // ---- the state of the beat
            float draw = 0f, bright = 0f, fuse = 0f, crack = 0f, fade = 0f, size = SigilSize, circleDraw = 1f, circleFade = 0f, circleSize = CircleSize;
            if (reach >= 0f)
            {
                float u = Mathf.Clamp01(reach / VoodooRules.ReachSeconds);
                draw = 0.80f * FxStudyKit.Ease(u); circleDraw = FxStudyKit.Ease(u); bright = 0.25f;
            }
            else if (mark >= 0f)
            {
                fuse = Mathf.Clamp01(mark / VoodooRules.HexArmSeconds);
                if (!armed) { draw = 0.80f; bright = 0.30f + 0.06f * Mathf.Sin(clock * 3f); }
                else
                {
                    // The last strokes write in fast, with a flare as they finish; then it burns and beats.
                    draw = Mathf.Lerp(0.80f, 1f, FxStudyKit.Run(since, 0f, 0.16f));
                    bright = 1.0f + 0.15f * Mathf.Sin(clock * 6.5f) + 0.5f * flare;
                    size *= 1f + 0.2f * flare + 0.03f * Mathf.Sin(clock * 6.5f);
                }
            }
            else if (stab >= 0f)
            {
                // Punched into the chest in a tenth of a second; the circle under them is blown out by the burst.
                float u = FxStudyKit.EaseIn(Mathf.Clamp01(stab / 0.10f));
                draw = 1f; bright = 1.5f;
                size *= Mathf.Lerp(1.25f, 0.45f, u);
                // (g8 simply popped out.) Blown apart under the burst: its plates fly and burn away over more than half a second.
                crack = FxStudyKit.EaseOut(FxStudyKit.Run(stab, 0.02f, StabCircle)); circleFade = FxStudyKit.Run(stab, 0.18f, StabCircle); circleSize *= 1f + 0.2f * FxStudyKit.EaseOut(FxStudyKit.Run(stab, 0f, 0.25f)); fuse = 1f;
            }
            else if (fray >= 0f)
            {
                float u = fray / FraySeconds;
                draw = 1f; bright = Mathf.Max(0.7f * (1f - u), 1.3f * FxStudyKit.Envelope(fray, 0f, 0.04f, 0.14f, 0.16f)); fuse = 1f;
                crack = 0.05f * FxStudyKit.Run(fray, 0f, 0.16f) + 0.95f * FxStudyKit.Ease(FxStudyKit.Run(fray, 0.16f, FraySeconds));
                fade = FxStudyKit.Run(fray, FraySeconds * 0.45f, FraySeconds); circleFade = fade;
            }

            // ---- the sigil on the chest
            // ⚠️ Owner: "isnt the sigil supposed to stay on the character?" It stays through the stab and for the whole of HEXED, read
            // from the replicated status, and it is the ONLY mark of it. It always faces the eye and its depth clears the body, so it
            // shows at the same strength from behind: a marker that dimmed there would stop marking whoever is running away.
            bool stayed = stab >= 0f || (hexed >= 0f && hexed < StatusRules.HexedSeconds);
            if (stayed)
            {
                float after = stab >= 0f ? stab : 10f;
                draw = 1f; fade = hexed >= 0f ? FxStudyKit.Run(hexed, StatusRules.HexedSeconds - 0.7f, StatusRules.HexedSeconds) : 0f;
                bright = Mathf.Lerp(1.5f, 0.92f + 0.12f * Mathf.Sin(clock * 3.2f), FxStudyKit.Run(after, 0.1f, 0.6f));
                size = SigilSize * Mathf.Lerp(1.25f, 1f, FxStudyKit.EaseIn(Mathf.Clamp01(after / 0.10f)));
            }
            bool showSigil = reach >= 0f || mark >= 0f || stayed || (fray >= 0f && fray < FraySeconds);
            // ⚠️ On the victim's own screen the sigil and the flashes would sit on the lens: only the circle and smoke.
            showSigil &= !ownView;
            On(_sigilGo, showSigil);
            if (showSigil && _sigil != null)
            {
                _sigil.SetVector("_Centre", chest); _sigil.SetFloat("_Size", size); _sigil.SetFloat("_Pull", ownView ? 0f : SigilPull);
                _sigil.SetFloat("_Draw", draw); _sigil.SetFloat("_Bright", bright); _sigil.SetFloat("_Crack", stab >= 0f ? 0f : crack);
                _sigil.SetFloat("_Fade", fade); _sigil.SetFloat("_Age", clock);
            }

            // ---- the circle on the ground, carrying the fuse
            bool showCircle = reach >= 0f || mark >= 0f || (stab >= 0f && stab < StabCircle) || (fray >= 0f && fray < FraySeconds);
            On(_runesGo, showCircle);
            if (showCircle && _runes != null)
            {
                float open = reach >= 0f ? FxStudyKit.EaseOut(FxStudyKit.Run(reach, 0f, 0.35f)) : 1f;
                _runes.SetVector("_Centre", feet + Vector3.up * 0.025f); _runes.SetFloat("_Size", circleSize * Mathf.Lerp(0.8f, 1f, open));
                _runes.SetFloat("_Draw", circleDraw); _runes.SetFloat("_Bright", bright); _runes.SetFloat("_Fuse", fuse);
                _runes.SetFloat("_Crack", crack); _runes.SetFloat("_Fade", circleFade); _runes.SetFloat("_Since", since); _runes.SetFloat("_Age", clock);
            }

            // ---- fog on the ground round the rim. It stops WITH the stab: the emitter stops and what is out is blown away in a
            // fifth of a second; at the fray it shrinks with the circle.
            bool marking = reach >= 0f || mark >= 0f;
            if (marking && _smokeFrom < 0f) { _smokeFrom = clock; _smokeTo = 1e6f; }
            if (!marking && _smokeTo > 1e5f) _smokeTo = clock;
            float fogSize = stab >= 0f ? 1f - FxStudyKit.Ease(FxStudyKit.Run(stab, 0f, 0.2f)) : fray >= 0f ? 1f - FxStudyKit.Ease(FxStudyKit.Run(fray, 0.1f, FraySeconds)) : 1f;
            bool fogging = CloudFog && _smokeFrom >= 0f && fogSize > 0.01f && (marking || stab >= 0f || fray >= 0f);
            if (_runes != null) _runes.SetFloat("_FogOn", CloudFog ? 0f : fogSize * (reach >= 0f ? FxStudyKit.Run(reach, 0.2f, 1.2f) : 1f));
            float rimAt = circleSize * 1.24f;
            for (int i = 0; i < Fog.Length; i++)
            {
                On(_fogAt[i].gameObject, fogging);
                if (!fogging) continue;
                var outward = new Vector3(Mathf.Cos(Fog[i] * Mathf.Deg2Rad), 0f, Mathf.Sin(Fog[i] * Mathf.Deg2Rad));
                _fogAt[i].position = feet + outward * rimAt;
                _fogAt[i].localScale = Vector3.one * fogSize;
                var e = FxCloud.Emitter.Column(Vector3.up * 0.02f, outward * 0.55f + Vector3.up * 0.10f);
                e.Spread = 0.55f; e.Life = FogLife; e.Widen = 0.30f; e.Weave = 0.06f; e.Small = 0.10f; e.Large = 0.25f; e.Gone = 0.04f;
                e.From = _smokeFrom; e.To = _smokeTo;
                _fog[i].Pose(clock, e);
            }
            // ---- the curse's light inside the body, by stage. Not on the victim's own screen, whose lens it would fill.
            if (_glow != null)
            {
                float level = 0f;
                if (reach >= 0f) level = 0.10f + 0.18f * Mathf.Clamp01(reach / VoodooRules.ReachSeconds);
                else if (mark >= 0f && !armed) level = 0.28f + 0.30f * Mathf.Clamp01(mark / VoodooRules.HexArmSeconds);
                else if (armed) level = 0.62f + 0.10f * Mathf.Sin(clock * 6.5f) + 0.5f * flare;
                else if (stab >= 0f && stab < GlowOut) level = stab < 0.08f ? Mathf.Lerp(0.8f, 1.5f, stab / 0.08f) : Mathf.Lerp(1.5f, 0.16f, FxStudyKit.EaseOut(FxStudyKit.Run(stab, 0.08f, GlowOut)));
                else if (hexed >= 0f) level = 0.16f * (1f - FxStudyKit.Run(hexed, StatusRules.HexedSeconds - 0.7f, StatusRules.HexedSeconds));
                else if (fray >= 0f) level = 0.6f * (1f - FxStudyKit.Run(fray, 0f, FraySeconds * 0.8f));
                _glow.Pose(ownView ? 0f : level, clock, chest, 0.62f);
            }

            // ---- motes rising round the body; flung out when it arms, at the stab and at the fray
            bool motes = !ownView && (showSigil || (stab >= 0f && stab < 0.8f));
            On(_motesGo, motes);
            if (motes && _motes != null)
            {
                float fling = 0f;
                if (armed) fling = Mathf.Sin(Mathf.PI * FxStudyKit.Run(since, 0.05f, 0.75f));
                if (stab >= 0f) fling = FxStudyKit.EaseOut(FxStudyKit.Run(stab, 0.04f, 0.7f));
                if (fray >= 0f) fling = FxStudyKit.EaseOut(FxStudyKit.Run(fray, 0.1f, FraySeconds));
                _motes.SetVector("_Centre", feet + Vector3.up * (height * 0.55f)); _motes.SetFloat("_Size", 1.05f); _motes.SetFloat("_Age", clock);
                _motes.SetFloat("_Burst", fling); _motes.SetFloat("_Bright", reach >= 0f ? 0.3f : armed || stab >= 0f ? 1.2f : 0.55f);
                _motes.SetFloat("_Fade", stab >= 0f ? FxStudyKit.Run(stab, 0.35f, 0.8f) : fray >= 0f ? FxStudyKit.Run(fray, 0.5f, FraySeconds) : 0f);
            }

            // ---- the flashes: a star at the chest when it arms, a blast of light when it is punched in
            _armFlash.At.position = chest + toEye * (ChestFront + 0.1f);
            _armFlash.Face(_eyeHint);
            _armFlash.At.localScale = Vector3.one * Mathf.Lerp(1.2f, 5.0f, FxStudyKit.EaseOut(FxStudyKit.Run(since, 0.08f, 0.35f)));
            _armFlash.Frame(clock, !ownView && since >= 0f && since < 0.7f ? FxStudyKit.Envelope(since, 0.08f, 0.02f, 0.18f, 0.35f) : 0f);
            _flash.At.position = chest + toEye * (ChestFront + 0.1f);
            _flash.Face(_eyeHint);
            _flash.At.localScale = Vector3.one * Mathf.Lerp(1.2f, 6.0f, FxStudyKit.EaseOut(FxStudyKit.Run(stab, 0.06f, 0.30f)));
            _flash.Frame(clock, !ownView && stab >= 0f && stab < 0.4f ? 0.55f * FxStudyKit.Envelope(stab, 0.06f, 0.02f, 0.11f, 0.26f) : 0f);

            // ---- the stab's burst on the ground, the eye opening in it
            bool frayBurst = fray >= 0.14f && fray < 0.14f + FrayBurst;
            bool burst = (stab >= 0f && stab < BlastSeconds) || frayBurst;
            On(_burstGo, burst);
            if (frayBurst && _burst != null)
            {
                // The fray's crackle: a ring and rays as the cracks let go, no eye: nothing opened, it died.
                _burst.SetVector("_Centre", feet + Vector3.up * 0.03f); _burst.SetFloat("_Size", CircleSize * 1.6f);
                _burst.SetFloat("_EyeOn", 0f); _burst.SetFloat("_Burst", (fray - 0.14f) / FrayBurst);
            }
            else if (burst && _burst != null)
            {
                _burst.SetFloat("_EyeOn", 1f);
                _burst.SetVector("_Centre", feet + Vector3.up * 0.03f); _burst.SetFloat("_Size", CircleSize * 1.9f);
                _burst.SetFloat("_Burst", Mathf.Clamp01((stab - 0.06f) / (BlastSeconds - 0.06f)));
            }

            // ---- two or three wide arcs sweeping round the body while it is marked
            for (int s = 0; s < 3; s++)
            {
                var piece = _sweeps[s];
                float cycle = armed ? 1.0f : 1.6f;
                float phase = Mathf.Repeat((clock + s * cycle / 3f) / cycle, 1f);
                float head01 = FxStudyKit.EaseOut(Mathf.Clamp01(phase / 0.65f)) * 1.1f;
                float tail01 = FxStudyKit.Ease(Mathf.Clamp01((phase - 0.25f) / 0.7f)) * 1.1f - 0.05f;
                piece.At.position = feet + Vector3.up * (height * (0.30f + 0.26f * s));
                piece.At.rotation = Quaternion.Euler(16f - 17f * s, clock * (s == 1 ? -115f : 140f) + s * 120f, 10f - 11f * s);
                piece.Open(tail01, head01);
                piece.Frame(clock, mark >= 0f && !ownView ? (armed ? 1f : 0.8f) : 0f);
            }

            // ---- the thread of her reach, to the chest: part-eaten always, so it reads as wisps and never as a cord
            bool thread = reach >= 0f;
            if (thread)
            {
                Vector3 span = chest + toEye * ChestFront - hand;
                _thread.At.position = hand;
                _thread.At.rotation = Quaternion.LookRotation(span.sqrMagnitude > 1e-4f ? span : Vector3.forward, Vector3.up);
                _thread.At.localScale = new Vector3(1f, 1f, span.magnitude);
                _thread.Open(-0.05f, FxStudyKit.EaseOut(FxStudyKit.Run(reach, 0f, 0.3f)) * 1.1f);
            }
            _thread.Frame(clock, thread ? 0.9f : 0f, thread ? Mathf.Max(0.35f, FxStudyKit.Run(reach, VoodooRules.ReachSeconds - 0.25f, VoodooRules.ReachSeconds)) : 0f);
        }
    }
}

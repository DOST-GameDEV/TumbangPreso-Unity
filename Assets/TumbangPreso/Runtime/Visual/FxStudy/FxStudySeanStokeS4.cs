// ⚠️ A KEPT EARLIER BENCH STATE OF RAGO'S STOKE STEP, so it can still be filmed by name beside the newer one
// (`SeanFireFilm.cs`, look `s4s6`). Nothing in a match reads it. The live look is `FxStudySeanStoke`.
using System.Collections.Generic;
using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// RAGO'S STOKE STEP (a first rough sample, 2026-10-17). He braces for 0.18 s and bursts 2 m along his aim, thrown at
    /// the root of 2 x friction 30 x 2 and stopped by that friction (`SeanHeroKit.StokeSpeed`, `StokeTravelSeconds`), then
    /// stands planted for 0.25 s. It hits nobody and leaves nothing that burns ("No knockdown or burning trail").
    /// So the picture is: a stamp of heat where he pushes off, a wake of flame that is only ever as long as he has gone,
    /// and nothing left on the road a second later.
    ///
    /// Built from the bench, as the method asks (`docs/VFX_BENCH_METHOD.md` section 4):
    ///   * THE WAKE is the approved trail ribbons' own flame picture (`trail_smoke`, the pleated ribbon, which is a row of
    ///     tongues) on an upright curtain laid down his path, coloured fire, front at his back. Two curtains crossed a
    ///     little, so it has a body from the side and from in front. Its tail is let go as he goes and what is left is
    ///     eaten from the tail (the ribbon shader's erosion, `erode_cloud`) when he stops: it never fades.
    ///   * THE STAMP is the scorch study's char and fissure pictures (`scorch_char`, `scorch_fissure`), small, under his
    ///     push-off, the cracks cooling by a rising cut from marigold to red as the scorch's do.
    ///   * THE ASH is the approved cloud, pooled (`CloudPuffs`): a kick at the push-off and a skid where he plants. This
    ///     class says when (`PushOffAt`, `PlantPuff`); whoever plays it spawns them.
    ///   * THE EMBERS ARE NEW: a dozen painted blocks (`SeanEmbers.shader`), the one thing the bench has no piece for. Its
    ///     motes are glow sprites, which are light and go pale on a sunlit road.
    /// Nothing is in front of him: the wake is behind his back, the stamp under his heel, the embers thrown backward.
    ///
    /// ⚠️ A PURE FUNCTION OF ITS CLOCK, MESHES BUILT ONCE. The two curtains and the embers are one mesh each for the whole
    /// process, shared by every Rago; a frame is a handful of floats handed to five materials. Nothing is built, uploaded
    /// or allocated per frame.
    /// </summary>
    public sealed class FxStudySeanStokeS4 : FxStudyEffect, ISeanStokeLook
    {
        /// <summary>The rule's numbers, read from the kit so the picture cannot drift from it.</summary>
        public static float Brace => SeanHeroKit.StokeAnticipation;
        public static float Launch => SeanHeroKit.StokeSpeed;
        public static float Travel => SeanHeroKit.StokeTravelSeconds;
        public static float Friction => Core.Balance.Friction;
        /// <summary>The length the curtains are built to (the rule's 2 m). A shorter dash draws them shorter, not squashed in height.</summary>
        public const float Nominal = SeanHeroKit.StokeDistance;
        /// <summary>When it is all gone, counted from the press, for a dash that went its whole way.</summary>
        public const float Life = 2.10f;
        private const float Lead = 0.20f;

        // Fire between two named extremes: ember red at the dark of the picture, marigold at its light, a thin pale yellow
        // only on the brightest tongue edges (white in thin lines, never a broad area).
        // PAINTED, NOT GLASS (the owner's notes on s3, 2026-10-17: "broad painted flame shapes with a hard-stepped two or
        // three tone ramp (dark red rim, orange body, yellow core), not soft glassy gradients", and "no pink or magenta").
        // Read off a stylised 2D fire sheet seen as an image-search thumbnail: each flame is one broad teardrop in three flat
        // tones, a thin dark red rim, an orange body, a yellow core low in it, with tips that break into separate licks.
        // So the ramp's steps are a few hundredths of the mask wide, the piece covers what is behind it (almost no light
        // laid on), there is no halo, and the hue drift is small: it only wanders, it never turns red toward magenta.
        private static readonly Color Deep = new Color(0.72f, 0.11f, 0.03f), Body = new Color(1.00f, 0.44f, 0.05f),
                                      Hot = new Color(1.00f, 0.84f, 0.28f), Halo = new Color(1.00f, 0.40f, 0.05f);

        /// <summary>
        /// WHICH LOOK. `Curtain` is the bench sample s4 as filmed (painted flame curtains and a crown of the approved trail
        /// tongues), kept to go back to. `Cel` is drawn to the owner's two fire references of 2026-10-17 ("for fire"): flat
        /// cel fire blobs and dark smoke in the approved cloud, needle sparks, a thin twisted ring and scorch cracks.
        /// A film names the look it wants; a match will read this.
        /// </summary>
        public enum Style { Curtain, Cel }
        public static Style Look = Style.Cel;
        private readonly Style _look;

        private static Mesh _curtainNear, _curtainFar, _embers, _crownMesh, _needles, _ringMesh;

        private readonly FxPiece _near, _far, _crown, _char, _cracks, _pool, _sparks, _streaks, _ring;
        private readonly CloudPuffField _flames;
        private float _scheduledFor = -1f;
        private readonly Material _sparkLook;
        private readonly float _light;

        public override float Seconds => Lead + Life;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { focus = new Vector3(0f, 0.7f, 1.0f); eye = focus + new Vector3(5.2f, 2.4f, -2.6f); fov = 38f; }

        public FxStudySeanStokeS4(Transform parent) : this(parent, AmihanWhirlwind.DayLight) { }

        /// <summary>`light` is how much of each flame is light laid on (the rest covers), as for the approved whirlwind in
        /// daylight: added light alone goes pale on a sunlit road.</summary>
        public FxStudySeanStokeS4(Transform parent, float light) : base("s4s6", parent)
        {
            _light = light;
            _look = Look;
            if (_curtainNear == null) _curtainNear = Curtain(1.95f, 0.50f, 38f, 0.20f);
            if (_curtainFar == null) _curtainFar = Curtain(1.70f, 0.42f, -36f, -0.20f);
            if (_embers == null) _embers = EmberMesh(Embers, "SeanEmbers");

            _near = new FxPiece("wake-near", Root, _curtainNear, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 12);
            _far = new FxPiece("wake-far", Root, _curtainFar, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 11);
            Dress(_near, 0.0f, -0.80f, 1f);
            Dress(_far, 0.37f, -0.55f, 0.85f);
            Own(_near.Material); Own(_far.Material);

            // THE FLARE AT THE PUSH-OFF: a crown of the same tongues standing round the back and both sides of where he
            // leaves, to his shoulders, so a target in front sees fire burst out either side of him as he goes. It covers
            // 220 degrees behind him and stops 20 degrees short of his front on each side: nothing of it is before his eyes.
            if (_crownMesh == null)
            {
                _crownMesh = FxStudyKit.Shell(new[] { new Vector2(0.34f, -0.05f), new Vector2(0.52f, 0.55f), new Vector2(0.66f, 1.15f), new Vector2(0.60f, 1.65f) }, -90f, 220f, 25f, 12, 30);
                _crownMesh.UploadMeshData(true);
            }
            _crown = new FxPiece("flare", Root, _crownMesh, FxStudyKit.Ribbon("trail_smoke", "erode_cloud"), 13);
            Dress(_crown, 0.6f, 0f, 1f);
            // On the crown the picture is laid crosswise: its row of tongues runs round the arc and points up it.
            _crown.Set("_Swap", 1f).Set("_Tile", 0.9f, 0.85f, 0.6f, 0.15f).Set("_Scroll", 0.35f, 0f)
                  .Set("_ErodeTile", 1f, 1.2f, 0.6f, 0f).Set("_ErodeScroll", 0.9f, 0f).Set("_Ends", 0f, 0.05f, 0.14f).Set("_Sweep", -1.1f);
            Own(_crown.Material);

            // The stamp: the scorch's own two pictures, small, lying along his path under his push-off.
            var lie = Quaternion.Euler(0f, 90f, 0f);
            _char = AddFlat("stamp-char", 1, "scorch_char", lying: true, erode: "erode_cloud");
            var soot = new Color(0.03f, 0.022f, 0.018f);
            _char.Colours(soot, soot, new Color(0.08f, 0.05f, 0.035f), soot).Set("_Cut", 0.12f, 0.40f).Set("_Add", 0f).Set("_Soft", 0.10f).Set("_Emit", 0f);
            _char.At.localPosition = new Vector3(0f, 0.010f, -0.12f); _char.At.localRotation = lie; _char.At.localScale = new Vector3(2.30f, 1f, 1.15f);
            _cracks = AddFlat("stamp-cracks", 3, "scorch_fissure", lying: true, erode: "erode_cloud");
            _cracks.Colours(new Color(0.55f, 0.04f, 0.0f), new Color(1f, 0.36f, 0.02f), new Color(1f, 0.88f, 0.45f), new Color(1f, 0.4f, 0.05f))
                   .Set("_Halo", 0.5f).Set("_Soft", 0.10f).Set("_Hue", 8f, 0f, 0.4f, 1f);
            _cracks.At.localPosition = new Vector3(0f, 0.016f, -0.12f); _cracks.At.localRotation = lie; _cracks.At.localScale = new Vector3(2.30f, 1f, 1.15f);
            // The cracks are painted heat, not light: they cover the char in flat yellow and orange.
            _cracks.Set("_Add", 0.25f);
            // The ground under his rear foot glows through the brace (the one soft thing in it, and only on the road).
            _pool = AddGlow("stamp-glow", 2, new Color(1f, 0.82f, 0.40f), new Color(1f, 0.40f, 0.05f), lying: true);
            _pool.At.localPosition = new Vector3(0f, 0.013f, -0.12f); _pool.At.localScale = new Vector3(1.6f, 1f, 1.9f);

            var shader = Shader.Find("TumbangPreso/SeanEmbers");
            _sparkLook = shader != null ? new Material(shader) { name = "SeanEmbers", hideFlags = HideFlags.HideAndDontSave } : null;
            _sparks = new FxPiece("embers", Root, _embers, _sparkLook, 0);
            if (_sparkLook != null)
            {
                _sparkLook.renderQueue = 2502;
                _sparkLook.SetFloat("_Length", Nominal); _sparkLook.SetFloat("_Launch", Launch); _sparkLook.SetFloat("_Friction", Friction); _sparkLook.SetFloat("_Brace", Brace);
                Own(_sparkLook);
            }
            _sparks.Renderer.localBounds = new Bounds(new Vector3(0f, 1f, 1f), new Vector3(4f, 4f, 6f));

            // ---- the cel look's own pieces (`Style.Cel`), from the owner's references.
            // THE NEEDLES: reference 1's long thin pale streaks radiating from the burst. The same material and the same
            // clock as the embers, in a mesh of their own so the curtain look stays exactly as it was filmed.
            if (_needles == null) _needles = EmberMesh(Needles, "SeanNeedles");
            _streaks = new FxPiece("needles", Root, _needles, _sparkLook, 0);
            _streaks.Renderer.localBounds = new Bounds(new Vector3(0f, 1f, 1f), new Vector3(8f, 5f, 9f));
            // THE RING: reference 2's thin bright twisted rim round the feet. The approved trail's `trail_bands` picture (two
            // thin bands that cross and cross again) laid round a flat ring, in hard-stepped gold.
            if (_ringMesh == null)
            {
                _ringMesh = FxStudyKit.Revolve(new[] { new Vector2(0.86f, 0.02f), new Vector2(0.93f, 0.02f), new Vector2(1.0f, 0.02f) }, 64);
                _ringMesh.UploadMeshData(true);
            }
            _ring = new FxPiece("ring", Root, _ringMesh, FxStudyKit.Ribbon("trail_bands", "erode_cloud"), 4);
            _ring.Colours(new Color(0.80f, 0.22f, 0.03f), new Color(1.00f, 0.55f, 0.08f), new Color(1.00f, 0.88f, 0.42f), Halo)
                 .Set("_Tile", 3f, 1f, 0f, 0f).Set("_Scroll", 0.25f, 0f).Set("_Cut", 0.34f, 0.37f).Set("_Curve", 1f).Set("_Stops", 0.40f, 0.62f, 0.65f)
                 .Set("_Ends", 0f, 0f, 0f).Set("_ErodeTile", 3f, 1f, 0f, 0f).Set("_ErodeScroll", 0.2f, 0f).Set("_Sweep", 0f).Set("_Soft", 0.04f)
                 .Set("_Halo", 0f).Set("_Emit", 0.3f).Set("_Add", 0.2f).Set("_Hue", 5f, 0f, 0.3f, 1f);
            Own(_ring.Material);
            // THE FIRE BLOBS AND THE SMOKE: the approved cloud, pooled, in a field of its own (one material, the shared
            // mesh). Reference 1's fire is lumpy blobs in flat tones with dark ember seams between them: the cloud's balls
            // are the lumps, its two tones the orange body and the red creases, and its line the dark seam.
            if (_look == Style.Cel)
            {
                _flames = new CloudPuffField(Root, "Sean fire blobs");
                _flames.Show(false);
            }
        }

        // A fire blob of reference 1: orange body, red creases, a dark ember seam round each lump.
        private static CloudPuffField.Puff Blob(Vector3 at, float scale, float life, float lift, float turn)
        {
            var p = CloudPuffField.Puff.Landing(at);
            p.Scale = scale; p.Life = life; p.Reach = 0.05f; p.Lift = lift; p.Turn = turn; p.Share = 1f;
            p.Lit = new Color(1.00f, 0.60f, 0.12f); p.Shade = new Color(0.90f, 0.22f, 0.04f); p.Line = new Color(0.22f, 0.06f, 0.03f);
            return p;
        }
        // Its yellow heart: smaller, inside it, gone first.
        private static CloudPuffField.Puff Heart(Vector3 at, float scale, float life, float lift, float turn)
        {
            var p = Blob(at, scale, life, lift, turn);
            p.Reach = 0.03f; p.Share = 0.8f;
            p.Lit = new Color(1.00f, 0.84f, 0.32f); p.Shade = new Color(1.00f, 0.58f, 0.10f); p.Line = new Color(0.86f, 0.30f, 0.04f);
            return p;
        }
        // Reference 1's smoke: dark grey, rising out of the fire after it, higher and slower.
        private static CloudPuffField.Puff Smoke(Vector3 at, float scale, float life, float lift, float turn)
        {
            var p = Blob(at, scale, life, lift, turn);
            p.Reach = 0.08f; p.Share = 0.85f;
            p.Lit = new Color(0.36f, 0.34f, 0.33f); p.Shade = new Color(0.21f, 0.20f, 0.20f); p.Line = new Color(0.11f, 0.10f, 0.10f);
            return p;
        }

        /// <summary>The time the rule has him `d` metres from where he left, counted from the press.</summary>
        public static float PassAt(float d)
        {
            d = Mathf.Clamp(d, 0f, Nominal);
            return Brace + (Launch - Mathf.Sqrt(Mathf.Max(0f, Launch * Launch - 2f * Friction * d))) / Friction;
        }

        // Writes every puff of the cast into the field once, each at the moment the rule has him there (the field runs on
        // this effect's own clock, so nothing more is written while it plays). Written again only if he stops short of
        // the full 2 m: then nothing is drawn past where he stopped, and his plant is where he really stopped.
        private void Schedule(float reach)
        {
            if (_flames == null || !_flames.Valid) return;
            _scheduledFor = reach;
            _flames.Clear();
            Vector3 W(float x, float z) => Root.TransformPoint(new Vector3(x, 0f, z));
            // The push-off: the biggest blob, its heart and its smoke, just behind where he leaves, beside him and above.
            _flames.Spawn(Blob(W(0f, -0.30f), 3.8f, 0.40f, 0.70f, 0.0f), Brace);
            _flames.Spawn(Heart(W(0f, -0.25f), 2.6f, 0.28f, 0.45f, 0.7f), Brace + 0.01f);
            _flames.Spawn(Smoke(W(0f, -0.35f), 3.0f, 0.70f, 1.50f, 1.9f), Brace + 0.16f);
            // Along the way: smaller blobs as he passes, each a step behind him, and one more smoke.
            for (int i = 1; i <= 3; i++)
            {
                float d = 0.5f * i;
                if (d > reach - 0.15f) break;
                _flames.Spawn(Blob(W(i % 2 == 0 ? 0.08f : -0.08f, d - 0.30f), 3.0f - 0.25f * i, 0.36f, 0.60f, i * 2.1f), PassAt(d));
                if (i == 2) _flames.Spawn(Smoke(W(0f, d - 0.40f), 2.4f, 0.65f, 1.30f, 0.5f), PassAt(d) + 0.14f);
            }
            // The plant: a small blob and a skid of dark ash where he stops.
            float stop = Brace + Travel;
            _flames.Spawn(Heart(W(0f, reach - 0.10f), 1.5f, 0.30f, 0.30f, 2.6f), stop);
            _flames.Spawn(PlantPuff(W(0f, reach)), stop + 0.02f);
        }

        // The ribbon shader's dress for a fire curtain. u runs along his path (0 where he left, 1 at his back), v up it.
        private void Dress(FxPiece piece, float seed, float stream, float gain)
        {
            piece.Colours(Deep, Body, Hot, Halo)
                 // About three tongues to a metre; the picture's dark foot is cut off so the tongues stand on the road.
                 .Set("_Tile", 0.45f * Nominal, 0.85f, seed, 0.15f)
                 // The tongues stream back off him; the erosion climbs them, so their tips lick and tear.
                 .Set("_Scroll", stream, 0f)
                 .Set("_ErodeTile", 1.2f, 1.0f, seed, 0f).Set("_ErodeScroll", 0f, 0.9f)
                 // Hard steps: the edge, the red rim, the orange body and the yellow core are each a few hundredths of grey.
                 .Set("_Cut", 0.10f, 0.13f).Set("_Curve", 1f).Set("_Stops", 0.20f, 0.50f, 0.54f)
                 .Set("_Ends", 0.18f, 0.03f, 0f).Set("_Sweep", 1.1f).Set("_Soft", 0.04f)
                 .Set("_Hue", 6f, 0f, seed + 0.2f, 1f).Set("_Warp", 0.03f, 1.6f, seed * 5f, 2.5f)
                 .Set("_Halo", 0f).Set("_Emit", 0.3f).Set("_Add", Mathf.Min(0.15f, _light)).Set("_Gain", gain).WindowSoft(0.03f);
        }

        private readonly List<Object> _mine = new List<Object>();
        private void Own(Object thing) { if (thing != null) _mine.Add(thing); }

        /// <summary>Frees this cast's materials as well as everything the bench owns. The meshes are the process's.</summary>
        public void Release()
        {
            foreach (var thing in _mine) { if (thing == null) continue; if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing); }
            _mine.Clear();
            _flames?.Dispose();
            Dispose();
        }

        /// <summary>When the push-off puff goes, counted from the press, and where (in this effect's own space).</summary>
        public static float PushOffAt => Brace;
        public static readonly Vector3 PushOffPoint = new Vector3(0f, 0f, -0.15f);

        /// <summary>The ash kicked up where he pushes off, in the approved cloud: warm grey, low and thrown back.</summary>
        public static CloudPuffField.Puff KickPuff(Vector3 at)
        {
            var p = CloudPuffField.Puff.Landing(at);
            p.Scale = 1.05f; p.Life = 0.46f; p.Reach = 0.50f; p.Lift = 0.30f; p.Share = 0.85f;
            p.Lit = new Color(0.44f, 0.40f, 0.37f); p.Shade = new Color(0.28f, 0.25f, 0.23f); p.Line = new Color(0.16f, 0.14f, 0.13f);
            return p;
        }

        /// <summary>The skid where he plants: the same ash, smaller and quicker.</summary>
        public static CloudPuffField.Puff PlantPuff(Vector3 at)
        {
            var p = KickPuff(at);
            p.Scale = 0.80f; p.Life = 0.34f; p.Reach = 0.34f; p.Lift = 0.14f; p.Share = 0.7f; p.Turn = 1.3f;
            return p;
        }

        // On the bench: a beat of nothing, then the brace, the dash its full 2 m by the rule's own sum, and the plant.
        public override void Sample(float age, Vector3 eye)
        {
            float t = age - Lead, s = Mathf.Clamp(t - Brace, 0f, Travel);
            Pose(t, eye, Launch * s - 0.5f * Friction * s * s, t - Brace - Travel);
        }

        /// <summary>The distance the rule has him from where he left, `s` seconds after the launch.</summary>
        public static float Gone(float s) { s = Mathf.Clamp(s, 0f, Travel); return Launch * s - 0.5f * Friction * s * s; }

        /// <summary>
        /// Poses it `t` seconds after the press. `front` is how far he is from where he left (m), read from his body in a
        /// match, so the wake is only ever as long as he has gone and never ahead of him. `stopped` is how long ago he
        /// stopped (below nothing while he is still going).
        /// </summary>
        public void Pose(float t, Vector3 eye, float front, float stopped)
        {
            float since = t - Brace;
            bool cel = _look == Style.Cel;

            // ---- the stamp: heat gathers under his heel through the brace, cracks at their hottest as he leaves, cooling
            // by a rising cut (the outer ones first), then the char is eaten.
            float shown = t < 0f ? 0f : 1f;
            _char.Frame(t, shown, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.0f, Brace)), FxStudyKit.Run(t, 1.25f, 1.80f)));
            float cooled = FxStudyKit.Run(t, Brace + 0.20f, 1.40f);
            _pool.Frame(t, shown * 0.85f * FxStudyKit.Envelope(t, 0.0f, Brace * 0.8f, Brace + 0.10f, 0.45f));
            float cut = Mathf.Lerp(0.04f, 0.96f, Mathf.Pow(cooled, 0.8f));
            _cracks.Set("_Cut", cut, cut + 0.10f).Set("_Stops", cut + 0.12f, Mathf.Lerp(0.30f, 1.2f, cooled), Mathf.Lerp(0.70f, 1.6f, cooled))
                   .Set("_Emit", 2.0f * (1f - cooled)).Set("_Gain", Mathf.Lerp(2.2f, 0.9f, cooled));
            _cracks.Frame(t, shown, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(t, 0.0f, Brace * 0.7f)), Mathf.Max(0.2f * cooled, FxStudyKit.Run(t, 1.15f, 1.50f))));

            // ---- the flare: stands up round where he leaves in a few hundredths of a second, burns, and is eaten from its tips.
            float flare = FxStudyKit.Pop(FxStudyKit.Run(since, -0.02f, 0.06f), 0.18f), grow = 1f + 0.25f * FxStudyKit.EaseOut(FxStudyKit.Run(since, 0f, 0.5f));
            _crown.At.localPosition = new Vector3(0f, 0f, -0.05f);
            _crown.At.localScale = new Vector3(grow, Mathf.Max(0.01f, flare * Mathf.Lerp(1f, 0.6f, FxStudyKit.Run(since, 0.2f, 0.6f))), grow);
            _crown.Frame(t, cel || since < -0.02f ? 0f : 1f, Mathf.Pow(FxStudyKit.Run(since, 0.16f, 0.62f), 0.8f));

            // ---- the wake: stretched to exactly where he is; its tail let go behind him as he runs; eaten from the tail
            // once he has planted, the tongues sinking as they go.
            float reach = Mathf.Max(0.04f, front);
            float stand = FxStudyKit.Pop(FxStudyKit.Run(since, 0f, 0.07f), 0.15f) * Mathf.Lerp(1f, 0.55f, FxStudyKit.Ease(FxStudyKit.Run(stopped, 0.25f, 0.65f)));
            float tail = 0.45f * FxStudyKit.Ease(FxStudyKit.Run(since, 0.10f, Travel + 0.05f)) + 0.45f * FxStudyKit.Ease(FxStudyKit.Run(stopped, 0.30f, 0.62f));
            // It lingers a beat after he plants (about 0.3 s, at the owner's note on s3) before it is eaten.
            float eaten = Mathf.Pow(FxStudyKit.Run(stopped, 0.30f, 0.64f), 0.8f);
            for (int i = 0; i < 2; i++)
            {
                var piece = i == 0 ? _near : _far;
                piece.At.localScale = new Vector3(1f, stand * (i == 0 ? 1f : 0.92f + 0.08f * Mathf.Sin(t * 19f)), reach / Nominal);
                piece.Set("_Tile", 0.45f * reach, 0.85f, i * 0.37f, 0.15f);
                piece.Open(tail - (i == 0 ? 0f : 0.06f), 1.2f);
                piece.Frame(t, cel || since < 0f ? 0f : 1f, eaten);
            }

            // ---- the embers: the shader does all of it from the clock and the reach.
            if (_sparkLook != null) { _sparkLook.SetFloat("_Reach", reach); _sparkLook.SetFloat("_Stopped", stopped >= 0f ? t - stopped : 10000f); }
            _sparks.Frame(t, t < 0f || t > Life ? 0f : 1f);
            _streaks.Frame(t, !cel || t < Brace - 0.02f || t > Brace + Travel + 0.8f ? 0f : 1f);

            if (!cel) return;
            // ---- the ring: drawn tight round his rear foot through the brace, thrown out as he leaves, then eaten.
            float gather = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, Brace)), thrown = FxStudyKit.EaseOut(FxStudyKit.Run(since, 0f, 0.35f));
            _ring.At.localPosition = new Vector3(0f, 0.02f, -0.15f);
            _ring.At.localScale = Vector3.one * (Mathf.Lerp(0.75f, 0.55f, gather) + 0.85f * thrown);
            _ring.Frame(t, t < 0f ? 0f : gather, Mathf.Pow(FxStudyKit.Run(since, 0.12f, 0.45f), 0.8f));
            // ---- the fire and smoke: the field plays on this clock; it is drawn only while any of it can be alive.
            if (_flames != null)
            {
                float landedReach = stopped >= 0f ? reach : Nominal;
                if (Mathf.Abs(landedReach - _scheduledFor) > 0.01f) Schedule(landedReach);
                bool live = t >= Brace - 0.01f && t <= Brace + Travel + 1.4f;
                _flames.Show(live);
                if (live) _flames.Hold(t);
            }
        }

        // ---------------------------------------------------------------------------------------------------- meshes, once

        // An upright curtain down his path, 2 m long: low at the tail and tallest just behind him, its top laid back the
        // way the flame is dragged, rolled `roll` degrees off upright, `across` metres off his line.
        private static Mesh Curtain(float tall, float lean, float roll, float across)
        {
            const int along = 24, up = 6;
            int columns = up + 1;
            var vertices = new Vector3[(along + 1) * columns];
            var uvs = new Vector2[vertices.Length];
            var colours = new Color[vertices.Length];
            var turn = Quaternion.AngleAxis(roll, Vector3.forward);
            for (int r = 0; r <= along; r++)
            {
                float u = r / (float)along;
                // Rises from a low lick at the tail to its full height a hand behind him, rounding off at his back.
                float h = tall * Mathf.Lerp(0.28f, 1f, FxStudyKit.Ease(Mathf.Pow(u, 0.8f))) * Mathf.Lerp(0.82f, 1f, FxStudyKit.Ease(FxStudyKit.Run(1f - u, 0f, 0.10f)));
                for (int c = 0; c <= up; c++)
                {
                    float v = c / (float)up, y = -0.06f + v * h;
                    var p = turn * new Vector3(0f, y, 0f);
                    vertices[r * columns + c] = new Vector3(across + p.x, p.y, u * Nominal - lean * v * h);
                    uvs[r * columns + c] = new Vector2(u, v);
                    colours[r * columns + c] = Color.white;
                }
            }
            var triangles = new int[along * up * 6];
            int n = 0;
            for (int r = 0; r < along; r++)
                for (int c = 0; c < up; c++)
                {
                    int i = r * columns + c;
                    triangles[n++] = i; triangles[n++] = i + columns; triangles[n++] = i + 1;
                    triangles[n++] = i + 1; triangles[n++] = i + columns; triangles[n++] = i + columns + 1;
                }
            var mesh = new Mesh { name = "SeanStokeCurtain", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.colors = colours; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        // One ember: where along the dash it leaves (below 0, off his heel in the brace), a delay, its life, its size,
        // where from off that point, how thrown, how fast it tumbles. A dozen and a few, placed, not sprayed: most leave
        // in the first metre where the push is, and they are thrown back and up, away from his eyes.
        private readonly struct Ember
        {
            public readonly Vector4 A, B, C, D;
            public Ember(float share, float delay, float life, float size, float x, float y, float z, float vx, float vy, float vz, float spin, float seed, float needle = 0f, float thick = 0f)
            { A = new Vector4(share, delay, life, size); B = new Vector4(x, y, z, spin); C = new Vector4(vx, vy, vz, seed); D = new Vector4(needle, thick, 0f, 0f); }
        }

        // The needles of reference 1: thrown from the push-off outward to the back and both sides and up, never forward
        // of him; a few more shed back off him on the way. Long while fast, shortening as they slow, then gone.
        private static readonly Ember[] Needles =
        {
            new Ember(0f, 0.00f, 0.42f, 1f,  0.10f, 0.40f, -0.20f,  5.5f, 2.2f, -3.0f, 0f, 0f, 0.075f, 0.022f),
            new Ember(0f, 0.01f, 0.38f, 1f, -0.10f, 0.50f, -0.20f, -6.0f, 1.8f, -2.4f, 0f, 0f, 0.075f, 0.022f),
            new Ember(0f, 0.00f, 0.45f, 1f,  0.05f, 0.60f, -0.25f,  2.0f, 4.8f, -5.0f, 0f, 0f, 0.070f, 0.020f),
            new Ember(0f, 0.02f, 0.40f, 1f, -0.05f, 0.55f, -0.25f, -2.6f, 5.2f, -4.2f, 0f, 0f, 0.070f, 0.020f),
            new Ember(0f, 0.01f, 0.36f, 1f,  0.12f, 0.30f, -0.25f,  6.8f, 0.8f, -1.2f, 0f, 0f, 0.080f, 0.022f),
            new Ember(0f, 0.00f, 0.36f, 1f, -0.12f, 0.30f, -0.25f, -6.6f, 1.0f, -1.6f, 0f, 0f, 0.080f, 0.022f),
            new Ember(0f, 0.03f, 0.44f, 1f,  0.00f, 0.70f, -0.30f,  0.4f, 6.4f, -2.8f, 0f, 0f, 0.065f, 0.020f),
            new Ember(0f, 0.02f, 0.40f, 1f,  0.08f, 0.35f, -0.30f,  3.8f, 1.4f, -5.6f, 0f, 0f, 0.075f, 0.022f),
            new Ember(0f, 0.01f, 0.40f, 1f, -0.08f, 0.35f, -0.30f, -3.4f, 1.6f, -5.8f, 0f, 0f, 0.075f, 0.022f),
            new Ember(0f, 0.04f, 0.34f, 1f,  0.00f, 0.25f, -0.30f,  0.6f, 2.6f, -6.6f, 0f, 0f, 0.070f, 0.020f),
            new Ember(0.35f, 0.00f, 0.32f, 1f,  0.15f, 0.70f, 0f,  2.4f, 2.6f, -4.0f, 0f, 0f, 0.060f, 0.018f),
            new Ember(0.60f, 0.00f, 0.30f, 1f, -0.15f, 0.80f, 0f, -2.6f, 2.8f, -3.6f, 0f, 0f, 0.060f, 0.018f),
            new Ember(0.85f, 0.00f, 0.28f, 1f,  0.05f, 0.90f, 0f,  0.8f, 3.4f, -3.2f, 0f, 0f, 0.060f, 0.018f),
        };
        private static readonly Ember[] Embers =
        {
            // Off his fists in the brace, two a side, lifting up and back: the stoke. Beside him, never before his eyes.
            new Ember(-1f, 0.02f, 0.50f, 0.060f,  0.44f, 1.00f, -0.05f,  0.3f, 1.6f, -0.7f,  9f, 0.11f),
            new Ember(-1f, 0.07f, 0.46f, 0.050f,  0.40f, 1.05f, -0.10f,  0.5f, 1.9f, -0.5f, -7f, 0.23f),
            new Ember(-1f, 0.04f, 0.50f, 0.060f, -0.44f, 1.00f, -0.05f, -0.3f, 1.7f, -0.7f, 11f, 0.37f),
            new Ember(-1f, 0.10f, 0.44f, 0.050f, -0.40f, 1.05f, -0.10f, -0.5f, 1.8f, -0.5f, -8f, 0.49f),
            new Ember(-1f, 0.05f, 0.42f, 0.050f,  0.08f, 0.06f, -0.20f,  0.3f, 2.0f, -0.6f, 10f, 0.31f),
            // Shed along the dash as he passes, thrown back and up.
            new Ember(0.05f, 0.00f, 0.62f, 0.098f,  0.18f, 0.45f, 0f,  0.7f, 2.4f, -2.2f,  8f, 0.41f),
            new Ember(0.12f, 0.01f, 0.55f, 0.077f, -0.20f, 0.70f, 0f, -0.8f, 2.0f, -1.8f, -9f, 0.53f),
            new Ember(0.20f, 0.00f, 0.70f, 0.112f,  0.05f, 0.95f, 0f,  0.2f, 2.8f, -2.6f,  6f, 0.67f),
            new Ember(0.30f, 0.02f, 0.50f, 0.070f, -0.12f, 0.35f, 0f, -0.5f, 1.6f, -1.4f, 12f, 0.71f),
            new Ember(0.40f, 0.00f, 0.66f, 0.091f,  0.22f, 0.80f, 0f,  0.9f, 2.2f, -2.0f, -8f, 0.83f),
            new Ember(0.52f, 0.01f, 0.58f, 0.084f, -0.15f, 1.05f, 0f, -0.6f, 2.5f, -1.6f,  7f, 0.97f),
            new Ember(0.64f, 0.00f, 0.52f, 0.063f,  0.10f, 0.55f, 0f,  0.4f, 1.9f, -1.2f, 10f, 0.13f),
            new Ember(0.78f, 0.01f, 0.60f, 0.098f, -0.05f, 0.90f, 0f, -0.2f, 2.6f, -1.0f, -6f, 0.29f),
            new Ember(0.90f, 0.00f, 0.48f, 0.070f,  0.16f, 0.60f, 0f,  0.6f, 2.0f, -0.8f,  9f, 0.47f),
            // Two thrown off as he plants, low and short.
            new Ember(1.00f, 0.03f, 0.40f, 0.063f,  0.20f, 0.10f, -0.1f,  1.2f, 1.4f, -0.4f, 13f, 0.59f),
            new Ember(1.00f, 0.05f, 0.36f, 0.056f, -0.22f, 0.10f, -0.1f, -1.1f, 1.2f, -0.5f, -12f, 0.61f),
        };

        private static Mesh EmberMesh(Ember[] embers, string name)
        {
            var points = new List<Vector3>(); var normals = new List<Vector3>(); var faces = new List<int>();
            var a = new List<Vector4>(); var b = new List<Vector4>(); var c = new List<Vector4>(); var dd = new List<Vector4>();
            Vector3[] sides = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var ember in embers)
                foreach (var n in sides)
                {
                    // A unit cube, half a unit each way: four corners a face, so each face is one flat painted tone.
                    Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up, w = Vector3.Cross(n, u);
                    int first = points.Count;
                    foreach (var corner in new[] { -u - w, u - w, u + w, -u + w })
                    { points.Add((n + corner) * 0.5f); normals.Add(n); a.Add(ember.A); b.Add(ember.B); c.Add(ember.C); dd.Add(ember.D); }
                    faces.Add(first); faces.Add(first + 1); faces.Add(first + 2);
                    faces.Add(first); faces.Add(first + 2); faces.Add(first + 3);
                }
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(points); mesh.SetNormals(normals); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetUVs(2, c); mesh.SetUVs(3, dd);
            mesh.SetTriangles(faces, 0, false);
            // The shader moves every corner: the bounds are the whole of where an ember can be.
            mesh.bounds = new Bounds(new Vector3(0f, 1f, 0.5f), new Vector3(5f, 4f, 7f));
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>What it costs to draw: one draw call a piece that is on, and the triangles they submit.</summary>
        public void Cost(out int calls, out int triangles)
        {
            calls = 0; triangles = 0;
            foreach (var renderer in Root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!renderer.enabled || renderer.forceRenderingOff) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                // A material with two passes (the cloud's balls and their line) is two draws of its whole mesh.
                int passes = renderer.sharedMaterial != null ? Mathf.Max(1, renderer.sharedMaterial.passCount) : 1;
                calls += passes; triangles += passes * (int)(filter.sharedMesh.GetIndexCount(0) / 3);
            }
        }
    }
}

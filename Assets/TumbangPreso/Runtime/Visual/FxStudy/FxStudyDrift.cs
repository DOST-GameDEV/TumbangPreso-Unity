using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY H, THE DRIFT (a first rough sample). Amihan throws herself 5 m forward in about a third of a second
    /// (`AmihanRules.QuickDashDistance` at `QuickDashSpeed`), and anyone within 0.9 m of that line is shoved and Whirled.
    /// So the picture is a straight streak 5 m long and about 1.8 m wide, there for the dash and a short tail after it.
    ///
    /// Like the whirlwind it is the bench's own effects, called as they are, in the whirlwind's colours
    /// (`FxStudyWhirlwind.Look.Picture`) so her kit reads as one hero:
    ///   * THE STREAK is three of the approved trail ribbons (`FxStudyKit.Sweep`, `FxStudyTrails.Dress`) swept along the
    ///     dash, wound round her line of travel, drawn on with her and let go from the tail;
    ///   * THE KICK at launch is the wisp's dish streaks (`FxStudyWisp.DressDishStreaks`) on a low ring thrown outward;
    ///   * THE BODY is the whirlwind's noise skin laid on its side as a horn along the dash, her at its mouth, with a
    ///     blunt cone of the same leading it; where she stops the streak curls up and unwinds;
    ///   * THE MOTES are the bench's glow, shed along the line.
    /// Nothing is drawn in front of her or on her line of sight: the mass is behind and beside.
    /// </summary>
    public sealed class FxStudyDrift : FxStudyEffect
    {
        private const float Far = 5.0f, Dash = 5.0f / 14.0f, High = 0.95f;

        // One ribbon of the streak: which trail it wears, how far off her line it winds (m), where round the line it
        // starts and how far it turns by the end (degrees), how broad it is (m), and how long after her its tail lets go (s).
        private readonly struct Streak
        {
            public readonly int Trail; public readonly float Out, From, Turn, Broad, Hold;
            public Streak(int trail, float out_, float from, float turn, float broad, float hold) { Trail = trail; Out = out_; From = from; Turn = turn; Broad = broad; Hold = hold; }
        }
        private static readonly Streak[] Streaks =
        {
            new Streak(4, 0.32f,  20f, 300f, 0.66f, 0.30f),
            new Streak(2, 0.38f, 150f, 250f, 0.56f, 0.22f),
            new Streak(4, 0.28f, 265f, 360f, 0.46f, 0.38f),
        };

        private readonly FxPiece[] _streak = new FxPiece[Streaks.Length];
        private readonly FxPiece _kick, _body, _bow;
        private readonly Transform _lead;
        private readonly bool _showLead;
        private readonly FxFlecks _motes;

        // ⚠️ ITS MESHES ARE MADE ONCE FOR THE PROCESS AND SHARED BY EVERY CAST (`AddShared`): they are the same every time,
        // and making them was most of what a cast cost (the owner: "remember this needs to be heavily optimized").
        private static readonly System.Collections.Generic.Dictionary<string, Mesh> Meshes = new System.Collections.Generic.Dictionary<string, Mesh>();
        private static Mesh Once(string key, System.Func<Mesh> make)
        {
            if (Meshes.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            mesh = make();
            if (mesh != null) mesh.hideFlags = HideFlags.HideAndDontSave;
            Meshes[key] = mesh;
            return mesh;
        }


        public override float Seconds => 1.4f;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override float BloomIntensity => 0.30f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            focus = new Vector3(0f, 0.9f, Far * 0.48f);
            eye = focus + new Vector3(8.2f, 3.4f, -4.6f);
            fov = 34f;
        }

        public FxStudyDrift(Transform parent) : this(parent, -1f, true) { }

        /// <summary>For a cast in a match: `light` is how much of it is light laid on (below 0 is the bench's), and `lead` off
        /// leaves out the cone that leads it, which in a match would be drawn round the caster's own eyes.</summary>
        public FxStudyDrift(Transform parent, float light, bool lead) : base("drift", parent)
        {
            var look = FxStudyWhirlwind.Look.Picture;
            if (light >= 0f) look.Light = light;
            _showLead = lead;
            for (int i = 0; i < Streaks.Length; i++)
            {
                Streak s = Streaks[i];
                var knots = new Knot[9];
                for (int k = 0; k < knots.Length; k++)
                {
                    float along = k / (float)(knots.Length - 1), turn = (s.From + s.Turn * along) * Mathf.Deg2Rad;
                    float broad = s.Broad * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.06f, 0.97f, along))), 0.5f) + 0.01f;
                    // Where she stops the streak does not end: it curls up and opens out, and unwinds there.
                    float curl = FxStudyKit.Ease(FxStudyKit.Run(along, 0.80f, 1f)), out_ = s.Out * (1f + 0.9f * curl);
                    knots[k] = new Knot(new Vector3(Mathf.Cos(turn) * out_, High + Mathf.Sin(turn) * out_ * 0.8f + 0.7f * curl, Far * (along - 0.10f * curl)), broad, s.From + s.Turn * along);
                }
                var piece = AddShared("streak-" + i, Once("streak-" + i, () => FxStudyKit.Sweep(knots, Vector3.up, perKnot: 8, across: 6)), FxStudyKit.Ribbon(FxStudyTrails.MaskOf(s.Trail)), 10 + i);
                FxStudyTrails.Dress(piece, s.Trail).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo)
                             .Set("_Tile", 1.5f, 1f, i * 0.37f, 0f).Set("_Add", Mathf.Min(0.7f, look.Light)).Set("_Hue", look.HueDrift, 6f, i * 0.37f, 1f).WindowSoft(0.08f);
                _streak[i] = piece;
            }
            // THE BODY: the whirlwind's own noise skin, laid on its side as a horn along the dash with her at its mouth.
            // ⚠️ A STREAK, NOT A BARREL. At 1.7 m across the owner, casting it: "the drift looks like a barrel, make it a bit
            // thinner like a streak". It is 0.84 m across at her and tapers to nothing at its tail, six times as long as wide.
            var lie = Group("lie");
            lie.localPosition = new Vector3(0f, High, 0f);
            lie.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _body = AddShared("body", Once("body", () => FxStudyKit.Shell(new[] { new Vector2(0.04f, 0f), new Vector2(0.16f, Far * 0.3f), new Vector2(0.28f, Far * 0.65f), new Vector2(0.38f, Far * 0.9f), new Vector2(0.42f, Far) }, 0f, 360f, 200f, 40, 40)),
                        FxStudyKit.Ribbon("noise_simplex_cells", "noise_simplex_cells"), 5, lie);
            _body.Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Tile", 1f, 1f, 0.2f, 0.1f).Set("_Scroll", 1.6f, 0.9f)
                 .Set("_Cut", 0.04f, 0.80f).Set("_Stops", 0.28f, 0.58f, 0.90f).Set("_Ends", 0.10f, 0.10f, 0f)
                 .Set("_ErodeTile", 1f, 1f, 0.2f, 0.1f).Set("_ErodeScroll", 1.6f, 0.9f).Set("_Sweep", 1.2f).Set("_Soft", 0.2f)
                 .Set("_Halo", 0f).Set("_Emit", 0.45f).Set("_Add", look.Light).Set("_Hue", look.HueDrift, 4f, 0.5f, 1f).WindowSoft(0.08f);
            // THE LEADING EDGE: a short blunt cone of the same noise that travels with her, so the eye knows which end leads.
            _lead = Group("lead");
            _bow = AddShared("bow", Once("bow", () => FxStudyKit.Shell(new[] { new Vector2(0.45f, 0f), new Vector2(0.40f, 0.25f), new Vector2(0.26f, 0.45f), new Vector2(0.08f, 0.60f) }, 0f, 360f, 120f, 16, 40)),
                       FxStudyKit.Ribbon("noise_simplex_cells", "noise_simplex_cells"), 6, _lead);
            _bow.Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Tile", 0.4f, 1f, 0.6f, 0.3f).Set("_Scroll", -1.4f, 1.2f)
                .Set("_Cut", 0.04f, 0.70f).Set("_Stops", 0.28f, 0.55f, 0.88f).Set("_Ends", 0.20f, 0.30f, 0f)
                .Set("_Halo", 0f).Set("_Emit", 0.5f).Set("_Add", look.Light).Set("_Hue", look.HueDrift, 4f, 0.9f, 1f);
            // THE KICK: small, of the same teal and streaks, and dragged forward into the streak.
            _kick = AddShared("kick", Once("kick", () => FxStudyKit.Revolve(new[] { new Vector2(0.15f, 0.03f), new Vector2(0.45f, 0.03f), new Vector2(0.80f, 0.03f), new Vector2(1.0f, 0.03f) }, 48)),
                        FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 2);
            FxStudyWisp.DressDishStreaks(_kick).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Add", look.Light).Set("_Ends", 0f, 0f, 0.32f).Set("_Hue", look.HueDrift, 4f, 0.3f, 1f);

            _motes = AddFlecks("motes", 40, "glow_soft");
            _motes.Piece.Colours(look.MoteHalo * 0.6f, look.MoteHalo, look.MoteHeart, look.MoteHalo).Set("_Cut", 0.04f, 0.55f).Set("_Stops", 0.30f, 0.55f, 0.92f).Set("_Gain", 1.5f);
        }

        // On the bench: a beat of nothing first, so the launch is seen; then she covers the whole length in the dash's time.
        public override void Sample(float age, Vector3 eye) => Pose(age - 0.15f, eye, FxStudyKit.Run(age - 0.15f, 0f, Dash), age - 0.15f - Dash);

        /// <summary>
        /// Poses it `t` seconds after the dash began. ⚠️ IT IS ONLY EVER AS LONG AS SHE HAS GONE. `reach` is how far she is
        /// from where she started, as a share of the 5 m it is built to, and the whole of it is drawn out to exactly that:
        /// its front is at her, nothing of it is ahead of her, and its pictures are laid as many times along it as its
        /// length asks, so a short dash is a short streak and not a long one squashed. `stopped` is how long ago she
        /// stopped (below nothing while she is still going): the curl and the unwinding happen where she is then.
        /// (The owner, of the first one, which ran its 5 m in the dash's time whatever she did: "it should also only go as
        /// far as amihan moves".)
        /// </summary>
        public void Pose(float t, Vector3 eye, float reach, float stopped)
        {
            float age = Mathf.Max(0f, t), gone = 1f, landed = stopped;
            reach = Mathf.Max(0.02f, reach);
            Root.localScale = new Vector3(1f, 1f, reach);
            for (int i = 0; i < _streak.Length; i++)
            {
                float tail = FxStudyKit.Ease(FxStudyKit.Run(landed, Streaks[i].Hold - 0.30f, Streaks[i].Hold + 0.15f));
                _streak[i].Open(tail * 1.1f - 0.1f, gone * 1.08f);
                _streak[i].Set("_Tile", 1.5f * reach, 1f, i * 0.37f, 0f);
                _streak[i].Frame(age, t < 0f ? 0f : 0.9f, 0.9f * FxStudyKit.Run(landed, 0.05f, 0.55f));
            }
            _body.Open(FxStudyKit.Ease(FxStudyKit.Run(landed, -0.15f, 0.35f)) * 1.1f - 0.1f, gone * 1.08f);
            _body.Set("_Tile", reach, 1f, 0.2f, 0.1f).Set("_ErodeTile", reach, 1f, 0.2f, 0.1f);
            _body.Frame(age, t < 0f ? 0f : 0.8f, 0.9f * FxStudyKit.Run(landed, 0f, 0.50f));
            _lead.localScale = new Vector3(1f, 1f / reach, 1f);
            _lead.localPosition = new Vector3(0f, High, Far - 0.15f / reach);
            _lead.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _bow.Frame(age, t < 0f || !_showLead ? 0f : 0.85f * (1f - FxStudyKit.Run(landed, 0f, 0.18f)), 0f);
            float thrown = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, 0.26f));
            // (The kick is on the ground at her start and is not drawn out with the streak; it never reaches past her.)
            float drag = Mathf.Min(0.7f * thrown, Far * reach * 0.6f);
            _kick.At.localPosition = new Vector3(0f, 0f, drag / reach);
            _kick.At.localScale = new Vector3(Mathf.Lerp(0.2f, 0.6f, thrown), 1f, Mathf.Lerp(0.2f, 0.9f, thrown) / reach);
            _kick.At.localRotation = Quaternion.Euler(0f, -age * 200f, 0f);
            _kick.Frame(age, t < 0f ? 0f : 0.7f, Mathf.Max(0.35f, FxStudyKit.Run(t, 0.08f, 0.45f)));

            _motes.Begin(eye);
            for (int i = 0; i < 6; i++)
            {
                float along = (i + FxStudyKit.Hash(i, 3)) / 6f, born = along * 0.2f, life = FxStudyKit.Run(landed, born - 0.2f, born + 0.45f);
                if (life <= 0f) continue;
                float turn = FxStudyKit.Hash(i, 5) * 6.2832f;
                var at = new Vector3(Mathf.Cos(turn) * 0.35f, High + Mathf.Sin(turn) * 0.30f + 0.5f * life, Far * along * 0.95f);
                _motes.Add(at, new Vector3(0f, 0.6f, 1f), 0.42f, 0.22f, Mathf.Sin(Mathf.PI * life));
            }
            _motes.End(age);
        }
    }
}

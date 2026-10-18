using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY A, THE FIVE TRAIL RIBBONS. Five ribbons rise side by side, each from the square greyscale mask that drives
    /// it, which stands at its foot and scrolls in step with it. This is the plainest statement of the whole technique
    /// and the one the first pass missed: THE PICTURE IS THE SHAPE. All five ribbons are the same kind of mesh (a strip
    /// that widens from its foot) with the same colours; the only thing that differs between a faceted glassy flame, a
    /// billow of smoke, a ribbon of loops with holes in it, a torn calligraphic one and a silk S is which picture it
    /// wears.
    ///
    /// The colour is a ramp through the picture: where the mask is dark grey the ribbon is clear violet, mid grey is
    /// magenta-violet, white is pink-white and nearly solid, and the bright parts carry a violet halo.
    ///
    /// It loops every four seconds: every scroll rate below is a whole number of quarter turns a second.
    /// </summary>
    public sealed class FxStudyTrails : FxStudyEffect
    {
        // ⚠️ THE REFERENCE'S CAMERA, SOLVED FROM ITS FLOOR (2026-10-10). Its grid's receding lines meet 640 px above the
        // middle of a 720 px frame and its cross lines fall at rows that only one camera produces: a vertical field of
        // view of 29.5 degrees, pitched down 25.08, standing 3.405 floor squares high, with a cross line 7.483 squares
        // ahead of it. The boards are 149 px squares whose feet rest on the floor 5.033 squares ahead, which makes a board
        // 0.654 of a square: so a 0.8 m board fixes the square at 1.2232 m and everything else follows in metres. The
        // first films guessed this camera by eye and every comparison made through it was of a different picture.
        private const float Board = 0.8f, Cell = 1.2232f, Pitch = 25.08f, Fov = 29.5f;
        private static readonly Vector3 Eye = new Vector3(0f, 3.405f * Cell, -5.033f * Cell);
        private static readonly Vector3 Forward = new Vector3(0f, -Mathf.Sin(Pitch * Mathf.Deg2Rad), Mathf.Cos(Pitch * Mathf.Deg2Rad));

        // Sampled from the reference: its white-pink core is (236, 190, 244), its body (150, 85, 172) over a grey floor,
        // its faint edge (105, 78, 118). Pinker than the first films' violet, and the body far more see-through.
        private static readonly Color Deep = new Color(0.60f, 0.27f, 0.74f), Body = new Color(0.84f, 0.34f, 0.97f),
                                      Hot = new Color(0.95f, 0.77f, 0.98f), Halo = new Color(0.58f, 0.22f, 0.74f);

        private struct Trail { public FxPiece Ribbon, Board, Foot; public float Scroll, Sway; }
        private readonly Trail[] _trails = new Trail[5];

        public override float Seconds => 4f;
        public override bool ChequeredFloor => true;
        public override float FloorCell => Cell;
        // A gentler bloom than the bench's usual: at full strength its white spill washed the violet out of the bodies
        // (measured: body share fell to half the reference's).
        public override float BloomIntensity => 0.16f;
        public override Vector2 FloorOffset => new Vector2(0f, (7.483f - 5.033f) * Cell);
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov) { eye = Eye; focus = Eye + Forward; fov = Fov; }

        // One ribbon of the five: its name, the mask it wears, where its board stands (m across), how fast its picture
        // climbs (pictures a second), how far it sways (degrees), its knots, and its cut, stops and gain.
        private readonly struct Plan
        {
            public readonly string Name, Mask; public readonly float X, Scroll, Sway, CutFrom, CutTo, Body, HotFrom, HotTo, Gain; public readonly Vector3[] Knots;
            public Plan(string name, string mask, float x, float scroll, float sway, Vector3[] knots, float cutFrom, float cutTo, float body, float hotFrom, float hotTo, float gain = 1.2f)
            { Name = name; Mask = mask; X = x; Scroll = scroll; Sway = sway; Knots = knots; CutFrom = cutFrom; CutTo = cutTo; Body = body; HotFrom = hotFrom; HotTo = hotTo; Gain = gain; }
        }

        // ⚠️ KEEP THIS TABLE. Every plan, cut and stop below, the five masks they read, the solved camera above and this
        // effect's own bloom (0.16) are the trails film t32, of which the owner said on 2026-10-11: "trails looks good".
        // They are backed up in `tools/cutscene_rework/approved/trails_t32/`. A change to anything the trails share with
        // another effect (`FxStudyRibbon.shader`, the bench's bloom) has to leave this film as it was: film it again and
        // lay it against the kept frames (`tools/cutscene_rework/fxsame.py trails <tag>`) before going on.
        // (The table stood inside the constructor until the whirlwind study needed to wear these same ribbons; it was
        // lifted out as it was, no number changed, so that `Dress` can hand the approved ribbon to another mesh.)
        // The boards stand 296 px and 533 px either side of the middle in the reference: 1.589 m and 2.861 m here.
        // Each ribbon's knots are (across, up, width) in metres in that plane, its tip where the reference's is.
        private static readonly Plan[] Plans =
        {
            // 1. The caustic: a wide fan of folding light leaning in from the left.
            new Plan("shards", "trail_shards", -2.861f, 0.75f, 2.5f,
                     new[] { K(0f, 0f, .27f), K(.35f, .62f, .60f), K(.74f, 1.30f, .74f), K(1.12f, 2.00f, .30f) },
                     cutFrom: .15f, cutTo: .80f, body: .44f, hotFrom: 0.57f, hotTo: 0.85f),
            // 2. The pleated ribbon.
            new Plan("smoke", "trail_smoke", -1.589f, 0.75f, -2.0f,
                     new[] { K(0f, 0f, .28f), K(.24f, .65f, 1.10f), K(.52f, 1.32f, 1.30f), K(.79f, 2.02f, .40f) },
                     cutFrom: .06f, cutTo: .70f, body: .38f, hotFrom: 0.49f, hotTo: 0.73f),
            // 3. The twisted ribbon, slow and broad.
            new Plan("ink", "trail_ink", 0f, 0.75f, 1.6f,
                     new[] { K(0f, 0f, .20f), K(-.02f, .62f, .46f), K(.02f, 1.24f, .63f), K(0f, 1.90f, .28f) },
                     cutFrom: .06f, cutTo: .62f, body: .34f, hotFrom: 0.49f, hotTo: 0.75f),
            // 4. The twisted ribbon, quick and thin.
            new Plan("squiggle", "trail_squiggle", 1.589f, 0.75f, -1.8f,
                     new[] { K(0f, 0f, .15f), K(-.22f, .60f, .50f), K(-.52f, 1.22f, .70f), K(-.80f, 1.90f, .32f) },
                     cutFrom: .06f, cutTo: .90f, body: .44f, hotFrom: 0.63f, hotTo: 0.89f, gain: 1.5f),
            // 5. The owner: "the 5th is one ribbon, fold applied, copied as is, then shifted down". The mask is far fuller
            // than the pair of thin ribbons it replaces, so its ramp sits higher: the stops are where two fifths of what
            // shows comes out core and a quarter faint, as measured on the reference's fifth ribbon.
            new Plan("bands", "trail_bands", 2.861f, 0.75f, 2.2f,
                     new[] { K(0f, 0f, .25f), K(-.36f, .62f, .59f), K(-.79f, 1.30f, .82f), K(-1.19f, 2.02f, .28f) },
                     cutFrom: .05f, cutTo: .70f, body: .41f, hotFrom: 0.57f, hotTo: 0.82f),
        };

        /// <summary>The mask ribbon `index` (0 to 4) wears.</summary>
        public static string MaskOf(int index) => Plans[index].Mask;

        public FxStudyTrails(Transform parent) : base("trails", parent)
        {
            // Every ribbon and board stands in a plane square to the camera, as the reference's do.
            Vector3 forward = Forward, right = Vector3.right, up = Vector3.Cross(forward, right);
            for (int i = 0; i < Plans.Length; i++) Build(i, right, up, forward);
        }

        // A knot in a ribbon's own plane: across (m), up (m), width (m).
        private static Vector3 K(float across, float rise, float width) => new Vector3(across, rise, width);

        private void Build(int index, Vector3 right, Vector3 up, Vector3 forward)
        {
            Plan p = Plans[index];
            string name = p.Name, mask = p.Mask; float x = p.X, scroll = p.Scroll, sway = p.Sway; Vector3[] plan = p.Knots;
            Vector3 centre = new Vector3(x, 0f, 0f) + up * (Board * 0.5f), foot = centre + up * (Board * 0.5f);

            // The board: the mask itself, plain, scrolling as the ribbon's does.
            var plain = Shader.Find("Unlit/Texture");
            Material shown = plain != null ? new Material(plain) { hideFlags = HideFlags.HideAndDontSave } : null;
            if (shown != null) shown.mainTexture = FxStudyKit.Mask(mask);
            var board = Add("board-" + name, FxStudyKit.Square(false), shown, 1);
            board.At.localPosition = centre;
            board.At.localRotation = Quaternion.LookRotation(forward, up);
            board.At.localScale = Vector3.one * Board;

            var knots = new FxStudyKit.Knot[plan.Length];
            for (int i = 0; i < plan.Length; i++) knots[i] = new FxStudyKit.Knot(right * plan[i].x + up * plan[i].y, plan[i].z);
            // A little cooler toward the tip, as the reference's ribbons are.
            Color Tint(float u) => Color.Lerp(Color.white, new Color(0.86f, 0.88f, 1f, 1f), FxStudyKit.Ease(FxStudyKit.Run(u, 0.55f, 1f)));
            var ribbon = Add("ribbon-" + name, FxStudyKit.Sweep(knots, -forward, perKnot: 14, across: 10, tint: Tint), FxStudyKit.Ribbon(mask), 10 + index);
            ribbon.At.localPosition = foot;
            Dress(ribbon, index);

            // The foot burns: every ribbon is narrowest and brightest where it leaves its board.
            var glow = AddGlow("foot-" + name, 30 + index, new Color(1f, 0.85f, 1f), new Color(0.62f, 0.18f, 1f));
            glow.At.localPosition = foot + up * 0.10f - forward * 0.02f;
            glow.At.localRotation = Quaternion.LookRotation(forward, up);
            glow.At.localScale = new Vector3(0.30f, 0.50f, 1f);

            _trails[index] = new Trail { Ribbon = ribbon, Board = board, Foot = glow, Scroll = index == 3 ? 1.0f : scroll, Sway = sway };
        }

        /// <summary>
        /// Dresses a piece as ribbon `index` (0 to 4) of the five: the approved colours, cut, stops, hue drift, torn tip
        /// and light. The piece must have been made with that ribbon's mask (`MaskOf`). The bench's own five are dressed
        /// by this, and so is anything else that is to wear one of them.
        /// </summary>
        public static FxPiece Dress(FxPiece ribbon, int index)
        {
            Plan p = Plans[index];
            float scroll = p.Scroll, cutFrom = p.CutFrom, cutTo = p.CutTo, body = p.Body, hotFrom = p.HotFrom, hotTo = p.HotTo, gain = p.Gain;
            ribbon.Colours(Deep, Body, Hot, Halo)
                  // The fourth is squeezed along its length (the owner: "compress 4th a bit more along the x axis"): the same approved
                  // mask at four thirds the tiling, scrolled a third faster so it still climbs at the measured speed and still
                  // loops in four seconds. Its drawing is not touched.
                  .Set("_Tile", index == 3 ? 4f / 3f : 1f, 1f, index * 0.37f, 0f).Set("_Scroll", index == 3 ? 1.0f : scroll, 0f)
                  // No ribbon is one flat violet (the owner: "hints of purple and pink ... some hue randomized offsetter"):
                  // each drifts by a slow noise from its own seed, and leans pinker where it is bright.
                  // The fifth is so much one even veil that the same drift read half again wider on it than the reference's
                  // (33 degrees against 23, measured), so it drifts less.
                  .Set("_Hue", index == 4 ? 33f : 46f, index == 4 ? 17f : 24f, index * 0.37f + 0.1f, 1f)
                  .Set("_Cut", cutFrom, cutTo).Set("_Stops", body, hotFrom, hotTo)
                  // A long fade at the tip: the fade thins the MASK, so the tip ends in the picture's own shapes and not in a line.
                  .Set("_Ends", 0.04f, 0.12f, 0.06f)
                  // The tip does not stop, it tears: the erosion map drifts up it and eats the far end first.
                  .Set("_ErodeTile", 1.5f, 1f, index * 0.21f, 0f).Set("_ErodeScroll", 0.75f, 0f).Set("_Sweep", -1.1f).Set("_Soft", 0.40f)
                  // The reference is LIGHT: its bright parts are blown to pink-white and bleed. So the piece is mostly added light,
                  // its mid greys stay see-through (`_Curve`) and only the mask's peaks turn solid.
                  .Set("_Burn", 0f)// ⚠️ UNDER A BLOOMING CAMERA THE TRAILS KEEP THEIR OWN HALO. The first film with bloom dropped it and pushed every
                  // bright part far over white: the ribbons went thin and wiry with blown cores, and every measured share left
                  // the reference (core three times over). A ribbon's halo is its faint edge, which is a measured part of its
                  // look; so it stays, and only its peaks go a little over white for the bloom to spill.
                  .Set("_Halo", 0.70f).Set("_Add", 0.55f).Set("_Gain", gain).Set("_Curve", 2.0f).Set("_KeepHalo", 1f).Set("_Emit", 0.05f);
            return ribbon;
        }

        public override void Sample(float age, Vector3 eye)
        {
            float turn = age / Seconds * Mathf.PI * 2f;
            Vector3 forward = Forward;
            for (int i = 0; i < _trails.Length; i++)
            {
                var trail = _trails[i];
                if (trail.Ribbon == null) continue;
                // A flame is never still: each leans a degree or two about its foot, out of step with its neighbours.
                trail.Ribbon.At.localRotation = Quaternion.AngleAxis(Mathf.Sin(turn + i * 1.3f) * trail.Sway, forward);
                trail.Ribbon.Frame(age, 1f, 0f);
                trail.Board.Frame(age, 1f);
                if (trail.Board.Material != null) trail.Board.Material.mainTextureOffset = new Vector2(-(age * trail.Scroll + i * 0.37f), 0f);
                trail.Foot.Frame(age, 0.18f + 0.04f * Mathf.Sin(turn * 3f + i * 2.1f));
            }
        }
    }
}

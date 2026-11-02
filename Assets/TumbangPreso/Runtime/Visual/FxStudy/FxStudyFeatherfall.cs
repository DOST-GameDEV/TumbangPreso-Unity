using TumbangPreso.Core;
using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY J, FEATHERFALL (a first sample). Amihan is thrown 2.8 m up in under half a second (`AmihanRules.UpdraftHeight`,
    /// `UpdraftRiseSeconds`), held there for up to 5 s while she moves and throws, and lets herself down at 3.5 m a second
    /// when she presses again, grabs, or the time runs out. So the picture has three beats: the GUST that throws her up,
    /// the AIR THAT HOLDS HER, which has to say how long it will go on holding her (coming down early is her choice, so
    /// she and everyone else should be able to read the time left), and the LANDING.
    ///
    /// Like the whirlwind and the drift, it is the bench's own approved pieces in the whirlwind's colours
    /// (`FxStudyWhirlwind.Look.Picture`, hue wandering between green and blue), so her kit reads as one hero:
    ///   * THE HOLD is the whirlwind's noise horn turned upside down: a narrow updraft from the court that narrows into
    ///     her soles and flares a little under them, two skins turning at different speeds, its cells climbing. It is
    ///     drawn to whatever the gap under her is, and thins as she comes down;
    ///   * THE TIME LEFT is a ring of trail ribbon lying flat under her soles, its band running round, and it closes as
    ///     the seconds go: a full ring at take-off, nothing when the hold is over. A faint full ring stays behind it so
    ///     the gone part reads as gone. It is under her, so from below, where the others see her, it is the thing they see;
    ///   * TWO TAPES of trail ribbon wind up round her from her soles to her waist, never higher: nothing in front of her
    ///     own eyes;
    ///   * THE GUST at take-off is a short wide horn of the same noise blown up out of the court with the wisp's dish
    ///     streaks thrown out round its foot, and a puff of the cloud (`CloudPuffField`) kicked out of the court;
    ///   * THE LANDING is the dish streaks thrown out again and a puff of the cloud.
    ///
    /// ⚠️ HEAVILY OPTIMISED, as the owner asked of all of it: every mesh is made once, every motion is the shader's
    /// (`_Age`, the open window, the tiling) or a transform, and the dust is the pooled cloud puffs. Nothing is rebuilt per
    /// frame and there are no sparks (the bench's sparks rebuild a mesh every frame).
    /// </summary>
    public sealed class FxStudyFeatherfall : FxStudyEffect
    {
        private const float Lead = 0.25f, Hold = AmihanRules.UpdraftSeconds, Rise = AmihanRules.UpdraftRiseSeconds,
            High = AmihanRules.UpdraftHeight, Down = AmihanRules.UpdraftDescentSpeed;
        /// <summary>How long the take-off's gust and the landing's dust last.</summary>
        public const float GustLife = 0.8f, LandLife = 0.7f;
        private static float Descent => High / Down;

        private readonly FxPiece _skin, _skinBack, _clock, _track, _gust, _kick, _landKick;
        private readonly FxPiece[] _tape = new FxPiece[2];
        private readonly Transform _column, _ring, _tapes, _launch, _land;
        private readonly CloudPuffField _benchPuffs;

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

        private readonly bool _below;

        public override float Seconds => Lead + Rise + Hold + Descent + LandLife;
        public override Color FloorColour => new Color(0.150f, 0.150f, 0.158f);
        public override Color FloorLine => new Color(0.120f, 0.120f, 0.128f);
        public override float BloomIntensity => 0.30f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            // From the court, a little below her held height and to one side, as a player near her sees her; or (`_below`)
            // from a player standing close under her, looking up, which is where the time left has to be read.
            if (_below) { focus = new Vector3(0f, 2.7f, 0f); eye = new Vector3(1.2f, 0.4f, -1.5f); fov = 60f; return; }
            focus = new Vector3(0f, 1.9f, 0f); eye = focus + new Vector3(5.2f, -0.4f, -6.6f); fov = 38f;
        }

        public FxStudyFeatherfall(Transform parent) : this(parent, -1f, true) { }

        /// <summary>`light` is how much of it is light laid on (below 0 is the bench's); `bench` stands a stand-in for her and
        /// makes the bench's own cloud puffs. In a match the caller spawns the match's puffs (`CloudPuffs`).</summary>
        public FxStudyFeatherfall(Transform parent, float light, bool bench, bool below = false) : base("featherfall", parent)
        {
            _below = below;
            var look = FxStudyWhirlwind.Look.Picture;
            if (light >= 0f) look.Light = light;

            // THE HOLD: the noise horn upside down, a metre tall here and stretched to the gap under her each frame.
            _column = Group("column");
            var updraft = new[] { new Vector2(0.46f, 0f), new Vector2(0.30f, 0.30f), new Vector2(0.19f, 0.66f), new Vector2(0.17f, 0.88f), new Vector2(0.26f, 1.0f) };
            _skin = AddShared("updraft", Once("updraft", () => FxStudyKit.Shell(updraft, 0f, 360f, 210f, 28, 36)), FxStudyKit.Ribbon("noise_simplex_cells", "noise_simplex_cells"), 4, _column);
            _skin.Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Tile", 1f, 1f, 0.1f, 0f).Set("_Scroll", 1.5f, 0.5f)
                 .Set("_Cut", 0.06f, 0.80f).Set("_Stops", 0.30f, 0.60f, 0.92f).Set("_Ends", 0.18f, 0.06f, 0f)
                 .Set("_ErodeTile", 1f, 1f, 0.3f, 0.2f).Set("_ErodeScroll", 1.5f, 0.5f).Set("_Sweep", 1.2f).Set("_Soft", 0.2f)
                 .Set("_Halo", 0f).Set("_Emit", 0.40f).Set("_Add", look.Light).Set("_Hue", look.HueDrift, 4f, 0.2f, 1f).WindowSoft(0.10f);
            var wider = new Vector2[updraft.Length];
            for (int i = 0; i < updraft.Length; i++) wider[i] = new Vector2(updraft[i].x * 1.16f, updraft[i].y);
            _skinBack = AddShared("updraft-back", Once("updraft-back", () => FxStudyKit.Shell(wider, 90f, 360f, -160f, 28, 36)), FxStudyKit.Ribbon("noise_simplex_cells", "noise_simplex_cells"), 3, _column);
            _skinBack.Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Tile", 1f, 1f, 0.6f, 0.4f).Set("_Scroll", 1.0f, -0.4f)
                     .Set("_Cut", 0.10f, 0.85f).Set("_Stops", 0.30f, 0.62f, 0.94f).Set("_Ends", 0.22f, 0.08f, 0f)
                     .Set("_ErodeTile", 1f, 1f, 0.7f, 0.1f).Set("_ErodeScroll", 1.0f, -0.4f).Set("_Sweep", 1.2f).Set("_Soft", 0.2f)
                     .Set("_Halo", 0f).Set("_Emit", 0.30f).Set("_Add", look.Light).Set("_Hue", look.HueDrift, 4f, 0.7f, 1f).WindowSoft(0.10f);

            // THE TIME LEFT: a flat ring under her soles. Its u runs round it, so the open window is the time left.
            _ring = Group("clock");
            var band = new[] { new Vector2(0.44f, 0f), new Vector2(0.58f, 0f), new Vector2(0.72f, 0f) };
            _clock = AddShared("time-left", Once("band", () => FxStudyKit.Revolve(band, 64)), FxStudyKit.Ribbon(FxStudyTrails.MaskOf(4)), 8, _ring);
            FxStudyTrails.Dress(_clock, 4).Colours(look.TapeDeep, look.Tape, look.TapeHot, look.Tape)
                .Set("_Tile", 3f, 1f, 0f, 0f).Set("_Scroll", 0.5f, 0f).Set("_Ends", 0f, 0f, 0f).Set("_Add", Mathf.Min(0.7f, look.Light))
                .Set("_Hue", look.HueDrift, 6f, 0.4f, 1f).WindowSoft(0.015f);
            _track = AddShared("time-track", Once("band", () => FxStudyKit.Revolve(band, 64)), FxStudyKit.Ribbon(FxStudyTrails.MaskOf(4)), 7, _ring);
            FxStudyTrails.Dress(_track, 4).Colours(look.TapeDeep, look.TapeDeep, look.Tape, look.TapeDeep)
                .Set("_Tile", 3f, 1f, 0.5f, 0f).Set("_Scroll", 0.5f, 0f).Set("_Ends", 0f, 0f, 0f).Set("_Add", Mathf.Min(0.7f, look.Light))
                .Set("_Hue", look.HueDrift, 6f, 0.9f, 1f);

            // TWO TAPES winding up round her from her soles to her waist.
            _tapes = Group("tapes");
            for (int i = 0; i < _tape.Length; i++)
            {
                var knots = new Knot[9];
                for (int k = 0; k < knots.Length; k++)
                {
                    float along = k / (float)(knots.Length - 1), turn = (i * 180f + 330f * along) * Mathf.Deg2Rad;
                    float r = Mathf.Lerp(0.52f, 0.36f, along), broad = 0.30f * Mathf.Sin(Mathf.PI * Mathf.Lerp(0.08f, 0.95f, along)) + 0.02f;
                    knots[k] = new Knot(new Vector3(Mathf.Cos(turn) * r, -0.10f + 1.0f * along, Mathf.Sin(turn) * r), broad, i * 180f + 330f * along);
                }
                int trail = i == 0 ? 2 : 4;
                var tape = AddShared("tape-" + i, Once("tape-" + i, () => FxStudyKit.Sweep(knots, Vector3.up, perKnot: 8, across: 6)), FxStudyKit.Ribbon(FxStudyTrails.MaskOf(trail)), 10 + i, _tapes);
                FxStudyTrails.Dress(tape, trail).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo)
                    .Set("_Tile", 1.2f, 1f, i * 0.37f, 0f).Set("_Add", Mathf.Min(0.7f, look.Light)).Set("_Hue", look.HueDrift, 6f, i * 0.37f, 1f).WindowSoft(0.08f);
                _tape[i] = tape;
            }

            // THE GUST: a short wide horn blown up out of the court where she left it, and the dish streaks thrown out round it.
            _launch = Group("launch");
            _gust = AddShared("gust", Once("gust", () => FxStudyKit.Shell(new[] { new Vector2(0.30f, 0f), new Vector2(0.42f, 0.55f), new Vector2(0.62f, 1.25f), new Vector2(0.86f, 1.9f) }, 0f, 360f, 260f, 24, 36)),
                        FxStudyKit.Ribbon("noise_simplex_cells", "noise_simplex_cells"), 6, _launch);
            _gust.Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Tile", 1f, 1f, 0.4f, 0.3f).Set("_Scroll", 2.6f, 0.9f)
                 .Set("_Cut", 0.05f, 0.78f).Set("_Stops", 0.28f, 0.58f, 0.90f).Set("_Ends", 0.10f, 0.30f, 0f)
                 .Set("_ErodeTile", 1f, 1f, 0.4f, 0.3f).Set("_ErodeScroll", 2.6f, 0.9f).Set("_Sweep", 1.2f).Set("_Soft", 0.2f)
                 .Set("_Halo", 0f).Set("_Emit", 0.45f).Set("_Add", look.Light).Set("_Hue", look.HueDrift, 4f, 0.5f, 1f).WindowSoft(0.10f);
            var dish = new[] { new Vector2(0.15f, 0.03f), new Vector2(0.45f, 0.03f), new Vector2(0.80f, 0.03f), new Vector2(1.0f, 0.03f) };
            _kick = AddShared("kick", Once("dish", () => FxStudyKit.Revolve(dish, 48)), FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 2, _launch);
            FxStudyWisp.DressDishStreaks(_kick).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Add", look.Light).Set("_Ends", 0f, 0f, 0.32f).Set("_Hue", look.HueDrift, 4f, 0.3f, 1f);

            // THE LANDING: the dish streaks again, where she comes down.
            _land = Group("land");
            _landKick = AddShared("land-kick", Once("dish", () => FxStudyKit.Revolve(dish, 48)), FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 2, _land);
            FxStudyWisp.DressDishStreaks(_landKick).Colours(look.SilkDeep, look.Silk, look.SilkHot, look.SilkHalo).Set("_Add", look.Light).Set("_Ends", 0f, 0f, 0.32f).Set("_Hue", look.HueDrift, 4f, 0.8f, 1f);

            if (!bench) return;
            // On the bench: a stand-in for her (a grey post her height) and the bench's own cloud puffs, at the beats.
            var plain = Shader.Find("Unlit/Color");
            if (plain != null)
            {
                var grey = new Material(plain) { hideFlags = HideFlags.HideAndDontSave, color = new Color(0.36f, 0.37f, 0.40f) };
                var post = Add("stand-in", FxStudyKit.Square(false), grey, 0, Group("her"));
                grey.renderQueue = 1950;
                post.At.localPosition = new Vector3(0f, 0.85f, 0f); post.At.localScale = new Vector3(0.5f, 1.7f, 1f); post.At.localRotation = Quaternion.Euler(0f, -38f, 0f);
                post.Frame(0f, 1f);
            }
            _benchPuffs = new CloudPuffField(Root);
            _benchPuffs.Spawn(TakeOffPuff(Root.position), Lead + 0.02f);
            _benchPuffs.Spawn(LandingPuff(Root.position), Lead + Rise + Hold + Descent);
        }

        /// <summary>The cloud puff the take-off kicks out of the court, and the one the landing does.</summary>
        public static CloudPuffField.Puff TakeOffPuff(Vector3 ground)
        {
            var p = CloudPuffField.Puff.Landing(ground);
            p.Scale = 1.7f; p.Reach = 0.40f; p.Lift = 0.05f; p.Life = 0.45f;
            return p;
        }
        public static CloudPuffField.Puff LandingPuff(Vector3 ground)
        {
            var p = CloudPuffField.Puff.Landing(ground);
            p.Scale = 1.45f; p.Turn = 1.1f;
            return p;
        }

        // On the bench: a beat of nothing, the take-off, the full hold, the descent, the landing.
        public override void Sample(float age, Vector3 eye)
        {
            float t = age - Lead, held = t - Rise, down = held - Hold, landed = down - Descent;
            float gap = t < 0f ? 0f : t < Rise ? High * FxStudyKit.EaseOut(t / Rise) : down < 0f ? High : Mathf.Max(0f, High - Down * down);
            var her = Root.Find("her");
            if (her != null) her.localPosition = new Vector3(0f, gap, 0f);
            Pose(t, gap, down < 0f ? 1f - Mathf.Clamp01(held / Hold) : -1f, t, landed);
            _benchPuffs?.Hold(age);
        }

        /// <summary>
        /// Poses it. The root stands on the court under her; the caller puts `launch` and `land` where those happened
        /// (<see cref="LaunchAt"/>, <see cref="LandAt"/>). `age` is the time since she took off; `gap` how far her soles are
        /// above the court; `left` the share of the 5 s still to come (below 0 once she is coming down); `sinceLaunch` and
        /// `sinceLand` the time since each (below 0 before).
        /// </summary>
        public void Pose(float age, float gap, float left, float sinceLaunch, float sinceLand)
        {
            float a = Mathf.Max(0f, age);
            bool aloft = gap > 0.05f;
            bool holding = left >= 0f;
            // The hold's strength: up with the gust, full while held, thinning as she comes down, gone at the court.
            float strength = holding ? FxStudyKit.Ease(FxStudyKit.Run(sinceLaunch, 0.05f, 0.40f)) : 0.55f * Mathf.Clamp01(gap / High);
            // In the last second the air begins to give: the hold breathes thinner.
            if (holding) strength *= Mathf.Lerp(0.55f, 1f, FxStudyKit.Ease(left * Hold));

            _column.localScale = new Vector3(1f, Mathf.Max(0.05f, gap), 1f);
            // The cells climb at a constant speed in metres, whatever the gap.
            float tile = Mathf.Max(0.4f, gap / 1.6f);
            _skin.Set("_Tile", tile, 1f, 0.1f, 0f).Set("_ErodeTile", tile, 1f, 0.3f, 0.2f);
            _skinBack.Set("_Tile", tile, 1f, 0.6f, 0.4f).Set("_ErodeTile", tile, 1f, 0.7f, 0.1f);
            // At take-off it is drawn up from the court as she rises.
            float drawn = FxStudyKit.EaseOut(FxStudyKit.Run(sinceLaunch, 0f, Rise + 0.1f)) * 1.1f;
            // ⚠️ AND IT IS THE TIME LEFT, READ FROM ANY SIDE. The ring under her soles is a clock only from under her; from
            // across the court it is a line. So the updraft itself runs out: its foot lets go of the court and climbs
            // toward her soles as the seconds go, a full column at take-off and a short stub under her at the end, and as
            // she comes down what is left of it goes with her.
            // ⚠️ IT NO LONGER SHORTENS ON ITS OWN. The owner, of the version whose foot let go of the court and climbed toward
            // her soles as the seconds went: "the trail suddenly shortens". The column now always stands from the court to
            // her soles (its length is the gap under her, so it grows and shrinks only as she rises and comes down) and the
            // time left is read from the ring under her soles alone; at the end it thins and dissolves (`strength`), never
            // jumps.
            _skin.Open(-0.1f, drawn); _skinBack.Open(-0.1f, drawn);
            _skin.Frame(a, aloft ? 0.75f * strength : 0f, 0.15f + 0.6f * (1f - strength));
            _skinBack.Frame(a, aloft ? 0.45f * strength : 0f, 0.25f + 0.6f * (1f - strength));

            // The time left, under her soles: it closes as the seconds go; it turns slowly so the band is seen to run.
            _ring.localPosition = new Vector3(0f, gap - 0.04f, 0f);
            _ring.localRotation = Quaternion.Euler(0f, -a * 20f, 0f);
            float come = FxStudyKit.EaseOut(FxStudyKit.Run(sinceLaunch, 0.15f, 0.55f));
            _ring.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, come);
            _clock.Open(0f, holding ? left : 0f);
            _clock.Frame(a, holding && aloft ? 0.95f * come : 0f, 0f);
            _track.Frame(a, holding && aloft ? 0.28f * come : 0f, 0f);

            // The tapes ride up with her and let go as she comes down.
            _tapes.localPosition = new Vector3(0f, gap, 0f);
            _tapes.localRotation = Quaternion.Euler(0f, -a * 45f, 0f);
            for (int i = 0; i < _tape.Length; i++)
            {
                float on = FxStudyKit.Ease(FxStudyKit.Run(sinceLaunch, 0.10f + 0.08f * i, 0.55f + 0.08f * i));
                _tape[i].Open(holding ? 0f : FxStudyKit.Ease(1f - Mathf.Clamp01(gap / High)) * 1.1f - 0.1f, on * 1.08f);
                _tape[i].Frame(a, aloft ? 0.8f * strength : 0f, holding ? 0.1f : Mathf.Lerp(0.1f, 0.5f, 1f - Mathf.Clamp01(gap / High)));
            }

            // The gust: blown up out of the court in a third of a second, then torn away.
            float g = sinceLaunch;
            _gust.Open(-0.1f, FxStudyKit.EaseOut(FxStudyKit.Run(g, 0f, 0.30f)) * 1.1f);
            _gust.At.localScale = new Vector3(Mathf.Lerp(0.6f, 1.15f, FxStudyKit.EaseOut(FxStudyKit.Run(g, 0f, 0.5f))), 1f, Mathf.Lerp(0.6f, 1.15f, FxStudyKit.EaseOut(FxStudyKit.Run(g, 0f, 0.5f))));
            _gust.Frame(g, g >= 0f && g < GustLife ? 0.9f : 0f, FxStudyKit.Run(g, 0.25f, GustLife));
            Throw(_kick, g, GustLife);
            Throw(_landKick, sinceLand, LandLife);
        }

        /// <summary>The dish streaks thrown outward from under her and spun, then torn away.</summary>
        private static void Throw(FxPiece kick, float t, float life)
        {
            float thrown = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, 0.30f));
            kick.At.localScale = new Vector3(Mathf.Lerp(0.3f, 1.25f, thrown), 1f, Mathf.Lerp(0.3f, 1.25f, thrown));
            kick.At.localRotation = Quaternion.Euler(0f, -t * 160f, 0f);
            kick.Frame(t, t >= 0f && t < life ? 0.8f : 0f, Mathf.Max(0.3f, FxStudyKit.Run(t, 0.10f, life)));
        }

        /// <summary>Where on the court the take-off and the landing happened (world).</summary>
        public void LaunchAt(Vector3 ground) { _launch.position = ground; _launch.rotation = Quaternion.identity; }
        public void LandAt(Vector3 ground) { _land.position = ground; _land.rotation = Quaternion.identity; }
    }
}

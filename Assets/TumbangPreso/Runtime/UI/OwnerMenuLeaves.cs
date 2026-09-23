using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A few leaves off the tree she painted in the top right corner, fluttering
    /// down on the breeze, landing on the open road, resting, and fading.
    ///
    /// ⚠️ THE LEAF IS ONE SHE ALREADY DREW. `tools/author_owner_menu_v3.py` lifts
    /// it out of the litter lying on the road in 48.png and wears it in her canopy's
    /// colours, so a falling leaf and a fallen leaf are the same object.
    ///
    /// ⚠️⚠️ WHAT WAS WRONG WITH THE FIVE-LEAF LOOP, MEASURED OFF A MOTION HEAT MAP:
    /// five leaves on one 11.4 second cycle kept three or four in the air at every
    /// moment, all on the same diagonal at the same speed, so any ten seconds of the
    /// screen showed four parallel streaks: rain, not a quiet street. Each fall also
    /// ended by fading out in mid-air, and the tumble was the width squashed through
    /// zero on a clock that ignored the fall, so a leaf flickered rather than turned.
    ///
    /// ⚠️⚠️ HOW A LEAF FALLS NOW, AND WHY EACH PART:
    /// - **Three slots on a 27 second cycle, each fall planned from a hash of its
    ///   slot and cycle.** On average 1.1 leaves are in the air, never more than 2,
    ///   and at least one is always on screen, in the air or resting, which
    ///   `OwnerMenuEditsTests` relies on. No two falls are the same.
    /// - **A pendulum, not a ramp.** A leaf swings 40 to 100 pixels either side on a
    ///   2.2 to 2.9 second cycle, falls fastest through the bottom of each swing and
    ///   nearly hangs at each end, and tilts like the bob of a pendulum: flat through
    ///   the middle, edge up at the ends. The first pass drifted 700 pixels against a
    ///   42 pixel swing and read as a glide down a slope.
    /// - **It lands.** The swing and the tilt die away into the road, the leaf lies
    ///   down flat at its own resting angle, slides a few pixels, rests three to five
    ///   seconds and fades. In sunlight it throws its own silhouette as a shadow that
    ///   gathers under it as it comes down, so it visibly meets the ground.
    /// - **About three in ten tumble end over end** in one or two whole turns that
    ///   finish face up, and show their darker underside while they are over.
    ///
    /// ⚠️⚠️ EVERY FALL IS CHECKED BEFORE IT STARTS. A path that would cross the can,
    /// the slipper, her painted road leaves, the caption or the bush in the bottom
    /// left is replanned, up to eight times, then falls back to one path known to
    /// clear everything. `OwnerMenuSkyTests` captures the leaves while asserting
    /// nothing moves over the can and the slipper strap, and a leaf resting on a
    /// prop would look wrong anyway. Checked against every plan for 1,215 cycles.
    ///
    /// ⚠️ POSITIONS ARE IN THE PAINTING'S OWN 1920x1080 PIXELS AND ARE MAPPED
    /// THROUGH THE BACKGROUND'S `uvRect`, exactly as `OwnerRoadDust` does, so the
    /// aspect crop can never move a leaf onto the wall.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerMenuLeaves : MaskableGraphic
    {
        public RawImage Background;
        private Texture2D _leaf;
        public override Texture mainTexture => _leaf != null ? (Texture)_leaf : Texture2D.whiteTexture;

        private const int Slots = 3;
        private const float Period = 27f, FadeOut = 2.6f;

        // ⚠️ LANDINGS ARE OPEN ROAD. The strip under the slipper was one too and was
        // cut: no path from the canopy reaches it without crossing the slipper.
        private struct Landing
        {
            public float X0, X1, Y0, Y1, Drift0, Drift1, Swing0, Swing1, Share;
        }
        private static readonly Landing[] Landings =
        {
            // the middle of the road, left of the slipper
            new Landing { X0 = 1030, X1 = 1180, Y0 = 700, Y1 = 880, Drift0 = 380, Drift1 = 560, Swing0 = 70, Swing1 = 100, Share = .42f },
            // the far road, above the slipper
            new Landing { X0 = 1150, X1 = 1470, Y0 = 590, Y1 = 640, Drift0 = 230, Drift1 = 420, Swing0 = 60, Swing1 = 90, Share = .36f },
            // straight down the right edge, in a lull
            new Landing { X0 = 1805, X1 = 1885, Y0 = 770, Y1 = 960, Drift0 = 25, Drift1 = 70, Swing0 = 26, Swing1 = 40, Share = .22f },
        };
        // xMin, yMin, xMax, yMax in source pixels: can, slipper, her two road leaves,
        // the caption, the bush.
        private static readonly Vector4[] KeepOut =
        {
            new Vector4(1535, 625, 1750, 950), new Vector4(1240, 655, 1545, 795),
            new Vector4(220, 880, 325, 930), new Vector4(1020, 925, 1165, 985),
            new Vector4(590, 955, 1335, 1080), new Vector4(0, 870, 410, 1080),
        };

        private struct Plan
        {
            public int Cycle;
            public bool Made;
            public float Spawn, Flight, Rest, Omega, Phi, RestRoll, Hue, X0, Y0, Xl, Yl, Swing, Size;
            public bool Tumbler;
            public int Turns;
        }
        // Two cycles per slot can be alive at once; indexed by slot and cycle parity.
        private readonly Plan[] _plans = new Plan[Slots * 2];
        private bool _reduced;

        protected override void Awake()
        {
            base.Awake();
            _leaf = OwnerMenuArt.Texture("main2-leaf");
            raycastTarget = false;
        }

        private void LateUpdate()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            if (reduced != _reduced) { _reduced = reduced; SetVerticesDirty(); }
            if (reduced) return;
            SetVerticesDirty();
        }

        /// <summary>The lowest leaves of her tree at this x, read off the sky mask's canopy edge.</summary>
        private static float CanopyEdge(float x)
        {
            if (x < 1520) return 70;
            if (x < 1600) return Mathf.Lerp(70, 100, Mathf.InverseLerp(1520, 1600, x));
            if (x < 1680) return Mathf.Lerp(100, 130, Mathf.InverseLerp(1600, 1680, x));
            if (x < 1730) return Mathf.Lerp(130, 170, Mathf.InverseLerp(1680, 1730, x));
            if (x < 1775) return Mathf.Lerp(170, 260, Mathf.InverseLerp(1730, 1775, x));
            return Mathf.Lerp(260, 300, Mathf.InverseLerp(1775, 1900, x));
        }

        private Plan PlanFor(int slot, int cycle)
        {
            int index = slot * 2 + (cycle & 1);
            if (_plans[index].Made && _plans[index].Cycle == cycle) return _plans[index];
            var p = new Plan { Cycle = cycle, Made = true };
            p.Spawn = slot * Period / Slots + cycle * Period + (OwnerMenuWind.Hash(slot, cycle, 1) - .5f) * 2.4f;
            p.Flight = 10f + 3f * OwnerMenuWind.Hash(slot, cycle, 2);
            p.Rest = 3.5f + 2f * OwnerMenuWind.Hash(slot, cycle, 3);
            p.Omega = Mathf.PI * 2 / (2.2f + .7f * OwnerMenuWind.Hash(slot, cycle, 4));
            p.Phi = Mathf.PI * 2 * OwnerMenuWind.Hash(slot, cycle, 5);
            p.Tumbler = OwnerMenuWind.Hash(slot, cycle, 7) < .3f;
            p.Turns = 1 + (int)(OwnerMenuWind.Hash(slot, cycle, 8) * 2);
            p.RestRoll = (OwnerMenuWind.Hash(slot, cycle, 9) - .5f) * .3f;
            p.Hue = OwnerMenuWind.Hash(slot, cycle, 10);
            // A leaf let go in a gust is carried further.
            float kick = OwnerMenuWind.Gust(p.Spawn);
            bool clear = false;
            for (int attempt = 0; attempt < 8 && !clear; attempt++)
            {
                float pick = OwnerMenuWind.Hash(slot, cycle, 20 + attempt);
                var zone = Landings[Landings.Length - 1];
                foreach (var candidate in Landings)
                {
                    if (pick < candidate.Share) { zone = candidate; break; }
                    pick -= candidate.Share;
                }
                p.Xl = Mathf.Lerp(zone.X0, zone.X1, OwnerMenuWind.Hash(slot, cycle, 30 + attempt));
                p.Yl = Mathf.Lerp(zone.Y0, zone.Y1, OwnerMenuWind.Hash(slot, cycle, 40 + attempt));
                float drift = Mathf.Lerp(zone.Drift0, zone.Drift1, OwnerMenuWind.Hash(slot, cycle, 50 + attempt)) * (.9f + .3f * kick);
                p.X0 = Mathf.Clamp(p.Xl + drift, 1470, 1895);
                p.Y0 = CanopyEdge(p.X0) - 10 - 40 * OwnerMenuWind.Hash(slot, cycle, 60 + attempt);
                p.Swing = Mathf.Lerp(zone.Swing0, zone.Swing1, OwnerMenuWind.Hash(slot, cycle, 70 + attempt));
                p.Size = SizeAt(p.Yl);
                clear = Clear(p);
            }
            if (!clear)
            {
                // The one path known to clear everything: off the left of the canopy
                // to the open road.
                p.X0 = 1500; p.Y0 = 40; p.Xl = 1060; p.Yl = 790; p.Swing = 40; p.Size = SizeAt(p.Yl);
            }
            _plans[index] = p;
            return p;
        }

        // Further down the road is nearer the lens, so bigger.
        private static float SizeAt(float landingY) => .36f + .34f * Mathf.Clamp01((landingY - 560) / 380);

        private struct Pose
        {
            public Vector2 At;
            public float Roll, Wide, Tall, Progress;
        }

        private static Pose PoseAt(in Plan p, float tau)
        {
            if (tau >= p.Flight)
            {
                float rest = tau - p.Flight;
                return new Pose { At = new Vector2(p.Xl - 5 * OwnerMenuWind.Smooth(0, 1, rest / 1.6f), p.Yl),
                    Roll = p.RestRoll, Wide = 1, Tall = 1, Progress = 1 + rest };
            }
            float s = tau / p.Flight;
            float theta = p.Omega * tau + p.Phi;
            float settle = Mathf.Min(1, tau / 1.2f);
            float swing = p.Swing * Mathf.Pow(1 - s, 1.2f) * settle;
            // Carried hardest while high up; the air near the road is stiller.
            float x = p.X0 - (p.X0 - p.Xl) * (1 - Mathf.Pow(1 - s, 1.5f)) + swing * Mathf.Sin(theta);
            float y = p.Y0 + (p.Yl - p.Y0) * (s + .075f * Mathf.Sin(2 * theta) * s * (1 - s));
            // Screen space, y down: a positive roll turns the leaf clockwise.
            float roll = -.55f * Mathf.Sin(theta) * settle * (1 - s * s * s) + p.RestRoll * s * s * s;
            float wide;
            if (p.Tumbler) wide = Mathf.Cos(p.Turns * 2 * Mathf.PI * s * (2 - s));
            else
            {
                wide = .8f + .2f * Mathf.Cos(2 * theta);
                wide += (1 - wide) * s * s * s * s;
            }
            float tall = 1 + (.4f + .15f * Mathf.Cos(2 * theta)) * (1 - s * s);
            return new Pose { At = new Vector2(x, y), Roll = roll, Wide = wide, Tall = tall, Progress = s };
        }

        private static float Opacity(in Plan p, float tau)
        {
            if (tau < 0) return 0;
            float a = OwnerMenuWind.Smooth(0, 1, tau / .7f);
            float end = p.Flight + p.Rest;
            if (tau > end) a *= 1 - OwnerMenuWind.Smooth(0, 1, (tau - end) / FadeOut);
            return .95f * a;
        }

        private bool Clear(in Plan p)
        {
            float half = Mathf.Max(_leaf.width * p.Size * .5f, _leaf.height * p.Size * 1.45f * .5f) + 6;
            for (int i = 0; i <= 61; i++)
            {
                // 61 samples of the flight, then the leaf after its slide.
                float tau = i <= 60 ? p.Flight * i / 60 : p.Flight + 1.6f;
                var at = PoseAt(p, tau).At;
                if (at.x - half < 0 || at.x + half > 1920) return false;
                foreach (var box in KeepOut)
                    if (at.x + half > box.x && at.x - half < box.z && at.y + half > box.y && at.y - half < box.w) return false;
            }
            return true;
        }

        private static readonly Color32 ShadowInk = new Color32(41, 23, 10, 255);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Background == null || _leaf == null || Settings.SettingsStore.Current.ReducedUiMotion) return;
            float t = OwnerMenuWind.Now;
            // Shadows first, so no leaf is ever drawn under another leaf's shadow.
            for (int pass = 0; pass < 2; pass++)
            for (int slot = 0; slot < Slots; slot++)
            {
                float start = slot * Period / Slots;
                int current = Mathf.FloorToInt((t - start) / Period);
                for (int cycle = current - 1; cycle <= current; cycle++)
                {
                    var p = PlanFor(slot, cycle);
                    float tau = t - p.Spawn;
                    if (tau < 0 || tau > p.Flight + p.Rest + FadeOut) continue;
                    var pose = PoseAt(p, tau);
                    float opacity = Opacity(p, tau);
                    if (pass == 0) DrawShadow(mesh, p, pose, opacity);
                    else DrawLeaf(mesh, p, pose, opacity);
                }
            }
        }

        private void DrawShadow(VertexHelper mesh, in Plan p, in Pose pose, float opacity)
        {
            float near = OwnerMenuWind.Smooth(.45f, 1, Mathf.Min(pose.Progress, 1));
            near *= near;
            if (near <= 0) return;
            // Cast down and a little to the right, away from her sun over the canopy,
            // and spread wider and fainter the higher the leaf still is.
            float height = Mathf.Max(p.Yl - pose.At.y, 0);
            float grow = 1 + .7f * Mathf.Min(height / 300, 1);
            float alpha = .30f * OwnerMenuWind.Sun(p.Xl, p.Yl) * near * opacity / .95f / grow;
            var tint = (Color)ShadowInk; tint.a = alpha;
            Quad(mesh, new Vector2(pose.At.x + height * .12f, p.Yl + 3),
                new Vector2(_leaf.width * p.Size * Mathf.Abs(pose.Wide) * grow, _leaf.height * p.Size * .9f * grow),
                pose.Roll * .5f, tint);
        }

        private void DrawLeaf(VertexHelper mesh, in Plan p, in Pose pose, float opacity)
        {
            // Each leaf a shade warmer or cooler than the last, and darker while its
            // underside is towards the street.
            float warm = .94f + .12f * p.Hue;
            var tint = pose.Wide < 0 ? new Color(.80f * warm, .76f, .66f * (2 - warm)) : new Color(warm, 1, 2 - warm);
            tint *= color;
            tint.a = opacity * color.a;
            Quad(mesh, pose.At, new Vector2(_leaf.width * p.Size * pose.Wide, _leaf.height * p.Size * pose.Tall), pose.Roll, tint);
        }

        // ⚠️ STATIC, BECAUSE THIS RUNS EVERY FRAME. `CLAUDE.md` § 7.1 records what a per-frame
        // allocation costs here: a HUD string rebuilt every frame took an eighth of the probe's
        // frames and most of its physics steps.
        // Corners in source space, y down, with the texture's top row at the top: the
        // previous quad mapped v = 0 to the top corner and drew every leaf upside down.
        private static readonly Vector2[] Corners =
            { new Vector2(-.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, -.5f), new Vector2(-.5f, -.5f) };
        private static readonly Vector2[] Uvs =
            { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };

        private void Quad(VertexHelper mesh, Vector2 centre, Vector2 size, float roll, Color tint)
        {
            if (tint.a <= .004f || Mathf.Abs(size.x) < .5f) return;
            float cos = Mathf.Cos(roll), sin = Mathf.Sin(roll);
            int first = mesh.currentVertCount;
            for (int i = 0; i < 4; i++)
            {
                var offset = new Vector2(Corners[i].x * size.x, Corners[i].y * size.y);
                var turned = new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
                mesh.AddVert(Local(centre + turned), tint, Uvs[i]);
            }
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private Vector3 Local(Vector2 source)
        {
            var uv = Background.uvRect;
            var rect = rectTransform.rect;
            return new Vector3(rect.xMin + (source.x / 1920f - uv.x) / uv.width * rect.width,
                rect.yMin + (1 - source.y / 1080f - uv.y) / uv.height * rect.height);
        }
    }
}

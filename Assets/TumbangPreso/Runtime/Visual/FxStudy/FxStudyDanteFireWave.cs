using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE LONGHORN'S SLAM WAVE (2026-10-30). The owner, of the longhorn cutscene's end: *"at the end theres like an
    /// orange/fiery shockwave effect- but it looks like a sticker circle outline. i need it to look fiery"*. The rings it replaces
    /// were two flat orange bands on the court growing to 4.8 and 9 m. This is fire instead, built from the bench's own approved
    /// parts and nothing new in kind: the scorch's char and cooling fissures (`FxStudyScorch`), its ember dabs (`fleck_sheet`),
    /// and the ribbon shader's flame picture (`zack_trail_flame`), on tongues and on two low lathe-turned walls.
    ///
    ///   * THE WALL: forty tongues of flame stand up out of the court on a front that rolls outward from the blow, faster ahead
    ///     (down the lane, +z) than behind. No two alike: each its own height, lean, width and moment, a few short, so the front
    ///     is a ragged wall of separate tongues with gaps, never a line. Each is a curved upright ribbon turned to the eye (a
    ///     flame has no back), its picture running up it, leaning out the way the front goes. They shoot up as the front
    ///     passes, lean over, and are EATEN from the tips down by the erosion picture (no fade).
    ///   * HEAT LEFT BEHIND: the scorch's char burnt outward under the front, and its fissures glowing in the burnt patch and
    ///     cooling by a rising cut, the thin ones first.
    ///   * A CONTINUOUS BASE: two low walls of the same flame picture on the front, so the tongues stand out of a band of fire
    ///     hugging the court and the wave is one rolling wall, not separate flames (bench b3).
    ///   * EMBERS thrown off the front. No smoke: as balls it read as cotton wool (b1) and then as black clumps (b2, b3).
    /// 1.8 s: the blow at 0, the wall at its tallest about 0.12 to 0.3, the front at 4 m ahead by 0.7, eaten by 1.05.
    /// Like every study effect it is a pure function of the age it is handed.
    /// </summary>
    public sealed class FxStudyDanteFireWave : FxStudyEffect
    {
        private const int Tongues = 40;
        private static readonly Color[] Tones = { new Color(1f, 0.62f, 0.14f), new Color(1f, 0.84f, 0.40f), new Color(1f, 0.42f, 0.08f), new Color(1f, 0.94f, 0.72f) };

        private readonly FxPiece[] _tongue = new FxPiece[Tongues];
        private readonly FxPiece _char, _cracks, _pool, _flash;
        private readonly FxPiece[] _base = new FxPiece[2];
        private readonly FxPiece _haze;

        /// <summary>
        /// ⚠️ THE HEAT HAZE'S SWITCH (owner, of the accepted b5: "fire blast looks okay, should also probably add lens distort to
        /// that"). On, the air over the front bends what is behind it (`FxStudyHeatHaze.shader`, one grab shared with Airburst's
        /// dome). Off, or on a lean graphics tier, or on a phone, the haze is not drawn and nothing is grabbed. Everything else
        /// about the wave is the approved b5 (kept in tools/cutscene_rework/approved/dante_firewave_b5) and does not depend on it.
        /// </summary>
        public static bool Haze = true;
        private readonly FxFlecks _embers;

        /// <summary>How far the front has rolled at `age` in a direction (`ahead` 1 straight down the lane, -1 straight back).</summary>
        // b1: 5.4 m ahead ran the wall through the slam shot's lens (3.5 m in front of him); 4 m ahead, 2.4 behind (where the
        // longhorn's fists are, and anything there is hidden by them anyway).
        /// <summary>The wave opens this far out (it is centred on him in the cutscene, and he is inside it, never behind it).</summary>
        public static float Clear = 1.0f;
        public static float Front(float age, float ahead) => Mathf.Lerp(Clear, Mathf.Lerp(2.4f, 4.0f, .5f + .5f * ahead), FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, .75f)));

        public override float Seconds => 1.8f;
        public override float FloorCell => 0.75f;
        public override Color FloorColour => new Color(0.22f, 0.22f, 0.235f);
        public override Color FloorLine => new Color(0.30f, 0.30f, 0.32f);
        public override float FloorDim => 0.6f;
        public override float FloorDimAt => 16f;
        // The cutscene's slam shot: in front of him at his right, a little above.
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(3.0f, 1.6f, 4.6f); focus = new Vector3(0f, .5f, .6f); fov = 58f; }

        public FxStudyDanteFireWave(Transform parent) : base("dantefirewave", parent)
        {
            _char = AddFlat("char", 1, "scorch_char", lying: true, erode: "erode_cloud");
            var soot = new Color(0.02f, 0.016f, 0.014f);
            _char.Colours(soot, soot, new Color(0.06f, 0.045f, 0.04f), soot).Set("_Cut", 0.12f, 0.40f).Set("_Add", 0f).Set("_Soft", 0.10f).Set("_Emit", 0f);
            _char.At.localPosition = new Vector3(0f, 0.010f, 0f);
            _pool = AddGlow("pool", 2, new Color(1f, 0.85f, 0.5f), new Color(1f, 0.40f, 0.06f), lying: true);
            _pool.At.localPosition = new Vector3(0f, 0.014f, 0f);
            _cracks = AddFlat("cracks", 3, "scorch_fissure", lying: true, erode: "erode_cloud");
            _cracks.Colours(new Color(0.55f, 0.04f, 0.0f), new Color(1f, 0.36f, 0.02f), new Color(1f, 0.92f, 0.55f), new Color(1f, 0.4f, 0.05f))
                   .Set("_Halo", 0.8f).Set("_Soft", 0.10f).Set("_Hue", 8f, 0f, 0.4f, 1f);
            _cracks.At.localPosition = new Vector3(0f, 0.018f, 0f);

            // The tongues: one upright curved ribbon each, root on the court, bending out at the top. Built standing on +y with
            // its flat side to +z; the pose turns it to the eye about the upright and leans it out.
            for (int i = 0; i < Tongues; i++)
            {
                float bend = 0.18f + 0.22f * FxStudyKit.Hash(i, 11), wide = 0.30f + 0.22f * FxStudyKit.Hash(i, 12);
                var mesh = FxStudyKit.Sweep(new[]
                {
                    new FxStudyKit.Knot(0f, 0f, 0f, wide), new FxStudyKit.Knot(bend * 0.25f, 0.35f, 0f, wide * 0.95f),
                    new FxStudyKit.Knot(bend * 0.7f, 0.7f, 0f, wide * 0.6f), new FxStudyKit.Knot(bend, 1.0f, 0f, wide * 0.18f),
                }, Vector3.forward, perKnot: 8, across: 4, cup: 0.15f);
                var piece = Add("tongue-" + i, mesh, FxStudyKit.Ribbon("zack_trail_flame", "erode_cloud"), 6 + (i % 3));
                piece.Colours(new Color(0.75f, 0.08f, 0.0f), new Color(1f, 0.40f, 0.04f), new Color(1f, 0.90f, 0.55f), new Color(1f, 0.45f, 0.06f))
                     .Set("_Scroll", 2.6f + 1.2f * FxStudyKit.Hash(i, 13), 0f).Set("_Tile", 1.2f, 1f, FxStudyKit.Hash(i, 14), 0f)
                     .Set("_Cut", 0.10f, 0.42f).Set("_Ends", 0.10f, 0.30f, 0.20f).Set("_Add", 1f).Set("_Emit", 1.3f).Set("_Soft", 0.12f)
                     .Set("_ErodeScroll", 0f, -0.8f);
                _tongue[i] = piece;
            }

            // The haze: a taller lathe-turned wall over the base, wearing nothing but the bend.
            var hazeShader = Shader.Find("TumbangPreso/FxStudyHeatHaze");
            if (hazeShader != null)
            {
                bool lean = !Haze || Application.isMobilePlatform || Settings.GraphicsProfiles.Of(Settings.GraphicsProfiles.Current).Lean;
                hazeShader.maximumLOD = lean ? 100 : 300;
                var look = new Material(hazeShader) { hideFlags = HideFlags.HideAndDontSave };
                look.SetTexture("_NoiseTex", FxStudyKit.Mask("erode_cloud"));
                _haze = Add("haze", FxStudyKit.Revolve(new[] { new Vector2(0.92f, 0f), new Vector2(1.0f, 0.5f), new Vector2(1.02f, 1f) }, 64), look, 20);
            }

            _flash = AddGlow("flash", 10, Color.white, new Color(1f, 0.70f, 0.25f));
            _flash.Set("_Sprite", 0f).Set("_Emit", 1.0f);
            _flash.At.localPosition = new Vector3(0f, 0.25f, 0f);

            // ⚠️ NO SMOKE BALLS (b1: pale balls read as cotton wool; b2 and b3: black balls, the same thing in soot). Dropped.
            // ⚠️ THE CONTINUOUS BASE (coordinator on b3: "reads as separate flames, make it a rolling wall with a continuous base of
            // fire hugging the ground"): two low walls of fire turned on the lathe, an outer one on the front and a lower inner one
            // lagging it, wearing the flame picture run UP them (laid crosswise) and tiled round them, cut by a threshold so
            // the top edge is ragged flame and never a line; the tongues stand up out of them.
            var wall = new[] { new Vector2(1f, 0f), new Vector2(1.03f, .45f), new Vector2(.97f, 1f) };
            for (int b = 0; b < _base.Length; b++)
            {
                var piece = Add("base-" + b, FxStudyKit.Revolve(wall, 72), FxStudyKit.Ribbon("zack_trail_flame", "erode_cloud"), 5);
                piece.Colours(new Color(0.75f, 0.08f, 0.0f), new Color(1f, 0.40f, 0.04f), new Color(1f, 0.90f, 0.55f), new Color(1f, 0.45f, 0.06f))
                     .Set("_Swap", 1f).Set("_Tile", 1f, b == 0 ? 15f : 11f, 0f, b * 0.37f).Set("_Scroll", 2.4f, 0f)
                     .Set("_Cut", 0.18f, 0.46f).Set("_Ends", 0f, 0f, 0.12f).Set("_Add", 1f).Set("_Emit", 1.2f).Set("_Soft", 0.12f)
                     .Set("_ErodeTile", 6f, 1f, b * 0.5f, 0f).Set("_ErodeScroll", 0f, -0.6f);
                _base[b] = piece;
            }

            _embers = AddFlecks("embers", 12, "fleck_sheet");
            _embers.Grid = 4;
            _embers.Piece.Colours(new Color(0.8f, 0.2f, 0f), new Color(1f, 0.6f, 0.1f), new Color(1f, 0.95f, 0.7f), new Color(1f, 0.5f, 0.1f)).Set("_Stops", 0.38f, 0.62f, 0.96f).Set("_Cut", 0.10f, 0.75f).Set("_Emit", 1.0f);
        }

        public override void Sample(float age, Vector3 eye)
        {
            // ---- the blow: one short flash at its foot.
            _flash.At.localScale = Vector3.one * Mathf.Lerp(1.0f, 3.0f, FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.1f)));
            _flash.Face(eye); _flash.Frame(age, 0.7f * FxStudyKit.Envelope(age, -0.01f, 0.02f, 0.04f, 0.12f));

            // ---- the burnt court under the front, and its fissures cooling.
            float reach = Front(age, 0.4f);
            // b1: at the front's full size it was a ten-metre black sticker. The char is the blow's own, about its foot.
            float burnt = Mathf.Min(reach, 1.6f);
            _char.At.localScale = new Vector3(burnt * 2.0f, 1f, burnt * 1.2f);
            _char.At.localPosition = new Vector3(0f, 0.010f, burnt * 0.15f);
            _char.Frame(age, age < 0f ? 0f : 0.85f, FxStudyKit.Run(age, 1.2f, 1.75f));
            // b2: the burnt court was gone with the flames; it holds its heat, the glow is what the wall leaves behind.
            float cooled = FxStudyKit.Run(age, 0.35f, 1.75f), cut = Mathf.Lerp(0.04f, 0.96f, Mathf.Pow(cooled, 0.8f));
            _cracks.At.localScale = new Vector3(reach * 1.9f, 1f, reach * 1.5f);
            _cracks.At.localPosition = new Vector3(0f, 0.018f, reach * 0.18f);
            _cracks.Set("_Cut", cut, cut + 0.10f).Set("_Stops", cut + 0.12f, Mathf.Lerp(0.30f, 1.2f, cooled), Mathf.Lerp(0.70f, 1.6f, cooled))
                   .Set("_Emit", 2.6f * (1f - cooled)).Set("_Gain", Mathf.Lerp(1.5f, 0.8f, cooled));
            _cracks.Frame(age, age < 0f ? 0f : 1f, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.2f)), FxStudyKit.Run(age, 1.3f, 1.7f)));
            _pool.At.localScale = new Vector3(reach * 2.2f, 1f, reach * 1.8f);
            _pool.At.localPosition = new Vector3(0f, 0.014f, reach * 0.18f);
            _pool.Frame(age, age < 0f ? 0f : 0.5f * (1f - FxStudyKit.Ease(FxStudyKit.Run(age, 0.15f, 1.1f))));

            // ---- the wall of tongues on the rolling front.
            for (int i = 0; i < Tongues; i++)
            {
                var piece = _tongue[i];
                float angle = (i + 0.7f * FxStudyKit.Hash(i, 1)) / Tongues * Mathf.PI * 2f;
                var way = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                // Each is born as the front reaches its place, a little early or late of its neighbours.
                float born = 0.02f * FxStudyKit.Hash(i, 2), lived = age - born;
                if (lived <= 0f) { piece.Frame(age, 0f); continue; }
                // It rides the front a while, then is left behind as the front outruns it.
                // ⚠️ NOT A RING (bench b1: forty tongues all on the front read as a ring of fire, the very outline it replaces). A
                // third ride a second, inner rank that lags the front; every one sits up to a fifth in or out of it; and their
                // heights swell and sink round the front in three clumps, so the wall is bursts with gaps between, not a line.
                bool inner = FxStudyKit.Hash(i, 15) < 0.35f;
                float riding = Mathf.Min(lived, (inner ? 0.12f : 0.28f) + 0.2f * FxStudyKit.Hash(i, 3));
                float r = Front(born + riding, way.z) * (inner ? 0.74f : 0.9f) * (0.84f + 0.36f * FxStudyKit.Hash(i, 4));
                float clump = 0.55f + 0.45f * Mathf.Sin(angle * 3f + 1.7f) * Mathf.Sin(angle * 5f + 0.4f) + 0.35f;
                float tall = (FxStudyKit.Hash(i, 5) < 0.25f ? 0.4f : 1f) * (0.6f + 0.9f * FxStudyKit.Hash(i, 6)) * Mathf.Clamp(clump, 0.25f, 1.3f)
                           * Mathf.Lerp(0.45f, 1.2f, 0.5f + 0.5f * way.z) * (inner ? 1.25f : 1f);
                float up = FxStudyKit.EaseOut(FxStudyKit.Run(lived, 0f, 0.09f)) * (1f - 0.55f * FxStudyKit.Ease(FxStudyKit.Run(lived, 0.25f, 0.8f)));
                // Low while it is still close to him (maplh9: at 1 to 1.6 m the wall stood between him and the slam's lens and hid him);
                // it stands up to its full height as it rolls out past 2 m.
                float h = tall * up * (1f + 0.08f * Mathf.Sin(age * 31f + i)) * Mathf.Lerp(0.3f, 1f, FxStudyKit.Run(r, Clear, Clear + 1.1f));
                if (h < 0.02f) { piece.Frame(age, 0f); continue; }
                piece.At.localPosition = way * r + Vector3.up * 0.01f;
                // Turned to the eye about the upright, then leant out the way the front runs (harder while it is fast).
                var toEye = piece.At.parent != null ? piece.At.parent.InverseTransformPoint(eye) - piece.At.localPosition : eye - piece.At.localPosition;
                toEye.y = 0f;
                var face = toEye.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(toEye.normalized, Vector3.up) : Quaternion.identity;
                // b2: at 40 degrees the fast ones lay down flat on the court like commas; a flame stands and leans, it does not lie.
                float lean = 10f + 16f * (1f - FxStudyKit.Run(lived, 0f, 0.6f));
                var outward = Vector3.Cross(Vector3.up, way);
                piece.At.localRotation = Quaternion.AngleAxis(lean, outward) * face * Quaternion.Euler(0f, 0f, (FxStudyKit.Hash(i, 7) - 0.5f) * 24f);
                float w = 1.1f + 0.7f * FxStudyKit.Hash(i, 8);
                piece.At.localScale = new Vector3(w * (0.8f + 0.4f * h), h, 1f);
                // Eaten from the tips down, never faded.
                float eaten = FxStudyKit.Ease(FxStudyKit.Run(lived, 0.32f + 0.25f * FxStudyKit.Hash(i, 9), 0.95f + 0.2f * FxStudyKit.Hash(i, 10)));
                piece.Frame(age, 1f, eaten);
            }

            // ---- the continuous base on the front: an ellipse (further ahead than behind), low, burning down and eaten.
            for (int b = 0; b < _base.Length; b++)
            {
                float lag = b == 0 ? 0f : 0.08f, a = age - lag;
                if (a <= 0f) { _base[b].Frame(age, 0f); continue; }
                float ahead = Front(a, 1f) * (b == 0 ? 1f : .72f), behind = Front(a, -1f) * (b == 0 ? 1f : .72f), side = Front(a, 0f) * (b == 0 ? 1f : .72f);
                float h = (b == 0 ? 0.55f : 0.38f) * FxStudyKit.EaseOut(FxStudyKit.Run(a, 0f, 0.08f)) * (1f - 0.6f * FxStudyKit.Ease(FxStudyKit.Run(a, 0.3f, 0.9f)));
                h *= Mathf.Lerp(0.3f, 1f, FxStudyKit.Run(side, Clear, Clear + 1.1f));
                _base[b].At.localPosition = new Vector3(0f, 0.01f, (ahead - behind) * 0.5f);
                _base[b].At.localScale = new Vector3(side, Mathf.Max(0.01f, h), (ahead + behind) * 0.5f);
                _base[b].Frame(age, 1f, FxStudyKit.Ease(FxStudyKit.Run(a, 0.45f, 1.05f)));
            }

            // ---- the heat haze over the front: from just over the court to about twice the tongues' height, rising with the
            // wall and thinning as the fire is eaten and the court cools.
            if (_haze != null)
            {
                float ahead = Front(age, 1f), behind = Front(age, -1f), side = Front(age, 0f);
                _haze.At.localPosition = new Vector3(0f, 0.05f, (ahead - behind) * 0.5f);
                _haze.At.localScale = new Vector3(side * 0.96f, 1.9f * (0.6f + 0.4f * FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.3f))), (ahead + behind) * 0.48f);
                _haze.Frame(age, age < 0f ? 0f : FxStudyKit.Envelope(age, 0.02f, 0.12f, 0.75f, 0.6f));
            }

            // ---- embers off the front, and lifting off the burnt court after it.
            _embers.Begin(eye);
            if (age > 0f)
            {
                for (int i = 0; i < 70; i++)
                {
                    float angle = FxStudyKit.Hash(i, 21) * Mathf.PI * 2f, born = 0.5f * FxStudyKit.Hash(i, 22), lived = age - born;
                    if (lived <= 0f || lived > 0.9f) continue;
                    var way = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    float r = Front(born, way.z), speed = 1.2f + 2.4f * FxStudyKit.Hash(i, 23), rise = 1.6f + 2.6f * FxStudyKit.Hash(i, 24);
                    Vector3 at = way * (r + speed * lived) + Vector3.up * Mathf.Max(0.03f, 0.15f + rise * lived - 4.4f * lived * lived);
                    _embers.Add(at, way * speed + Vector3.up * (rise - 8.8f * lived), 0.10f + 0.18f * FxStudyKit.Hash(i, 25), 0.06f,
                                FxStudyKit.Envelope(lived, 0f, 0.03f, 0.35f + 0.4f * FxStudyKit.Hash(i, 26), 0.3f), i * 5 + 2, Tones[i % Tones.Length]);
                }
            }
            _embers.End(age);
        }
    }
}

using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY C, THE SEAL AND THE DOME OF SIGILS. Our own effect: our own sigils (`glyph_atlas`), our own circle art
    /// (`circle_art`, `circle_seal`), our own proportions and timing. The reference was used for the KIND of stages only
    /// (a circle appears, turns, gathers to a point, a dome of glyphs bursts up and dissolves upward), never as a frame to
    /// match, and nothing of its drawing is in ours.
    ///
    /// What it is built from is everything the trails and the wisp taught:
    ///   * LINE ART IS A PICTURE ON THE FLOOR, cut by a threshold and eaten by the cloud map, never faded as a whole;
    ///   * THE DOME IS A SHELL (`FxStudyKit.Shell`, all the way round) that is itself invisible: what is seen is the sigil
    ///     sheet run slowly up and round it, so the sigils climb, and the cloud eats it from the crown down;
    ///   * every layer is see-through and its light adds; gold drifts a little in hue inside gold; the sparks are the
    ///     rough dabs of `fleck_sheet`, each its own; and the glow is drawn by hand, since nothing blooms here.
    ///
    /// 4.8 seconds: the pale circle is drawn and turns (0.3), the gold seal burns through it (0.9), it holds and breathes
    /// (1.3), it gathers to a point (2.25), the dome bursts up (2.45), its sigils climb and it is eaten from the crown
    /// (3.0), embers hang (3.5 to 4.3).
    /// </summary>
    public sealed class FxStudyCircle : FxStudyEffect
    {
        private const float Radius = 1.5f;
        private static readonly Color GoldDeep = new Color(0.62f, 0.34f, 0.05f), Gold = new Color(1f, 0.74f, 0.24f), GoldHot = new Color(1f, 0.97f, 0.82f),
                                      GoldHalo = new Color(1f, 0.58f, 0.14f);
        // The sparks' golds: none of them the same.
        private static readonly Color[] Tones = { new Color(1f, 0.80f, 0.34f), new Color(1f, 0.92f, 0.62f), Color.white, new Color(1f, 0.66f, 0.22f), new Color(1f, 0.86f, 0.48f) };

        private readonly FxPiece _shade, _pool, _fill, _art, _seal, _flare, _point, _skirt, _dome, _veil, _ring, _ringHalo;
        private readonly FxFlecks _sparks;

        public override float Seconds => 4.8f;
        public override float FloorCell => 0.75f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(0f, 3.25f, -3.55f); focus = new Vector3(0f, 0.30f, 0f); fov = 40f; }

        public FxStudyCircle(Transform parent) : base("circle", parent)
        {
            // A shadow laid over the floor while the dome stands, so its light has something dark to be bright against.
            _shade = Add("shade", FxStudyKit.Square(true), FxStudyKit.Ribbon(null), 0);
            _shade.Colours(Color.black, Color.black, Color.black, Color.black).Set("_Ends", 0f, 0f, 0f).Set("_Scroll", 0f, 0f).Set("_Cut", 0f, 0.01f).Set("_Halo", 0f).Set("_Add", 0f);
            _shade.At.localPosition = new Vector3(0f, 0.004f, 0f);
            _shade.At.localScale = Vector3.one * 40f;
            _pool = AddGlow("pool", 1, GoldHot, GoldHalo, lying: true);
            _pool.At.localPosition = new Vector3(0f, 0.008f, 0f);

            // The pale first circle: a wash between its rings, and the line art over it.
            _fill = AddFlat("fill", 2, "circle_fill", lying: true, erode: "erode_cloud");
            _fill.Colours(new Color(0.04f, 0.08f, 0.36f), new Color(0.10f, 0.20f, 0.66f), new Color(0.26f, 0.42f, 0.92f), new Color(0.1f, 0.2f, 0.7f))
                 .Set("_Cut", 0.05f, 0.9f).Set("_Add", 0.45f).Set("_Soft", 0.15f).Set("_Hue", 36f, 0f, 0.3f, 1.6f);
            _art = AddFlat("art", 3, "circle_art", lying: true, erode: "erode_cloud");
            _art.Colours(new Color(0.40f, 0.56f, 1f), new Color(0.72f, 0.84f, 1f), Color.white, new Color(0.35f, 0.55f, 1f))
                .Set("_Cut", 0.10f, 0.55f).Set("_Halo", 0.9f).Set("_Gain", 0.9f).Set("_Emit", 0.4f).Set("_Soft", 0.15f).Set("_Burn", 0.3f).Set("_Hue", 40f, 14f, 0.7f, 1.6f);
            // The gold seal that burns through it and stays.
            _seal = AddFlat("seal", 4, "circle_seal", lying: true, erode: "erode_cloud");
            _seal.Colours(GoldDeep, Gold, GoldHot, GoldHalo).Set("_Cut", 0.10f, 0.50f).Set("_Halo", 1.3f).Set("_Gain", 1.0f).Set("_Emit", 0.5f).Set("_Soft", 0.12f).Set("_Burn", 0.5f)
                 .Set("_Hue", 12f, 6f, 0.2f, 1f);
            foreach (var flat in new[] { _fill, _art, _seal }) flat.At.localPosition = new Vector3(0f, 0.012f, 0f);

            _flare = AddGlow("flare", 6, Color.white, Gold, mask: "flare_star");
            _flare.At.localPosition = new Vector3(0f, 0.22f, 0f);
            _point = AddGlow("point", 12, Color.white, new Color(0.80f, 0.88f, 1f));
            _point.At.localPosition = new Vector3(0f, 0.25f, 0f);

            // What the burst throws across the floor: rays of gold, light only.
            _skirt = AddFlat("skirt", 5, "erode_radial", lying: true, erode: "erode_cloud");
            _skirt.Colours(GoldDeep, Gold, GoldHot, GoldHalo).Set("_Cut", 0.42f, 0.68f).Set("_ErodeTile", 1.6f, 1.6f, 0.3f, 0.7f).Set("_Soft", 0.22f).Set("_Hue", 10f, 0f, 0.5f, 1f);
            _skirt.At.localPosition = new Vector3(0f, 0.016f, 0f);

            // The dome: a quarter circle from foot to crown, turned all the way round. Typed, a little taller than a
            // true hemisphere and drawn in at the foot, so it stands like a bell and not like half a ball.
            var bell = new[] { new Vector2(1.00f, 0.00f), new Vector2(1.04f, 0.30f), new Vector2(0.94f, 0.66f), new Vector2(0.66f, 0.96f), new Vector2(0.30f, 1.12f), new Vector2(0.02f, 1.16f) };
            _veil = Add("veil", FxStudyKit.Revolve(bell, 48), FxStudyKit.Orb(), 7);
            _veil.Set("_Core", GoldHot).Set("_Rim", Gold).Set("_CoreAlpha", 0.04f).Set("_RimAlpha", 0.30f).Set("_Power", 2.2f).Set("_Add", 1f).Set("_Band", 1f, 0.3f, 0f);
            _dome = Add("dome", FxStudyKit.Shell(bell, 0f, 360f, 0f, 30, 64), FxStudyKit.Ribbon("glyph_atlas", "erode_cloud"), 8);
            _dome.Colours(GoldDeep, Gold, GoldHot, GoldHalo)
                 // The sheet is laid crosswise on the dome, so a sigil stands UPRIGHT on it (it lay on its side at first).
                 .Set("_Swap", 1f).Set("_Scroll", 0f, 0f).Set("_Cut", 0.22f, 0.55f).Set("_Stops", 0.40f, 0.62f, 0.95f)
                 // Its foot and crown fade; round it there is no border to hide.
                 .Set("_Ends", 0.04f, 0.14f, 0f)
                 .Set("_ErodeTile", 1.2f, 3f, 0.1f, 0.3f).Set("_ErodeScroll", -0.15f, 0.05f).Set("_Sweep", -1.3f).Set("_Soft", 0.14f).Set("_Burn", 0.9f)
                 .Set("_Halo", 1.1f).Set("_Add", 1f).Set("_Gain", 0.95f).Set("_Emit", 0.4f).Set("_Hue", 14f, 8f, 0.6f, 1f);

            // The ring it stands on, and that ring's glow.
            _ringHalo = Add("ring-halo", FxStudyKit.Revolve(new[] { new Vector2(0.78f, 0.02f), new Vector2(1.22f, 0.02f) }, 64), FxStudyKit.Ribbon(null), 9);
            _ringHalo.Colours(GoldHalo, GoldHalo, Gold, GoldHalo).Set("_Scroll", 0f, 0f).Set("_Cut", 0f, 1.6f).Set("_Ends", 0f, 0f, 0.5f).Set("_Halo", 0f).Set("_Add", 1f);
            _ring = Add("ring", FxStudyKit.Revolve(new[] { new Vector2(0.955f, 0.025f), new Vector2(1.045f, 0.025f) }, 64), FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 10);
            _ring.Colours(Gold, GoldHot, Color.white, GoldHalo).Set("_Tile", 3f, 1f).Set("_Scroll", 0.6f, 0f).Set("_Cut", 0.0f, 0.25f).Set("_Stops", 0.2f, 0.45f, 0.85f)
                 .Set("_Ends", 0f, 0f, 0.30f).Set("_Halo", 0.6f).Set("_Add", 1f).Set("_Gain", 1.0f).Set("_Emit", 0.8f).Set("_Soft", 0.12f);

            _sparks = AddFlecks("sparks", 14, "fleck_sheet");
            _sparks.Grid = 4;
            _sparks.Piece.Colours(GoldDeep, Gold, GoldHot, GoldHalo).Set("_Gain", 1.1f).Set("_Stops", 0.38f, 0.62f, 0.96f).Set("_Cut", 0.10f, 0.75f);
        }

        public override void Sample(float age, Vector3 eye)
        {
            // ---- the pale circle: drawn outward with a turn, overshoots, settles; then the cloud eats it as the seal comes.
            float drawn = FxStudyKit.Pop(FxStudyKit.Run(age, 0.30f, 0.80f), 0.14f);
            float size = Radius * 2f * Mathf.Lerp(0.12f, 1f, drawn);
            float turn = 150f * (1f - FxStudyKit.EaseOut(FxStudyKit.Run(age, 0.30f, 1.0f))) + age * 14f;
            _art.At.localScale = Vector3.one * size; _fill.At.localScale = Vector3.one * size;
            _art.At.localRotation = Quaternion.Euler(0f, turn, 0f); _fill.At.localRotation = Quaternion.Euler(0f, -turn * 0.5f, 0f);
            float paleGone = FxStudyKit.Run(age, 0.90f, 1.35f);
            _art.Frame(age, FxStudyKit.Run(age, 0.30f, 0.36f), paleGone);
            _fill.Frame(age, 0.8f * FxStudyKit.Run(age, 0.32f, 0.50f), paleGone);

            // ---- the seal: burns in from the cloud, turns slowly the other way, breathes, then gathers to a point.
            float gather = FxStudyKit.EaseIn(FxStudyKit.Run(age, 2.22f, 2.42f));
            // A beat before it gathers: it swells a little and stops turning, as a thing does that is about to be pulled in.
            float held = FxStudyKit.Envelope(age, 1.98f, 0.20f, 2.22f, 0.06f);
            _seal.At.localScale = Vector3.one * (Radius * 2f * (1f + 0.025f * Mathf.Sin(age * 5.2f) + 0.085f * held) * Mathf.Lerp(1f, 0.04f, gather));
            _seal.At.localRotation = Quaternion.Euler(0f, -Mathf.Min(age, 2.0f) * 22f + 14f * held - 260f * gather, 0f);
            _seal.Frame(age, age < 0.78f || age > 2.44f ? 0f : 1f, 1f - FxStudyKit.Ease(FxStudyKit.Run(age, 0.78f, 1.25f)));
            _seal.Set("_Gain", 1.0f + 0.15f * Mathf.Sin(age * 5.2f) + 0.5f * held + 0.8f * gather);

            float lit = FxStudyKit.Envelope(age, 0.80f, 0.35f, 2.36f, 0.08f);
            _pool.At.localScale = Vector3.one * (Radius * 4.2f * Mathf.Lerp(1f, 0.3f, gather));
            _pool.Frame(age, Mathf.Max(0.42f * lit, 0.55f * FxStudyKit.Envelope(age, 2.44f, 0.05f, 3.30f, 0.50f)));
            _flare.At.localScale = Vector3.one * (1.5f + 0.35f * Mathf.Sin(age * 5.2f) + 1.4f * gather);
            _flare.Face(eye); _flare.Frame(age, 0.85f * FxStudyKit.Envelope(age, 0.95f, 0.30f, 2.40f, 0.05f));

            // ---- the point: three frames of white before the dome.
            _point.At.localScale = Vector3.one * Mathf.Lerp(0.8f, 4.6f, FxStudyKit.EaseOut(FxStudyKit.Run(age, 2.40f, 2.52f)));
            _point.Face(eye); _point.Frame(age, 0.7f * FxStudyKit.Envelope(age, 2.38f, 0.03f, 2.47f, 0.10f));

            // ---- the dome: up past its height and back, its sigils climbing, eaten from the crown down.
            float up = FxStudyKit.Pop(FxStudyKit.Run(age, 2.44f, 2.66f), 0.20f);
            var domeScale = new Vector3(Radius, Radius * Mathf.Lerp(0.2f, 1f, up), Radius);
            _dome.At.localScale = domeScale; _veil.At.localScale = domeScale * 0.985f;
            _dome.At.localRotation = Quaternion.Euler(0f, age * 20f, 0f);
            float climbed = 0.16f * Mathf.Max(0f, age - 2.44f) + 0.5f * FxStudyKit.EaseIn(FxStudyKit.Run(age, 2.95f, 3.70f));
            // The sheet is eight sigils wide and four high: twenty-four round the dome, five rows up it, climbing.
            _dome.Set("_Tile", 3f, 1.25f, 0.1f, -climbed);
            float domeGone = FxStudyKit.Ease(FxStudyKit.Run(age, 2.95f, 3.62f));
            _dome.Frame(age, age < 2.44f ? 0f : 1f, domeGone);
            _veil.Frame(age, FxStudyKit.Envelope(age, 2.44f, 0.04f, 2.56f, 0.30f));

            _skirt.At.localScale = Vector3.one * Mathf.Lerp(2.2f, 8.5f, FxStudyKit.EaseOut(FxStudyKit.Run(age, 2.44f, 2.80f)));
            _skirt.At.localRotation = Quaternion.Euler(0f, 37f, 0f);
            _skirt.Frame(age, 0.8f * FxStudyKit.Envelope(age, 2.44f, 0.03f, 2.54f, 0.26f), 0.42f + 0.5f * FxStudyKit.Run(age, 2.50f, 2.85f));

            float ring = FxStudyKit.Envelope(age, 2.44f, 0.05f, 3.55f, 0.45f);
            _ring.At.localScale = Vector3.one * Radius; _ringHalo.At.localScale = Vector3.one * Radius;
            _ring.Frame(age, ring, FxStudyKit.Run(age, 3.50f, 4.00f));
            _ringHalo.Frame(age, 0.30f * ring);

            _shade.Frame(age, 0.55f * FxStudyKit.Envelope(age, 2.36f, 0.10f, 3.40f, 0.60f));

            // ---- sparks: lines flung from the burst, then embers that hang over the ring and fray.
            _sparks.Begin(eye);
            for (int i = 0; i < 20; i++)
            {
                float turnAt = (i + FxStudyKit.Hash(i, 1)) / 20f * Mathf.PI * 2f, flung = FxStudyKit.EaseOut(FxStudyKit.Run(age, 2.44f, 3.05f));
                var going = new Vector3(Mathf.Cos(turnAt), 0.12f + 0.55f * FxStudyKit.Hash(i, 2), Mathf.Sin(turnAt)).normalized;
                float reach = 0.5f + 0.9f * FxStudyKit.Hash(i, 3);
                _sparks.Add(going * (Radius * 0.9f + 3.2f * flung * reach) + Vector3.up * 0.2f, going, Mathf.Lerp(1.1f, 0.25f, flung) * reach, 0.07f + 0.05f * FxStudyKit.Hash(i, 4),
                            0.9f * FxStudyKit.Envelope(age, 2.44f, 0.02f, 2.62f + 0.2f * FxStudyKit.Hash(i, 5), 0.28f), i * 5 + 1, Tones[i % Tones.Length]);
            }
            for (int i = 0; i < 28; i++)
            {
                float turnAt = (i + FxStudyKit.Hash(i, 6)) / 28f * Mathf.PI * 2f, from = 3.05f + 0.55f * FxStudyKit.Hash(i, 7);
                float risen = FxStudyKit.EaseOut(FxStudyKit.Run(age, from, from + 0.9f)), far = Radius * (0.75f + 0.4f * FxStudyKit.Hash(i, 8));
                var at = new Vector3(Mathf.Cos(turnAt) * far, 0.35f + 1.1f * FxStudyKit.Hash(i, 9) + 0.5f * risen, Mathf.Sin(turnAt) * far);
                _sparks.Add(at, new Vector3(Mathf.Cos(turnAt) * 0.25f, 1f, Mathf.Sin(turnAt) * 0.25f), 0.10f + 0.12f * FxStudyKit.Hash(i, 10), 0.07f,
                            (0.55f + 0.4f * FxStudyKit.Hash(i, 11)) * FxStudyKit.Envelope(age, from, 0.06f, from + 0.45f, 0.45f), i * 3 + 2, Tones[(i + 2) % Tones.Length]);
            }
            _sparks.End(age);
        }
    }
}

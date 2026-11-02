using UnityEngine;
using Part = TumbangPreso.Visual.ZackKitPaint.Part;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// LIVE WIRE, PAINTED: the wire itself as `ZackLiveWireLook` draws it with its `Painted` switch on, in place of the seven
    /// line renderers it rewrote point by point every frame. The two low floor cleats the owner asked for are the look's own
    /// and are kept as they are.
    ///
    /// THE GAMEPLAY SHAPE THE PICTURE TELLS THE TRUTH ABOUT. A segment 1.5 to 6 m long between two floor posts, at
    /// `ZackRules.LiveWirePostHeight` (0.28 m) at the posts and sagging to `LiveWireSagHeight` (0.24 m) in the middle, the
    /// rule's own `LiveWireHeightAt` parabola. An attacker whose feet cross it is Zapped; it has no collider. It stays until
    /// the round ends, a new cast or a reel-in; a trip darkens it for 5 s, then it rearms. So the picture is ONE LINE ON THAT
    /// PARABOLA, and nothing drawn wider than the place that trips:
    ///   * THE CURRENT is b13d's silk, at a wire's width (9 cm, a core of 4.5 cm) instead of a shoe's trail's 75 cm, laid in
    ///     two halves from each socket to the middle so the picture pours in from both anchors, with z4's BOLT riding inside
    ///     it, re-struck fourteen times a second (twenty overcharged). Overcharged strings a second silk line above it.
    ///   * THE DARK EDGE under it is a plain line on the same turned-to-the-eye shader: Zack's ink while lit (the look family's
    ///     dark edge on a bright court), cold copper when dark, so the dead wire is still on the court and still reads.
    ///   * THE SOCKETS throw b13d's spark bursts while lit; A TRIP throws a burst at the middle and lays z4's strike on the
    ///     court under it; THE REARM is the silk crawling back in from both sockets until the halves meet.
    /// Who sees what: everyone sees the same line. The silk opens square to each eye, so from a target walking at it, from
    /// above and from the side it is the same width. b13d's dissolve-near-the-lens is switched OFF here (`_Near` 0): a wire
    /// that faded as a target came close would lie about where it is.
    ///
    /// ⚠️ WHERE A REUSED PIECE DOES NOT FIT. The silk was approved as a 75 cm trail that is born, swells and dies; here it is
    /// a 9 cm line that lives for minutes, so its ageing is off (`_Life` 0) and only its churn and its four values are kept.
    /// At 9 cm its picture is fine detail, not the broad body the owner approved: that is the place to judge in the film.
    /// </summary>
    public sealed class ZackWirePaint
    {
        private const float Width = 0.09f, CoreWidth = 0.045f, EdgeWidth = 0.13f, Above = 0.07f;
        private static readonly Color DarkCopper = new Color(.42f, .36f, .3f);
        private readonly Transform _root;
        private readonly Part _edge, _bolt, _over, _strike;
        private readonly Part[] _half = new Part[2], _core = new Part[2], _socket = new Part[2];
        private readonly Part _trip;
        private float _socketBorn;
        private int _socketBeat = -1;

        public ZackWirePaint(Transform parent)
        {
            var go = new GameObject("ZackWirePaint") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _root = go.transform;
            _edge = new Part("wire-edge", _root, ZackKitPaint.Even, ZackKitPaint.Line);
            for (int h = 0; h < 2; h++)
            {
                _half[h] = new Part("wire-current-" + h, _root, ZackKitPaint.Even, ZackKitPaint.Silk);
                _core[h] = new Part("wire-core-" + h, _root, ZackKitPaint.Even, ZackKitPaint.Silk);
                _socket[h] = new Part("socket-sparks-" + h, _root, ZackKitPaint.Burst, ZackKitPaint.Sparks);
            }
            _over = new Part("wire-overcharged", _root, ZackKitPaint.Even, ZackKitPaint.Silk);
            _bolt = new Part("wire-bolt", _root, ZackKitPaint.BoltShape(0, false), ZackKitPaint.Bolt);
            _strike = new Part("trip-strike", _root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
            _trip = new Part("trip-sparks", _root, ZackKitPaint.Burst, ZackKitPaint.Sparks);
        }

        /// <summary>
        /// One frame. `a`, `b` are the two wire ends now (at post height, drawn toward his hand while reeling), `sag` how far
        /// the middle hangs below the line between them, `lit` how much gold is in it (1 lit, 0 dark), `crawl` the share of the
        /// way the rearm has crawled in from the sockets (0 none, 1 met), `overcharged`, `aimed` his own reel highlight's pulse
        /// (0 to 1), `alpha` the whole wire's fade (a reel's end), `tripAgo` seconds since a trip (below 0 for none), `tripAt`
        /// the court under the place it is drawn, `live` whether the sockets crackle.
        /// </summary>
        public void Pose(float age, Vector3 a, Vector3 b, float sag, float lit, float crawl, bool overcharged, float aimed, float alpha,
                         float tripAgo, Vector3 tripAt, bool live, bool reduced)
        {
            Vector3 Point(float t) => Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4f * t * (1f - t));
            Vector3 mid = Point(0.5f);
            float length = Vector3.Distance(a, b);
            float pulse = aimed;

            // The dark edge: one line, the whole span.
            _edge.Lay(a, b, mid);
            _edge.Facing(EdgeWidth * (1f + 0.9f * pulse), 0f);
            var edge = Color.Lerp(DarkCopper, ZackFx.Ink, lit);
            _edge.Colour("_Deep", edge); _edge.Colour("_Body", edge); _edge.Colour("_Hot", edge);
            _edge.Window(-1f, 2f, 0.02f);
            _edge.Frame(age, alpha * Mathf.Lerp(0.95f, 0.85f, lit), 0f);

            // The current, in two halves, socket (tail) to middle (head), so it pours in from both ends.
            float shown = Mathf.Max(lit, crawl > 0f ? 0.9f : 0f);
            float open = lit >= 0.999f ? 2f : crawl;
            for (int h = 0; h < 2; h++)
            {
                Vector3 end = h == 0 ? a : b, quarter = Point(h == 0 ? 0.25f : 0.75f);
                float half = Mathf.Max(0.02f, length * 0.5f);
                Dress(_half[h], end, mid, quarter, half, Width * (1f + 0.25f * pulse), h, false, age);
                Dress(_core[h], end, mid, quarter, half, CoreWidth, h, true, age);
                _half[h].Window(-1f, open, 0.04f); _core[h].Window(-1f, open, 0.04f);
                _half[h].Gain(1.2f + 0.6f * pulse);
                _half[h].Frame(age, alpha * shown * 0.95f, 0f);
                _core[h].Frame(age, alpha * shown, 0f);
            }
            if (overcharged && lit > 0.5f)
            {
                Dress(_over, a + Vector3.up * Above, b + Vector3.up * Above, mid + Vector3.up * Above, Mathf.Max(0.02f, length), CoreWidth * 1.3f, 2, true, age);
                _over.Window(-1f, 2f, 0.02f);
                _over.Frame(age, alpha * lit, 0f);
            }
            else _over.Hide();

            // The bolt inside it, the full span, a new shape on every strike, never sliding.
            float rate = overcharged ? 20f : ZackKitPaint.Strikes;
            int strike = Mathf.FloorToInt(age * rate);
            if (lit > 0.5f && alpha > 0.05f && !reduced)
            {
                Vector3 along = b - a;
                Vector3 x = along / Mathf.Max(1e-4f, length), z = Vector3.Cross(x, Vector3.up);
                if (z.sqrMagnitude < 1e-4f) z = Vector3.forward;
                z.Normalize();
                _bolt.Mesh = ZackKitPaint.BoltShape(strike, false);
                // Its line is laid straight between the ends and dropped by half the sag, so it stays inside the silk.
                _bolt.At.SetPositionAndRotation(a + Vector3.down * (sag * 0.66f), Quaternion.LookRotation(z, Vector3.Cross(z, x)));
                // A wire's size: its knees and its width a fifth of the 2 m cut's.
                _bolt.At.localScale = new Vector3(length / ZackKitPaint.BoltLength, 0.18f, 0.18f);
                _bolt.Window(-1f, 2f, 0.03f);
                _bolt.Tile(0.8f * length / ZackKitPaint.BoltLength, 1f, FxStudyKit.Hash(strike, 5), 0f);
                _bolt.Frame(age, alpha * lit, 0f);
            }
            else _bolt.Hide();

            // The sockets crackle on a beat while it is lit.
            float every = overcharged ? 0.22f : 0.35f;
            int beat = Mathf.FloorToInt(age / every);
            if (live && beat != _socketBeat) { _socketBeat = beat; _socketBorn = beat * every; }
            for (int h = 0; h < 2; h++)
                ZackKitPaint.PoseBurst(_socket[h], (h == 0 ? a : b) + Vector3.up * 0.04f, Vector3.up, live && !reduced ? _socketBorn + h * 0.06f : -1f, age, 0.45f);

            // A trip: a burst at the crossing and the strike on the court under it.
            ZackKitPaint.PoseBurst(_trip, tripAt + Vector3.up * Core.ZackRules.LiveWireSagHeight, Vector3.up, tripAgo >= 0f ? 0f : -1f, Mathf.Max(0f, tripAgo), 1f);
            if (tripAgo >= 0f) ZackKitPaint.PoseStrike(_strike, tripAt, 60f, 1.2f, tripAgo, 0.06f, 0.30f, age);
            else _strike.Hide();
        }

        // A silk piece at a wire's width: b13d's colours and churn, its ageing off.
        private static void Dress(Part piece, Vector3 tail, Vector3 head, Vector3 middle, float length, float width, int h, bool core, float age)
        {
            piece.Lay(tail, head, middle);
            piece.Size(length, 1f);
            piece.Life(0f, 0f);
            piece.Vec("_Near", Vector4.zero);
            piece.Vec("_Wobble", new Vector4(core ? 0.15f : 0.25f, 1.3f, core ? -0.8f : 0.9f, h * 0.23f));
            piece.Vec("_Ends", new Vector4(0.10f, 0.0f, 0.30f, 0f));
            piece.Colour("_Deep", ZackKitPaint.Amber);
            if (core) { piece.Colour("_Body", ZackKitPaint.LemonDeep); piece.Colour("_Fold", ZackKitPaint.Lemon); piece.Vec("_Stops", new Vector4(0.10f, 0.26f, 0.58f, 0f)); piece.Num("_Add", 0.80f); }
            else { piece.Colour("_Body", ZackKitPaint.Gold); piece.Vec("_Bands", new Vector4(1f, 0.025f, 0.50f, 0.85f)); piece.Vec("_Stops", new Vector4(0.12f, 0.46f, 0.80f, 0f)); piece.Num("_Add", 0.38f); }
            piece.Facing(width, h * 60f + (core ? 75f : 30f));
            // The picture is laid at b13d's own proportion to its width (one repeat about 4.3 widths long, the second read
            // 1.4 times finer), so a 9 cm wire wears the same shapes as the 75 cm trail, only smaller. It runs from the socket
            // in to the middle (a positive pan carries it toward the head) at a little over 1 m/s, the core faster.
            float tile = length / (4.27f * width);
            piece.Reads(tile, h * 0.31f, tile * 1.4f, h * 0.57f);
            piece.Pans(core ? 4.5f : 3.0f, 0.05f, core ? 7.5f : 5.5f, -0.08f);
            // Taut: no wave runs along a wire.
            piece.Wave(0f, 1f, 0f, 0f);
        }

        public void Hide()
        {
            foreach (var part in _root.GetComponentsInChildren<Transform>(true))
                if (part != _root && part.gameObject.activeSelf) part.gameObject.SetActive(false);
        }
    }
}

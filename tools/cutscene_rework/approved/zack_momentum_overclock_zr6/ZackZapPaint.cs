using UnityEngine;
using Part = TumbangPreso.Visual.ZackKitPaint.Part;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE OVERCLOCK ZAP, PAINTED: what `ZackZapLook` draws with its `Painted` switch on, in place of its seven line renderers
    /// and their ink edges. His stance and the owner's two-handed read stay as they are (*"BOTH hands must visibly
    /// participate"*): current bridges his hands, then leaves the lead palm for the rival.
    ///
    /// THE GAMEPLAY SHAPE THE PICTURE TELLS THE TRUTH ABOUT. Hold to lock a rival within 9 m; the lock builds, then Zaps them, and
    /// for 3 s after they are Grounded and cannot be Zapped again. So:
    ///   * SEARCHING: the bridge, and short z4 bolts spit a metre ahead of the palm and die.
    ///   * CONNECTED: a held z4 bolt, re-struck 28 times a second, palm to chest, with one long fork; under the rival z4's strike
    ///     and round it the soft ring of Full Throttle's ground circle (never a thin line), closing in as the host's lock builds.
    ///   * THE ZAP: a burst on the rival and z4's strike under them.
    ///   * GROUNDED: the bolt thins to cold copper and the ring goes grey: it cannot Zap them yet.
    /// ⚠️ GPU-DRIVEN, as `ZackMomentumPaint`: prebuilt meshes, transforms and property blocks only.
    /// </summary>
    public sealed class ZackZapPaint
    {
        private static readonly Color Copper = new Color(0.46f, 0.27f, 0.13f), Spent = new Color(0.56f, 0.53f, 0.50f);
        private const float RingRadius = 0.72f, RingWidth = 0.28f, RingFrom = 1.3f, RingTo = 0.62f;

        private readonly Part _bridge, _bridgeCross, _bolt, _boltCross, _fork, _forkCross, _sputter, _sputterCross, _ring, _star, _burst;
        private float _hitAge = -100f;
        private bool _wasGrounded;
        private int _lastTarget = -1;

        /// <summary>Whether the held bolt, and the bridge, are drawn this frame (diagnostics and capture traces).</summary>
        public bool BoltDrawn => _bolt.Shown;
        public bool BridgeDrawn => _bridge.Shown;

        public ZackZapPaint(Transform parent)
        {
            var go = new GameObject("ZackZapPaint") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var root = go.transform;
            Part Upright(string n) => new Part(n, root, ZackKitPaint.BoltShape(0, false), ZackKitPaint.Bolt);
            Part Crossed(string n) => new Part(n, root, ZackKitPaint.BoltShape(0, true), ZackKitPaint.Bolt);
            _bridge = Upright("zap-bridge"); _bridgeCross = Crossed("zap-bridge-cross");
            _bolt = Upright("zap-bolt"); _boltCross = Crossed("zap-bolt-cross");
            _fork = Upright("zap-fork"); _forkCross = Crossed("zap-fork-cross");
            _sputter = Upright("zap-sputter"); _sputterCross = Crossed("zap-sputter-cross");
            _ring = new Part("zap-lock", root, ZackKitPaint.RingMesh("throttle-ring", 33, 0f, Mathf.PI * 2f, RingRadius, RingWidth, 0f, 0f, 0, 2), ZackKitPaint.Streak);
            _star = new Part("zap-star", root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
            _burst = new Part("zap-burst", root, ZackKitPaint.Burst, ZackKitPaint.Sparks);
        }

        public void Hide()
        {
            _bridge.Hide(); _bridgeCross.Hide(); _bolt.Hide(); _boltCross.Hide(); _fork.Hide(); _forkCross.Hide();
            _sputter.Hide(); _sputterCross.Hide(); _ring.Hide(); _star.Hide(); _burst.Hide();
            _lastTarget = -1; _wasGrounded = false;
        }

        // A bolt held whole: `LayBolt` at a point of its life where it is fully drawn and nothing has let go.
        private static void Held(Part bolt, Part cross, Vector3 from, Vector3 to, int strike, int salt, float age, float opacity)
            => ZackMomentumPaint.LayBolt(bolt, cross, from, to, 0.3f, strike, salt, age, opacity);

        private static void Tint(Part part, Color colour)
        {
            part.Colour("_Deep", colour); part.Colour("_Body", colour); part.Colour("_Hot", colour); part.Colour("_HaloColor", colour * 0.5f); part.Num("_Add", 0.1f);
        }
        private static void Untint(Part part, Color deep, Color body, Color halo)
        {
            part.Colour("_Deep", deep); part.Colour("_Body", body); part.Colour("_Hot", ZackKitPaint.White); part.Colour("_HaloColor", halo); part.Num("_Add", Mathf.Min(0.80f, ZackKitPaint.Light));
        }

        /// <summary>
        /// One frame while the stance is up. `on` 0 to 1 is the stance, `rear` and `emitter` are the bridge's ends, `hasTarget` and
        /// `target` the rival it holds, `aimAt` their chest (or where he aims), `feet` theirs, `lockNow` 0 to 1, `grounded` whether it cannot
        /// Zap them yet, `reduced` the reduced-effects setting.
        /// </summary>
        public void Pose(float age, float on, Vector3 rear, Vector3 emitter, Vector3 aimAt, bool hasTarget, int target, Vector3 feet, float lockNow, bool grounded, bool reduced)
        {
            int strike = reduced ? 0 : Mathf.FloorToInt(age * 28f);
            Held(_bridge, _bridgeCross, rear, emitter, strike, 1, age, on);
            if (hasTarget)
            {
                if (target != _lastTarget) { _hitAge = age; _wasGrounded = false; }
                else if (grounded && !_wasGrounded) _hitAge = age;
                _wasGrounded = grounded;
                _lastTarget = target;
                _sputter.Hide(); _sputterCross.Hide();
                Held(_bolt, _boltCross, emitter, aimAt, strike, 2, age, on * (grounded ? (Mathf.Repeat(age, 0.12f) < 0.05f ? 0.3f : 0.75f) : 1f));
                if (grounded) { Tint(_bolt, Copper); Tint(_boltCross, Copper); }
                else
                {
                    Untint(_bolt, ZackKitPaint.LemonDeep, ZackKitPaint.Lemon, ZackKitPaint.LemonVeil);
                    Untint(_boltCross, ZackKitPaint.LemonDeep, ZackKitPaint.Lemon, ZackKitPaint.LemonVeil);
                }
                if (grounded || reduced) { _fork.Hide(); _forkCross.Hide(); }
                else
                {
                    Vector3 knee = Vector3.Lerp(emitter, aimAt, 0.5f);
                    Vector3 across = Vector3.Cross(aimAt - emitter, Vector3.up);
                    across = across.sqrMagnitude > 1e-4f ? across.normalized : Vector3.right;
                    float s = FxStudyKit.Hash(Mathf.FloorToInt(age * 9f), 61) < 0.5f ? -1f : 1f;
                    Held(_fork, _forkCross, knee, aimAt + Vector3.up * 0.35f + across * (0.75f * s), strike + 3, 3, age, on * 0.85f);
                }
                float closed = grounded ? 1f : Mathf.Clamp01(lockNow);
                float scale = Mathf.Lerp(RingFrom, RingTo, closed);
                _ring.At.SetPositionAndRotation(feet + Vector3.up * 0.03f, Quaternion.Euler(0f, age * 140f, 0f));
                _ring.At.localScale = new Vector3(scale, 1f, scale);
                _ring.Window(-1f, 2f, 0.03f);
                _ring.Tile(1.4f, 1f, 0f, 0f);
                if (grounded) { _ring.Colour("_Deep", Spent); _ring.Colour("_Body", Spent); _ring.Colour("_Hot", Spent); _ring.Colour("_HaloColor", Spent * 0.3f); }
                else { _ring.Colour("_Deep", ZackKitPaint.AmberDeep); _ring.Colour("_Body", ZackKitPaint.Amber); _ring.Colour("_Hot", ZackKitPaint.AmberHot); _ring.Colour("_HaloColor", ZackKitPaint.AmberVeil); }
                _ring.Frame(age, on * Mathf.Lerp(0.6f, 1f, closed), 0f);
            }
            else
            {
                _lastTarget = -1;
                _bolt.Hide(); _boltCross.Hide(); _fork.Hide(); _forkCross.Hide(); _ring.Hide();
                float phase = Mathf.Repeat(age, 0.17f) / 0.17f;
                int beat = Mathf.FloorToInt(age / 0.17f);
                Vector3 ahead = (aimAt - emitter).normalized;
                Vector3 to = emitter + ahead * Mathf.Lerp(0.7f, 1.2f, FxStudyKit.Hash(beat, 62)) + Vector3.up * ((FxStudyKit.Hash(beat, 63) - 0.5f) * 0.4f);
                ZackMomentumPaint.LayBolt(_sputter, _sputterCross, emitter, to, phase, strike, 4, age, on);
            }
            float since = age - _hitAge;
            if (since >= 0f && since < 0.45f && _lastTarget >= 0)
            {
                ZackKitPaint.PoseBurst(_burst, aimAt, Vector3.up, _hitAge, age, _wasGrounded ? 1.2f : 0.7f);
            }
            else _burst.Hide();
            // The mark under them: it fills with the lock, pops at the Zap and is eaten; Grounded leaves nothing under them but the grey ring.
            if (hasTarget && !_wasGrounded)
            {
                _star.At.SetPositionAndRotation(feet + Vector3.up * 0.025f, Quaternion.Euler(0f, 37f + age * 25f, 0f));
                _star.At.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.7f, lockNow);
                _star.Frame(age, on * Mathf.Lerp(0.55f, 0.9f, lockNow), Mathf.Lerp(0.3f, 0.1f, lockNow));
            }
            else if (hasTarget && since >= 0f && since < 0.45f) ZackKitPaint.PoseStrike(_star, feet, 37f, 2.0f, since, 0.05f, 0.4f, age);
            else _star.Hide();
        }
    }
}

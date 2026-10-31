using UnityEngine;
using Part = TumbangPreso.Visual.ZackKitPaint.Part;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BACKTRACK'S RETURN POINT AS A MARK ON THE COURT, what `ZackBacktrackEcho` draws with `MarkInsteadOfBody` on. The owner,
    /// playing with the walking echo: *"i get this weird ghost thing walking around"*, and his pick of the fixes, "b": a small
    /// ground mark where Backtrack would take him, no copy of his body.
    ///
    /// THE GAMEPLAY SHAPE THE PICTURE TELLS THE TRUTH ABOUT. While Backtrack is ready the spot it would take him to is wherever he
    /// stood 3 s ago (`ZackHeroKit.TryBacktrackPoint`), so the spot itself runs along his route 3 s behind him. A decal sliding
    /// along the road reads as a sticker being dragged, so the mark is PLANTED instead: it stays where it struck, and when the spot
    /// has moved more than `Replant` from it a new one strikes in at the spot and the old one is eaten outward where it lies.
    /// Standing still it does not move at all. Running, it steps after him like footfalls of lightning.
    ///   * THE PLANT is z4's STRIKE, the approved star of cracks on the road, small (`Size`) and turned by a fixed draw so no two
    ///     match. It lands whole with z4's quick overshoot, then its heart is eaten back so it settles to cracks with a dark
    ///     centre, and the eaten edge breathes in and out slowly: it is alive without blinking.
    ///   * THE PIN is z4's BOLT, crossed pair, stood upright and driven down into the newest plant: a short lightning tack that
    ///     strikes when the plant lands and again about every second and a half, with a small b13d spark burst at its foot. It is
    ///     what makes the spot read as "his" and as live current rather than paint, and it is the one thing tall enough to be
    ///     found across the court.
    ///   * THE SCORCH under each plant: the same star picture in dark umber that COVERS rather than lights (`_Add` 0), a little
    ///     larger and a little turned. On a sunlit warm court an amber light on a tan road is a value step of almost nothing
    ///     (the first film: the plant read as a pale fleck past a few metres); a dark burn under it is what gives it an edge.
    ///     It outlives its plant by 0.15 s when he runs, so the footfalls leave short-lived burns behind.
    /// The light is three approved pieces at their approved values, sized and placed by their transforms. The scorch is a new
    /// material on the approved strike picture (`Scorch`, below), not a change to `ZackKitPaint.Strike`.
    ///
    /// COST: at most three plants and three scorches (one draw each, two triangles), one pin (two draws of a prebuilt bolt
    /// shape) and one burst (one draw, 16 triangles): 9 draws, 4 materials, no per-frame mesh work.
    /// </summary>
    public sealed class ZackBacktrackMark
    {
        private const int Plants = 3;
        /// <summary>How far the spot may move from a plant before a new one strikes in (metres).</summary>
        public const float Replant = 1.4f;
        private const float Size = 1.15f, ScorchSize = 1.30f, PinTall = 0.80f, PinLife = 0.30f, EatOut = 0.30f, ScorchOut = 0.45f, MinPinGap = 0.7f;
        // Dark umber, warm (never a cold ink): the burn under the light.
        private static readonly Color Umber = new Color(0.20f, 0.09f, 0.03f), UmberDeep = new Color(0.12f, 0.05f, 0.02f);
        private static Material _scorch;

        /// <summary>The scorch: the approved strike picture and its outward eating, dark and covering instead of lit.</summary>
        private static Material Scorch
        {
            get
            {
                if (_scorch != null) return _scorch;
                _scorch = FxStudyKit.Ribbon("zack_ground_star", "erode_radial");
                if (_scorch == null) return null;
                _scorch.renderQueue = 3002; _scorch.hideFlags = HideFlags.HideAndDontSave;
                _scorch.SetColor("_Deep", UmberDeep); _scorch.SetColor("_Body", Umber); _scorch.SetColor("_Hot", Umber); _scorch.SetColor("_HaloColor", Umber);
                _scorch.SetVector("_Ends", Vector4.zero); _scorch.SetVector("_Scroll", Vector4.zero);
                _scorch.SetVector("_Cut", new Vector4(0.06f, 0.40f, 0f, 0f)); _scorch.SetVector("_Stops", new Vector4(0.40f, 0.74f, 0.96f, 0f));
                _scorch.SetFloat("_Polar", 1f); _scorch.SetFloat("_Sweep", 0.9f); _scorch.SetFloat("_Soft", 0.14f);
                _scorch.SetFloat("_Halo", 0f); _scorch.SetFloat("_Add", 0f); _scorch.SetFloat("_Gain", 1f); _scorch.SetFloat("_Emit", 0f);
                return _scorch;
            }
        }

        private readonly Transform _root;
        private readonly Part[] _plant = new Part[Plants], _burn = new Part[Plants];
        private readonly Vector3[] _at = new Vector3[Plants];
        private readonly float[] _born = new float[Plants], _retired = new float[Plants], _yaw = new float[Plants], _heldErode = new float[Plants];
        private readonly Part _pin, _pinCross, _burst;
        private int _current = -1, _planted;
        private float _pinBorn = -100f, _nextPin;

        public ZackBacktrackMark(Transform parent)
        {
            var go = new GameObject("ZackBacktrackMark") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _root = go.transform;
            for (int i = 0; i < Plants; i++)
            {
                _plant[i] = new Part("plant-" + i, _root, ZackKitPaint.FloorSquare, ZackKitPaint.Strike);
                _burn[i] = new Part("scorch-" + i, _root, ZackKitPaint.FloorSquare, Scorch);
                _born[i] = _retired[i] = -1f;
            }
            _pin = new Part("pin", _root, ZackKitPaint.BoltShape(0, false), ZackKitPaint.Bolt);
            _pinCross = new Part("pin-cross", _root, ZackKitPaint.BoltShape(0, true), ZackKitPaint.Bolt);
            _burst = new Part("pin-burst", _root, ZackKitPaint.Burst, ZackKitPaint.Sparks);
        }

        /// <summary>
        /// One frame on the caller's clock `now` (seconds). `show` is whether the spot is live on this screen; `spot` is where it is
        /// (his feet 3 s ago, on the court). With `show` false the standing plant is eaten away rather than cut. `visible` false
        /// keeps the mark's state and draws nothing this frame (his own first person, with the spot under his feet).
        /// </summary>
        public void Step(float now, bool show, Vector3 spot, bool visible = true)
        {
            if (show)
            {
                if (_current < 0 || Flat(spot - _at[_current]) > Replant) Plant(now, spot);
                if (now >= _nextPin && _current >= 0 && _retired[_current] < 0f) StrikePin(now);
            }
            else if (_current >= 0) { Retire(_current, now); _current = -1; }

            for (int i = 0; i < Plants; i++) PosePlant(i, now);
            PosePin(now);
            if (!visible) { foreach (var plant in _plant) plant.Hide(); foreach (var burn in _burn) burn.Hide(); _pin.Hide(); _pinCross.Hide(); _burst.Hide(); }
        }

        /// <summary>Gone at once (a round ended, the look is leaving): nothing is left standing for the next round.</summary>
        public void Clear()
        {
            for (int i = 0; i < Plants; i++) { _born[i] = _retired[i] = -1f; _plant[i].Hide(); _burn[i].Hide(); }
            _current = -1; _pinBorn = -100f;
            _pin.Hide(); _pinCross.Hide(); _burst.Hide();
        }

        public void Destroy()
        {
            if (_root == null) return;
            if (Application.isPlaying) Object.Destroy(_root.gameObject); else Object.DestroyImmediate(_root.gameObject);
        }

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        private void Plant(float now, Vector3 spot)
        {
            if (_current >= 0) Retire(_current, now);
            _planted++;
            _current = _planted % Plants;
            _at[_current] = spot; _born[_current] = now; _retired[_current] = -1f;
            _yaw[_current] = 360f * FxStudyKit.Hash(_planted, 31);
            if (now - _pinBorn >= MinPinGap) StrikePin(now);
        }

        private void Retire(int i, float now)
        {
            if (_born[i] < 0f || _retired[i] >= 0f) return;
            _heldErode[i] = Held(i, now);
            _retired[i] = now;
        }

        // The settled plant: heart eaten back to cracks, the eaten edge breathing slowly (about 2.4 s a breath).
        private float Held(int i, float now)
        {
            float since = now - _born[i];
            float breathe = 0.20f + 0.07f * Mathf.Sin(now * 2.6f + i * 2.1f);
            return Mathf.Lerp(0f, breathe, FxStudyKit.Ease(FxStudyKit.Run(since, 0.10f, 0.55f)));
        }

        private void PosePlant(int i, float now)
        {
            var plant = _plant[i];
            var burn = _burn[i];
            if (_born[i] < 0f) { plant.Hide(); burn.Hide(); return; }
            float since = now - _born[i];
            float land = FxStudyKit.Pop(FxStudyKit.Run(since, 0f, 0.07f));
            plant.At.position = _at[i] + Vector3.up * 0.02f;
            plant.At.rotation = Quaternion.Euler(0f, _yaw[i], 0f);
            plant.At.localScale = Vector3.one * (Size * Mathf.Lerp(0.5f, 1f, land));
            // The burn lands at once under the light (no overshoot: it is the road, not the charge) and stays a beat longer.
            burn.At.position = _at[i] + Vector3.up * 0.012f;
            burn.At.rotation = Quaternion.Euler(0f, _yaw[i] + 9f, 0f);
            burn.At.localScale = Vector3.one * (ScorchSize * Mathf.Lerp(0.8f, 1f, land));
            float erode, burnt;
            if (_retired[i] >= 0f)
            {
                float after = now - _retired[i];
                float gone = FxStudyKit.Run(after, 0f, EatOut), cooled = FxStudyKit.Run(after, 0f, ScorchOut);
                if (cooled >= 1f) { _born[i] = -1f; plant.Hide(); burn.Hide(); return; }
                erode = gone >= 1f ? 1f : Mathf.Lerp(_heldErode[i], 1f, FxStudyKit.Ease(gone));
                burnt = Mathf.Lerp(0.05f, 1f, FxStudyKit.Ease(cooled));
            }
            else { erode = Held(i, now); burnt = 0.05f; }
            if (erode >= 0.999f) plant.Hide(); else plant.Frame(now, 0.9f, erode);
            burn.Frame(now, 0.5f, burnt);
        }

        private void StrikePin(float now)
        {
            _pinBorn = now;
            _nextPin = now + 1.3f + 0.5f * FxStudyKit.Hash(Mathf.FloorToInt(now * 10f), 41);
        }

        // The pin: drawn down from its head into the plant in 50 ms, held, then eaten from the top; a small burst at its foot.
        private void PosePin(float now)
        {
            float age = now - _pinBorn;
            bool live = age >= 0f && age < PinLife && _current >= 0 && _born[_current] >= 0f && _retired[_current] < 0f;
            if (!live) { _pin.Hide(); _pinCross.Hide(); _burst.Hide(); return; }
            Vector3 foot = _at[_current] + Vector3.up * 0.02f, top = foot + Vector3.up * PinTall;
            float yaw = _yaw[_current] * Mathf.Deg2Rad;
            Vector3 across = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            Vector3 x = Vector3.down, z = Vector3.Cross(x, across).normalized;
            var turn = Quaternion.LookRotation(z, Vector3.Cross(z, x));
            int strike = Mathf.FloorToInt(age * ZackKitPaint.Strikes) + _planted;
            float drawn = FxStudyKit.EaseOut(FxStudyKit.Run(age, 0f, 0.05f)) * 1.1f;
            float tail = FxStudyKit.Ease(FxStudyKit.Run(age, 0.13f, PinLife)) * 1.1f - 0.1f;
            float scale = PinTall / ZackKitPaint.BoltLength;
            for (int k = 0; k < 2; k++)
            {
                bool cross = k == 1;
                var piece = cross ? _pinCross : _pin;
                piece.Mesh = ZackKitPaint.BoltShape(strike % ZackKitPaint.Shapes, cross);
                piece.At.SetPositionAndRotation(top, turn);
                // Uniform: a pin is z4's bolt made small, its corners and width in proportion, never stretched.
                piece.At.localScale = Vector3.one * scale;
                piece.Window(tail, drawn, 0.03f);
                piece.Tile(0.8f, 1f, FxStudyKit.Hash(strike, 7) + (cross ? 0.37f : 0f), 0f);
                piece.Frame(now, cross ? 0.85f : 1f, 0.8f * FxStudyKit.Run(age, 0.22f, PinLife));
            }
            ZackKitPaint.PoseBurst(_burst, foot + Vector3.up * 0.05f, Vector3.up, _pinBorn + 0.04f, now, 0.45f);
        }
    }
}

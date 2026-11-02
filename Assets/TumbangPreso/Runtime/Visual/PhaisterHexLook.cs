using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE CURSE: HEX MARK OVER A VICTIM, IN THE TWO LOOKS THE OWNER IS CHOOSING BETWEEN (`PhaisterLook`). Drawn by
    /// `VoodooCursePresenter` in place of its line button when `PhaisterLook.Direction` is not `Current`, from the same
    /// replicated mark age and armed flag, so it changes no rule and no timing.
    ///
    /// | | A, moonlit moths | B, sleepy cosmos |
    /// |---|---|---|
    /// | the mark | a fat cloth button, a crossed stitch through it, on a dark pool; a moth asleep on it, wings folded, breathing | a cosmic bubble: a dark disc under a light swirl, a few stars, a gloss crescent |
    /// | the 10 s fuse | a ring round the pool fills clockwise from twelve | the bubble's rim fills clockwise from twelve, and its swirl brightens |
    /// | armed | the moth wakes: wings spread and beat; the ring beats | the rim lights all round and the bubble beats; the swirl runs faster |
    /// | unused, at 25 s | eaten away | eaten away |
    ///
    /// Both face whoever is looking (billboarded in the vertex program), so they read from her eyes, the court and above.
    /// The fuse is a CLOCK rather than a brightness, because a brightness change is what daylight flattens (the board's note
    /// on today's hairline button). Cost: A two draws (one quad, one moth of three cards), B one draw (one quad).
    /// </summary>
    public sealed class PhaisterHexLook
    {
        /// <summary>Radius of A's pool and of B's bubble, metres.</summary>
        public const float RadiusA = 0.44f, RadiusB = 0.36f;
        /// <summary>A's moth, a share of the swarm moth (0.72 m span): the button's own width.</summary>
        public const float MothSize = 0.56f;
        /// <summary>How long an unused mark takes to leave at the end of its life.</summary>
        public const float ExpirySeconds = 0.8f;

        private static Mesh _moth;
        private readonly PhaisterLook.Style _style;
        private readonly GameObject _root, _markGo, _mothGo;
        private readonly Material _mark, _mothInk;

        public PhaisterHexLook(Transform parent, PhaisterLook.Style style)
        {
            _style = style;
            _root = new GameObject("PhaisterHexLook-" + style);
            _root.transform.SetParent(parent, false);
            _markGo = PhaisterLook.Draw("Mark", _root.transform, PhaisterLook.Quad);
            _mark = PhaisterLook.Paint(_markGo, "PhaisterCosmos");
            if (_mark != null)
            {
                _mark.SetFloat("_Mode", style == PhaisterLook.Style.A ? 2f : 1f);
                _mark.SetFloat("_Size", style == PhaisterLook.Style.A ? RadiusA : RadiusB);
                _mark.SetFloat("_Spin", 0.6f);
            }
            if (style == PhaisterLook.Style.A)
            {
                if (_moth == null)
                    _moth = PhaisterLook.Moths("PhaisterHexMoth", new[] { new PhaisterLook.MothRow { Size = 1f, WingHz = 5.0f, Phase = 30f } });
                _mothGo = PhaisterLook.Draw("Moth", _root.transform, _moth);
                _mothInk = PhaisterLook.Paint(_mothGo, "PhaisterMoths");
                _mothInk?.SetFloat("_Mode", 1f);
            }
            _root.SetActive(false);
        }

        public GameObject Root => _root;

        /// <summary>
        /// Draws the mark at <paramref name="centre"/>, <paramref name="age"/> seconds after it landed, armed or not, with
        /// <paramref name="clock"/> for its idle motion (the game's time; the film's clock).
        /// </summary>
        public void Pose(Vector3 centre, float age, bool armed, float clock)
        {
            if (!_root.activeSelf) _root.SetActive(true);
            float grow = Mathf.Clamp01(age / 0.28f);
            // Pops in a little past full and settles.
            float open = grow < 0.7f ? (1f - (1f - grow / 0.7f) * (1f - grow / 0.7f)) * 1.15f : Mathf.Lerp(1.15f, 1f, (grow - 0.7f) / 0.3f);
            float fuse = Mathf.Clamp01(age / VoodooRules.HexArmSeconds);
            // Unused at the end of its life it does not shred (round 1): A's ring drains, the moth lifts off and flies, then
            // the button sinks into its pool; B's bubble swells and POPS from the middle out.
            float pop = Mathf.Clamp01((age - (VoodooRules.HexMarkLifeSeconds - ExpirySeconds)) / ExpirySeconds);
            float since = armed || age >= VoodooRules.HexArmSeconds ? age - VoodooRules.HexArmSeconds : -1f;
            float wake = since >= 0f ? Mathf.Clamp01(since / 0.30f) : 0f;
            float size = _style == PhaisterLook.Style.A
                ? open * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, pop)))
                : open * (1f + 0.22f * Mathf.Sqrt(pop));
            if (_mark != null)
            {
                _mark.SetVector("_Centre", centre);
                _mark.SetFloat("_Open", Mathf.Max(0.0001f, size));
                _mark.SetFloat("_Fuse", fuse);
                _mark.SetFloat("_Armed", armed ? 1f : 0f);
                _mark.SetFloat("_Since", since);
                _mark.SetFloat("_Pop", pop);
                _mark.SetFloat("_Erode", 0f);
                _mark.SetFloat("_Age", clock);
                _mark.SetFloat("_Clock", clock);
            }
            if (_mothInk != null)
            {
                // It sits on the button's top edge, so the holes and the stitch stay in view below it.
                _mothInk.SetVector("_Perch", new Vector4(centre.x, centre.y, centre.z, MothSize * Mathf.Min(1f, open)));
                _mothInk.SetFloat("_PerchLift", RadiusA * 0.34f);
                // A beat before it arms the sleeper twitches: its wings shiver open a little, twice, then it wakes.
                float twitch = age > VoodooRules.HexArmSeconds - 0.35f && age < VoodooRules.HexArmSeconds
                    ? 0.22f * Mathf.Abs(Mathf.Sin((age - VoodooRules.HexArmSeconds) * 18f)) : 0f;
                _mothInk.SetFloat("_Open", pop > 0f ? 1f : Mathf.Max(wake, twitch));
                _mothInk.SetFloat("_Fade", Mathf.Clamp01(pop / 0.75f));
                _mothInk.SetFloat("_Age", clock);
            }
        }

        public void Hide() { if (_root != null && _root.activeSelf) _root.SetActive(false); }

        public void Free() { PhaisterLook.Free(_root); }
    }
}

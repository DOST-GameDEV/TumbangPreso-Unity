using System.Collections.Generic;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The comic callout: BONK, ZAP, TAGGED, KABOOM.
    ///
    /// ⚠️⚠️ REWRITTEN 2026-08-23 OFF THREE COMPLAINTS AT ONCE, 🧑: *"make sure as well to make
    /// the text popups feel better, bcz they feel diff earlier and weird and overwhelming bcz
    /// like 10 show up at once and u cant read and they were a weird font"*. Three separate
    /// faults, and none of them was fixable by tuning a number:
    ///
    ///  1. **The font really was wrong, and not by choice.** These were `TextMesh`, which is
    ///     3D text drawn straight off a font's atlas material. Darumadrop is a DYNAMIC font, so
    ///     Unity rebuilds that atlas whenever a new character is requested at a new size, and a
    ///     `TextMesh` does not rebuild its own mesh when that happens: its UVs keep pointing at
    ///     where its glyphs USED to be. What draws afterwards is other letters' pixels in the
    ///     shape of your string. It is intermittent, it depends on what else asked for text
    ///     that frame, and it looks exactly like "a weird font". A world-space `Canvas` with a
    ///     `Text` on it re-runs its own layout on `Font.textureRebuilt`, which is the whole
    ///     reason uGUI can use dynamic fonts and `TextMesh` cannot.
    ///
    ///  2. **Ten really did show up at once.** The cap was 4 and it evicted the OLDEST, so in a
    ///     hero exchange the callout that mattered (TAGGED, a knockdown, a penalty) was the
    ///     first one thrown away and four flavour hits survived it. It now evicts the LEAST
    ///     IMPORTANT, and the call sites that fired one per victim have been cut.
    ///
    ///  3. **They stacked into an unreadable column.** Six text meshes each (five ink offsets
    ///     plus the face), lifted 0.48 m per live popup, all billboarded to the same camera.
    ///     One `Text` with a `GodotOutline` is one graphic and one draw, and the ink is a real
    ///     eight-direction radius rather than five hand-placed copies.
    /// </summary>
    public sealed class ComicPopup : MonoBehaviour
    {
        /// <summary>
        /// How much a callout is worth keeping when the screen is full.
        ///
        /// ⚠️⚠️ THIS IS THE FIX FOR "TEN AT ONCE" AND IT IS NOT A CAP. A cap alone throws away
        /// whichever one it can reach; what a player needs is for the one that CHANGED THE
        /// SCORE to be the one that survives. Dante's ultimate fires a cast callout, a knockback
        /// per victim and an explosion callout in the same frame, and the score award lands a
        /// frame later: under the old oldest-first rule the score award was the thing evicted.
        /// </summary>
        public enum Weight
        {
            /// <summary>A hit landing on somebody. First to go.</summary>
            Flavour = 0,

            /// <summary>A power going off. One per cast, never one per victim.</summary>
            Cast = 1,

            /// <summary>Points, penalties, tags, the lata. Never evicted for anything below.</summary>
            Score = 2,
        }

        // ⚠️ THREE, NOT FOUR. Four 64 pt callouts stacked 0.48 m apart is taller than a player
        // model and covers the lata behind them, which is the one thing on the court nobody can
        // afford to lose track of.
        private const int MaxLive = 3;

        /// <summary>
        /// The size the glyphs are rasterised at, in pixels, and the height the callout is
        /// drawn at, in metres. Keep the ratio and the text stays the same size on screen.
        ///
        /// ⚠️⚠️ THESE TWO ARE THE ANTI-BLUR PAIR AND THEY MOVE TOGETHER. Raising one alone
        /// either fattens the text or softens it. 110 px into 0.48 m gives about 229 pixels per
        /// metre, which is oversampled at every distance a callout is ever read from.
        /// </summary>
        private const int RasterSize = 110;
        private const float WorldHeight = 0.48f;

        private const float Lifetime = 1.25f;
        private const float FloatSpeed = 1.1f;
        private const float StackLift = 0.42f;

        /// <summary>
        /// ⚠️⚠️ THE SAME WORD TWICE IN A THIRD OF A SECOND IS ONE EVENT, NOT TWO. Cheska's nova
        /// froze three players and fired three FREEZE callouts on top of each other; her frost
        /// sheet fired SLIP ZONE from the kit and SLIP AND SLIDE from the hazard, at the same
        /// point, on the same frame, saying the same thing. A duplicate refreshes the live one
        /// and gives it a small kick instead of stacking a second copy behind it.
        /// </summary>
        private const float DedupeSeconds = 0.35f;
        private const float DedupeMetres = 3.0f;

        /// <summary>
        /// ⚠️ A CALLOUT NOBODY CAN READ IS PURE CLUTTER. Flavour hits happening on the far side
        /// of a 17 m court are noise in the corner of the frame with no information in them;
        /// the events that change the score are worth drawing from anywhere.
        /// </summary>
        private const float FlavourVisibleRange = 15.0f;

        private static readonly List<ComicPopup> Live = new List<ComicPopup>(8);

        private Text _text;
        private CanvasGroup _group;
        private Transform _cameraTransform;
        private float _elapsed;
        private float _tiltAngle;
        private float _baseScale;
        private float _kick;

        private string _phrase;
        private Weight _weight;

        // ------------------------------------------------------------------ spawning

        public static void Spawn(Vector3 worldPos, string text, Color color, float scale = 1.0f)
            => Spawn(worldPos, text, color, scale, Weight.Cast);

        public static void Spawn(Vector3 worldPos, string text, Color color, float scale,
                                 Weight weight)
        {
            if (string.IsNullOrEmpty(text)) return;

            Live.RemoveAll(p => p == null);

            if (weight == Weight.Flavour && !WorthDrawing(worldPos)) return;

            // A repeat of something already on screen kicks the original rather than stacking.
            for (int i = 0; i < Live.Count; i++)
            {
                var live = Live[i];
                if (live._phrase != text) continue;
                if (live._elapsed > DedupeSeconds) continue;
                if ((live.transform.position - worldPos).sqrMagnitude > DedupeMetres * DedupeMetres) continue;

                live.Kick();
                return;
            }

            if (!MakeRoomFor(weight)) return;

            var go = new GameObject("ComicPopup_" + text);
            go.transform.position = worldPos + Vector3.up * (1.35f + Live.Count * StackLift);

            var popup = go.AddComponent<ComicPopup>();
            popup.Init(text, color, scale, weight);
            Live.Add(popup);
        }

        private static bool WorthDrawing(Vector3 worldPos)
        {
            var camera = UnityEngine.Camera.main;
            if (camera == null) return true;

            return (camera.transform.position - worldPos).sqrMagnitude
                   <= FlavourVisibleRange * FlavourVisibleRange;
        }

        /// <summary>
        /// Make space, by dropping the least important thing on screen.
        ///
        /// ⚠️ A FLAVOUR HIT NEVER EVICTS A SCORE CALLOUT. If everything live outranks the
        /// newcomer, the newcomer is the one that does not get drawn. That is the correct answer
        /// and it used to be impossible to express, because the queue only knew arrival order.
        /// </summary>
        private static bool MakeRoomFor(Weight weight)
        {
            while (Live.Count >= MaxLive)
            {
                int worst = 0;
                for (int i = 1; i < Live.Count; i++)
                {
                    // Lower weight loses; on a tie the older one loses, so a run of identical
                    // hits still cycles rather than freezing on the first.
                    if (Live[i]._weight < Live[worst]._weight) worst = i;
                }

                if (Live[worst]._weight > weight) return false;

                var doomed = Live[worst];
                Live.RemoveAt(worst);
                if (doomed != null) Destroy(doomed.gameObject);
            }

            return true;
        }

        private void OnDestroy() => Live.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Live.Clear();

        // ------------------------------------------------------------------ the named callouts

        // ⚠️ THE HIT CONFIRMS ARE `Flavour` AND THE SCORING ONES ARE NOT. This is the only place
        // the weights are assigned, so a new callout has to pick one rather than inheriting a
        // default that happens to outrank a tag.
        public static void Bonk(Vector3 pos) => Hit(pos, "BONK!", UiTheme.HeroEarthBright, 1.2f);
        public static void Zap(Vector3 pos) => Hit(pos, "ZAP!", UiTheme.HeroElectricBright, 1.2f);
        public static void Freeze(Vector3 pos) => Hit(pos, "FREEZE!", UiTheme.HeroIceBright, 1.2f);
        public static void Bam(Vector3 pos) => Hit(pos, "BAM!", UiTheme.HeroFire, 1.2f);
        public static void Whoa(Vector3 pos) => Hit(pos, "WHOA!", UiTheme.HeroIce, 1.1f);
        public static void Boo(Vector3 pos) => Hit(pos, "BOO!", UiTheme.HeroSpiritBright, 1.2f);
        public static void Clang(Vector3 pos) => Hit(pos, "CLANG!", UiTheme.Highlight, 1.15f);

        public static void Kaboom(Vector3 pos) => Spawn(pos, "KABOOM!", UiTheme.HeroFireBright, 1.45f, Weight.Cast);
        public static void Super(Vector3 pos) => Spawn(pos, "SUPER!", UiTheme.Highlight, 1.55f, Weight.Cast);
        public static void Wheee(Vector3 pos) => Spawn(pos, "WHEEE!", UiTheme.HeroElectricBright, 1.15f, Weight.Cast);

        /// <summary>A hit landing. Culled by distance and first to be evicted.</summary>
        public static void Hit(Vector3 pos, string text, Color colour, float scale)
            => Spawn(pos, text, colour, scale, Weight.Flavour);

        /// <summary>Points, a penalty, a tag, the lata. Outranks everything else on screen.</summary>
        public static void Score(Vector3 pos, string text, Color colour, float scale)
            => Spawn(pos, text, colour, scale, Weight.Score);

        // ------------------------------------------------------------------ the drawing

        private void Init(string text, Color colour, float scaleMultiplier, Weight weight)
        {
            _phrase = text;
            _weight = weight;
            _tiltAngle = Random.Range(-6.0f, 6.0f);

            // ⚠️⚠️ RASTERISED AT 110 AND SHOWN AT 0.48 m, WHICH IS THE ANSWER TO "IT FEELS
            // BLURRY". A dynamic font is rendered into its atlas at `fontSize` PIXELS and then
            // stretched to whatever the world scale asks for. At the old 64 pt the glyph was a
            // 64 px bitmap, and a 0.48 m callout seen from 4 m on a 1080p screen covers about
            // 112 px, so every letter was being blown up nearly two to one. `RasterSize` over
            // `WorldHeight` is the pixels-per-metre this is authored at; keep the product
            // constant and the text stays the same size on screen while getting sharper.
            _baseScale = (WorldHeight / RasterSize) * scaleMultiplier;

            // ⚠️⚠️ A WORLD-SPACE CANVAS, NOT A `TextMesh`. See the class note: `TextMesh` does
            // not survive a dynamic font atlas rebuild and Darumadrop is dynamic. This is the
            // fault behind "they were a weird font", and no amount of restyling reaches it.
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // ⚠️ A WORLD-SPACE SCALER WITH A DYNAMIC MULTIPLIER, so the atlas is rasterised
            // denser still than  asks for. The callout vocabulary is about twenty
            // short words, so the extra atlas costs nothing measurable and it buys headroom for
            // the case that actually bites: a callout going off two metres from the camera.
            // ⚠️ `uiScaleMode` IS DEAD ON A WORLD-SPACE CANVAS AND `dynamicPixelsPerUnit` IS NOT.
            // `CanvasScaler.Handle` tests `renderMode == WorldSpace` FIRST and returns through
            // `HandleWorldCanvas` before it ever reads the scale mode, so whichever of the three
            // modes is set here changes nothing. `dynamicPixelsPerUnit` is what that branch
            // actually uses, and on a world canvas it raises the RESOLUTION dynamic text is
            // rasterised at without touching the layout size. There is no `ScaleMode.WorldSpace`
            // to name; the enum has only the three screen modes.
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 2.0f;

            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(760, 150);

            _group = gameObject.AddComponent<CanvasGroup>();

            var textGo = new GameObject("Face");
            textGo.transform.SetParent(transform, false);

            _text = textGo.AddComponent<Text>();
            _text.font = MenuKit.Font;
            _text.text = text;
            _text.fontSize = RasterSize;
            _text.fontStyle = FontStyle.Bold;
            _text.color = colour;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;
            _text.alignByGeometry = true;
            MenuKit.Stretch(_text.rectTransform);

            // ⚠️ THE INK IS WHAT MAKES IT READABLE OVER ASPHALT, and it is the eight-direction
            // radius rather than uGUI's four corner copies. Five hand-placed offsets left the
            // top and sides of every stroke bare, which reads as a smeared drop shadow.
            var ink = textGo.AddComponent<GodotOutline>();
            ink.OutlineColour = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.95f);
            ink.Radius = 6.0f;

            transform.localScale = Vector3.zero;

            if (UnityEngine.Camera.main != null)
                _cameraTransform = UnityEngine.Camera.main.transform;
        }

        /// <summary>A duplicate arriving: refresh the life and give it a visible bump.</summary>
        private void Kick()
        {
            _elapsed = Mathf.Min(_elapsed, Lifetime * 0.35f);
            _kick = 0.16f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / Lifetime;

            if (t >= 1.0f)
            {
                Destroy(gameObject);
                return;
            }

            if (_cameraTransform == null && UnityEngine.Camera.main != null)
                _cameraTransform = UnityEngine.Camera.main.transform;

            if (_cameraTransform != null)
                transform.rotation = _cameraTransform.rotation * Quaternion.Euler(0, 0, _tiltAngle);

            // Rises fast and slows, so the eye is pulled up to it and then let go.
            transform.position += Vector3.up * (FloatSpeed * (1.0f - t * 0.55f) * Time.deltaTime);

            // ⚠️ THE OVERSHOOT IS 1.30, DOWN FROM 1.48. At the old value three callouts landing
            // together briefly filled a third of the screen each, which is most of why a hero
            // exchange read as a wall of text rather than as three things happening.
            float scale;
            if (t < 0.13f) scale = Mathf.Sin((t / 0.13f) * Mathf.PI * 0.5f) * 1.30f;
            else if (t < 0.26f) scale = Mathf.Lerp(1.30f, 1.0f, (t - 0.13f) / 0.13f);
            else scale = 1.0f;

            if (_kick > 0.0f)
            {
                _kick = Mathf.Max(0.0f, _kick - Time.deltaTime);
                scale += Mathf.Sin((_kick / 0.16f) * Mathf.PI) * 0.22f;
            }

            transform.localScale = Vector3.one * (_baseScale * scale);

            // ⚠️ ONE `CanvasGroup` FADES THE FACE AND ITS INK TOGETHER. Fading them separately
            // is how a ring of ink ends up floating over the court with nothing inside it.
            if (_group != null)
                _group.alpha = t > 0.6f ? 1.0f - (t - 0.6f) / 0.4f : 1.0f;
        }
    }
}

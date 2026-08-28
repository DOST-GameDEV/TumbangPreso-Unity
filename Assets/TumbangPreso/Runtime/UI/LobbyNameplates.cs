using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The name, the ready tick and the taya tag floating over each body in the lobby line.
    ///
    /// ⚠️⚠️ THEY ARE UI PROJECTED ONTO THE SURFACE, NOT WORLD-SPACE GEOMETRY IN THE ARENA. A
    /// world-space canvas inside the preview scene would be photographed by the preview camera
    /// and therefore baked into a 960x540 render texture, so every name would be resampled to
    /// roughly half resolution and the Darumadrop edges would go soft, which is the exact thing
    /// `ConvertedScreen.Start` turns `pixelPerfect` on to prevent. Drawing them on the real canvas
    /// and moving them to follow the projection keeps them crisp at the panel's own resolution.
    ///
    /// ⚠️ THE PROJECTION MAPS THE VIEWPORT INTO THE RAWIMAGE RECT, not into screen space. The
    /// surface is a render texture stretched across whatever rect it was given, so the camera's
    /// own aspect and the rect's aspect need not agree; going through the rect is correct in both
    /// cases, and going through `WorldToScreenPoint` is correct only when they happen to match.
    ///
    /// ⚠️⚠️ NOTHING HERE IS TINTED WITH `Offense` OR `Defense`. Those two colours mean "attacker"
    /// and "defender" and are the only colours in the game a player has to READ rather than merely
    /// see. `UiTheme.ForRole`'s note is explicit that the taya ROTATES every round, so a fixed
    /// per-seat role colour would tell the player the wrong thing for three rounds out of four.
    /// Cream for the name, amber for the taya tag, and nothing else.
    ///
    /// ⚠️⚠️ AND EVERY PLATE IS SIZED AGAINST ITS OWN STRING. A player name arrives from another
    /// machine and can be any width; legacy `Text` defaults to WRAP and everything `MenuKit` makes
    /// is `Overflow`, so an un-fitted plate either reflows out of its box or draws straight past
    /// it. This project has shipped that bug at least four times (`ConvertedScreen.SetHeadline`
    /// records three, `GameVersion.ApplyTo` the fourth). `MenuKit.Fit` is the shared answer and the
    /// plate is resized to what it measures.
    /// </summary>
    public sealed class LobbyNameplates : MonoBehaviour
    {
        /// <summary>Plate geometry, in the authored 1920x1080 space.</summary>
        private const float PlateHeight = 40.0f;
        private const float PlatePadding = 22.0f;
        private const float PlateMinWidth = 120.0f;
        private const float PlateMaxWidth = 420.0f;
        private const int NameSize = 22;

        /// <summary>The tag under the plate, for the seat that defends first.</summary>
        private const float TagHeight = 26.0f;
        private const int TagSize = 18;

        private RectTransform _surfaceRect;
        private MapPreviewSurface _surface;
        private LobbyCast _cast;

        private readonly RectTransform[] _plates = new RectTransform[Balance.PlayerCount];
        private readonly Image[] _plateFills = new Image[Balance.PlayerCount];
        private readonly Text[] _names = new Text[Balance.PlayerCount];
        private readonly RectTransform[] _tags = new RectTransform[Balance.PlayerCount];
        private readonly Text[] _tagLabels = new Text[Balance.PlayerCount];
        private readonly bool[] _shown = new bool[Balance.PlayerCount];

        public static LobbyNameplates Attach(RectTransform surfaceRect, MapPreviewSurface surface,
                                             LobbyCast cast)
        {
            if (surfaceRect == null || surface == null || cast == null) return null;

            var go = new GameObject("LobbyNameplates");
            go.transform.SetParent(surfaceRect, false);

            var rt = go.AddComponent<RectTransform>();
            MenuKit.Stretch(rt, 0.0f);

            var plates = go.AddComponent<LobbyNameplates>();
            plates._surfaceRect = surfaceRect;
            plates._surface = surface;
            plates._cast = cast;
            plates.Construct();

            return plates;
        }

        private void Construct()
        {
            for (int seat = 0; seat < _plates.Length; seat++)
            {
                var plate = new GameObject($"Plate{seat}");
                plate.transform.SetParent(transform, false);

                var fill = plate.AddComponent<Image>();
                fill.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
                fill.type = Image.Type.Sliced;
                fill.color = Color.white;

                // ⚠️ IT EATS NO CLICKS. The seat rows and the selectors are underneath this
                // full-surface layer, and `UiClickProbe` reports a control the player can see and
                // cannot press as unreachable, which is correct and is the single most confusing
                // failure a menu can have.
                fill.raycastTarget = false;

                var plateRect = fill.rectTransform;
                plateRect.anchorMin = Vector2.zero;
                plateRect.anchorMax = Vector2.zero;
                plateRect.pivot = new Vector2(0.5f, 0.0f);
                plateRect.sizeDelta = new Vector2(PlateMinWidth, PlateHeight);

                var name = MenuKit.Label(plate.transform, "", NameSize, UiTheme.Cream,
                                         Vector2.zero, Vector2.zero, Vector2.zero,
                                         TextAnchor.MiddleCenter);
                name.raycastTarget = false;
                MenuKit.Stretch(name.rectTransform, 0.0f);

                var tag = new GameObject($"Tag{seat}");
                tag.transform.SetParent(plate.transform, false);

                var tagFill = tag.AddComponent<Image>();
                tagFill.sprite = GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.Amber);
                tagFill.type = Image.Type.Sliced;
                tagFill.color = Color.white;
                tagFill.raycastTarget = false;

                var tagRect = tagFill.rectTransform;
                tagRect.anchorMin = new Vector2(0.5f, 0.0f);
                tagRect.anchorMax = new Vector2(0.5f, 0.0f);
                tagRect.pivot = new Vector2(0.5f, 1.0f);
                tagRect.anchoredPosition = new Vector2(0.0f, -4.0f);
                tagRect.sizeDelta = new Vector2(PlateMinWidth, TagHeight);

                var tagLabel = MenuKit.Label(tag.transform, "", TagSize, UiTheme.Amber,
                                             Vector2.zero, Vector2.zero, Vector2.zero,
                                             TextAnchor.MiddleCenter);
                tagLabel.raycastTarget = false;
                MenuKit.Stretch(tagLabel.rectTransform, 0.0f);

                _plates[seat] = plateRect;
                _plateFills[seat] = fill;
                _names[seat] = name;
                _tags[seat] = tagRect;
                _tagLabels[seat] = tagLabel;

                plate.SetActive(false);
            }
        }

        /// <summary>
        /// Writes one seat's plate. Called from the lobby's `Refresh`, so it must be cheap and
        /// must not allocate a rebuild.
        /// </summary>
        public void SetSeat(int seat, string displayName, bool ready, bool taya, bool you)
        {
            if (seat < 0 || seat >= _plates.Length) return;

            _shown[seat] = !string.IsNullOrEmpty(displayName);
            _plates[seat].gameObject.SetActive(_shown[seat]);

            if (!_shown[seat]) return;

            // ⚠️ THE TICK IS PART OF THE STRING RATHER THAN A SECOND GRAPHIC, so it is measured
            // and fitted along with the name instead of being a sprite that can overhang a plate
            // sized for text alone.
            string label = ready ? $"{displayName}   ✓" : displayName;
            if (you) label = $"{label}   ◀";

            var text = _names[seat];
            text.text = label;
            text.fontSize = NameSize;
            text.color = ready ? UiTheme.Cream : UiTheme.CreamMuted;

            // ⚠️⚠️ THE PLATE IS SIZED FROM THE MEASURED STRING, AND THEN THE STRING IS FITTED TO
            // WHAT THE PLATE ENDED UP BEING. Doing only the first lets a pasted 40-character name
            // stretch a plate wider than the screen; doing only the second shrinks a short name's
            // type for no reason. Together: grow to fit up to a cap, then shrink the type if the
            // cap was reached. See `MenuKit.Fit`.
            float wanted = Mathf.Clamp(text.preferredWidth + (PlatePadding * 2.0f),
                                       PlateMinWidth, PlateMaxWidth);

            _plates[seat].sizeDelta = new Vector2(wanted, PlateHeight);
            _tags[seat].sizeDelta = new Vector2(wanted, TagHeight);

            MenuKit.Fit(text, wanted - (PlatePadding * 2.0f));

            _plateFills[seat].color = ready ? Color.white : new Color(1.0f, 1.0f, 1.0f, 0.82f);

            _tags[seat].gameObject.SetActive(taya);

            if (!taya) return;

            var tagText = _tagLabels[seat];
            tagText.text = "TAYA FIRST";
            tagText.fontSize = TagSize;
            MenuKit.Fit(tagText, wanted - (PlatePadding * 2.0f));
        }

        /// <summary>
        /// ⚠️ IT FOLLOWS IN `LateUpdate`, AFTER `LobbyCast` HAS MOVED THE BODIES. Both the camera
        /// sway and the line's re-derivation from it happen there; reading the head point in
        /// `Update` would draw every plate one frame behind its body, which on a slowly swaying
        /// shot looks like the names are lagging on elastic.
        /// </summary>
        private void LateUpdate()
        {
            if (_surface == null || _cast == null || _surfaceRect == null) return;

            var camera = _surface.Camera;
            if (camera == null) return;

            Rect rect = _surfaceRect.rect;

            for (int seat = 0; seat < _plates.Length; seat++)
            {
                if (!_shown[seat]) continue;

                if (!_cast.TryHeadPoint(seat, out var world))
                {
                    _plates[seat].gameObject.SetActive(false);
                    continue;
                }

                var viewport = camera.WorldToViewportPoint(world);

                // ⚠️ A NEGATIVE Z IS A POINT BEHIND THE CAMERA, AND ITS X AND Y ARE MIRRORED
                // GARBAGE. Without this a body that the sway has swung behind the lens gets a
                // plate drawn on the opposite side of the screen, which reads as a stray label
                // rather than as an off-screen character.
                if (viewport.z <= 0.0f)
                {
                    _plates[seat].gameObject.SetActive(false);
                    continue;
                }

                _plates[seat].gameObject.SetActive(true);

                _plates[seat].anchoredPosition = new Vector2(
                    rect.xMin + (viewport.x * rect.width),
                    rect.yMin + (viewport.y * rect.height));
            }
        }
    }
}

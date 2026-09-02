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
        /// <summary>
        /// What is sitting in a seat, which decides what the plate is MADE of.
        ///
        /// ⚠️⚠️ IT IS A SURFACE DECISION AND NOT A LABEL ONE, WHICH IS THE WHOLE OF `docs/TODO.md`
        /// § 118.1 ROW 3. The lobby used to draw three identical filled plates reading `BOT`, and a
        /// player who has never played this game cannot tell whether that means "a bot is here" or
        /// "this seat is free". Both readings are reasonable and only one is true, so the screen was
        /// silently teaching half its players the wrong thing about the only question the lobby
        /// exists to answer.
        /// </summary>
        public enum SeatKind
        {
            /// <summary>A person. A full cream `Sheet`: the heaviest object in the row.</summary>
            Person,

            /// <summary>A bot. A `Tray`, so it reads as filled but recessed: something is there and
            /// it is not somebody.</summary>
            Bot,

            /// <summary>Nobody. A `Ghost`: an outline with almost nothing inside it.</summary>
            Open,
        }

        /// <summary>Plate geometry, in the authored 1920x1080 space.</summary>
        private const float PlateHeight = 40.0f;
        private const float PlatePadding = 22.0f;
        private const float PlateMinWidth = 120.0f;
        private const float PlateMaxWidth = 420.0f;
        private const int NameSize = 22;

        /// <summary>The tag under the plate, for the seat that defends first.</summary>
        private const float TagHeight = 26.0f;
        private const int TagSize = 18;

        /// <summary>The ready mark above the plate. Square, so it reads as a badge rather than as
        /// another label.</summary>
        private const float TickSize = 34.0f;
        private const int TickFont = 22;

        private RectTransform _surfaceRect;
        private MapPreviewSurface _surface;
        private LobbyCast _cast;

        /// <summary>
        /// What a plate does when it is pressed: take that chair.
        ///
        /// ⚠️⚠️ THE PLATE IS THE SEAT BUTTON NOW. 🧑 2026-08-28, pointing at the four `P1..P4` rows
        /// in the right-hand panel: *"i want to remove ts"*. They were the only way to move seats
        /// and they said the same thing the cast already says, twice, in a smaller font. Moving the
        /// press onto the plate over each character puts the control where the information is,
        /// which is the whole argument for standing the cast in the room in the first place.
        ///
        /// ⚠️ THE AUTHORED ROWS ARE HIDDEN, NOT DELETED, and they still carry their own handler.
        /// `ConvertedScreen` finds every control by name and logs an error on a miss, and the
        /// PRACTICE tab has no cast to click, so it keeps them. See
        /// `ConvertedMatchSetup.RefreshSeatRowVisibility`.
        /// </summary>
        private System.Action<int> _onSeatPressed;

        private readonly Button[] _buttons = new Button[Balance.PlayerCount];

        private readonly RectTransform[] _plates = new RectTransform[Balance.PlayerCount];
        private readonly Image[] _plateFills = new Image[Balance.PlayerCount];
        private readonly Text[] _names = new Text[Balance.PlayerCount];
        private readonly RectTransform[] _tags = new RectTransform[Balance.PlayerCount];
        private readonly Text[] _tagLabels = new Text[Balance.PlayerCount];

        /// <summary>The banner title strip, between the plate and the taya tag. See `SetSeat`.</summary>
        private readonly RectTransform[] _titles = new RectTransform[Balance.PlayerCount];
        private readonly Text[] _titleLabels = new Text[Balance.PlayerCount];

        /// <summary>
        /// The ready tick, which is its own mark ABOVE the plate rather than part of the name.
        ///
        /// ⚠️⚠️ IT WAS APPENDED TO THE NAME AND THAT IS WHY THE PLATE JUMPED WIDER THE MOMENT
        /// SOMEBODY READIED. The plate is sized from the measured string, so "✓" on the end
        /// of it added about 30 px to a box floating over a character's head: every ready press
        /// nudged the plate sideways under its own name. Above the plate the mark has its own
        /// space, it is the same size for a short name and a long one, and it is the thing the
        /// eye is scanning for when four people are getting ready.
        /// </summary>
        private readonly GameObject[] _ticks = new GameObject[Balance.PlayerCount];
        private readonly bool[] _shown = new bool[Balance.PlayerCount];

        public static LobbyNameplates Attach(RectTransform surfaceRect, MapPreviewSurface surface,
                                             LobbyCast cast, System.Action<int> onSeatPressed)
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
            plates._onSeatPressed = onSeatPressed;
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

                // ⚠️⚠️ THE PLATE'S SURFACE IS THE SEAT'S STATE, WHICH IS `docs/TODO.md` § 118.1
                // ROW 3 ANSWERED. Three identical wooden plates reading `BOT` could not tell a
                // player whether a bot was sitting there or whether the seat was free, and no
                // colour fixes that: an empty seat cannot be drawn with a filled surface however
                // it is painted. `SetSeat` swaps between `Sheet` (a person), `Tray` (a bot) and
                // `Ghost` (nobody), and a `Ghost` is two hairlines with almost nothing inside
                // them. Among Us is where the mechanism comes from, § 118.3.
                PaperSkin.Apply(plate, PaperCraft.Surface.Sheet);

                // ⚠️⚠️ THE PLATE ITSELF TAKES CLICKS AND NOTHING ELSE THIS COMPONENT DRAWS DOES.
                // It is the seat button (see `_onSeatPressed`), so it has to be hit-testable; the
                // name and the taya tag inside it must NOT be, or a press on the word lands on the
                // label instead of the button under it. `UiClickProbe` reports a control the
                // player can see and cannot press as unreachable, which is the single most
                // confusing failure a menu can have.
                fill.raycastTarget = true;

                int pressed = seat;
                var button = plate.AddComponent<Button>();
                button.targetGraphic = fill;

                // ⚠️⚠️ NO TINT TRANSITION, BECAUSE UNITY'S DISABLED GREY ATE THE PLAYER'S OWN
                // PLATE. `Button` defaults to `ColorTint`, and a non-interactable one multiplies
                // its target graphic by a pale grey: the local player's plate is deliberately
                // non-interactable (you cannot take the chair you are in), so in
                // `Logs/shots-runtime/Lobby-v12.png` the "YOU" plate is the one plate on screen
                // with no wooden box behind it, which reads as a rendering fault. This component
                // paints the fill itself in `SetSeat` for ready and not-ready, and two writers on
                // one graphic is the fault, not the grey.
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => _onSeatPressed?.Invoke(pressed));
                _buttons[seat] = button;

                var plateRect = fill.rectTransform;
                plateRect.anchorMin = Vector2.zero;
                plateRect.anchorMax = Vector2.zero;
                plateRect.pivot = new Vector2(0.5f, 0.0f);
                plateRect.sizeDelta = new Vector2(PlateMinWidth, PlateHeight);

                var name = MenuKit.Label(plate.transform, "", NameSize, UiTheme.PaperInk,
                                         Vector2.zero, Vector2.zero, Vector2.zero,
                                         TextAnchor.MiddleCenter);
                name.raycastTarget = false;
                MenuKit.Stretch(name.rectTransform, 0.0f);

                var tag = new GameObject($"Tag{seat}");
                tag.transform.SetParent(plate.transform, false);

                var tagFill = tag.AddComponent<Image>();

                // ⚠️ THE TAYA TAG IS THE ONE `Sign` OUT HERE, and it is the same accent rule the
                // room code follows: amber is a BAND under ink, never the colour of the lettering.
                // `ffba00` on `f4ecdd` measures 1.7:1 and is unreadable.
                PaperSkin.Apply(tag, PaperCraft.Surface.Sign);
                tagFill.raycastTarget = false;

                // ⚠️⚠️ IT SITS ABOVE THE PLATE NOW AND HE ASKED FOR THAT BY NAME. 🧑 2026-09-02,
                // with a crop of his own seat: **"Taya first is ugly and unreadable, too much
                // empty space too"**, *"maybe tighten its box and add outline to Taya first or
                // smth (its okay if player you and taya first boxes doesnt match), js keep
                // everything centered still"*, and **"ALSO i want taya first to be ABOVE the
                // player you, instead of it being button"**.
                //
                // **Three faults, and "instead of it being button" is the one that explains the
                // other two.** A full-plate-width dark bar hanging under a name, with its own cast
                // shadow, is the silhouette of a pressable control: `CLAUDE.md` § 6.3 says *one
                // that does nothing must not look pressable*, and this is that rule broken in the
                // one place a player is looking at four of them at once. The width came from
                // `wanted`, which is the NAME's width, so a long handle gave the badge a long
                // empty plaque and the two-word label floated in the middle of it, which is the
                // *"too much empty space"*.
                //
                // ⚠️ ABOVE RATHER THAN BELOW IS ALSO THE RIGHT READING ORDER. The role is what
                // this seat is about to DO and the name is who it is; the eye arrives at the head,
                // and the thing worth knowing before the name is that this one defends first.
                // The title strip stays below, where it belongs, because a title is a property OF
                // the name rather than a fact about the round.
                var tagRect = tagFill.rectTransform;
                tagRect.anchorMin = new Vector2(0.5f, 1.0f);
                tagRect.anchorMax = new Vector2(0.5f, 1.0f);
                tagRect.pivot = new Vector2(0.5f, 0.0f);
                tagRect.anchoredPosition = new Vector2(0.0f, 4.0f);
                tagRect.sizeDelta = new Vector2(PlateMinWidth, TagHeight + PaperCraft.Drop);

                // ⚠️⚠️ CREAM, BECAUSE THE TAG IS A WOOD PLAQUE. `PaperCraft.Surface.Sign` stopped
                // being a cream plate with an amber band on 2026-09-01 (🧑: *"this yellow dont look
                // good withh creme too btw"*) and became a dark wood plaque, so ink lettering on it
                // measures 1.2:1 and is invisible. `Logs/shots-runtime/Lobby-v54.png` shows exactly
                // that: a dark bar under the player name with nothing legible in it. **A colour
                // that was correct against one surface is not a colour.**
                var tagLabel = MenuKit.Label(tag.transform, "", TagSize, UiTheme.Cream,
                                             Vector2.zero, Vector2.zero, Vector2.zero,
                                             TextAnchor.MiddleCenter);
                tagLabel.fontStyle = FontStyle.Bold;
                tagLabel.raycastTarget = false;
                MenuKit.Stretch(tagLabel.rectTransform, 0.0f);

                // ⚠️ THE TITLE STRIP IS BUILT LIKE THE TAG AND COLOURED UNLIKE IT. Same shape,
                // because they stack and a stack of two different shapes reads as a mistake;
                // cream on dark wood rather than amber, because amber is this screen's accent and
                // spending it on both would leave TAYA FIRST competing with a cosmetic.
                var title = new GameObject($"Title{seat}");
                title.transform.SetParent(plate.transform, false);

                var titleFill = title.AddComponent<Image>();
                PaperSkin.Apply(title, PaperCraft.Surface.Tray);
                titleFill.raycastTarget = false;

                var titleRect = titleFill.rectTransform;
                titleRect.anchorMin = new Vector2(0.5f, 0.0f);
                titleRect.anchorMax = new Vector2(0.5f, 0.0f);
                titleRect.pivot = new Vector2(0.5f, 1.0f);
                titleRect.anchoredPosition = new Vector2(0.0f, -4.0f);
                titleRect.sizeDelta = new Vector2(PlateMinWidth, TagHeight);

                var titleLabel = MenuKit.Label(title.transform, "", TagSize,
                                               UiTheme.PaperInkSoft,
                                               Vector2.zero, Vector2.zero, Vector2.zero,
                                               TextAnchor.MiddleCenter);
                titleLabel.raycastTarget = false;
                MenuKit.Stretch(titleLabel.rectTransform, 0.0f);

                _titles[seat] = titleRect;
                _titleLabels[seat] = titleLabel;
                title.SetActive(false);

                // ⚠️ A CHILD OF THE PLATE, ANCHORED TO ITS TOP EDGE, so it follows the plate
                // without the projection having to place two things per seat. Pivot at the bottom
                // means it grows upward off the plate rather than into it.
                var tick = new GameObject($"Tick{seat}");
                tick.transform.SetParent(plate.transform, false);

                var tickFill = tick.AddComponent<Image>();
                PaperSkin.Apply(tick, PaperCraft.Surface.Token);
                tickFill.raycastTarget = false;

                var tickRect = tickFill.rectTransform;
                tickRect.anchorMin = new Vector2(0.5f, 1.0f);
                tickRect.anchorMax = new Vector2(0.5f, 1.0f);
                tickRect.pivot = new Vector2(0.5f, 0.0f);
                tickRect.anchoredPosition = new Vector2(0.0f, 6.0f);
                tickRect.sizeDelta = new Vector2(TickSize, TickSize);

                // ⚠⚠⚠ THE TICK IS A DRAWN CHALK MARK AND IT WAS THE CHARACTER `✓`, WHICH
                // THE GAME'S OWN FONT DOES NOT HAVE. Darumadrop One's cmap carries 525 glyphs and
                // U+2713 is not one of them, so Unity's dynamic-font fallback drew it from
                // whatever system face it found: a different typeface, weight and baseline from
                // every other mark in the game, **on the four plates that float over the cast in
                // the middle of the lobby**. `LobbyChrome.BuildIdentity` records the identical
                // fault for the pencil `✎` and fixed it by using a word; a tick has no word, so
                // it gets a shape. `UiMaterials.ChalkTick` carries the rest.
                var tickMark = new GameObject("ReadyTick", typeof(RectTransform), typeof(Image));
                tickMark.transform.SetParent(tick.transform, false);

                var tickImage = tickMark.GetComponent<Image>();
                tickImage.sprite = UiMaterials.ChalkTick();
                // ⚠️ THE MARK IS INK ON A PAPER TOKEN. Amber chalk on a cream chip is the same
                // 1.7:1 that the room code's lettering could not be drawn at.
                tickImage.color = UiTheme.PaperInk;
                tickImage.raycastTarget = false;
                tickImage.preserveAspect = true;
                MenuKit.Stretch(tickImage.rectTransform, -5.0f);

                _ticks[seat] = tick;
                tick.SetActive(false);

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
        public void SetSeat(int seat, string displayName, bool ready, bool taya, bool you,
                            bool canTake = false)
            => SetSeat(seat, displayName, "", ready, taya, you, canTake);

        /// <summary>
        /// ⚠️⚠️ THE TITLE IS THE BANNER, AND THE LOBBY IS ONE OF THE TWO PLACES IT IS DRAWN.
        /// `docs/TODO.md` § 101. A banner exists to say who you are next to your name, so it is
        /// worthless until somebody else can see it, and this is the screen where four people
        /// look at each other before a match.
        ///
        /// ⚠️⚠️ AND IT IS DELIBERATELY **NOT** ON THE IN-MATCH NAMEPLATE. `docs/VISION.md` § 2 is
        /// a readability budget for a 14 by 14 metre box holding four players, one lata, four
        /// tsinelas and up to twelve live abilities, and § 3's rule is blunt: *"the in-match HUD
        /// carries no sentences."* A title over every head during a round spends the budget on
        /// something nobody reads while they are being chased. **The banner belongs where people
        /// look at each other, which is the lobby and the end-of-match board.**
        ///
        /// ⚠️ IT IS THE LABEL, RESOLVED FROM THE ID BY `ProgressionRules.LabelForRewardId`, and
        /// an id this build has never heard of resolves to nothing and draws nothing. A peer on a
        /// newer build wearing a newer title is a plate with a name on it, not a plate with
        /// `mastery.zack.title.katuwang` on it.
        /// </summary>
        public void SetSeat(int seat, string displayName, string title, bool ready, bool taya,
                            bool you, bool canTake = false, SeatKind kind = SeatKind.Person)
        {
            if (seat < 0 || seat >= _plates.Length) return;

            // ⚠️ THE SURFACE IS SET BEFORE ANYTHING IS MEASURED. `PaperSkin.Rebuild` reads the
            // rect, and the width below is written from `preferredWidth`, so a swap made after
            // the measurement would be a frame late on the one control this screen is about.
            var skin = _plates[seat].GetComponent<PaperSkin>();
            if (skin != null)
            {
                skin.Surface = kind switch
                {
                    SeatKind.Open => PaperCraft.Surface.Ghost,
                    SeatKind.Bot => PaperCraft.Surface.Tray,
                    _ => PaperCraft.Surface.Sheet,
                };
                skin.Rebuild();
            }

            // ⚠️ THE SAME RULE THE SEAT ROWS FOLLOW, NOT A NEW ONE. Nobody may press a chair
            // somebody else is in, or their own, which would be a request that changes nothing.
            // The authority is still `LobbySession.TryTakeSeat`: this is the display half, and
            // greying it is what stops a live-looking plate that silently refuses.
            if (_buttons[seat] != null) _buttons[seat].interactable = canTake;

            _shown[seat] = !string.IsNullOrEmpty(displayName);
            _plates[seat].gameObject.SetActive(_shown[seat]);

            if (!_shown[seat]) return;

            // ⚠️ THE TICK IS NO LONGER PART OF THE STRING. See `_ticks`: appending it made the
            // plate resize itself every time somebody readied.
            if (_ticks[seat] != null) _ticks[seat].SetActive(ready);

            // ⚠⚠⚠ `◀` IS NOT IN THE GAME'S FONT EITHER, AND THIS ONE SHIPPED ON THE
            // PLAYER'S OWN NAMEPLATE. U+25C0 is absent from Darumadrop One's 525-glyph cmap
            // (U+25BC and U+25B2, the two carets the drawer headers use, ARE present and were
            // checked), so the marker identifying which of the four bodies is YOU was drawn by a
            // fallback system face at a foreign weight and baseline, three units from a name
            // drawn in the game's own.
            //
            // ⚠⚠ AND A WORD IS BETTER THAN A SHAPE HERE ANYWAY. An arrow pointing left from
            // the end of a name is a symbol the player has to be taught; `YOU` is not. This is the
            // same conclusion `LobbyChrome` reached for the pencil: **the game's font has letters
            // and it is missing exactly the symbols somebody reached for instead.**
            // ⚠️⚠️ THE MARKER IS AMBER AND THE NAME IS NOT, BECAUSE THEY ARE TWO DIFFERENT
            // THINGS IN ONE STRING. Measured off `Logs/shots-runtime/Lobby-v44.png`: drawn in the
            // name's own colour it read as `Player#1296 YOU`, one five-character-longer name,
            // rather than as a name with a marker on it. Amber is this screen's "look here"
            // colour and this is the only place on the cast it is spent.
            //
            // ⚠️ RICH TEXT RATHER THAN A SECOND LABEL, and that is a deliberate limit. The plate
            // sizes itself from `preferredWidth` and then fits the type back into what it got
            // (see `SetSeat`'s note below); a sibling label would have to be measured and placed
            // in the same pass, on a plate whose width depends on the string the sibling is part
            // of. Unity measures rich text with the tags stripped, so one label stays one
            // measurement.
            // ⚠️ THE `YOU` MARK IS DRAWN IN THE WOOD INK RATHER THAN IN AMBER, for the reason
            // every other accent on this screen moved: on cream, amber is a 1.7:1 word. The mark
            // is legible because it is BOLD and because your own plate is the only `Sheet` in the
            // row, which is two signals and neither of them is hue.
            string label = you
                ? $"{displayName}   <b>YOU</b>"
                : displayName;

            var text = _names[seat];
            text.text = label;
            text.fontSize = NameSize;
            text.color = ready ? UiTheme.PaperInk : UiTheme.PaperInkSoft;

            // ⚠️⚠️ THE PLATE IS SIZED FROM THE MEASURED STRING, AND THEN THE STRING IS FITTED TO
            // WHAT THE PLATE ENDED UP BEING. Doing only the first lets a pasted 40-character name
            // stretch a plate wider than the screen; doing only the second shrinks a short name's
            // type for no reason. Together: grow to fit up to a cap, then shrink the type if the
            // cap was reached. See `MenuKit.Fit`.
            // ⚠️⚠️ THE TITLE STRIP IS MEASURED TOO, AND IT WAS NOT UNTIL 2026-09-01. The plate
            // was sized from the NAME alone and the title strip was then given that same width,
            // so a title longer than the name overflowed silently. It never showed while a title
            // was one earned word (`TAGA-KANTO`); the moment the strip started carrying the
            // Hero Strike build as well (`docs/TODO.md` § 114.16), `Seismic Stomp / Demonic
            // Carapace` wanted **273 px in a 209 px box** on every plate in the lobby.
            // `LobbyStyleProbe.EveryLabelFitsItsBoxInBothStyles` is what found it.
            //
            // ⚠️ A PLATE IS SIZED FROM ITS CONTENT, AND THE TITLE IS CONTENT. Measuring it means
            // the strip below the name can push the plate wider, which is correct: the two strips
            // share an edge and a plate narrower than the thing hanging off it reads as broken.
            float titleWidth = 0.0f;
            if (!string.IsNullOrEmpty(title))
            {
                var probe = _titleLabels[seat];
                probe.text = title;
                probe.fontSize = TagSize;
                titleWidth = probe.preferredWidth;
            }

            float wanted = Mathf.Clamp(
                Mathf.Max(text.preferredWidth, titleWidth) + (PlatePadding * 2.0f),
                PlateMinWidth, PlateMaxWidth);

            _plates[seat].sizeDelta = new Vector2(wanted, PlateHeight);
            _tags[seat].sizeDelta = new Vector2(wanted, TagHeight + PaperCraft.Drop);

            MenuKit.Fit(text, wanted - (PlatePadding * 2.0f));

            // ⚠️⚠️ THE PLATE NO LONGER FADES WITH READINESS, AND THE TICK IS WHY. A cream sheet
            // at 0.82 alpha over a lit street picks up the street's colour, so an un-readied
            // player's plate came out a different HUE from a readied one rather than a different
            // strength: `CLAUDE.md` § 6.4's whole point about a colour tuned against one
            // background. Readiness is said by the tick above the plate and by the weight of the
            // name, both of which are opaque.
            _plateFills[seat].color = Color.white;

            // ⚠️⚠️ TWO STRIPS, STACKED, AND NEITHER MAY BORROW THE OTHER'S. The title strip and
            // the TAYA FIRST strip mean completely different things — one is who you are, one is
            // what you are about to do — and putting a title into the tag strip when there is no
            // taya would be two behaviours behind one control, which is exactly what
            // `SignInScreen`'s guest button was rebuilt to stop (`docs/TODO.md` § 97).
            //
            // ⚠️ THE TITLE SITS DIRECTLY UNDER THE PLATE AND TAYA GOES UNDER IT. A title is a
            // property of the name above it, so it belongs against the name; the round's role is
            // the outer fact and can afford the extra step away.
            bool hasTitle = !string.IsNullOrEmpty(title);

            _titles[seat].gameObject.SetActive(hasTitle);

            if (hasTitle)
            {
                _titles[seat].sizeDelta = new Vector2(wanted, TagHeight);
                _titles[seat].anchoredPosition = new Vector2(0.0f, -4.0f);

                float room = wanted - (PlatePadding * 2.0f);

                var titleText = _titleLabels[seat];
                titleText.text = title;
                titleText.fontSize = TagSize;
                MenuKit.Fit(titleText, room);

                // ⚠️⚠️ AND IT IS TRUNCATED WHEN EVEN THE WIDEST PLATE CANNOT HOLD IT, BECAUSE
                // `MenuKit.Fit` STOPS AT THE READABLE FLOOR AND THEN OVERFLOWS SILENTLY. That is
                // the shape of `ConvertedScreen.SetHeadline`'s note and of `GameVersion.ApplyTo`'s:
                // legacy `Text` does not shrink past what it is told and does not wrap here, so it
                // simply draws over the plate beside it. A player carrying a long earned title AND
                // two alternates can exceed `PlateMaxWidth`, and this is the one screen where the
                // overflow would land on somebody else's head.
                //
                // ⚠️ THE TAIL IS WHAT IS CUT, so the title the player earned survives and the
                // build is the half that fades. The build is printed in full on the result board,
                // which has a whole column for it.
                //
                // ⚠️ TWO ASCII DOTS, NOT AN ELLIPSIS CHARACTER. Darumadrop One has no `×` and
                // assuming it has `…` is the same bet one glyph further on; a missing glyph draws
                // as an empty box on the one label whose job is to say it has been shortened.
                if (titleText.preferredWidth > room)
                {
                    string full = titleText.text;
                    for (int keep = full.Length - 2;
                         keep > 1 && titleText.preferredWidth > room;
                         keep -= 2)
                        titleText.text = full.Substring(0, keep).TrimEnd() + "..";
                }
            }

            _tags[seat].gameObject.SetActive(taya);

            if (!taya) return;

            // ⚠️ THE OFFSET IS COMPUTED FROM WHETHER THE TITLE IS THERE, not from a second
            // constant. Two literals for one stack is how a layout ends up correct at exactly one
            // combination of states, which is fault 3 of § 92.1 in miniature.
            _tags[seat].anchoredPosition = new Vector2(
                0.0f, hasTitle ? -(4.0f + TagHeight + 2.0f) : -4.0f);

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

                // ⚠️⚠️ THE VIEWPORT MAPS STRAIGHT ONTO THE RECT'S SIZE, WITH NO `rect.xMin`, AND
                // ADDING ONE PUT EVERY PLATE IN THE BOTTOM-LEFT CORNER OF THE SCREEN. A plate is
                // anchored at (0,0), so its `anchoredPosition` is ALREADY measured from the
                // parent's bottom-left; `rect.xMin` on a stretched rect whose pivot is centred is
                // minus half the width, so adding it subtracted half a screen twice over. It read
                // as four stray "BOT" chips stacked over the BACK button, which looks like a
                // layout bug in the chrome rather than a projection one.
                _plates[seat].anchoredPosition = new Vector2(
                    viewport.x * rect.width,
                    viewport.y * rect.height);
            }
        }
    }
}

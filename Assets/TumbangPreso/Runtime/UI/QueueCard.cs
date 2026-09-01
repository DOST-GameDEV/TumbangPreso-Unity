using System;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The queue, on the lobby, in the corner the player is already looking at.
    ///
    /// ⚠️⚠️ THE ONE THING ON THIS SURFACE IS "AM I IN THE QUEUE, AND IS IT STILL WORKING",
    /// READABLE FROM ACROSS THE ROOM. `FUTURE.md` § 0.5b's phase 7 row is the brief and it is
    /// unusually specific: "a queue state on the mode screen, not a screen of its own", and the
    /// trap is "a spinner is not a state. Say the mode, the time elapsed, and how to cancel, and
    /// never block the menu behind it". All four are asserted by `QueueCardLayoutProbe`.
    ///
    /// ⚠️⚠️ IT DOES NOT COVER THE LOBBY AND IT HAS NO SCRIM, WHICH IS `CLAUDE.md` § 6.2c QUESTION
    /// 3 ANSWERED RATHER THAN INHERITED. `LobbyJoinPanel` has a 68 per cent scrim because it is a
    /// modal asking a question, and everything on it must be read against a live 3D street. This
    /// is the opposite: the player is queueing so that they can carry on doing something else,
    /// and every word on it already sits on an opaque wood plate. A scrim here would dim the
    /// lobby, the cast and the chat for no legibility at all, and it would eat every click on the
    /// screen underneath. **The card is a plate in a corner, and it blocks exactly its own
    /// rectangle.**
    ///
    /// ⚠️⚠️ AND ITS WIDTH IS MEASURED AGAINST ITS CONTENT, WHICH IS § 6.2c QUESTION 1. The widest
    /// line it ever draws is `MatchmakingRules.TayaRotationPromise` wrapped to two lines, and the
    /// card is <see cref="CardWidth"/> because that is that sentence plus one margin either side.
    /// It is not a fraction of the window: `AspectSafeCanvas` scales on the short axis, so a
    /// percentage is two very different widths at 4:3 and on the short wide window 🧑 actually
    /// plays in (`docs/TODO.md` § 100).
    ///
    /// ⚠️ EVERY ROW COMES OUT OF A LAYOUT GROUP AND NOT OUT OF A Y OFFSET. `FUTURE.md` § 0.5b:
    /// "built out of `UiRows`, never out of hand-written offsets". `UiRows` itself is a settings
    /// ROW kit and this is not a settings-shaped screen, so it uses the same discipline rather
    /// than the same file: one `VerticalLayoutGroup`, `LayoutElement` heights, no offsets.
    /// </summary>
    public sealed class QueueCard : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ 560 UNITS IS THE TAYA SENTENCE AT 18 UNITS OVER TWO LINES PLUS A 24-UNIT MARGIN
        /// EITHER SIDE. It is a content measurement, not a taste, and it is the number to change
        /// if that sentence changes.
        /// </summary>
        private const float CardWidth = 560.0f;

        private const float Pad = 24.0f;
        private const float RowHeight = 34.0f;
        private const float BarHeight = 12.0f;

        /// <summary>
        /// Where the door sits: the centre of a <see cref="DoorHeight"/> button, that far up from
        /// the bottom edge.
        /// </summary>
        private const float DoorCentreY = 96.0f;
        private const float DoorHeight = 64.0f;

        /// <summary>
        /// ⚠️⚠️ 280 UNITS OF CARD HUNG OFF A CENTRE AT 96, SO ITS BOTTOM 44 UNITS WERE UNDER THE
        /// BOTTOM OF THE SCREEN AND THE CANCEL BUTTON WAS THE THING IN THEM. 🧑 2026-09-01, over a
        /// shot of the lobby mid-queue: *"bug with quick mat ui"*. `MenuKit.Place` PIVOTS AT
        /// (0.5, 0.5), so the offset it takes is a CENTRE and not an edge: a 280-tall card placed
        /// at y 96 spans -44 to 236. **The one control on the card that the player needs, the way
        /// out of the queue, was the one control off the screen.**
        ///
        /// ⚠️ THE CARD IS PLACED FROM THE DOOR'S BOTTOM EDGE NOW, ARITHMETICALLY, because the two
        /// are the two states of one control (see `Refresh`) and a state change that moves the
        /// furniture reads as the screen jumping. `CustomCharacterScreen.LeftAt` records the same
        /// pivot trap one screen over: *"passing a left margin straight in is the mistake, and it
        /// is silent"*.
        ///
        /// ⚠️ 348 AND IT WAS 280: PHASE 11'S OFFER IS TWO MORE ROWS (a 48-unit button and a
        /// 20-unit line of disclosure) PLUS THE COLUMN'S 8-UNIT SPACING TWICE. The number is the
        /// content added up, which is `CLAUDE.md` § 6.2c question 1, and the two rows are
        /// deactivated rather than absent so the card does not change size when the offer lands
        /// under the player's cursor.
        /// </summary>
        private const float CardHeight = 348.0f;

        private const float CardCentreY = DoorCentreY - (DoorHeight * 0.5f) + (CardHeight * 0.5f);

        /// <summary>
        /// How wide a line inside the card actually is.
        ///
        /// ⚠⚠ IT IS READ FROM THE RECT WHEN THE CARD IS DOCKED AND FROM THE CONSTANT WHEN IT IS
        /// NOT, because in the rail the WIDTH IS THE RAIL'S and `CardWidth` is a number this class
        /// no longer decides. Fitting a headline to 560 inside a 460 rail is
        /// `CLAUDE.md` § 6.2c question 4 exactly: a width measured against a box the control does
        /// not live in, failing silently because `MenuKit.Label` overflows rather than wrapping.
        /// </summary>
        private float InnerWidth
        {
            get
            {
                float width = CardWidth;

                if (_docked && _card != null)
                {
                    float measured = ((RectTransform)_card.transform).rect.width;
                    if (measured > 1.0f) width = measured;
                }

                return width - (Pad * 2.0f);
            }
        }

        private Matchmaker _queue;

        private GameObject _card;
        private Text _headline;
        private Text _band;
        private Text _elapsed;
        private Text _promise;
        private Image _barFill;
        private Button _cancel;
        private Button _open;

        /// <summary>PHASE 11's offer: the button, its one line of disclosure, and the label so the
        /// count can be rewritten as people arrive. See <see cref="BotFillRules"/>.</summary>
        private Button _fill;
        private Text _fillLabel;
        private Text _fillCaveat;

        /// <summary>Raised when the queue lands the player in a match, so the lobby redraws.</summary>
        public event Action Joined;

        /// <summary>
        /// Raised when the player accepts PHASE 11's bot offer.
        ///
        /// ⚠⚠ THIS CARD DOES NOT START THE MATCH AND MUST NOT LEARN HOW. The lobby owns every
        /// decision a match start needs (the map, the seats, the mode, whether the room is
        /// networked) and `ConvertedMatchSetup.StartMatch` is the one path through them.
        /// A second start path here would be `docs/TODO.md` § 38.5's three dead protocols
        /// arriving one convenience at a time: the maintained one is the one nothing calls.
        /// </summary>
        public event Action StartWithBots;

        /// <summary>Raised with a line for the lobby's own status label, so this never becomes a
        /// second place that reports network failures. Same contract as `LobbyJoinPanel.Status`.</summary>
        public event Action<string> Status;

        /// <summary>
        /// Build the door and the card under <paramref name="parent"/>.
        ///
        /// ⚠️⚠️ THE DOOR IS A BUTTON THAT LOOKS PRESSABLE AND SAYS WHAT IT DOES, WHICH IS
        /// `CLAUDE.md` § 6.3's FIRST RULE. § 96 is the entry where the player hub had exactly one
        /// door, a corner chip stating a name and a level, and "the person who commissioned the
        /// hub never found it". A queue reached by opening the join card and then finding a third
        /// tab would be the same mistake with a different feature behind it.
        ///
        /// ⚠️ AND IT IS ONE DOOR, NOT A SECOND ONE BESIDE `JOIN A GAME`. They answer different
        /// questions: JOIN is "put me in my friend's room" and QUICK MATCH is "find me a room".
        /// The rule § 0.5b bans is adding a door to fix a FINDABILITY problem; this is a
        /// destination that did not exist before.
        /// </summary>
        /// <summary>
        /// Put the queue in the lobby's action rail, under the primary.
        ///
        /// ⚠⚠⚠ IT USED TO FLOAT IN THE MIDDLE OF THE SCREEN AND THAT WAS THE BIGGEST SINGLE
        /// THING WRONG WITH THE LOBBY. 🧑 2026-09-01: *"our UI is ugly and repetitive and
        /// unimaginative"*. QUICK MATCH was a 560-unit amber bar across the bottom centre, over
        /// the cast, and it was **the loudest control on the screen while not being the primary
        /// one**: START MATCH is. `game-ui-design` puts position first among the ordering tools
        /// and calls the result of getting it wrong `UI Blocking Action`; two accented controls
        /// competing is not a hierarchy, it is a coin toss the player has to make every time.
        ///
        /// **The rail is the PLAY column** and both ways of starting a game belong in it: START
        /// MATCH is "these seats, now" and QUICK MATCH is "find me people". One under the other,
        /// one accent, and the centre of the screen goes back to the cast.
        ///
        /// ⚠⚠ THE CARD IS A RAIL CHILD TOO, NOT A FLOATING PLATE, and that is what deletes
        /// § 115.2 rather than fixing it again. A plate placed by hand had to be positioned against
        /// an edge, and `MenuKit.Place` pivots at the centre, so 44 units of it were under the
        /// bottom of the screen with CANCEL in them. A child of a `VerticalLayoutGroup` cannot be
        /// off the screen: the rail grows upward from its own bottom margin.
        /// </summary>
        public static QueueCard Dock(Transform rail)
        {
            var go = new GameObject("QueueCard", typeof(RectTransform));
            go.transform.SetParent(rail, false);

            var card = go.AddComponent<QueueCard>();
            card._docked = true;
            card.Construct();
            return card;
        }

        /// <summary>True when this lives inside the lobby's rail rather than on the canvas.</summary>
        private bool _docked;

        public static QueueCard Build(Transform parent)
        {
            // ⚠️⚠️ A STRETCHED `RectTransform`, AND WITHOUT IT QUICK MATCH DREW IN THE MIDDLE OF
            // THE SCREEN, ACROSS THE CAST'S HEADS. 🧑 2026-09-01, over a screenshot of the lobby:
            // *"fucked up UI"*. `new GameObject("QueueCard")` comes with a plain `Transform`, and
            // nothing here adds a `Graphic` to this object itself, so it never acquired a
            // `RectTransform` the way `LobbyJoinPanel` does by putting its scrim on its own root.
            // **A `RectTransform` whose parent is a plain `Transform` has no parent rect to
            // resolve against**, so every anchor below it resolved against a zero-sized point at
            // the canvas centre: the door is anchored to the BOTTOM edge, 96 units up, and it
            // landed 96 units above the middle of the window instead.
            //
            // ⚠️⚠️ THIS IS `SplashScreen.BuildSurface`'S "THE LOGO IS A POSTAGE STAMP" NOTE AND
            // `CLAUDE.md` § 6.2c QUESTION 1, FOR THE THIRD TIME: **a size and a position are only
            // correct against the rectangle they are actually measured against**, and when that
            // rectangle does not exist the failure is silent. `QueueCardLayoutProbe` was green
            // throughout, because every row inside the card fits the card: the card was in the
            // wrong place on the screen, and no probe in this repository looks at that.
            var go = new GameObject("QueueCard", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuKit.Stretch((RectTransform)go.transform, 0.0f);

            var card = go.AddComponent<QueueCard>();
            card.Construct();
            return card;
        }

        private void Construct()
        {
            _queue = Matchmaker.Ensure();
            _queue.Changed += Refresh;
            _queue.Joined += OnQueueJoined;

            // ⚠️ A DOCKED QUEUE IS A ROW IN A COLUMN AND OWNS NO POSITION OF ITS OWN. The rail's
            // `VerticalLayoutGroup` controls width and height, so this object contributes a
            // height and nothing else. See `Dock`.
            if (_docked)
            {
                var group = gameObject.AddComponent<VerticalLayoutGroup>();
                group.spacing = 10.0f;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;

                var fitter = gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                gameObject.AddComponent<LayoutElement>().flexibleHeight = 0.0f;

                BuildDockedDoor();
                BuildCard();
                Refresh();
                return;
            }

            // ---- the door -------------------------------------------------------------
            //
            // ⚠️ AMBER, WHICH IS THE ONE ACCENT ON THIS SCREEN AND IS SPENT ON THE ONE ACTION.
            // `FUTURE.md` § 0.5b: "one accent, used for the one thing", and colour is the LAST
            // ordering tool rather than the first. The lobby's other controls are plain wood.
            _open = MenuKit.WoodButton(transform, "QUICK MATCH", new Vector2(0.5f, 0.0f),
                                       new Vector2(0.0f, DoorCentreY),
                                       new Vector2(CardWidth, DoorHeight),
                                       OnQuickMatchPressed, "WoodAmberButton");
            _open.name = "QuickMatchButton";

            BuildCard();
            Refresh();
        }

        /// <summary>
        /// QUICK MATCH as a rail row: the same words, one step down in weight from the primary.
        ///
        /// ⚠⚠ PLAIN WOOD WITH A CHALK RULE UNDER IT, NOT AMBER. The old note on this control
        /// said *"amber, which is the one accent on this screen and is spent on the one action"*,
        /// and that was written when the card was alone in the middle of the screen. In the rail
        /// it sits directly under START MATCH, which IS the one action, so two accents would be
        /// two primaries eight units apart. The chalk rule is what keeps it from reading as a
        /// disabled control: it says "this is a way in" without claiming to be THE way in.
        /// </summary>
        private void BuildDockedDoor()
        {
            var holder = new GameObject("QuickMatchRow", typeof(RectTransform));
            holder.transform.SetParent(transform, false);

            var element = holder.AddComponent<LayoutElement>();
            element.minHeight = DoorHeight + 14.0f;
            element.preferredHeight = DoorHeight + 14.0f;
            element.flexibleHeight = 0.0f;

            _open = MenuKit.WoodButton(holder.transform, "QUICK MATCH", new Vector2(0.5f, 1.0f),
                                       new Vector2(0.0f, -(DoorHeight * 0.5f)),
                                       new Vector2(CardWidth, DoorHeight),
                                       OnQuickMatchPressed);
            _open.name = "QuickMatchButton";

            // ⚠️ THE BUTTON STRETCHES TO THE RAIL AND THE RULE FOLLOWS IT. `MenuKit.WoodButton`
            // takes a size, and a fixed 560 in a 460 rail would hang 50 units off each end; the
            // rail controls width, so the rect is re-anchored to fill it.
            var buttonRect = (RectTransform)_open.transform;
            buttonRect.anchorMin = new Vector2(0.0f, 1.0f);
            buttonRect.anchorMax = new Vector2(1.0f, 1.0f);
            buttonRect.offsetMin = new Vector2(0.0f, -DoorHeight);
            buttonRect.offsetMax = Vector2.zero;

            var rule = UiMaterials.Underline(holder.transform, 0.0f, 0.0f, UiTheme.Amber);
            var ruleRect = rule.rectTransform;
            ruleRect.anchorMin = new Vector2(0.0f, 0.0f);
            ruleRect.anchorMax = new Vector2(1.0f, 0.0f);
            ruleRect.pivot = new Vector2(0.5f, 0.0f);
            ruleRect.offsetMin = new Vector2(18.0f, 2.0f);
            ruleRect.offsetMax = new Vector2(-18.0f, 10.0f);

            FocusRing.Attach(_open.gameObject, 4.0f);
        }

        private void BuildCard()
        {
            _card = new GameObject("QueueState");
            _card.transform.SetParent(transform, false);

            var plate = _card.AddComponent<Image>();
            plate.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            plate.type = Image.Type.Sliced;
            plate.color = Color.white;

            // ⚠️ IT BLOCKS ITS OWN RECTANGLE AND NOTHING ELSE. `CLAUDE.md` § 6.2c question 4:
            // anything covering the screen is also eating clicks, and naming the blocker is part
            // of shipping it. This plate is the blocker for the card, the card is 560 by about
            // 260, and every other pixel of the lobby stays live on purpose: a player waiting in
            // a queue is meant to be able to keep talking in chat and keep reading the code.
            plate.raycastTarget = true;

            if (_docked)
            {
                // ⚠️ THE RAIL DECIDES THE WIDTH AND THIS DECIDES THE HEIGHT. A docked card cannot
                // be placed off the screen, which is what § 115.2 was.
                var docked = _card.AddComponent<LayoutElement>();
                docked.minHeight = CardHeight;
                docked.preferredHeight = CardHeight;
                docked.flexibleHeight = 0.0f;
            }
            else
            {
                MenuKit.Place(plate.rectTransform, new Vector2(0.5f, 0.0f),
                              new Vector2(0.0f, CardCentreY), new Vector2(CardWidth, CardHeight));
            }

            var column = new GameObject("Column");
            column.transform.SetParent(_card.transform, false);

            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset((int)Pad, (int)Pad, (int)Pad, (int)Pad);
            layout.childControlWidth = true;

            // ⚠️ TRUE, SO `LayoutElement.preferredHeight` IS ACTUALLY READ. `LobbyJoinPanel`
            // records what leaving this off costs: every heading was 0 px tall and only drew at
            // all because a legacy `Text` set to Overflow renders outside its rect.
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            MenuKit.Stretch(column.GetComponent<RectTransform>(), 0);

            // ---- the headline: the ONE thing, and it is the biggest text on the card ----
            _headline = Row(column.transform, "SEARCHING FOR A MATCH", 30, UiTheme.Amber, 38);

            // ---- the band, which is the widening made into words ------------------------
            _band = Row(column.transform, "", 20, UiTheme.Cream, 26);

            BuildBar(column.transform);

            _elapsed = Row(column.transform, "", 18, UiTheme.CreamMuted, 24);

            // ---- the thing this game has never said out loud ----------------------------
            //
            // ⚠️⚠️ IT IS THE ONLY SENTENCE ON THE CARD AND IT IS HERE ON INSTRUCTION.
            // `FUTURE.md` § 7: "THE TAYA ROTATION IS WHAT MAKES THIS FAIR AT ALL, and it is worth
            // saying in the queue UI: everyone defends once, so a bad first round is not a lost
            // match." `INSPIRATION.md` § 4.5 is titled "the taya rotation is a gift and nobody
            // knows it". A queue is the one screen where a player is doing nothing but waiting,
            // which is the only moment in the whole game where a sentence is not in the way.
            //
            // ⚠️ THE STRING IS `MatchmakingRules.TayaRotationPromise` AND IS NOT TYPED HERE.
            // It is a claim about the rules (`(round - 1) % 4`, derived and never accumulated) and
            // it has a test, so it lives beside the rule it describes.
            _promise = Row(column.transform, MatchmakingRules.TayaRotationPromise, 18,
                           UiTheme.CreamMuted, 52);
            _promise.horizontalOverflow = HorizontalWrapMode.Wrap;
            _promise.alignment = TextAnchor.UpperLeft;

            // ---- PHASE 11: the offer, and the disclosure under it ----------------------
            //
            // ⚠⚠ IT IS THE ONLY OTHER PRESSABLE THING ON THE CARD AND IT IS AMBER, WHICH TAKES
            // THE ACCENT OFF NOTHING: the door that WAS amber is hidden while the card is up (see
            // `Refresh`), so at any moment exactly one control on this surface is the accent.
            // `FUTURE.md` § 0.5b: one accent, used for the one thing.
            //
            // ⚠⚠ AND THE DISCLOSURE IS A ROW, NOT A TOOLTIP. `FUTURE.md` § 11 makes it a
            // constraint: *"a player who thinks they beat a person and did not will be angrier
            // when they find out than they would have been to know"*. For a RANKED queue it also
            // says the thing the player would otherwise have to work out, which is that the result
            // will not move their rating.
            _fill = MenuKit.WoodButton(column.transform, "START WITH 3 BOTS", Vector2.zero,
                                       Vector2.zero, new Vector2(0.0f, 48.0f), OnFillPressed,
                                       "WoodAmberButton");
            _fill.name = "StartWithBotsButton";
            _fillLabel = _fill.GetComponentInChildren<Text>();

            var fillElement = _fill.gameObject.AddComponent<LayoutElement>();
            fillElement.minHeight = 48.0f;
            fillElement.preferredHeight = 48.0f;
            fillElement.flexibleHeight = 0.0f;

            _fillCaveat = Row(column.transform, "", MenuKit.MinReadableUnits,
                              UiTheme.CreamMuted, 20.0f);
            _fillCaveat.horizontalOverflow = HorizontalWrapMode.Wrap;
            _fillCaveat.alignment = TextAnchor.UpperLeft;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(column.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleHeight = 1.0f;

            _cancel = MenuKit.WoodButton(column.transform, "CANCEL", Vector2.zero, Vector2.zero,
                                         new Vector2(0.0f, 48.0f), OnCancelPressed);
            _cancel.name = "CancelQueueButton";
            var cancelElement = _cancel.gameObject.AddComponent<LayoutElement>();
            cancelElement.minHeight = 48.0f;
            cancelElement.preferredHeight = 48.0f;
            cancelElement.flexibleHeight = 0.0f;

            _card.SetActive(false);
        }

        /// <summary>
        /// The widening bar.
        ///
        /// ⚠️⚠️ IT IS THE POINT OF THE WHOLE CARD AND IT IS NOT A SPINNER. `FUTURE.md` § 7: "show
        /// the widening, so a long queue reads as progress rather than as a hang". A spinner turns
        /// at the same speed whether anything is happening or not, so after forty seconds it is
        /// indistinguishable from a frozen game; this fills, and while it fills the two numbers
        /// above it are visibly moving apart, so the player can see the search getting easier.
        ///
        /// ⚠️ IT FILLS AND STAYS FULL RATHER THAN RESETTING. `MatchmakingRules.WideningProgress`
        /// clamps at 1 and the headline changes to say the band is as wide as it goes, because a
        /// bar that loops is a bar that says "still nothing" for ever.
        /// </summary>
        private void BuildBar(Transform parent)
        {
            var holder = new GameObject("WideningBar");
            holder.transform.SetParent(parent, false);

            var element = holder.AddComponent<LayoutElement>();
            element.minHeight = BarHeight;
            element.preferredHeight = BarHeight;
            element.flexibleHeight = 0.0f;

            var track = holder.AddComponent<Image>();
            track.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.45f);
            track.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(holder.transform, false);

            _barFill = fillGo.AddComponent<Image>();
            _barFill.color = UiTheme.Amber;
            _barFill.raycastTarget = false;

            var rt = _barFill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0.0f, 1.0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text Row(Transform parent, string text, int size, Color colour, float height)
        {
            var holder = new GameObject("Line");
            holder.transform.SetParent(parent, false);

            var element = holder.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0.0f;

            var label = MenuKit.Label(holder.transform, text, size, colour,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            label.raycastTarget = false;
            MenuKit.Stretch(label.rectTransform, 0);

            return label;
        }

        // -------------------------------------------------------------------

        private void OnQuickMatchPressed()
        {
            MenuSfx.Click();

            // ⚠️ THE MODE COMES FROM THE LOBBY THE PLAYER IS STANDING IN, not from a picker on
            // this card. `INSPIRATION.md` § 3.1: the mode is the ruleset and the queue is the
            // stakes, and they already chose the ruleset one row up.
            bool started = _queue.StartQueue(SceneFlow.SelectedMode, QueueStake.Casual,
                                             HumanSeatCount());

            if (!started) Status?.Invoke(_queue.Refusal);
            Refresh();
        }

        /// <summary>
        /// ⚠️⚠️ THE PARTY IS THE HUMANS ALREADY SEATED IN THIS LOBBY, WHICH IS `PartyRules`' WHOLE
        /// DEFINITION OF A PARTY. Phase 6 shipped "be in the same lobby" as the party mechanism
        /// (`docs/TODO.md` § 102.2) because there was no queue to join together; this is the
        /// sentence `FUTURE.md` § 6 promised would follow, "when Phase 7 lands, a party becomes a
        /// queue ticket and the rail does not change". Pressing QUICK MATCH in a room of three
        /// looks for a lobby with three chairs.
        /// </summary>
        private static int HumanSeatCount()
        {
            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return 1;

            int seated = lobby.SeatedPeerCount();
            return seated < 1 ? 1 : seated;
        }

        /// <summary>
        /// PHASE 11: the player accepted bots.
        ///
        /// ⚠️ THE QUEUE IS CANCELLED FIRST, so the room stops advertising itself before it
        /// loads a match. A lobby that starts a match while still in the pool is a lobby somebody
        /// else joins on the way out, which is `Cancel`'s own note read forwards.
        /// </summary>
        private void OnFillPressed()
        {
            MenuSfx.Click();
            _queue.Cancel();
            Refresh();
            StartWithBots?.Invoke();
        }

        private void OnCancelPressed()
        {
            MenuSfx.Click();
            _queue.Cancel();
            Status?.Invoke("Left the queue. You are still in your own room.");
            Refresh();
        }

        private void OnQueueJoined()
        {
            Refresh();
            Joined?.Invoke();
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE DOOR AND THE CARD ARE NEVER BOTH ON SCREEN. They are the two states of one
        /// control, and showing a QUICK MATCH button over a card that says SEARCHING would be a
        /// second door to a place the player is already standing in.
        /// </summary>
        private void Refresh()
        {
            if (_queue == null) return;

            bool queueing = _queue.IsQueueing;

            if (_open != null) _open.gameObject.SetActive(!queueing);
            if (_card != null) _card.SetActive(queueing);

            if (!queueing)
            {
                // ⚠️ A REFUSAL IS SHOWN ON THE LOBBY'S OWN STATUS LINE RATHER THAN ON A CARD THAT
                // IS NOT THERE. `Matchmaker.Start` already handed it over; this keeps the button
                // available so the player can press it again once the reason has passed.
                return;
            }

            if (_headline != null)
            {
                _headline.text = _queue.State switch
                {
                    QueueState.Joining => "FOUND A MATCH",
                    QueueState.Hosting => "OPENING A ROOM",
                    _ => MatchmakingRules.TakesAnybody(_queue.Elapsed)
                        ? "SEARCHING EVERYWHERE"
                        : "SEARCHING FOR A MATCH",
                };

                MenuKit.Fit(_headline, InnerWidth);
            }

            if (_band != null)
            {
                // ⚠️ THE MODE IS ON THE CARD BECAUSE § 0.5b'S TRAP ROW ASKS FOR IT BY NAME: "say
                // the mode, the time elapsed, and how to cancel". A player who queued from the
                // wrong tab finds out here rather than at the character select.
                _band.text = $"{MenuKit.ModeLabel(_queue.Mode)}  ·  {_queue.SearchLabel}";
                MenuKit.Fit(_band, InnerWidth);
            }

            if (_barFill != null)
            {
                var rt = _barFill.rectTransform;
                rt.anchorMax = new Vector2(Mathf.Clamp01(_queue.WideningProgress), 1.0f);
            }

            // ⚠⚠ THE OFFER APPEARS RATHER THAN THE CARD GROWING, and the two rows exist from the
            // start for that reason: the card is a fixed 348 and the button lands INSIDE it. A
            // card that grew at 45 seconds would move CANCEL out from under a cursor that had
            // been resting on it for the whole wait.
            bool offering = _queue.OffersBotFill;
            int bots = _queue.BotsToFill;

            if (_fill != null) _fill.gameObject.SetActive(offering);
            if (_fillCaveat != null) _fillCaveat.gameObject.SetActive(offering);

            if (offering)
            {
                if (_fillLabel != null)
                {
                    _fillLabel.text = BotFillRules.FillOffer(bots);
                    MenuKit.Fit(_fillLabel, InnerWidth - 32.0f);
                }

                if (_fillCaveat != null)
                    _fillCaveat.text = BotFillRules.FillCaveat(
                        _queue.Stake, _queue.PartySize, Balance.PlayerCount);
            }

            if (_elapsed != null)
            {
                int seconds = Mathf.FloorToInt(_queue.Elapsed);
                string clock = seconds < 60
                    ? $"{seconds}s"
                    : $"{seconds / 60}m {seconds % 60:00}s";

                _elapsed.text = _queue.PartySize > 1
                    ? $"{clock} waiting  ·  {_queue.PartySize} of you"
                    : $"{clock} waiting";
            }
        }

        private void OnDestroy()
        {
            if (_queue == null) return;

            _queue.Changed -= Refresh;
            _queue.Joined -= OnQueueJoined;
        }
    }
}

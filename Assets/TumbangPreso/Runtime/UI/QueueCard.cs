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

        private Matchmaker _queue;

        private GameObject _card;
        private Text _headline;
        private Text _band;
        private Text _elapsed;
        private Text _promise;
        private Image _barFill;
        private Button _cancel;
        private Button _open;

        /// <summary>Raised when the queue lands the player in a match, so the lobby redraws.</summary>
        public event Action Joined;

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
        public static QueueCard Build(Transform parent)
        {
            var go = new GameObject("QueueCard");
            go.transform.SetParent(parent, false);

            var card = go.AddComponent<QueueCard>();
            card.Construct();
            return card;
        }

        private void Construct()
        {
            _queue = Matchmaker.Ensure();
            _queue.Changed += Refresh;
            _queue.Joined += OnQueueJoined;

            // ---- the door -------------------------------------------------------------
            //
            // ⚠️ AMBER, WHICH IS THE ONE ACCENT ON THIS SCREEN AND IS SPENT ON THE ONE ACTION.
            // `FUTURE.md` § 0.5b: "one accent, used for the one thing", and colour is the LAST
            // ordering tool rather than the first. The lobby's other controls are plain wood.
            _open = MenuKit.WoodButton(transform, "QUICK MATCH", new Vector2(0.5f, 0.0f),
                                       new Vector2(0.0f, 96.0f), new Vector2(CardWidth, 64.0f),
                                       OnQuickMatchPressed, "WoodAmberButton");
            _open.name = "QuickMatchButton";

            BuildCard();
            Refresh();
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

            MenuKit.Place(plate.rectTransform, new Vector2(0.5f, 0.0f),
                          new Vector2(0.0f, 96.0f), new Vector2(CardWidth, 280.0f));

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
            bool started = _queue.Start(SceneFlow.SelectedMode, QueueStake.Casual,
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

                MenuKit.Fit(_headline, CardWidth - (Pad * 2.0f));
            }

            if (_band != null)
            {
                // ⚠️ THE MODE IS ON THE CARD BECAUSE § 0.5b'S TRAP ROW ASKS FOR IT BY NAME: "say
                // the mode, the time elapsed, and how to cancel". A player who queued from the
                // wrong tab finds out here rather than at the character select.
                _band.text = $"{MenuKit.ModeLabel(_queue.Mode)}  ·  {_queue.SearchLabel}";
                MenuKit.Fit(_band, CardWidth - (Pad * 2.0f));
            }

            if (_barFill != null)
            {
                var rt = _barFill.rectTransform;
                rt.anchorMax = new Vector2(Mathf.Clamp01(_queue.WideningProgress), 1.0f);
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

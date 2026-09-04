using System.Collections.Generic;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// CONTROLLER MAP: a picture of the pad with every job written around it, and every one of
    /// them rebindable by pressing it.
    ///
    /// ⚠️⚠️ THE ONE THING ON THIS SCREEN IS THE PAD, AND THAT IS WHY IT IS A PICTURE RATHER THAN
    /// A BETTER LIST. `CLAUDE.md` § 6.2 question 1: *"What is the ONE thing on this screen?
    /// Everything else is sized, placed and coloured against it."* The settings panel's GAMEPAD
    /// page is a column of action names against a column of control names, and it answers
    /// *"what is LUNGE bound to"*. **That is not the question anybody has.** The question a
    /// player holding a controller asks is *"what does this button under my thumb do"*, and a
    /// list can only be read that way round by somebody who already knows the answer. 🧑 handed
    /// over a labelled diagram of a DualShock as the reference for exactly that reason.
    ///
    /// ⚠️⚠️ AND IT IS THE CURE FOR `GenericPadBridge`'S GUESS, WHICH IS THE OTHER HALF OF WHY IT
    /// EXISTS. `docs/TODO.md` § 138.4 accepts that a fallback mapping for an unrecognised pad
    /// *"will be wrong for some pads and right for many"*, on the argument that **"a wrong
    /// mapping the player can SEE beats a dead pad they cannot"**. This is the seeing. A player
    /// whose no-name pad has its face buttons rotated can look at the drawing, press the two
    /// callouts that are wrong, and be done.
    ///
    /// § THE FOUR QUESTIONS § 6.2 ASKS BEFORE A SCREEN IS WRITTEN
    ///
    /// 1. **The one thing** is the controller in the middle. Everything else is a label on it.
    /// 2. **The first press** is a callout, and the hint under the title says what happens when
    ///    you press one. § 6.2's own test is whether the player can guess it: a row with a
    ///    control's picture and a job written beside it, on a screen called CONTROLLER MAP, is
    ///    guessable in a way a bare list of key names is not.
    /// 3. **What is not needed right now** is the keyboard, the volumes and the spectator set.
    ///    The keyboard has its own page one screen back. ⚠️ The spectator controls are
    ///    deliberately absent and the footnote says so: drawing both contexts would put two jobs
    ///    on nine of these controls at once, which is § 6.2's third claim (*"everything the
    ///    feature can do is on screen at once"*) on the one screen that exists to be scanned.
    /// 4. **Getting out** is one press of Escape or the pad's own B, through
    ///    <see cref="MenuNav"/>, and a DONE chip for a mouse.
    ///
    /// ⚠️⚠️ NOTHING HERE DECIDES WHAT A BUTTON DOES. Every label is resolved live from the asset
    /// through `Settings.Rebinding`, so a rebind made on the settings page shows here and a
    /// rebind made here shows there. `docs/VISION.md` § 3: *"Key labels come from the live
    /// binding, never from a literal. A screen that teaches the wrong key is worse than one that
    /// teaches none."* A diagram with THROW painted beside the right trigger would be a literal
    /// in the most convincing possible costume.
    /// </summary>
    public sealed class ControllerMapScreen : MonoBehaviour
    {
        // -------------------------------------------------------------------------------------
        // § THE LAYOUT
        //
        // ⚠️⚠️ EVERY NUMBER BELOW IS MEASURED AGAINST THE 1920 UNITS THE CANVAS IS GUARANTEED TO
        // HAVE, WHICH IS `CLAUDE.md` § 6.2c's FIRST QUESTION ANSWERED OUT LOUD. `AspectSafeCanvas`
        // uses `ScreenMatchMode.Expand`, so the reference width is `pixelWidth / min(pw/1920,
        // ph/1080)`, which is **exactly 1920 at 4:3 and more than that at everything wider**. So
        // a fixed 380-unit callout is 380 units on every monitor in the world, and the room a
        // wider screen adds all goes into the gutters where the leader lines live. A percentage
        // would have been two different widths, which is the § 100 fault that section records.
        // -------------------------------------------------------------------------------------

        /// <summary>How wide one callout is. See the note above for why this is not a fraction.</summary>
        private const float CalloutWidth = 380.0f;

        /// <summary>The room between the drawing and a callout, where the leader lines run.</summary>
        private const float Gutter = 72.0f;

        /// <summary>
        /// The widest the drawing may be. ⚠️ THE ARITHMETIC, RATHER THAN A NUMBER THAT LOOKED
        /// RIGHT: 1920 less two callouts and two gutters is 1016, and 980 leaves 18 units of
        /// margin either side so the drawing never touches the edge of a 4:3 screen.
        /// </summary>
        private const float DiagramMaxWidth = 980.0f;

        private const float RowHeight = 76.0f;
        private const float RowGap = 8.0f;

        /// <summary>
        /// ⚠️ THE BOARD SITS 30 UNITS BELOW CENTRE so the title and the hint have their own room
        /// at the top without being squeezed against the first callout.
        /// </summary>
        private const float BoardCentreY = -30.0f;

        private const float GlyphSize = 46.0f;

        /// <summary>
        /// How tall the ring of callouts is, which is the band everything else is measured in.
        /// Nine rows a side, so half the table's length in gaps, less the one that hangs off
        /// the end.
        /// </summary>
        /// ⚠️⚠️ IT COUNTS `Declared`, NOT `Ring`, AND SWAPPING THE TWO IS A NULL REFERENCE IN A
        /// STATIC INITIALISER. `Ring` is built by `BuildRing`, which calls `Uncross`, which needs
        /// this number to know where the rows are: reading `Ring` here would read the field that
        /// is in the middle of being assigned. The two lists always hold the same controls, so
        /// the count is the same fact from the side that already exists.
        private static float BandHeight => Declared.Length / 2 * (RowHeight + RowGap) - RowGap;

        /// <summary>
        /// The drawing's size, fitted to the band rather than to the leftover width.
        ///
        /// ⚠️⚠️ HEIGHT FIRST, AND THE FIRST RENDER IS WHY. `CLAUDE.md` § 6.2c's first question is
        /// *"what is this size measured AGAINST?"*, and the honest answer here is the CALLOUTS:
        /// the drawing sits between two columns of them and must not be taller than they are, or
        /// it runs into the hint above and the status line below. Taking the width first and
        /// letting the height fall out gave an 803-unit drawing in a 748-unit band, which
        /// overlapped the footer by nine units — a number nobody would have found by looking.
        ///
        /// ⚠️ AND THE ART IS CROPPED TO ITS OWN INK BY THE GENERATOR, so this is the size of the
        /// PAD and not of a canvas with the pad somewhere inside it. Before that crop,
        /// `preserveAspect` fitted 170 units of transparent margin into the box and drew a
        /// 640-unit controller in a 980-unit hole.
        /// </summary>
        private static Vector2 DiagramSize
        {
            get
            {
                float height = Mathf.Min(BandHeight, DiagramMaxWidth / PadDiagram.Aspect);
                return new Vector2(height * PadDiagram.Aspect, height);
            }
        }

        // -------------------------------------------------------------------------------------
        // § WHICH CONTROL IS DRAWN WHERE
        // -------------------------------------------------------------------------------------

        private enum Side { Left = -1, Right = 1 }

        /// <summary>Which column a callout goes in. Its ROW is derived; see <see cref="Ring"/>.</summary>
        private readonly struct Slot
        {
            public readonly string Control;
            public readonly Side Side;
            public readonly int Order;

            public Slot(string control, Side side, int order)
            {
                Control = control;
                Side = side;
                Order = order;
            }

            public Slot WithOrder(int order) => new Slot(Control, Side, order);
        }

        /// <summary>
        /// Which column each control's callout goes in, and the tie-break when two share a height.
        ///
        /// ⚠️⚠️ THE ROW IS NOT IN THIS TABLE, AND THAT IS WHAT THE FIRST RENDER WITH LEADER LINES
        /// TAUGHT. It WAS typed in, under a comment claiming the order was *"top-to-bottom by
        /// where the control actually is on the pad"* — **and it was not**: SELECT and START sat
        /// at the BOTTOM of their columns while their anchors are near the TOP of the drawing, so
        /// two leader lines ran diagonally across the whole picture and crossed four others. A
        /// comment asserting a property the data does not have is the same failure as a stale
        /// table, and `CLAUDE.md` § 4a's answer applies: **construction, not discipline.**
        ///
        /// ⚠️ THE DECLARED ORDER SURVIVES AS THE TIE-BREAK FOR TWO CONTROLS AT THE SAME HEIGHT,
        /// and there the rule is **nearest target first**: from the left gutter the d-pad's left
        /// arm before its right, from the right gutter the east face button before the west.
        /// Reverse either and the two lines cross for no reason at all.
        /// </summary>
        private static readonly Slot[] Declared =
        {
            new Slot("leftTrigger", Side.Left, 0),
            new Slot("leftShoulder", Side.Left, 1),
            new Slot("select", Side.Left, 2),
            new Slot("dpad/up", Side.Left, 3),
            new Slot("dpad/left", Side.Left, 4),
            new Slot("dpad/right", Side.Left, 5),
            new Slot("dpad/down", Side.Left, 6),
            new Slot("leftStick", Side.Left, 7),
            new Slot("leftStickPress", Side.Left, 8),

            new Slot("rightTrigger", Side.Right, 0),
            new Slot("rightShoulder", Side.Right, 1),
            new Slot("start", Side.Right, 2),
            new Slot("buttonNorth", Side.Right, 3),
            new Slot("buttonEast", Side.Right, 4),
            new Slot("buttonWest", Side.Right, 5),
            new Slot("buttonSouth", Side.Right, 6),
            new Slot("rightStick", Side.Right, 7),
            new Slot("rightStickPress", Side.Right, 8),
        };

        /// <summary>
        /// The ring, each column sorted top-to-bottom by where its control actually sits on the
        /// drawing, so the claim the leader lines depend on is arithmetic rather than a promise.
        ///
        /// ⚠️ A CONTROL THE DRAWING DOES NOT HAVE KEEPS ITS DECLARED PLACE. `PadDiagram` comes
        /// back empty on a checkout with no generated art, and treating "no anchor" as zero would
        /// shuffle the whole list into an order nobody chose, on the one run where there is no
        /// picture to justify it.
        ///
        /// ⚠️⚠️ THE SECOND SORT KEY IS NOT DECORATION: `List.Sort` IS NOT STABLE. Four pairs share
        /// a height exactly — the two d-pad arms, the two side face buttons, and each stick with
        /// its own click — and without the tie-break their order would be whatever the sort
        /// happened to do that day, which is a diagram that crosses its own lines on some builds.
        /// </summary>
        private static readonly Slot[] Ring = BuildRing();

        private static Slot[] BuildRing()
        {
            var ring = new List<Slot>(Declared.Length);

            foreach (var side in new[] { Side.Left, Side.Right })
            {
                var column = new List<Slot>();

                foreach (var slot in Declared)
                    if (slot.Side == side) column.Add(slot);

                column.Sort((a, b) =>
                {
                    int byFan = FanKey(a).CompareTo(FanKey(b));
                    return byFan != 0 ? byFan : a.Order.CompareTo(b.Order);
                });

                Uncross(column, side);

                for (int i = 0; i < column.Count; i++) ring.Add(column[i].WithOrder(i));
            }

            return ring.ToArray();
        }

        /// <summary>
        /// Swaps neighbouring rows until no two leader lines in this column cross.
        ///
        /// ⚠️⚠️ THE ANGULAR SORT IS AN APPROXIMATION AND THIS IS THE EXACT ANSWER, AND BOTH ARE
        /// HERE BECAUSE THE APPROXIMATION GOT ONE PAIR WRONG IN THE FIRST RENDER WITH THE REAL
        /// ARTWORK. Sorting a fan by angle cannot produce a crossing when every line starts at ONE
        /// point; these start spread over the whole 750-unit gutter, so for two targets at nearly
        /// the same angle the order can still come out inverted. It did: HIDE HUD's line ended
        /// exactly on the point ABILITY INFO's line passed through.
        ///
        /// ⚠️ IT TESTS THE ACTUAL SEGMENTS RATHER THAN A PROXY FOR THEM, so the claim the whole
        /// diagram rests on — that a reader never has to trace a line with a finger — is a
        /// property of the output instead of a promise in a comment. `docs/TODO.md` § 142.3
        /// records the version of this table that carried the promise and not the property.
        ///
        /// ⚠️ ADJACENT SWAPS ONLY, AND BOUNDED. A full crossing-minimal assignment is a
        /// quadratic-cost problem for a nine-row column that is already nearly right; bubbling
        /// adjacent pairs fixes exactly the local inversions the angular sort leaves and cannot
        /// run away. The bound is the column length, which is the most passes a bubble sort can
        /// need.
        /// </summary>
        private static void Uncross(List<Slot> column, Side side)
        {
            var size = DiagramSize;
            float top = BoardCentreY + BandHeight * 0.5f;
            float startX = (int)side * (size.x * 0.5f + Gutter);

            for (int pass = 0; pass < column.Count; pass++)
            {
                bool swapped = false;

                for (int i = 0; i + 1 < column.Count; i++)
                {
                    var upper = new Vector2(startX, top - i * (RowHeight + RowGap));
                    var lower = new Vector2(startX, top - (i + 1) * (RowHeight + RowGap));

                    if (!TryTarget(column[i], size, out var a)) continue;
                    if (!TryTarget(column[i + 1], size, out var b)) continue;

                    if (!Crosses(upper, a, lower, b)) continue;

                    (column[i], column[i + 1]) = (column[i + 1], column[i]);
                    swapped = true;
                }

                if (!swapped) return;
            }
        }

        /// <summary>Where a control's leader line ends, in the same space the callouts live in.</summary>
        private static bool TryTarget(Slot slot, Vector2 size, out Vector2 target)
        {
            target = Vector2.zero;
            if (!PadDiagram.TryAnchor(slot.Control, out var anchor)) return false;

            target = new Vector2((anchor.x - 0.5f) * size.x,
                                 BoardCentreY + (0.5f - anchor.y) * size.y);
            return true;
        }

        /// <summary>
        /// Whether two segments properly cross.
        ///
        /// ⚠️ THE ORIENTATION TEST, NOT AN INTERSECTION POINT. Solving for the point needs a
        /// divide and a parallel case; four cross products answer the only question asked here
        /// and cannot divide by zero. ⚠️ **Touching counts as crossing**, which is deliberate:
        /// the pair that started this was two lines meeting exactly at one of their endpoints,
        /// and on screen that reads as a crossing whatever the strict definition says.
        /// </summary>
        private static bool Crosses(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            float d1 = Cross(b2 - b1, a1 - b1);
            float d2 = Cross(b2 - b1, a2 - b1);
            float d3 = Cross(a2 - a1, b1 - a1);
            float d4 = Cross(a2 - a1, b2 - a1);

            return d1 * d2 <= 0.0f && d3 * d4 <= 0.0f;
        }

        private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        /// <summary>
        /// The angle a control sits at, seen from this column, which is the order that cannot
        /// cross.
        ///
        /// ⚠️⚠️ IT WAS THE ANCHOR'S PLAIN Y UNTIL THE REAL ARTWORK LANDED, AND Y IS ONLY RIGHT
        /// WHEN EVERY TARGET IS THE SAME DISTANCE AWAY. The drawn pad had its controls in two
        /// tidy vertical bands, so "sort by height" and "sort by angle" agreed. The photographed
        /// one does not: **SELECT and START sit near the CENTRE of the pad**, much further from
        /// their columns than the d-pad or the sticks, and a far target sandwiched between two
        /// near ones by height forces its line to cut across both of them. The first render with
        /// this art had four crossings and all four were those two labels.
        ///
        /// ⚠️ SORTING A FAN BY ANGLE FROM ONE ORIGIN CANNOT PRODUCE A CROSSING, which is the
        /// whole reason to spend a trigonometric call here rather than tune the table by eye.
        /// The origin is a point out beyond this column's own side at mid height, so it stands in
        /// for "where the labels are"; the callouts are not literally all at that point, but the
        /// gutter they start from is short next to the distance across the drawing, and the
        /// approximation is what makes the result stable when the art moves again.
        ///
        /// ⚠️ A CONTROL THE DRAWING DOES NOT HAVE FALLS BACK TO ITS DECLARED RANK, scaled to sit
        /// in the same range as an angle so the two orderings do not interleave arbitrarily.
        /// `PadDiagram` comes back empty on a checkout with no generated art.
        /// </summary>
        private static float FanKey(Slot slot)
        {
            if (!PadDiagram.TryAnchor(slot.Control, out var anchor))
                return slot.Order * 0.01f;

            // The virtual eye, out past the callout column on this control's own side.
            float originX = slot.Side == Side.Left ? -0.35f : 1.35f;

            float dx = Mathf.Abs(anchor.x - originX);
            float dy = anchor.y - 0.5f;

            return Mathf.Atan2(dy, dx);
        }

        /// <summary>
        /// Every control the map has a callout for, in ring order.
        ///
        /// ⚠️⚠️ IT IS PUBLIC SO A TEST CAN ASSERT THE THREE TABLES AGREE, AND THAT ASSERTION IS
        /// THE WHOLE GUARD ON THIS SCREEN. Three independently written lists have to line up: the
        /// bindings in `InputCatalogue` and `ScreenInputCatalogue`, the anchors in
        /// `tools/build_controller_diagram.py`, and this ring. **A pad binding with no slot here
        /// is a control the map silently does not show**, which is `docs/TODO.md` § 96 in
        /// miniature: a feature the player cannot find, with everything green.
        /// </summary>
        public static IEnumerable<string> MappedControls
        {
            get
            {
                foreach (var slot in Ring) yield return slot.Control;
            }
        }

        // -------------------------------------------------------------------------------------
        // § LIFETIME
        // -------------------------------------------------------------------------------------

        public static ControllerMapScreen Instance { get; private set; }

        public static ControllerMapScreen Open()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("ControllerMapScreen");
            return Instance = go.AddComponent<ControllerMapScreen>();
        }

        public bool IsOpen => _canvas != null;

        private Canvas _canvas;
        private InputActionAsset _actions;
        private RebindSession _session;
        private Text _status;
        private int _revision = -1;

        /// <summary>Every callout, so a rebind can put the labels back in step without a rebuild.</summary>
        private readonly List<Callout> _callouts = new List<Callout>();

        private sealed class Callout
        {
            public string Control;
            public string Action;
            public Text Label;
            public Button Button;
        }

        private void Awake()
        {
            Instance = this;

            // ⚠️⚠️ REGISTERED, FOR THE REASON `PlayerHub.Install` RECORDS AFTER GETTING IT WRONG.
            // `ScreenTakeover`'s header is about chrome asking *"is anything on top of me"*
            // rather than keeping a list, and a full-screen canvas that never registers means the
            // settings panel underneath reads the same Escape press that closed this and backs
            // itself out too. One press, two layers, which is the fault § 6.3 calls out by name.
            ScreenTakeover.Register(this, () => IsOpen);

            _actions = Resources.Load<InputActionAsset>("TumbangPreso");

            // ⚠️ THE OVERRIDES ARE LOADED BEFORE ANYTHING IS READ. `Rebinding.Load`'s own note:
            // *"an unloaded asset silently shows and uses the defaults, which reads to the player
            // as their rebind having been forgotten."* This screen may be the first thing in the
            // process to touch the asset.
            Rebinding.Load(_actions);

            Build();
        }

        private void OnDestroy()
        {
            _session?.Dispose();
            ScreenTakeover.Unregister(this);

            if (Instance == this) Instance = null;
        }

        public void Close()
        {
            _session?.Dispose();
            _session = null;

            if (_canvas != null) Destroy(_canvas.gameObject);
            _canvas = null;

            Destroy(gameObject);
        }

        private void Update()
        {
            // ⚠️ THE LABELS FOLLOW A REBIND MADE ANYWHERE, INCLUDING THE SETTINGS PAGE BEHIND
            // THIS ONE. `Rebinding.Revision` is bumped by every mutation in that file and exists
            // precisely so a screen can cache a label without going stale; polling one integer is
            // what buys this screen the right not to resolve eighteen bindings every frame.
            if (_revision != Rebinding.Revision) Refresh();

            if (!MenuNav.CancelPressed) return;

            // ⚠️ INNERMOST FIRST. A listening rebind is a layer above this screen: the first
            // press abandons it and the second closes the map. Closing the screen out from under
            // a live rebind would leave the action disabled, which is the fault
            // `RebindSession.Close` carries a warning about.
            //
            // ⚠️⚠️ EXCEPT THAT A PAD'S B ALREADY CANCELS THE OPERATION ITSELF, so this branch is
            // reached by Escape and by a pad only through the operation's own `OnCancel`. Both
            // land on `Cancelled`, and `Dispose` here is silent by design: see
            // `ConvertedSettingsPanel.CancelRebind` for the same sound played twice.
            ScreenTakeover.ConsumeEscape();

            if (_session != null)
            {
                _session.Dispose();
                _session = null;
                Say("Rebind cancelled.");
                MenuSfx.Back();
                Refresh();
                return;
            }

            MenuSfx.Back();
            Close();
        }

        // -------------------------------------------------------------------------------------
        // § BUILDING IT
        // -------------------------------------------------------------------------------------

        private void Build()
        {
            _canvas = MenuKit.BuildCanvas(transform, "ControllerMapCanvas");

            // ⚠️ ABOVE THE SETTINGS PANEL AND THE HUB, BELOW NOTHING. This is opened FROM the
            // settings panel and has to cover it; `PlayerHub` sits at 500, so 520 puts this over
            // the whole front end and keeps the ordering readable as a list rather than a race.
            _canvas.sortingOrder = 520;

            var root = (RectTransform)_canvas.transform;

            // ⚠️⚠️ AN OPAQUE GROUND, AND IT IS ALSO THE BLOCKER. `CLAUDE.md` § 6.2c's last
            // question: *"if I delete this, what else was it doing?"* A full-screen sheet is what
            // stops a press on this screen reaching the settings rows underneath, which are still
            // built and still raycasting. § 100 records exactly this: a scrim that was silently
            // the only thing stopping a press falling through, deleted for looking decorative.
            var ground = PaperKit.Sheet(root, "Ground");
            MenuKit.Stretch(ground.rectTransform);

            BuildHeader(root);
            BuildDiagram(root);
            BuildCallouts(root);
            BuildFooter(root);

            Refresh();
        }

        private void BuildHeader(RectTransform root)
        {
            var title = PaperKit.Ink(root, "CONTROLLER MAP", PaperKit.Display,
                                     TextAnchor.UpperCenter);
            MenuKit.Place(title.rectTransform, new Vector2(0.5f, 1.0f),
                          new Vector2(0.0f, -52.0f), new Vector2(900.0f, 56.0f));

            // ⚠️ THE HINT SAYS WHAT TO DO, NOT WHAT THE SCREEN IS. § 6.2 question 2 asks whether
            // the player can guess the first press; `TouchLayoutScreen` records the same decision
            // for the same reason, because on both screens the first press is not a button that
            // looks like a button.
            var hint = PaperKit.Ink(root, "Press any label to change what that control does.",
                                    PaperKit.Body, TextAnchor.UpperCenter, soft: true);
            MenuKit.Place(hint.rectTransform, new Vector2(0.5f, 1.0f),
                          new Vector2(0.0f, -108.0f), new Vector2(1100.0f, 30.0f));
        }

        private RectTransform _diagram;

        /// <summary>
        /// The layer the leader lines live on.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE `SetAsFirstSibling` PUT ALL EIGHTEEN OF THEM BEHIND THE OPAQUE
        /// GROUND AND THE SECOND RENDER CAME BACK WITH NO LINES AT ALL. The intent was right — a
        /// leader must draw under the tray it starts from — but "first sibling" is not "under the
        /// callouts", it is **under everything**, and the first child of this canvas is the
        /// full-screen paper sheet. A named container built between the drawing and the callouts
        /// says the ordering out loud instead of computing it from a sibling index.
        /// </summary>
        private RectTransform _leaders;

        private void BuildDiagram(RectTransform root)
        {
            var go = new GameObject("Diagram", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);

            _diagram = (RectTransform)go.transform;
            MenuKit.Place(_diagram, new Vector2(0.5f, 0.5f),
                          new Vector2(0.0f, BoardCentreY), DiagramSize);

            var image = go.GetComponent<Image>();
            image.sprite = PadDiagram.Art;
            image.preserveAspect = true;

            // ⚠️⚠️ IT DOES NOT EAT PRESSES. A 980-unit image across the middle of the screen with
            // `raycastTarget` left on is a wall between the player and nothing, and worse, on a
            // pad it is the sort of thing that swallows a click aimed at a callout whose hit area
            // `ScreenFocus.ApplyTouchTargets` has grown out over it. The ground behind it is the
            // blocker; this is a picture.
            image.raycastTarget = false;

            // ⚠️ NO PICTURE IS A HANDLED CASE AND NOT A CRASH. See `PadDiagram`: the generated
            // PNG can be absent from a fresh clone. The callouts still lay out in their two
            // columns, the leader lines are skipped because there is nothing to point at, and the
            // screen degrades into the two-column list it replaced rather than into an empty
            // frame with an error in the log.
            image.enabled = PadDiagram.Art != null;

            // ⚠️ AFTER THE DRAWING AND BEFORE THE CALLOUTS, which is the whole ordering this
            // screen needs and the reason it is a container rather than a sibling index.
            var layer = new GameObject("Leaders", typeof(RectTransform));
            layer.transform.SetParent(root, false);

            _leaders = (RectTransform)layer.transform;
            MenuKit.Stretch(_leaders);
        }

        /// <summary>
        /// What every gamepad control currently does, keyed by the path after `&lt;Gamepad&gt;/`.
        ///
        /// ⚠️⚠️ BUILT FROM THE LIVE ASSET, BACKWARDS, WHICH IS THE ONLY WAY THIS SCREEN CAN BE
        /// HONEST. Reading `InputCatalogue` directly would draw the SHIPPED defaults, so the one
        /// screen a player opens after rebinding something would be the one screen still showing
        /// them the old control. Walking the actions and asking each for its live pad path means
        /// a rebind moves a label from one callout to another with nothing to keep in step.
        ///
        /// ⚠️ THE SPECTATOR SET IS EXCLUDED, DELIBERATELY, AND THE FOOTER SAYS SO. Nine of these
        /// controls carry a second job while watching a match (`Rebinding.SpectatorContext` is
        /// the whole argument for why that is legal), and drawing both would put two labels on
        /// most of the pad. § 6.2's third claim is about exactly that, and the spectator rows
        /// keep their own group in the settings list with their own "watching only" blurb.
        /// </summary>
        private Dictionary<string, string> BindingsByControl()
        {
            var map = new Dictionary<string, string>(24);
            if (_actions == null) return map;

            foreach (string action in Rebinding.RebindableActions)
            {
                if (Rebinding.IsSpectatorAction(action)) continue;

                string path = Rebinding.PathFor(_actions, action, InputDeviceKind.Gamepad);
                string control = Suffix(path);

                if (control.Length == 0) continue;

                map[control] = action;
            }

            return map;
        }

        /// <summary>
        /// The two analogue controls no rebind row owns, and they are the two most used on the
        /// pad. See `Rebinding.PlainPathFor` for why they cannot come from the loop above.
        /// </summary>
        private Dictionary<string, string> SharedByControl()
        {
            var map = new Dictionary<string, string>(2);
            if (_actions == null) return map;

            string move = Suffix(Rebinding.PlainPathFor(_actions, "Move", InputDeviceKind.Gamepad));
            string look = Suffix(Rebinding.PlainPathFor(_actions, "Look", InputDeviceKind.Gamepad));

            if (move.Length > 0) map[move] = "Move";
            if (look.Length > 0) map[look] = "Aim / Look";

            return map;
        }

        private static string Suffix(string path)
            => string.IsNullOrEmpty(path) || !path.StartsWith(Rebinding.GamepadDevice + "/")
                ? ""
                : path.Substring(Rebinding.GamepadDevice.Length + 1);

        private void BuildCallouts(RectTransform root)
        {
            var size = DiagramSize;
            float top = BoardCentreY + BandHeight * 0.5f;

            // ⚠️ THE COLUMNS ARE PLACED AGAINST THE DRAWING'S REAL WIDTH, NOT THE CAP. The pad is
            // fitted to the band, so on a short window it is narrower than `DiagramMaxWidth` and
            // a callout positioned off the cap would leave a gutter twice the size it says it is.
            foreach (var slot in Ring)
            {
                float x = (int)slot.Side * (size.x * 0.5f + Gutter + CalloutWidth * 0.5f);
                float y = top - slot.Order * (RowHeight + RowGap);

                var callout = BuildCallout(root, slot, new Vector2(x, y));
                _callouts.Add(callout);

                BuildLeader(root, slot, new Vector2(x, y), size);
            }
        }

        private Callout BuildCallout(RectTransform root, Slot slot, Vector2 at)
        {
            var go = new GameObject("Callout_" + slot.Control,
                                    typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);

            var rt = (RectTransform)go.transform;
            MenuKit.Place(rt, new Vector2(0.5f, 0.5f), at, new Vector2(CalloutWidth, RowHeight));

            PaperSkin.Apply(go, PaperCraft.Surface.Tray);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = go.GetComponent<Image>();

            string name = HumanName(slot.Control);

            // ⚠️⚠️ THE GLYPH IS `UI.InputGlyphs`' OWN PAD SHEET AND NOT A SECOND SET OF PICTURES.
            // That class already holds a recoloured, brand-legal cap for every control on a pad,
            // keyed on exactly the string `ToHumanReadableString` produces, and its header spells
            // out why a second resolver would rot. ⚠️ `onDark: false`: this is a cream paper
            // screen, and its note is blunt that the same cap has two variants because *"the
            // sprite is not correct on its own, only against the thing it is drawn on."*
            var sprite = InputGlyphs.For(name, onDark: false);

            if (sprite != null)
            {
                var glyphGo = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
                glyphGo.transform.SetParent(go.transform, false);

                var glyphRt = (RectTransform)glyphGo.transform;
                glyphRt.anchorMin = glyphRt.anchorMax = new Vector2(0.0f, 0.5f);
                glyphRt.pivot = new Vector2(0.0f, 0.5f);
                glyphRt.anchoredPosition = new Vector2(PaperKit.Pad, 0.0f);
                glyphRt.sizeDelta = new Vector2(GlyphSize, GlyphSize);

                var glyph = glyphGo.GetComponent<Image>();
                glyph.sprite = sprite;
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
            }

            // ⚠️ THE CONTROL'S NAME IS THE FALLBACK RATHER THAN A SECOND LINE, AND THAT IS § 6.2's
            // third claim applied to one row. The picture already says which control this is: the
            // leader line ends on it. Printing "LEFT SHOULDER" beside a drawing of the left
            // shoulder is a word the reader has to skip past to reach the one thing the row is
            // for. It appears only when the glyph sheet has no cap for the control, where it is
            // the only thing naming it.
            float textLeft = sprite != null ? PaperKit.Pad + GlyphSize + 10.0f : PaperKit.Pad;

            var label = PaperKit.Ink(go.transform, name, PaperKit.Body, TextAnchor.MiddleLeft);
            label.name = "Job";
            label.raycastTarget = false;

            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0.0f, 0.0f);
            lrt.anchorMax = new Vector2(1.0f, 1.0f);
            lrt.offsetMin = new Vector2(textLeft, 0.0f);
            lrt.offsetMax = new Vector2(-PaperKit.Pad, 0.0f);

            var callout = new Callout { Control = slot.Control, Label = label, Button = button };

            button.onClick.AddListener(() => Press(callout));

            go.AddComponent<PaperButton>();
            FocusRing.Attach(go, 3.0f);

            return callout;
        }

        /// <summary>
        /// The line from a callout to the control it names.
        ///
        /// ⚠️⚠️ IT IS DRAWN FROM THE GENERATED ANCHOR TABLE AND NEVER FROM A TYPED-IN POINT. See
        /// `PadDiagram`: the picture and the anchors come out of one Python pass, so moving the
        /// d-pad in the drawing moves the four lines that point at it. A hand-written arrow-head
        /// would be a second table that goes stale silently, which is the fault this repository
        /// has recorded three separate times (`docs/TODO.md` §§ 96, 114, 124.11).
        ///
        /// ⚠️ NO ANCHOR MEANS NO LINE, WHICH IS THE HONEST ANSWER RATHER THAN A GUESS. A control
        /// the drawing does not have still gets its callout, so a binding can never vanish off
        /// this screen; it simply has nothing to point at.
        /// </summary>
        private void BuildLeader(RectTransform root, Slot slot, Vector2 callout, Vector2 size)
        {
            if (PadDiagram.Art == null) return;
            if (!PadDiagram.TryAnchor(slot.Control, out var normalised)) return;

            // ⚠️ THE Y FLIP HAPPENS HERE AND NOWHERE ELSE. The manifest measures DOWN from the
            // top of the picture, the way an image is addressed; a `RectTransform` measures UP.
            // Doing it in two places is how every line ends up mirrored on one axis.
            var target = new Vector2((normalised.x - 0.5f) * size.x,
                                     BoardCentreY + (0.5f - normalised.y) * size.y);

            var start = new Vector2((int)slot.Side * (size.x * 0.5f + Gutter), callout.y);

            var delta = target - start;
            float length = delta.magnitude;
            if (length < 1.0f) return;

            var go = new GameObject("Leader_" + slot.Control, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_leaders != null ? _leaders : root, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.0f, 0.5f);
            rt.anchoredPosition = start;
            rt.sizeDelta = new Vector2(length, 2.0f);
            rt.localRotation = Quaternion.Euler(0.0f, 0.0f,
                                                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;

            // ⚠️ SOFT INK AT HALF STRENGTH. A leader line is the quietest mark on the screen by
            // construction: it carries no information of its own, it only joins two things that
            // do. At full ink weight eighteen of them read as a cage over the drawing.
            var ink = UiTheme.PaperInkSoft;
            image.color = new Color(ink.r, ink.g, ink.b, 0.5f);
        }

        private void BuildFooter(RectTransform root)
        {
            _status = PaperKit.Ink(root, "", PaperKit.Body, TextAnchor.MiddleCenter);
            MenuKit.Place(_status.rectTransform, new Vector2(0.5f, 0.0f),
                          new Vector2(0.0f, 118.0f), new Vector2(1400.0f, 30.0f));

            // ⚠️ THE SPECTATOR FOOTNOTE, BECAUSE SILENCE HERE READS AS A BUG. A broadcast
            // operator who knows TAB cycles a target will look for it on this picture and not
            // find it, and the honest reason is one sentence long. `Rebinding.BlurbFor` puts the
            // same sentence over the same rows in the settings list.
            var note = PaperKit.Ink(root,
                                    "Spectator camera controls are not on this map. They share "
                                    + "these buttons and are listed in SETTINGS, CONTROLS.",
                                    PaperKit.Caption, TextAnchor.MiddleCenter, soft: true);
            MenuKit.Place(note.rectTransform, new Vector2(0.5f, 0.0f),
                          new Vector2(0.0f, 86.0f), new Vector2(1500.0f, 24.0f));

            var reset = PaperKit.Chip(root, "Reset", "RESET ALL");
            MenuKit.Place((RectTransform)reset.transform, new Vector2(0.5f, 0.0f),
                          new Vector2(-220.0f, 40.0f), new Vector2(320.0f, 60.0f));

            reset.onClick.AddListener(() =>
            {
                // ⚠️ IT RESETS EVERY DEVICE, NOT ONLY THE PAD, AND THE LABEL SAYS "ALL" FOR THAT
                // REASON. `Rebinding.ResetAll` calls `RemoveAllBindingOverrides`, which has no
                // per-device form; a chip reading "RESET PAD" over a call that also puts the
                // keyboard back is the screen lying about what the button did.
                Rebinding.ResetAll(_actions);
                Say("Every control is back to its default, on the pad and on the keyboard.");
                MenuSfx.Click();
                Refresh();
            });

            var done = PaperKit.Chip(root, "Done", "DONE");
            MenuKit.Place((RectTransform)done.transform, new Vector2(0.5f, 0.0f),
                          new Vector2(220.0f, 40.0f), new Vector2(320.0f, 60.0f));

            done.onClick.AddListener(() => { MenuSfx.Back(); Close(); });
        }

        // -------------------------------------------------------------------------------------
        // § READING AND WRITING BINDINGS
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The name of a control as this game already says it everywhere else.
        ///
        /// ⚠️⚠️ COMPUTED FROM THE PATH RATHER THAN TYPED INTO THE `Ring` TABLE, AND THAT IS WHAT
        /// KEEPS THIS SCREEN, THE SETTINGS PANEL AND THE GLYPH SHEET SAYING THE SAME WORD.
        /// `Settings.Rebinding.DisplayNameFor` runs this exact call, and `UI.InputGlyphs` is
        /// keyed on its uppercased output — its own note says so: *"the keys are exactly what
        /// `Hud.KeyLabel` returns, uppercase."* A hand-typed "LEFT BUMPER" here would be a third
        /// vocabulary, would miss the glyph, and would disagree with the row one screen back.
        /// </summary>
        public static string HumanName(string control)
            => InputControlPath.ToHumanReadableString(
                Rebinding.GamepadDevice + "/" + control,
                InputControlPath.HumanReadableStringOptions.OmitDevice).ToUpperInvariant();

        private void Refresh()
        {
            _revision = Rebinding.Revision;

            var bound = BindingsByControl();
            var shared = SharedByControl();

            foreach (var callout in _callouts)
            {
                if (callout.Label == null) continue;

                if (bound.TryGetValue(callout.Control, out string action))
                {
                    callout.Action = action;
                    callout.Label.text = Rebinding.LabelFor(action);
                    callout.Label.color = UiTheme.PaperInk;
                    Pressable(callout, true);
                    continue;
                }

                if (shared.TryGetValue(callout.Control, out string job))
                {
                    // ⚠️⚠️ SHOWN AND NOT PRESSABLE, WHICH IS `CLAUDE.md` § 6.3's RULE STATED AS
                    // CODE: *"a control that does something must react to the pointer; one that
                    // does nothing must not look pressable."* Rebinding one direction of a stick
                    // is not a thing — `Rebinding.SharedDisplayNameFor`'s note has the argument in
                    // full — so the two sticks read as jobs the pad already does and refuse the
                    // press instead of starting a rebind that cannot finish.
                    callout.Action = null;
                    callout.Label.text = job;
                    callout.Label.color = UiTheme.PaperInkSoft;
                    Pressable(callout, false);
                    continue;
                }

                // ⚠️ AN EMPTY CONTROL IS DRAWN AS EMPTY RATHER THAN HIDDEN. A gap in the ring
                // where a callout used to be is a diagram the reader has to count; the word says
                // the button is free, which is a fact worth knowing on a screen about bindings.
                callout.Action = null;
                callout.Label.text = "-";
                callout.Label.color = UiTheme.PaperInkSoft;
                Pressable(callout, false);
            }

            if (_status != null && _status.text.Length == 0) Say(Opening());
        }

        private static void Pressable(Callout callout, bool live)
        {
            if (callout.Button == null) return;

            callout.Button.interactable = live;

            // ⚠️ THE SURFACE CHANGES WITH IT, NOT ONLY THE FLAG. `Selectable.interactable` alone
            // is invisible on a control whose transition is `None`, which every paper control's
            // is: the row would refuse the press and look exactly like the rows that take one.
            PaperSkin.Apply(callout.Button.gameObject,
                            live ? PaperCraft.Surface.Tray : PaperCraft.Surface.Ghost);
        }

        /// <summary>
        /// The line under the drawing when nothing has happened yet.
        ///
        /// ⚠️ IT NAMES AN UNRECOGNISED PAD, BECAUSE THIS IS THE SCREEN SOMEBODY OPENS WHEN THEIR
        /// CONTROLLER IS BEHAVING STRANGELY. `ControllerWatch.StatusLine` is the same sentence the
        /// settings CONTROLS tab carries; a player sent here by it should not have to go back to
        /// read why the labels might be wrong.
        /// </summary>
        private static string Opening()
        {
            string watch = ControllerWatch.StatusLine();
            if (watch.Length > 0) return watch;

            return Gamepad.current != null
                ? "Reading " + Gamepad.current.displayName + "."
                : "No controller is connected. This map still shows what one would do.";
        }

        private void Press(Callout callout)
        {
            if (_session != null) return;   // one at a time

            if (callout.Action == null)
            {
                // ⚠️ THE STICKS GET THEIR OWN SENTENCE. `RebindSession.RefusalFor` produces it for
                // the four MOVE rows on the settings page; here the row IS the stick, so the same
                // fact needs saying about the object rather than about a direction of it.
                Say(callout.Control == "leftStick" || callout.Control == "rightStick"
                        ? "The sticks cannot be rebound. They are what they are on every pad."
                        : "Nothing is bound to " + HumanName(callout.Control) + " yet.");
                MenuSfx.Error();
                return;
            }

            string action = callout.Action;
            string refusal = RebindSession.RefusalFor(_actions, action, InputDeviceKind.Gamepad);

            if (refusal != null)
            {
                Say(refusal);
                MenuSfx.Error();
                return;
            }

            Say($"Press a button for \"{Rebinding.LabelFor(action)}\"…  (B or Esc to cancel)");
            callout.Label.text = "…";

            _session = RebindSession.Begin(_actions, action, InputDeviceKind.Gamepad,
                                           (outcome, conflict) =>
            {
                _session = null;

                switch (outcome)
                {
                    case RebindOutcome.Bound:
                        Say($"\"{Rebinding.LabelFor(action)}\" moved.");
                        MenuSfx.Click();
                        break;

                    case RebindOutcome.Conflict:
                        Say($"That button is already \"{conflict}\". Choose a different one.");
                        MenuSfx.Error();
                        break;

                    default:
                        Say("Rebind cancelled.");
                        MenuSfx.Back();
                        break;
                }

                Refresh();
            });

            // ⚠️ A NULL SESSION MEANS THE OPERATION COULD NOT EVEN BE BUILT, which `RefusalFor`
            // above should already have ruled out. Putting the label back is what stops a row
            // reading "…" for ever on a screen that is no longer listening.
            if (_session == null) Refresh();
        }

        private void Say(string line)
        {
            if (_status != null) _status.text = line;
        }
    }
}

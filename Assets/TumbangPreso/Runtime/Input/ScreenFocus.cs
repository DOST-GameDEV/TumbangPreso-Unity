using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// Gives one screen a controller focus path and a thumb-sized hit area on every control.
    ///
    /// ⚠️⚠️ IT IS INSTALLED BY `MenuKit.BuildCanvas` AND BY `ConvertedScreen`, WHICH IS EVERY
    /// SCREEN IN THE GAME, AND THAT IS THE ONLY REASON THIS FEATURE SURVIVES THE NEXT SCREEN.
    /// 🧑, twice: *"make that shit future proof and to update mobile and controller version every
    /// time we change ui or some shit"*, then *"anytime we add a feature, make sure all
    /// controller and mobile is considered"*. **A rule a human applies per screen is the rule
    /// that failed here three times**: `docs/TODO.md` § 96 (the hub's one door, which nobody
    /// found), § 114 (`PlayerNameplate` no longer installed by any screen while
    /// `PlayerHubLayoutProbe` still drove it) and § 124.11 (a probe knocking on a screen § 122
    /// had moved). All three are a list that a move made stale. There is no list here: a screen
    /// that goes through the two constructors every screen goes through gets this.
    ///
    /// ⚠️⚠️ AND IT REBUILDS RATHER THAN BEING BUILT ONCE. Nearly every screen in this project
    /// populates itself AFTER its canvas exists: `UiRows` fills a scroll list, `PaperDress`
    /// re-skins a converted screen, the loadout board builds tiles per hero. A focus path
    /// computed in `Awake` would name the controls a screen had before it had any. It recomputes
    /// on enable and whenever the control count changes.
    ///
    /// ⚠️ IT DOES NOT STEAL A SELECTION IT DID NOT MAKE. If something is already selected inside
    /// this screen, that is where the player is; re-selecting the first control on every layout
    /// pass would drag the highlight back to the top of the list every time a row updated.
    /// </summary>
    /// <summary>
    /// An invisible hit area: it takes a press and draws nothing at all.
    ///
    /// ⚠️⚠️ IT EMITS NO VERTICES, WHICH IS THE WHOLE POINT AND IS STRONGER THAN A TRANSPARENT
    /// `Image`. A zero-alpha Image is invisible only for as long as nobody sets its colour, and
    /// this front end is full of things that set colours on their children after the fact:
    /// `PaperDress.Screen` re-skins a whole converted screen, and `WoodSkin` and `GodotPanel`
    /// watch their rects and repaint. A pad on every control on every screen is a large surface
    /// for that to go wrong on, and it did: the first render of the thumb layer had pale grey
    /// squares floating over the street. **Nothing can repaint a mesh that is never built.**
    ///
    /// ⚠️ `Graphic.Raycast` STILL HITS IT, because a graphic raycast tests the RECT and the
    /// `raycastTarget` flag rather than any pixel. That is the same reason a fully transparent
    /// Image works at all (`MenuKit.EnsureHitArea`'s note: *"alpha plays no part in a graphic
    /// raycast"*); this simply removes the drawing half entirely.
    /// </summary>
    /// ⚠️⚠️ AND THE `RequireComponent` BELOW IS NOT DECORATION: WITHOUT IT THIS COMPONENT THREW
    /// ON EVERY SCREEN, ON THE ONE PLATFORM WHERE IT IS ALWAYS ON. `Graphic` carries its own
    /// `[RequireComponent(typeof(CanvasRenderer))]`, and `AddComponent` did not apply it to this
    /// subclass, so the pad object came up with a `TouchHitArea` and no `CanvasRenderer` and the
    /// base class threw `MissingComponentException` the moment it tried to draw. **On a phone
    /// `TouchHud.ShouldShow` is true, so this runs on every control of every screen**, which
    /// makes it a front end that throws rather than a probe that fails.
    ///
    /// ⚠️ IT WAS INVISIBLE ON THE DESKTOP FOR THE REASON THAT MAKES IT DANGEROUS. A Windows
    /// machine with no touchscreen never calls `ApplyTouchTargets`, so no pad is ever built and
    /// nothing throws. It surfaced in a full PlayMode suite only because `InputSurfaceProbe`
    /// forces the layer on, and then only because that probe threw before it could put
    /// `TouchHud.ForceVisible` back: **one probe's leaked static turned 2 red tests into 42.**
    /// `docs/TODO.md` § 126.1 has the run.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TouchHitArea : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
    }

    /// <summary>
    /// Marks a row that <see cref="ScreenFocus.MakeRoomForThumbs"/> grew, and remembers what its
    /// own layout asked for first.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE `TouchHud.ShouldShow` IS NOT A CONSTANT, WHICH IS EASY TO FORGET ON
    /// A MACHINE WHERE IT IS ALWAYS FALSE. Two things force the thumb layer on from a desktop:
    /// `TouchLayoutScreen.Open`, so the customiser can be used with a mouse, and
    /// `InputSurfaceProbe`, so the layout can be measured at all on a machine with no touchscreen.
    /// A pass that only ever raised a row would turn the settings panel into a phone layout the
    /// first time somebody opened the touch customiser and leave it that way for the session.
    ///
    /// ⚠️ THE PREVIOUS VALUES ARE STORED RATHER THAN RECOMPUTED, because "what this row would ask
    /// for on a desktop" is not a function of anything visible here: it is whatever the screen
    /// that built it decided, and on a converted screen that decision came out of a `.tscn`.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThumbRoom : MonoBehaviour
    {
        public float PreviousMin;
        public float PreviousPreferred;
    }

    [DisallowMultipleComponent]
    public sealed class ScreenFocus : MonoBehaviour
    {
        private const string PadName = "TouchPad";

        private readonly List<Selectable> _order = new List<Selectable>();
        private int _lastCount = -1;

        /// <summary>The focus path, top to bottom, as the pad walks it.</summary>
        public IReadOnlyList<Selectable> Order => _order;

        /// <summary>
        /// Installs one on <paramref name="root"/> if it has none.
        ///
        /// ⚠️ IDEMPOTENT, because both constructors may reach the same object: a converted screen
        /// whose canvas was built by `MenuKit` would otherwise carry two, and two would fight over
        /// the selection every frame.
        /// </summary>
        public static ScreenFocus Install(GameObject root)
        {
            if (root == null) return null;

            var existing = root.GetComponent<ScreenFocus>();
            return existing != null ? existing : root.AddComponent<ScreenFocus>();
        }

        private void OnEnable() => Rebuild();

        private void Update()
        {
            FollowSelectionIntoView();

            // ⚠️⚠️ THE THUMB LAYER GOING ON OR OFF IS THE SECOND REASON TO REBUILD, AND THE COUNT
            // CANNOT SEE IT. `TouchHud.ShouldShow` is not a constant: `TouchLayoutScreen` forces
            // it true while the customiser is open, so the customiser is usable with a mouse, and
            // false again when it closes. A screen behind it that grew its rows for a thumb would
            // otherwise keep them until something else added or removed a control, which on the
            // settings panel is never. `MakeRoomForThumbs` is what puts them back, and `Rebuild`
            // is its only caller.
            bool touch = TouchHud.ShouldShow;

            // ⚠️ A COUNT, NOT A DEEP COMPARE. Rebuilding a focus path costs a hierarchy walk and
            // a sort; doing it every frame on a settings list of forty rows is real. The count
            // changes whenever a screen adds, removes, enables or disables a control, which is
            // every case that can invalidate the path.
            int count = CountSelectables();
            if (count == _lastCount && touch == _lastTouch) return;

            Rebuild();
        }

        /// <summary>Whether the thumb layer was on the last time this screen was rebuilt.</summary>
        private bool _lastTouch;

        /// <summary>The control this screen last scrolled to, so the work is done once per move.</summary>
        private GameObject _followed;

        /// <summary>
        /// Scrolls the focused control into its own viewport.
        ///
        /// ⚠️⚠️ WITHOUT THIS A PAD WALKS OFF THE BOTTOM OF THE SETTINGS LIST AND KEEPS GOING,
        /// SELECTING ROWS NOBODY CAN SEE. Unity's input module moves the selection and does
        /// nothing about scrolling; the settings panel is about forty rows in a viewport that
        /// shows around ten, so pressing DOWN eleven times left the highlight on a row below the
        /// fold with the list still at the top. **"A controller can reach every control" was
        /// being asserted about a path a controller could not actually see**, which is `CLAUDE.md`
        /// § 4a's § 96 in a new costume: the probe proved the plate was there, not that somebody
        /// could get to it. `InputSurfaceProbe.InsideOwnViewport` exists to skip exactly these
        /// rows and its note is the other half of this bug written down.
        ///
        /// ⚠️ IT ALSO MAKES THE SCROLLBAR AN AFFORDANCE RATHER THAN THE ONLY WAY DOWN. Before
        /// this, the bar was the single mechanism a pad had for reaching row thirty, which is why
        /// it was on the focus path at 14 units wide in the first place. A pad scrolls by moving
        /// the selection now, a thumb scrolls by dragging the list, and the bar is left to say
        /// where you are.
        ///
        /// ⚠️ ONCE PER SELECTION CHANGE, NOT PER FRAME. Writing `normalizedPosition` every frame
        /// fights the player's own drag and the scroll wheel, and `SettingsWheelProbe` is the
        /// test that would find that the hard way.
        /// </summary>
        private void FollowSelectionIntoView()
        {
            var system = EventSystem.current;
            if (system == null) return;

            var selected = system.currentSelectedGameObject;

            if (selected == _followed) return;
            _followed = selected;

            if (selected == null || !selected.transform.IsChildOf(transform)) return;

            var scroll = selected.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;

            var target = selected.transform as RectTransform;
            if (target == null) return;

            // ⚠️ IN THE CONTENT'S OWN SPACE. The viewport scrolls the CONTENT, so "how far down
            // this row is" is only meaningful measured against the content's height; screen
            // pixels and world units are both the wrong unit here for `AspectRatioProbes`'
            // reason, which is that the canvas is scaled and may be rendering to a texture.
            float contentHeight = scroll.content.rect.height;
            float viewHeight = scroll.viewport.rect.height;

            // Nothing to scroll: the list fits.
            if (contentHeight <= viewHeight + 1.0f) return;

            Vector3 local = scroll.content.InverseTransformPoint(target.TransformPoint(target.rect.center));

            // Distance from the TOP of the content down to the row's centre, in content units.
            float fromTop = scroll.content.rect.yMax - local.y;

            float half = target.rect.height * 0.5f + TouchMetrics.MinGapUnits;
            float travel = contentHeight - viewHeight;

            // The window of scroll offsets that keep this row fully visible, as a fraction of the
            // travel. `rowAtViewTop` is the most-scrolled end of it and `rowAtViewBottom` the
            // least, because scrolling DOWN moves the content UP past the row.
            float rowAtViewTop = Mathf.Clamp((fromTop - half) / travel, 0.0f, 1.0f);
            float rowAtViewBottom = Mathf.Clamp((fromTop + half - viewHeight) / travel, 0.0f, 1.0f);

            // ⚠️ A ROW TALLER THAN ITS OWN VIEWPORT INVERTS THAT WINDOW, and `Mathf.Clamp` with
            // min above max returns the min, which would align such a row's BOTTOM edge and hide
            // its label. Showing the top of it is the useful answer.
            if (rowAtViewBottom > rowAtViewTop) rowAtViewBottom = rowAtViewTop;

            // `verticalNormalizedPosition` is 1 at the top of the content and 0 at the bottom.
            float current = 1.0f - scroll.verticalNormalizedPosition;

            // ⚠️ IT ONLY MOVES WHEN THE ROW IS ACTUALLY OUT OF VIEW. Snapping every selection to
            // the middle of the viewport makes the whole list lurch on every press, which reads
            // as the screen fighting the player rather than following them.
            float wanted = Mathf.Clamp(current, rowAtViewBottom, rowAtViewTop);
            if (Mathf.Abs(wanted - current) < 0.0005f) return;

            scroll.verticalNormalizedPosition = 1.0f - wanted;
        }

        private int CountSelectables()
        {
            int count = 0;

            foreach (var s in GetComponentsInChildren<Selectable>(includeInactive: false))
                if (s.IsInteractable() && Owns(s)) count++;

            return count;
        }

        /// <summary>
        /// Whether this screen, rather than a nested one, owns <paramref name="control"/>.
        ///
        /// ⚠️⚠️ A DROPDOWN'S OPEN LIST IS A SCREEN INSIDE A SCREEN, AND WITHOUT THIS RULE BOTH
        /// WOULD WIRE THE SAME CONTROLS. `WoodDropdown` builds its popup as a child canvas of the
        /// screen that opened it, so the outer `ScreenFocus` sees the option rows in its own
        /// `GetComponentsInChildren` sweep and would chain them into the settings list. Pressing
        /// DOWN in an open dropdown would then walk out of the dropdown and into the row behind
        /// it, which is the exact shape of `CLAUDE.md` § 6.3's *"a player who learns Escape is
        /// reliable and then meets one screen where it is not"*, said about a stick.
        ///
        /// ⚠️ THE INNER ONE WINS AND THE OUTER ONE DOES NOT STEAL THE SELECTION BACK.
        /// <see cref="AdoptSelection"/> returns early when the current selection is already
        /// inside this screen, and a nested screen's controls ARE inside it, so the popup keeps
        /// focus for as long as it is open with no modal stack to maintain.
        /// </summary>
        private bool Owns(Component control) => OwnerOf(control) == this;

        /// <summary>
        /// The screen that owns <paramref name="control"/>: the nearest one at or above it.
        ///
        /// ⚠️ PUBLIC SO THE PROBE APPLIES THE SAME RULE. `InputSurfaceProbe` counted every
        /// `Selectable` under a screen and then compared that count against the focus path, so on
        /// the main menu with the settings panel open it reported *"visits 5 of 49 controls"* and
        /// called 44 of them unreachable. All 44 belonged to the settings panel's OWN
        /// `ScreenFocus` and were perfectly reachable there. **Two components disagreeing about
        /// which controls belong to a screen is how a probe invents a fault**, so there is one
        /// answer and both callers ask it.
        /// </summary>
        public static ScreenFocus OwnerOf(Component control)
            => control == null ? null : control.GetComponentInParent<ScreenFocus>();

        /// <summary>
        /// Recomputes the focus path and the hit areas.
        ///
        /// ⚠️ READING ORDER, NOT HIERARCHY ORDER. A screen's hierarchy is whatever order it
        /// happened to build in, and `UiRows` builds a section's header after its rows on more
        /// than one screen. A pad walks what the eye walks: top to bottom, then left to right.
        /// </summary>
        public void Rebuild()
        {
            _order.Clear();

            foreach (var s in GetComponentsInChildren<Selectable>(includeInactive: false))
            {
                if (!s.IsInteractable()) continue;
                if (s.navigation.mode == Navigation.Mode.None) continue;

                // A nested screen owns its own controls. See `Owns`.
                if (!Owns(s)) continue;

                _order.Add(s);
            }

            _lastCount = _order.Count;
            _lastTouch = TouchHud.ShouldShow;

            _order.Sort(Reading);

            for (int i = 0; i < _order.Count; i++) Link(i);

            // ⚠️⚠️ THE LAYOUT IS REBUILT BETWEEN THE TWO PASSES, AND WITHOUT IT THE SECOND ONE
            // MEASURES THE FIRST ONE'S OLD SCREEN. Writing `LayoutElement.minHeight` only marks
            // the group dirty; the rects it changes do not move until Unity's next layout pass.
            // `ApplyTouchTargets` reads every control's rect to work out what room it has, so
            // running it in the same frame would compute the clamps against the rows this pass
            // just replaced and pad to the OLD gaps. It is forced only when something actually
            // moved, because a full layout rebuild of a 49-control screen is not free.
            // ⚠️ THE `as` CAN RETURN NULL AND `ForceRebuildLayoutImmediate(null)` THROWS. This
            // component is installed on whatever `MenuKit.BuildCanvas` or `ConvertedScreen.Start`
            // hands it, and a converted screen's script does not have to sit on a UI node. The
            // throw would only ever happen where the thumb layer is on, which is a phone, which
            // is the one place there is nobody to read the exception.
            var root = transform as RectTransform;

            if (MakeRoomForThumbs() && root != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            ApplyTouchTargets();
            AdoptSelection();
        }

        /// <summary>
        /// On a touch screen, makes ROOM for a thumb before <see cref="ApplyTouchTargets"/> tries
        /// to pad one in.
        ///
        /// ⚠️⚠️ THIS IS THE HALF THAT WAS MISSING, AND WITHOUT IT THE THUMB FLOOR WAS UNREACHABLE
        /// BY CONSTRUCTION. `ApplyTouchTargets` grows a hit area only as far as the nearest
        /// neighbour allows, and this front end was authored for a mouse: the settings rows are
        /// stacked with no gap at all, so the clamp came out at zero and **1519 measurements
        /// across twelve shapes sat at exactly their artwork size** with the pad unable to add a
        /// single unit. `docs/TODO.md` § 125.13 called that a layout pass on the converted
        /// screens, and this is that pass done once, here, rather than 79 times in 79 places.
        ///
        /// ⚠️⚠️ IT GROWS THE BOX A LAYOUT GROUP OWNS, AND WHAT THAT IS DEPENDS ON THE SCREEN.
        /// Where the control sits inside a row (the converted settings panel's
        /// `MasterVolumeSlider` lives in `MasterVolumeRow`, and the group is `Content` above
        /// that) the ROW grows, the neighbours move apart, and the existing transparent pad
        /// reaches the floor with the slider's own picture untouched. Where the control IS the
        /// row (a rebind keycap is a direct child of its `HorizontalLayoutGroup`) the control
        /// itself grows, which on a phone is a bigger key and is the right answer;
        /// `MenuKit.BalancedButtonUnits` caps its type at 28 units so it cannot turn into a
        /// headline. **Both cases are the layout making room, which is the thing padding could
        /// not do.**
        ///
        /// ⚠️ WHAT IT NEVER DOES IS STRETCH A CONTROL THAT NO GROUP OWNS. `CLAUDE.md` § 6.2c:
        /// the artwork is sized against its content and is correct; the hit area is sized against
        /// a thumb and is not. Writing a `sizeDelta` here would be resizing a picture somebody
        /// composed, with nothing to push its neighbours out of the way, which is how "make it
        /// bigger for mobile" usually wrecks a layout.
        ///
        /// ⚠️⚠️ AND IT GROWS THE NEAREST ANCESTOR THAT A LAYOUT GROUP ACTUALLY CONTROLS, WHICH IS
        /// RARELY THE CONTROL ITSELF. On the converted settings panel the slider's parent is
        /// `MasterVolumeRow`, an anchored container, and the row's parent `Content` is the
        /// vertical group; setting `minHeight` on the slider would have reached nothing at all.
        /// Walking up to the child OF the group is what makes one rule cover the code-built rows,
        /// the converted rows, the lobby rails and the character-select tab bar together.
        ///
        /// ⚠️ AN ABSOLUTELY PLACED CONTROL IS LEFT ALONE ON PURPOSE. Nothing here can move the
        /// main menu's pennants apart without knowing what the screen means, so those keep
        /// whatever room they already have and the probe reports the shortfall. **A pass that
        /// silently overlapped two hard-placed controls would be trading a small target for a
        /// stolen press**, which is the fault this file already carries a whole probe check for.
        /// </summary>
        private bool MakeRoomForThumbs()
        {
            if (!TouchHud.ShouldShow) return ReleaseThumbRoom();

            bool moved = false;

            foreach (var control in _order)
            {
                if (control == null) continue;

                var rt = control.transform as RectTransform;
                if (rt == null) continue;

                // Already a thumb target. Nothing to make room for.
                if (rt.rect.height >= TouchMetrics.MinTargetUnits - 0.5f) continue;

                var row = LayoutRowFor(rt);
                if (row == null) continue;

                var element = row.GetComponent<LayoutElement>();
                if (element == null) element = row.gameObject.AddComponent<LayoutElement>();

                // ⚠️ RAISED, NEVER LOWERED. A row that already asks for more than the floor asked
                // for it against its own content, and shrinking it here would undo a decision
                // this pass knows nothing about.
                if (element.minHeight >= TouchRowUnits
                    && element.preferredHeight >= TouchRowUnits) continue;

                // ⚠️⚠️ WHAT THE ROW ASKED FOR IS RECORDED BEFORE IT IS RAISED, AND WITHOUT THAT
                // THIS PASS IS A ONE-WAY DOOR ONTO THE DESKTOP. `TouchHud.ShouldShow` is not a
                // constant: `TouchLayoutScreen.Open` FORCES the thumb layer on so the customiser
                // is usable with a mouse (its own note: *"a row that appears only on Android is a
                // row nobody can test on the machine this game is built on"*), and
                // `InputSurfaceProbe` forces it on for a sweep. Any `Rebuild` while it is forced
                // would grow every row on a desktop screen, and with nothing recorded there would
                // be no way back: the settings panel would keep 168-unit rows for the rest of the
                // session and the next screenshot would be of a phone layout on a monitor.
                var room = row.GetComponent<ThumbRoom>();

                if (room == null)
                {
                    room = row.gameObject.AddComponent<ThumbRoom>();
                    room.PreviousMin = element.minHeight;
                    room.PreviousPreferred = element.preferredHeight;
                }

                element.minHeight = Mathf.Max(element.minHeight, TouchRowUnits);
                element.preferredHeight = Mathf.Max(element.preferredHeight, TouchRowUnits);
                moved = true;
            }

            return moved;
        }

        /// <summary>
        /// Puts every row this screen grew back to what its own layout asked for.
        ///
        /// ⚠️ IT WALKS THE CHILDREN RATHER THAN `_order`, because the rows it grew are ANCESTORS
        /// of the controls in `_order` and, on a screen whose control list has changed since, may
        /// no longer be above any of them at all. The marker component is the record; the focus
        /// path is not.
        /// </summary>
        private bool ReleaseThumbRoom()
        {
            var rooms = GetComponentsInChildren<ThumbRoom>(includeInactive: true);
            if (rooms.Length == 0) return false;

            foreach (var room in rooms)
            {
                var element = room.GetComponent<LayoutElement>();

                if (element != null)
                {
                    element.minHeight = room.PreviousMin;
                    element.preferredHeight = room.PreviousPreferred;
                }

                Destroy(room);
            }

            return true;
        }

        /// <summary>
        /// The row height that lets two stacked thumb targets both reach the floor with the
        /// required gap between them: <see cref="TouchMetrics.MinTargetUnits"/> plus
        /// <see cref="TouchMetrics.MinGapUnits"/>.
        ///
        /// ⚠️ IT IS THE SAME SUM `UiRows.TouchRowHeight` USES, deliberately, so a code-built row
        /// and a converted one come out the same height on a phone. Two numbers here would be two
        /// row heights on one screen.
        /// </summary>
        private const float TouchRowUnits = TouchMetrics.MinTargetUnits + TouchMetrics.MinGapUnits;

        /// <summary>
        /// The ancestor of <paramref name="rt"/> whose height a layout group actually decides, or
        /// null when nothing in the chain is under one.
        ///
        /// ⚠️ IT STOPS AT THIS SCREEN. Walking past the `ScreenFocus` would let one screen's
        /// thumb pass resize the panel, the canvas or the scene root above it.
        /// </summary>
        private RectTransform LayoutRowFor(RectTransform rt)
        {
            for (var node = rt; node != null && node != transform; node = node.parent as RectTransform)
            {
                var parent = node.parent as RectTransform;
                if (parent == null) return null;

                var group = parent.GetComponent<HorizontalOrVerticalLayoutGroup>();

                // ⚠️ `childControlHeight` IS THE QUESTION, NOT "IS THERE A GROUP". A group that
                // does not control its children's height ignores `minHeight` entirely, so
                // writing one would be a silent no-op and this pass would report itself as done.
                if (group != null && group.childControlHeight) return node;
            }

            return null;
        }

        private static int Reading(Selectable a, Selectable b)
        {
            var ra = (RectTransform)a.transform;
            var rb = (RectTransform)b.transform;

            Vector3 pa = ra.TransformPoint(ra.rect.center);
            Vector3 pb = rb.TransformPoint(rb.rect.center);

            // ⚠️ AN 8-UNIT BAND, SO A ROW OF CONTROLS READS AS A ROW. Two buttons whose centres
            // differ by a pixel of layout rounding are side by side, not one above the other, and
            // sorting them strictly by y makes a pad walk a horizontal row vertically.
            if (Mathf.Abs(pa.y - pb.y) > 8.0f) return pb.y.CompareTo(pa.y); // higher first
            return pa.x.CompareTo(pb.x);                                     // then left first
        }

        /// <summary>
        /// Wires one control's four directions.
        ///
        /// ⚠️⚠️ EXPLICIT, NOT `Automatic`, AND THE DIFFERENCE IS WHETHER THE PATH IS COMPLETE.
        /// Unity's automatic navigation picks the nearest selectable in the direction pressed,
        /// which quietly leaves a control unreachable whenever the geometry is awkward: a button
        /// in a corner, a row inside a scroll viewport, a control that overlaps its neighbour.
        /// The screens in this project are built by layout groups at nine aspect ratios, so
        /// "awkward geometry" is the normal case rather than the exception. An explicit chain
        /// visits every control exactly once by construction, and `InputSurfaceProbe` asserts
        /// that walking it reaches all of them.
        ///
        /// ⚠️ IT WRAPS. The last control's DOWN is the first, so a pad cannot arrive at the
        /// bottom of a list and appear to be stuck. `CLAUDE.md` § 6.3: a dead end is a bug.
        /// </summary>
        private void Link(int index)
        {
            var s = _order[index];
            int count = _order.Count;

            var nav = new Navigation { mode = Navigation.Mode.Explicit };

            if (count > 1)
            {
                var previous = _order[(index - 1 + count) % count];
                var next = _order[(index + 1) % count];

                nav.selectOnUp = previous;
                nav.selectOnDown = next;
                nav.selectOnLeft = previous;
                nav.selectOnRight = next;
            }

            s.navigation = nav;
        }

        /// <summary>
        /// Selects the first control when nothing in this screen is selected.
        ///
        /// ⚠️⚠️ WITHOUT THIS A PAD DOES NOTHING ON A FRESHLY OPENED SCREEN AND THE PLAYER
        /// CONCLUDES THE CONTROLLER IS NOT SUPPORTED. Unity's navigation moves the CURRENT
        /// selection; with none, the first stick press has nothing to move from and the screen is
        /// inert. It is the exact shape of `docs/TODO.md` § 108's EQUIP button with no listener:
        /// the screen looks right and does nothing.
        /// </summary>
        private void AdoptSelection()
        {
            if (_order.Count == 0) return;

            var system = EventSystem.current;
            if (system == null) return;

            var current = system.currentSelectedGameObject;

            if (current != null && current.activeInHierarchy
                && current.transform.IsChildOf(transform))
                return;

            system.SetSelectedGameObject(_order[0].gameObject);
        }

        /// <summary>
        /// Grows every control's hit area as far as it can WITHOUT reaching a neighbour.
        ///
        /// ⚠️⚠️ THE UNCLAMPED VERSION OF THIS SHIPPED A REAL BUG AND THE PROBE CAUGHT IT ON ITS
        /// FIRST HONEST RUN. Padding every control to 144 units regardless of what was beside it
        /// put each rebind row's pad straight over the row below, and
        /// `InputSurfaceProbe` reported it exactly: *"a press at the centre of 'Button_W' lands on
        /// 'Button_S/TouchPad' instead"*, for ten rows of the settings panel. **That is not a
        /// mobile problem, it is a MOUSE problem**: a desktop player clicking a rebind row would
        /// have rebound the wrong action, on a screen that looked completely correct. The pad's
        /// own note predicted this ("it can still steal a neighbour's press, and that is what the
        /// probe is for") and the answer is to bound the growth rather than to accept it.
        ///
        /// ⚠️⚠️ AND IT ONLY RUNS WHERE THERE IS A THUMB. A mouse is precise to a pixel, so a
        /// desktop build gains nothing from a pad and risks exactly the fault above. Off a touch
        /// device the pads are switched off, which is also what makes the change safe to ship on
        /// the branch a desktop tournament is played from.
        ///
        /// ⚠️ O(n²) OVER ONE SCREEN'S CONTROLS, AND ONLY WHEN THE COUNT CHANGES. The settings
        /// panel is the largest screen in the game at 49 controls, so the worst case is about
        /// 2400 comparisons on a rebuild, not per frame. `Update` gates it on the count.
        /// </summary>
        private void ApplyTouchTargets()
        {
            bool touch = TouchHud.ShouldShow;

            int count = _order.Count;
            var rects = new Rect[count];

            for (int i = 0; i < count; i++)
            {
                var rt = (RectTransform)_order[i].transform;
                Vector3 centre = transform.InverseTransformPoint(rt.TransformPoint(rt.rect.center));
                Vector2 size = SizeInCanvasUnits(transform as RectTransform ?? rt, rt);

                // The screen's own transform is the common space; size is taken from the same
                // conversion so both sides of every comparison below are in one unit.
                rects[i] = new Rect(centre.x - size.x * 0.5f, centre.y - size.y * 0.5f,
                                    size.x, size.y);
            }

            for (int i = 0; i < count; i++)
            {
                if (!touch)
                {
                    // No thumb here: the artwork IS the hit area, exactly as it was before.
                    var existing = _order[i].transform.Find(PadName);
                    if (existing != null) existing.gameObject.SetActive(false);
                    continue;
                }

                float maxGrowX = float.MaxValue;
                float maxGrowY = float.MaxValue;

                for (int j = 0; j < count; j++)
                {
                    if (i == j) continue;

                    // The gap along each axis. Whichever is larger is the axis that currently
                    // KEEPS these two apart, so that is the one growth must respect; growing on
                    // the other axis cannot bring them together.
                    float gapX = Mathf.Abs(rects[i].center.x - rects[j].center.x)
                                 - (rects[i].width + rects[j].width) * 0.5f;

                    float gapY = Mathf.Abs(rects[i].center.y - rects[j].center.y)
                                 - (rects[i].height + rects[j].height) * 0.5f;

                    // ⚠️ HALF THE GAP, BECAUSE THE NEIGHBOUR IS GROWING TOO. Each side takes half
                    // and one unit is left between them, so two pads meet without overlapping.
                    if (gapX >= gapY) maxGrowX = Mathf.Min(maxGrowX, Mathf.Max(0.0f, gapX * 0.5f - 1.0f));
                    else maxGrowY = Mathf.Min(maxGrowY, Mathf.Max(0.0f, gapY * 0.5f - 1.0f));
                }

                EnsureTouchTarget(_order[i], maxGrowX, maxGrowY);
            }
        }

        /// <summary>
        /// Grows a control's HIT AREA, never its artwork, up to the thumb floor.
        ///
        /// ⚠️⚠️ THE HIT AREA AND THE PICTURE ARE DIFFERENT RECTANGLES, AND CONFLATING THEM IS
        /// WHY "MAKE IT BIGGER FOR MOBILE" ALWAYS WRECKS A LAYOUT. `CLAUDE.md` § 6.2c asks what
        /// a size is measured against; the artwork is measured against its content and is
        /// correct, while the hit area is measured against a thumb and is not. A transparent
        /// child grown to `TouchMetrics.MinTargetUnits` fixes the second and cannot move the
        /// first. This is `MenuKit.EnsureHitArea`'s trick, generalised from sliders to
        /// everything.
        ///
        /// ⚠️ IT CAN STILL STEAL A NEIGHBOUR'S PRESS, AND THAT IS WHAT THE PROBE IS FOR. Padding
        /// a 40-unit chip out to 144 in a row of 60-unit chips would put it over the two beside
        /// it. `InputSurfaceProbe` raycasts every control's own centre afterwards, so a pad that
        /// covered something reports as that something being unreachable. Growing blindly and
        /// checking is right; the alternative is a layout that silently cannot be pressed.
        ///
        /// ⚠️ IDEMPOTENT AND FIRST-SIBLING, so a screen reopened twenty times grows one pad and
        /// the artwork still draws over it.
        /// </summary>
        public static void EnsureTouchTarget(Selectable control,
                                             float maxGrowX = float.MaxValue,
                                             float maxGrowY = float.MaxValue)
        {
            if (control == null) return;

            var rt = control.transform as RectTransform;
            if (rt == null) return;

            var rect = rt.rect;

            float growX = Mathf.Max(0.0f, TouchMetrics.MinTargetUnits - rect.width) * 0.5f;
            float growY = Mathf.Max(0.0f, TouchMetrics.MinTargetUnits - rect.height) * 0.5f;

            // ⚠️ NEVER PAST A NEIGHBOUR. See `ApplyTouchTargets`: an unclamped pad put every
            // rebind row's hit area over the row below it.
            growX = Mathf.Min(growX, Mathf.Max(0.0f, maxGrowX));
            growY = Mathf.Min(growY, Mathf.Max(0.0f, maxGrowY));

            var existing = rt.Find(PadName);

            if (growX <= 0.01f && growY <= 0.01f)
            {
                // Already big enough. A pad from an earlier, smaller layout is now wrong.
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            // ⚠️⚠️ THE `CanvasRenderer` IS LISTED HERE AS WELL AS ON `TouchHitArea`, AND BOTH
            // HALVES EARN THEIR PLACE. `new GameObject(name, params Type[])` adds exactly the
            // types it is given and resolves no dependencies at all, so naming it here is what
            // guarantees a pad born with one; the `[RequireComponent]` on the component covers a
            // pad that some earlier layout built without it and that this pass is reusing. A pad
            // is created for every control on every screen whenever the thumb layer is on, which
            // is always on a phone, so a graphic that throws here is the whole front end.
            var go = existing != null
                ? existing.gameObject
                : new GameObject(PadName, typeof(RectTransform), typeof(CanvasRenderer));

            if (existing == null)
            {
                go.transform.SetParent(rt, false);
                go.transform.SetAsFirstSibling();
            }

            go.SetActive(true);

            var pad = (RectTransform)go.transform;
            pad.anchorMin = Vector2.zero;
            pad.anchorMax = Vector2.one;
            pad.offsetMin = new Vector2(-growX, -growY);
            pad.offsetMax = new Vector2(growX, growY);

            // ⚠️⚠️ A GRAPHIC THAT DRAWS NOTHING, NOT A TRANSPARENT `Image`, AND THE RENDER IS WHY.
            // The first version used an `Image` at zero alpha, which is the trick
            // `MenuKit.EnsureHitArea` uses for sliders and is correct in isolation. It is NOT
            // correct here: these pads are added to every control on every screen, and this front
            // end RE-SKINS screens after they are built. `PaperDress.Screen` and the `WoodSkin`
            // and `GodotPanel` watchers walk their children and repaint the Images they find, so
            // the pads came back as pale plates and photographed as **grey squares floating over
            // the arena**, behind every touch control and over the houses.
            //
            // `TouchHitArea` has no sprite and emits no vertices, so there is nothing for a
            // re-skinner to colour and nothing for the renderer to draw, while `Graphic.Raycast`
            // still hits its rect. It cannot be made visible by any later pass.
            if (go.GetComponent<Image>() is Image stale && stale != null) DestroyImmediate(stale);

            // ⚠️ AND ONCE MORE ON THE REUSE PATH, because the line above can leave a pad that a
            // previous version of this method built as a bare `RectTransform`. See `TouchHitArea`.
            if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();

            var area = go.GetComponent<TouchHitArea>();
            if (area == null) area = go.AddComponent<TouchHitArea>();

            area.raycastTarget = true;
        }

        /// <summary>
        /// The rectangle a thumb actually gets: the pad when one was needed, else the artwork.
        ///
        /// ⚠️ IT RETURNS THE TRANSFORM RATHER THAN A `Rect`, AND THE FIRST VERSION DID NOT. That
        /// one returned world corners, and a caller then had to guess what unit they were in.
        /// They are not canvas units and they are not screen pixels: on a `ScreenSpaceCamera`
        /// canvas Unity scales the whole canvas down to sit at `planeDistance`, so a 144-unit
        /// control measures about 0.5 in world space. `InputSurfaceProbe` divided by
        /// `scaleFactor`, which is a different number again, and every control on every screen
        /// reported as **0x0 units** against a 144 floor. **A probe that fails everything is as
        /// useless as one that fails nothing**, and it took a run to notice because "0" is a
        /// plausible answer for a control that has not laid out yet. Handing back the transform
        /// lets the caller convert into whichever space its own assertion is written in.
        /// </summary>
        public static RectTransform HitRectOf(Selectable control)
        {
            var rt = (RectTransform)control.transform;
            var pad = rt.Find(PadName) as RectTransform;

            return pad != null && pad.gameObject.activeSelf ? pad : rt;
        }

        /// <summary>
        /// The size of <paramref name="what"/> in <paramref name="canvasSpace"/>'s own units.
        ///
        /// ⚠️ THIS IS THE CONVERSION `AspectRatioProbes.AssertInside` ALREADY USES, and it is
        /// the only one that is correct at every render mode: world corners pushed back through
        /// the canvas's inverse transform land in the canvas's reference units, which is the unit
        /// every layout number in this project is written in.
        /// </summary>
        public static Vector2 SizeInCanvasUnits(RectTransform canvasSpace, RectTransform what)
        {
            if (canvasSpace == null || what == null) return Vector2.zero;

            var corners = new Vector3[4];
            what.GetWorldCorners(corners);

            Vector3 a = canvasSpace.InverseTransformPoint(corners[0]);
            Vector3 b = canvasSpace.InverseTransformPoint(corners[2]);

            return new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        }
    }
}

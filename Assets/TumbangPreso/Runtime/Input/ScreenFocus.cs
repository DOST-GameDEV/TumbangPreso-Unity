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
    public sealed class TouchHitArea : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
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
            // ⚠️ A COUNT, NOT A DEEP COMPARE. Rebuilding a focus path costs a hierarchy walk and
            // a sort; doing it every frame on a settings list of forty rows is real. The count
            // changes whenever a screen adds, removes, enables or disables a control, which is
            // every case that can invalidate the path.
            int count = CountSelectables();
            if (count == _lastCount) return;

            Rebuild();
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

            _order.Sort(Reading);

            for (int i = 0; i < _order.Count; i++) Link(i);

            ApplyTouchTargets();
            AdoptSelection();
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

            var go = existing != null
                ? existing.gameObject
                : new GameObject(PadName, typeof(RectTransform));

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

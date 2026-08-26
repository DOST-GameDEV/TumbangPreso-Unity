using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Keeps independently positioned HUD elements from being drawn on top of one another, by
    /// moving the less important one out of the way at runtime.
    ///
    /// ⚠️⚠️ THIS IS A GAME FEATURE, NOT A TEST, AND THAT DISTINCTION WAS MADE EXPLICITLY. 🧑,
    /// 2026-08-26: *"i want ut o make sure too that no Ui's stack on each otehr and if they do
    /// force one to go below it or smth"*, and when a probe was offered instead: *"i dont want a
    /// probe for it i want it in th egame as a feature"*. A probe would have caught the pairs
    /// somebody thought to list, on the resolutions somebody thought to test, and shipped a build
    /// that still stacked whenever a string went long. This runs in the player.
    ///
    /// ⚠️⚠️ IT IS THE BACKSTOP AND NOT THE FIRST ANSWER. Where two elements can be made rows of
    /// one layout group they are, because a layout group physically cannot stack its own
    /// children, and that is how the reported collision was actually fixed: the toast and the
    /// lata alert became rows of `TopCentre` instead of labels pinned at literal offsets. Prefer
    /// that every time. This exists for the elements that genuinely cannot share a parent,
    /// because they are anchored to different screen corners or belong to different systems.
    ///
    /// ⚠️ WHAT WENT WRONG THAT THIS PREVENTS. `ToastLabel` was `Place`d at a hard-coded y = -160
    /// with a height of 44, so it occupied 160..204. `TimerPressure` is a child of a
    /// `VerticalLayoutGroup` whose height depends on which of its siblings are switched on, and
    /// it landed at 166..198. Neither number was wrong when it was written. The bug was that one
    /// of them was a literal and the other was computed, and nothing in the game compared them.
    ///
    /// ⚠️ REGISTRATION ORDER IS PRIORITY ORDER. The first element registered never moves; every
    /// later one yields to everything before it. That is deliberate rather than a rank field: the
    /// order things are built in `Hud.Build` already reflects how important they are, and a
    /// second numbering to keep in step is a second thing to get wrong.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class HudDeclutter : MonoBehaviour
    {
        /// <summary>
        /// Gap left between two elements that had to be separated.
        ///
        /// ⚠️ 6, NOT 0. Pushing until the rects merely stop intersecting leaves two cards sharing
        /// an edge, which reads as one wrongly drawn card rather than as two. This is the same
        /// spacing `TopCentre` uses between its own rows, so a separated pair looks like it was
        /// laid out on purpose.
        /// </summary>
        private const float Gap = 6.0f;

        /// <summary>
        /// ⚠️ HOW FAR THIS IS ALLOWED TO SHOVE SOMETHING, and it is a guard rather than a limit
        /// anybody should reach. An element that needs more than this to clear is not overlapping
        /// a neighbour, it is in the wrong place, and quietly walking it 400 units across the
        /// screen would hide that. Past the cap it stops and leaves the element where the design
        /// put it, so the fault is visible instead of being silently relocated.
        /// </summary>
        private const float MaxPush = 220.0f;

        private sealed class Slot
        {
            public RectTransform Rt;
            public Vector2 Base;
            public Graphic[] Graphics;
            public bool Down;
        }

        private readonly List<Slot> _slots = new List<Slot>();

        /// <summary>
        /// Track an element. Call in priority order, most important first.
        ///
        /// ⚠️⚠️ THE BASE POSITION IS CAPTURED ONCE, HERE, AND EVERY FRAME'S OFFSET IS APPLIED TO
        /// IT RATHER THAN TO WHEREVER THE ELEMENT ENDED UP LAST FRAME. Accumulating would make
        /// the push a ratchet: a card nudged down 20 units would be measured from its new home on
        /// the next pass, find itself still clear, and never come back. Recomputing from the base
        /// every time is also what makes this self-healing when the thing it was avoiding goes
        /// away.
        /// </summary>
        public void Track(RectTransform rt)
        {
            if (rt == null) return;

            foreach (var existing in _slots)
                if (existing.Rt == rt) return;

            // ⚠️⚠️ A LAYOUT GROUP ALREADY GUARANTEES ITS CHILDREN CANNOT STACK, SO THIS MUST
            // NOT TOUCH THEM. He asked for exactly this bound when the system was proposed:
            // *"well i feel like that might break something so make sure it dont break shit too /
            // and touch shit that dont have the capability to stack on each other already"*.
            //
            // A `HorizontalOrVerticalLayoutGroup` WRITES its children anchoredPosition every
            // rebuild. Pushing one from here would be overwritten on the next layout pass, and in
            // the frames where this wrote last the element would jitter between two positions.
            // Worse, the base captured below would be a value the layout owns, so the reset pass
            // would fight the parent for the rest of the match. Refusing them is not a
            // limitation: a child of a layout group is already safe by construction, which is
            // what makes it the preferred fix in the first place.
            if (rt.parent != null && rt.parent.GetComponent<LayoutGroup>() != null)
            {
                Debug.LogWarning(
                    $"[HudDeclutter] refusing {rt.name}: its parent is a layout group, which " +
                    "already prevents it stacking. Nothing to do, and moving it would fight " +
                    "the layout.");
                return;
            }

            // ⚠️ A CONTENT-SIZED ELEMENT IS STILL FINE. This only ever writes anchoredPosition
            // and never a size, so a `ContentSizeFitter` and this system do not contend.

            _slots.Add(new Slot
            {
                Rt = rt,
                Base = rt.anchoredPosition,
                Graphics = rt.GetComponentsInChildren<Graphic>(true),

                // ⚠️ WHICH WAY "OUT OF THE WAY" IS DEPENDS ON WHAT THE ELEMENT IS ANCHORED TO.
                // A top-anchored element is pushed DOWN, away from the top edge; a bottom-anchored
                // one is pushed UP. Always pushing down would walk the bottom-centre prompts off
                // the bottom of the screen, which is a worse bug than the overlap.
                Down = rt.anchorMin.y > 0.5f,
            });
        }

        /// <summary>
        /// ⚠️⚠️ `LateUpdate` AND `[DefaultExecutionOrder(200)]`, SO IT RUNS AFTER EVERYTHING THAT
        /// MOVES OR SIZES A HUD ELEMENT. uGUI rebuilds layout at the end of the frame and `Hud`
        /// itself writes card widths in its own Update; measuring before either would read last
        /// frame's rects and push against positions that no longer exist.
        /// </summary>
        private void LateUpdate()
        {
            if (_slots.Count < 2) return;

            // ⚠️⚠️ THE CHEAP EXIT COMES FIRST, BECAUSE THIS RUNS EVERY FRAME OF EVERY MATCH.
            // `CLAUDE.md` section 7.1 records a HUD string rebuilt per frame costing the 6x
            // behaviour probe an eighth of its frames and most of its physics steps. Almost every
            // frame has at most one of these elements up, so counting visible slots and returning
            // is the common path and it touches no transforms at all.
            int live = 0;
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].Rt != null && IsVisible(_slots[i])) live++;

            if (live < 2)
            {
                // Still restore anything left pushed by an earlier frame, or an element would
                // stay displaced after the thing it was avoiding disappeared.
                for (int i = 0; i < _slots.Count; i++)
                {
                    var s = _slots[i];
                    if (s.Rt != null && s.Rt.anchoredPosition != s.Base) s.Rt.anchoredPosition = s.Base;
                }
                return;
            }

            // ⚠️ EVERY SLOT IS RETURNED TO ITS BASE FIRST, IN A SEPARATE PASS. Measuring some
            // elements at their pushed position and others at their base within one pass makes
            // the result depend on iteration order, which is how a de-overlap system starts
            // oscillating between two arrangements on alternate frames.
            foreach (var slot in _slots)
            {
                if (slot.Rt == null) continue;
                if (slot.Rt.anchoredPosition != slot.Base) slot.Rt.anchoredPosition = slot.Base;
            }

            // ⚠️ NO `Canvas.ForceUpdateCanvases()` HERE, DELIBERATELY. It was in the first
            // version and it is a full canvas rebuild every frame, which is precisely the class
            // of per-frame HUD cost this project has already been bitten by. It is not needed:
            // writing `anchoredPosition` updates a RectTransform immediately, `GetWorldCorners`
            // reads the live values, and this system never changes a SIZE, so there is no
            // pending layout for it to flush.

            for (int i = 1; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Rt == null || !IsVisible(slot)) continue;

                float push = 0.0f;

                for (int j = 0; j < i; j++)
                {
                    var above = _slots[j];
                    if (above.Rt == null || !IsVisible(above)) continue;

                    // Measured with the push already applied, so an element clearing two
                    // neighbours in a row does not have to be moved twice.
                    Rect mine = WorldRect(slot.Rt, slot.Down ? -push : push);
                    Rect theirs = WorldRect(above.Rt, 0.0f);

                    float overlapX = Mathf.Min(mine.xMax, theirs.xMax) - Mathf.Max(mine.xMin, theirs.xMin);
                    float overlapY = Mathf.Min(mine.yMax, theirs.yMax) - Mathf.Max(mine.yMin, theirs.yMin);

                    // ⚠️ BOTH AXES HAVE TO INTERSECT. Two cards in opposite screen corners share
                    // a horizontal band and overlap on Y alone; shoving one of those apart would
                    // move a card that was never on top of anything.
                    if (overlapX <= 0.0f || overlapY <= 0.0f) continue;

                    push += overlapY + Gap;
                    if (push >= MaxPush) { push = MaxPush; break; }
                }

                if (push <= 0.0f) continue;

                slot.Rt.anchoredPosition = slot.Base + new Vector2(0.0f, slot.Down ? -push : push);
            }
        }

        /// <summary>
        /// ⚠️⚠️ AN ELEMENT HIDDEN WITH `Text.enabled = false` IS STILL AN ACTIVE GameObject, AND
        /// TREATING IT AS PRESENT IS THE FAILURE MODE THAT WOULD MAKE THIS SYSTEM WORSE THAN
        /// NOTHING. Most of this HUD hides that way rather than by `SetActive`: the toast, the
        /// lata alert, the vulnerable line and the ready prompt all do. A declutterer that
        /// reserved space for every one of them would permanently push the visible elements
        /// apart to avoid things nobody can see.
        /// </summary>
        private static bool IsVisible(Slot slot)
        {
            if (!slot.Rt.gameObject.activeInHierarchy) return false;
            if (slot.Graphics == null) return false;

            foreach (var g in slot.Graphics)
            {
                if (g == null || !g.enabled) continue;
                if (!g.gameObject.activeInHierarchy) continue;
                if (g.color.a <= 0.01f) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The element's world-space rect, optionally with a vertical offset applied first.
        ///
        /// ⚠️ WORLD CORNERS ARE CORRECT HERE AND WRONG IN A PROBE, which looks like a
        /// contradiction and is not. `docs/TODO.md` § 18.1b records a probe reporting 3,323,799
        /// units of overflow because it converted between two different canvases. This compares
        /// elements that are siblings under ONE canvas, so their world corners share a space and
        /// a scale, and no conversion happens at all. The offset is applied in the rect's own
        /// local space and transformed with it, so it survives any canvas scale.
        /// </summary>
        private static Rect WorldRect(RectTransform rt, float offsetY)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector3 shift = offsetY == 0.0f
                ? Vector3.zero
                : rt.TransformVector(new Vector3(0.0f, offsetY, 0.0f));

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 c = corners[i] + shift;
                minX = Mathf.Min(minX, c.x);
                minY = Mathf.Min(minY, c.y);
                maxX = Mathf.Max(maxX, c.x);
                maxY = Mathf.Max(maxY, c.y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}

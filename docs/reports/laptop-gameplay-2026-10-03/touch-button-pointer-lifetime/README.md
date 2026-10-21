# Touch button pointer lifetime

Corrected original8 reproduced two pointer-lifetime failures and passed six
controls. The first TouchButton candidate passed all eight cases. Main verified exact frozen bytes and restoration; the unit is ready for publication.

A second finger could press and lift from a button while another finger remained
down, and TouchButton unconditionally released the verb. The correction records
(pointerId, mouse-button) ownership and releases after the last owner lifts.
It synchronizes a global touch release or customization boundary before both
Down and Up, so stale captured pointers cannot restore a released action.
Direct SetHeld(false) and the unchanged OnDisable callback clear ownership.
Accepted mouse buttons, visuals, layout, stick, look area and backend are unchanged.

The installed Input System 1.20 UI module defaults to concurrent touch pointers
(SingleMouseOrPenButMultiTouchAndTrack), gives each active finger a separate pointer,
and dispatches each lift to its pressed object. The shipping UiInputModule assigns
those default actions and does not override pointer behavior. Tuple identity
preserves the existing accepted Left/Right/Middle policy because those mouse
buttons can share a pointerId; multi-button routing is source-reviewed only here.

| Cohort | Fixture | Native outcome | End UTC |
| --- | --- | --- | --- |
| First original8 | 82ff56f2… | Two intended causes, five controls, one fixture/native SendMessage assertion | 14:44:28 |
| Corrected original8 | 55308427… | Two intended causes, six controls | 14:48:29 |
| First candidate8 | 55308427… | Eight passing cases | 14:52:04 |

All three cohorts ended October 3, 2026 with guard terminal, preservation completed and lease free. The original cohorts exited2; candidate exited0. Main verified all 3,392 qualified file
hashes unchanged during each job. The logical source is
67d57b118eeedf65aa29449716616457d5ddf3ca over older worker Git base
8e7cfc7feb4eee614d456347ccbb26ad962d1714. Full qualified-source maps identify the
actual native bytes; the worker Git base alone is not the qualified source.
Local worktree d34cffb and fetched525ccd7 had the same original TouchControls
bytes. The two original cohorts use the original runtime; candidate overlays only TouchControls. Its complete map identifies the checked candidate bytes.

The first assertion stack points to Component.SendMessage at fixture line78 and
its native ShouldRunBehaviour assertion. The sole bounded fixture repair replaces
that invocation with reflection on the existing shipping OnDisable method.
Reversing this one line reproduces the complete previous fixture bytes; setup,
assertions and the other seven cases did not change. No production ExecuteAlways
change, assertion weakening or LogAssert suppression was used. First raw evidence
and the exact tested first fixture are retained, rather than relabeling that run
as a valid eight-case baseline.

The eight cases manually call public pointer handlers with supplied pointer IDs
and assert IsHeld plus the shipping TouchInput pressed state. Two failures cover
each order of lifting while the other captured finger remains down. Controls cover
single-pointer release, fresh pointers after all release, the disable callback,
direct SetHeld, customization followed by a fresh hold, and an old lift during
customization. The latter two protect global-release cleanup.

A prepared disabled Canvas and raycaster hold the existing TouchButton and
OwnerTouchSurface components. The existing painter binding/path is invoked;
Selected/pixels are not asserted. No mesh/render, raycast, input-module delivery
or physics step is invoked. The disable control directly invokes the managed
shipping callback method in EditMode. This qualifies handler/input lifetime and
callback-body behavior, not a naturally scheduled player transition, physical
phone, actual multi-touch delivery, rendered art, throw/lunge outcome or current
player build. Main alone runs native jobs; the agent made no worker/cache writes.

Byte references:

- Original TouchControls raw SHA256 `7eaeaf5b57ead03f4fa3ee4411e20d6e946b1fd53427faf465bf78be6d58a3e9`;
  LF `342d4d35d477059b860208e0874d3717e98f2b4d18bfcbd44bf9c58ed904d1cb`.
- Candidate raw `792addf59ce60d2e1629b11b0ad070862c3d8e8ff3f15f27332ed30d8b636e8e`;
  LF `5496095c8ff8aafa61ef0ae14ea0b2082cf9b12992cf31057e7b8c14e82e538c`.
- First fixture `82ff56f263091403dd58783d329b0bb255b89e41747603cece1dd6d5759b5177`;
  corrected fixture `55308427712a88572a2db060cb5cf81a625acb57060f7ee15e798dc7d19ffc09`;
  unchanged meta `d1a571cbdfe0970c264fdfe58b7e27f6cdfd881f57401c1d1611e33b1131f03d`.

Each cohort preserves raw XML, receipt and full source map unchanged. Report-local
attributes preserve their bytes in Git. The separate F6 fixture remains frozen
awaiting RAM; the held HopRestore API fixture is unqualified and excluded from
this unit's overlays, filters and publication.

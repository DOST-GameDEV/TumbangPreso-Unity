# Owner-painted preparation, rooms and custom settings

The preparation view, room browser, queue ticket, custom match settings and chat
now have new owner-art compositions. Prior builders remain in source as inactive
references; the existing map, rules, account, lobby and matchmaking services own
their behavior. This is a scoped implementation, not the finished whole UI.

Source field frames, action buttons, strokes and portraits retain their aspect
ratios. The first preparation render stretched a 16:9 map into1028x405; the
corrected surface is1028x578.25 and has a regression assertion. The two large
generic preparation panels were reduced to one map frame and individual source
fields. Current route selection uses green lettering and a source-art underline.
The map has loading feedback, and ranked hides its editing arrows.

Custom settings use stable native rows, a match/room split, actual working-copy
rule services, separate password input and client read-only viewing. Values
update without reconstructing the active input/row. Local rule changes refresh
preparation without restoring a stale bot tier. Reset cannot change a client's
rules; clients retain tabs and Close. Copy describes custom rooms accurately
rather than promising ranked credit for merely using default rules.

Preparation preserves map/mode/bots, loadout, profile, settings, seating,
spectating, room code/address copying, online switch, browser and readiness/start
routes. The new adapter explicitly restores QueueStake.Ranked and the queue-join
callback from the old installer. The nonmodal queue keeps loadout accessible.
Unused ranked seats read OPEN, not BOT. Existing matchmaking services still own
party/refusal checks and network outcomes.

Chat's actual field uses the supplied frame. Hidden chat remains enabled and
subscribed, while CanvasGroup prevents hidden input/raycast access. Conversation
history expands over its compact duplicate; the composer remains beneath it.
History owns its Escape through ScreenTakeover. No test sends a real chat message.

## Focused evidence

Fresh NUnit receipts and original Unity PNGs are in owner-ui-u3-evidence.

- Preparationv1:1/1, map/mode/picker/return path.
- Preparationv2:5/6, offline custom settings flow, ranked state and the three
  existing join/queue cases passed. The read-only fixture failed because a missing
  INetProvider defaults to offline host, despite SceneFlow.Networked being true.
- Preparationv3:3/3, explicit client-authority fixture, ranked state and actual
  local listening host with hidden chat/history/browser return. The fixture was
  corrected at the existing authority boundary; production permission was not relaxed.
- Chatv4:1/1, final history composition and local host flow after removing the
  duplicated compact transcript. Earlier queue-only receipt also passed1/1.

Profiles and shared Editor input preferences were restored by the guarded runs.
No Desktop player was updated. Actual ranked rendering used the test profile's
signed-in UNRANKED branch; the conditional guest assertion did not prove a new
external sign-in session. The read-only fixture proves the UI authority boundary,
not delivery from a separate client process. Relay/service availability, complete
multi-peer flow, controller/touch navigation and in-match chat still need the
later integrated U8/U6 checks. No transport/protocol change was needed.

## Critique and remaining work

Font roles/colors and supplied action shapes now agree across these screens.
Map readability, selection feedback and custom-row clarity improved visibly.
History no longer repeats the same message above another copy of itself.
The invented large paper frames are still more geometric than the artist's
brush edges. They need deliberate whole-interface art refinement in U8; this
report does not certify them as final artistic quality. The orange pattern is
busy around dense pages, so quiet reading areas must remain. Compact portraits
are functional but could carry more character as U4 establishes the picker.
Preserve the existing bindings/services while finishing U4-U8, then resume the
gameplay queue in GAMEPLAY_RESUME_AFTER_UI.md.

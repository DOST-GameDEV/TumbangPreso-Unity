# Tutorial comment refinements

Continue the existing Feedback report QA_TUMP_0044, preserving its earlier notes.
Harry's new Human notes/comments ask for2.5-second reading, no star highlight on
the slipper/can, no completed-route progress bar and active attacker AIs at the end.

- The continuous reading threshold is already2.5seconds in100d8725. Its actual
  Tab-input qualification in [tutorial-flow](tutorial-flow.md) is unchanged and
  reused. Completion still has the existing0.70-second green feedback beat; it is
  not a5-second reading requirement. No new timing implementation was needed.
- Remove tutorial objective-star/glow bindings from retrieval, can-hit/retrieval
  and can-reset lessons. Ordinary world highlights stay intact. Person-target
  markers still work and cannot leak onto the next object lesson.
- Hide both completed-route progress displays. Quit remains at the left and the
  finished card retains its existing completion message.
- Keep both practice attackers in place, return their own slippers and enable
  their existing AI controllers at completion. Stop overwriting them with the
  earlier scripted roaming. The defender stays friendly/reset-only; the student
  keeps full controls. Reentering a lesson disables free-play AI again.

## Focused qualification

Unity6000.5.8f1 LinuxOpenGL/llvmpipe, named isolated profiles, guarded graphics
runs with mip2 texture residency. Two incremental native cases pass:

- Object-marker transitions:1/1 in4.72seconds, exit0. Enter each object lesson
  after a marked person lesson, verify no active tutorial marker renderers, then
  return to a person lesson. Retrieval/reset captures inspected.
- Completed free play:1/1 in7.50seconds, exit0. Both same attacker identities and
  positions remain; both get their slippers and perform an actual AI-driven throw
  without forced throw calls. Completion progress is hidden, student can act,
  defender AI stays disabled, and returning to a lesson restores progress/stops AI.

Three960x540captures inspected. No new OOM, memory guard stop or tooling retry.
The known cloud character-material artifact remains; these claims do not approve
character visuals, certify physical devices, or constitute a new player build.
[Raw XML, receipts, hashes and captures](tutorial-comment-checks/).

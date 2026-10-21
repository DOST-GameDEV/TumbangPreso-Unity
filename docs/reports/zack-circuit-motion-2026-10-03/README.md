# Closed Circuit acquisition presentation

Zack's defending action previously requested `cast`, which had no body action
chain, and reused the generic first-person thrust. It now requests its own
`hero-zack-circuit` body clip and `closed-circuit` owner gesture. The 0.64-second
aiming motion is serialized into the shipping roster, not generated only in Editor.
All 36 previous GLB clips, original binary data and geometry are preserved.

Native review also exposed the existing targeting line starting near the face.
The acquisition tell now follows the measured left palm, choosing the owner's
visible hand when available. Hand bindings are resolved once per acquisition;
no per-frame scene search, new collider, target selection or gameplay effect.

The six-metre range, 0.4-second lock, two-second Zapped, 35-second cooldown,
Overclock follow-up, movement freedom and protocol141 are unchanged. No names,
descriptions, model features, other clips or sound were redesigned.

## Evidence

- Unity6000.5.8f1 Linux isolated validation checkout with explicit frozen overlays.
- EditMode3/3: dedicated action names, actual owner dispatcher selection, serialized
  clip reference, sampled arm motion and neutral recovery.0.1708536s.
- Final PlayMode9/9: eight existing aim/LOS/authority/recovery/tell controls plus
  the accepted authoritative cast through the real confirmation bridge. It observes
  registered body playback and real arm motion, correct acquisition contact,
  ordinary movement permission, recovery and the moving-palm line attachment.
  Final hand candidate1.04603s; exit0/no resource stop. Peak tree3,359,748,096 and
  container7,276,101,632bytes. Both private EditorSettings and QualitySettings
  restore, named profile restores, all13 final frozen inputs match main/native.
- Native ClipMotionStrip resolves the action's own serialized clip (37 total),
  renders six times from side and quarter views with the real game palette/shader,
  and measures241 trace steps. Ground minimum0.000m; final bone positions equal
  idle frame0; peak bone speed2.48m/s on arm-left at0.176s. Both strips inspected:
  short opposing preparation, directed hand, settled hold and quiet recovery.
  Import reports looping, but the actual one-shot player overrides playback and
  the runtime recovery assertion passes.

## Retained failures and limits

Compilation repeatedly completed/reloaded before late-import memory guards.
Separate warm runtime stages were used, not repeated compilation retries.
The first playback fixture omitted ApplyModel and failed before action playback
(eight controls passed); the fixture was repaired without production changes.
An idle owned compiler occupied477,761,536bytes and was safely retired before
the bounded first successful playback retry. No user's computer was used.

LLVMpipe graphical PlayMode stopped at the cgroup headroom guard before images.
A distinct task-only Mesa softpipe experiment captured46 body frames, including
acquisition and recovery, but stopped before a completed test result. Those frames
revealed the old face-origin line; they are diagnostic, not a graphical test pass.
The final native pose renderer produced both complete strips and RESULT:PASS,
then the run still recorded cgroup-headroom despite exit0. Peak tree4,460,601,344 /
container8,367,435,776bytes. This is usable inspected native pose evidence, not a
guard-free render/performance result. Settings/profile restoration remains verified.

No new full-court film, final live owner-view capture, actual peers, packaged player,
listening or human approval is claimed. The accepted-cast stage checks body and
hand attachment; the owner gesture has dispatcher coverage, with visual owner
review still outstanding. The whole Zack presentation is not marked complete.

Failures, XML, frozen hashes and resource receipts are retained under evidence.
The native-motion text and PNGs are the final authored clip review.

## Owner-hand follow-through

The next focused stage instantiates the actual CameraRig and ViewmodelArms,
follows the caster, and confirms the accepted action reaches `closed-circuit`,
rotates the visible left hand by more than10degrees and attaches the tell to that
visible palm. The body-only control and eight authority/timing cases remain.
All10 PlayMode cases pass in1.245365s; exit0/no guard, peak tree3,349,143,552 and
container7,336,460,288bytes. Settings/profile and all13 frozen inputs verify.

Source follow-through found that the shared IsFirstPersonFor helper performs a
scene search. The tell now checks its cached arm's active state and bound character
directly each frame, retaining the one acquisition-time lookup. No visual keys,
model, mechanics or protocol changed. This removes that search; no frame-time
improvement is claimed without a performance measurement.

The first owner fixture omitted CameraRig.Follow and failed before creating arms
(nine other cases passed). One fixture correction connected the real camera.
Compile/import memory stops and the original fixture failure remain in evidence.
This is actual owner-transform/dispatch evidence, not an owner-view pixel or film
verdict. Those visual/player/peer/audio/human limits above remain open.

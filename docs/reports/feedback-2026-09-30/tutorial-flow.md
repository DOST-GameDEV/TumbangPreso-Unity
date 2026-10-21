# Continuous tutorial flow and final practice

Latest additions to the existing refinement row are implemented in the same
training route. Earlier ed60d6e5 evidence retains the1.5-second Look,7.5-metre
Move/Run targets, authored body copy and left-aligned completion Quit.

## Changes

- Attacker placement uses the centre mark. Ordinary attacker lesson transitions
  retain the student's position and facing; Throw/Shove and necessary initial/
  role-change setup place the student. Completion does not teleport the student.
- Retrieve preserves a real slipper already in flight instead of inventing a
  loose snapshot on entering the lesson. A skipped throw still stages a shoe.
- Curve Throw accepts the curved throw alone. Straight throws still fail the
  objective. Throw and Lunge use the exact new HOLD THEN RELEASE captions.
- Training hides the pictured MatchEventFeed along with the earlier callouts.
- Description reading requires2.5continuous seconds. Accepted signature/role casts
  wait1second; rejected casts still cannot finish. Ultimate timing is unchanged.
- Completion retains the same two roaming attackers, without moving them, and
  brings back the already assigned defender to reset the can. Skipped routes also
  prepare both partners. The reset actor cannot steal an attacker's role.
- The actual emote-wheel release is routed to the training student. Practice actors
  have intentionally disabled AI, so the ordinary AI-disabled-seat heuristic was
  choosing a dummy instead. The guard applies only to guided training.

## Reproductions and checks

The first native baseline expected an airborne shoe to remain airborne across the
Throw/Retrieve handoff; old code immediately changed it to Loose. Final flow
checks preserve the actual trajectory and measure road-relative rest height after
landing. This qualifies the reproduced training handoff defect, not every possible
slipper ground/contact report in other maps or modes.

The second baseline uses actual T press, mouse selection and T release on the
completed practice ground. The wheel chooses an emote but the student's Current
emote remains null. The guarded routing passes the same real wheel check and
confirms other practice actors do not emote instead.

Four initial flow cases pass for positions, hidden event feed, real curved throw,
lunge, emote lesson and real landing, plus the read/cast path at that stage.
The latest1-second cast/2.5-second read check then passes. One new companion check
caught conversion of a retained attacker into the defender; selecting the existing
defender corrects that regression. Final companion/reset/wheel results are retained
alongside the earlier raw failed runs. Final verification: seven distinct focused cases pass across the retained runs.
The final three-case companion/wheel/reset pass exits0 in37.22seconds.

Unity6000.5.8f1 Linux OpenGL, isolated named profiles and explicit overlays, restored
texture mip limit2. The visible Throw/Lunge captions and grounded-shoe capture
were inspected. No protected Paete/Phaister behavior, art or descriptions changed.
No new player build, actual-peer tutorial qualification or human approval is
claimed. Existing cloud character/slipper material artifacts remain visible and
are not disguised by these changes.

The cold final companion run was stopped by the private memory guard during
import: the Editor, an asset-import worker and ILPP overlapped. Unity faulted
while shutting down; no fresh test result is claimed for that run. One unchanged
warmed retry passes3/3. Kernel OOM/kill counts remain10/5 throughout. No
unrelated process was stopped and no global resource limits were changed.
The final practice capture was inspected: two attacker partners and the separate
reset defender remain present, with the compact left-aligned Quit footer.

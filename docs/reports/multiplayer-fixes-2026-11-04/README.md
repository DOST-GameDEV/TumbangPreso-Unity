# Multiplayer and presentation fixes

Work started November 4, 2026 and continued November 5. Unity 6000.5.8f1,
Windows, Direct3D 11. Main incorporates the owner ability-icon update 4769d9ba7.
This report records evidence; current scope belongs to TODO and the checkpoint.

## Changes

- Banaue floating: the caster and stage use support immediately under the feet,
  rather than a six-metre overhead scan with a one-metre acceptance gap. Dense
  support queries retain the highest valid walkable hit. Gameplay positions stay
  authoritative; this correction is visual.
- Banaue HUD after round swaps: a client snapshot does not raise gameplay
  RoundStarted. HalftimePresentation now explicitly retires its matching card
  by match and completed round. Cleanup releases the card/clock even on failure.
- Phaister doll movement: VoodooBodySnapshot accepts bounded body subjects 0-7,
  while curse producers remain players. Its former four-player subject check
  discarded the entire doll pose packet on clients.
- Doll catch replay: client flair accepts body seats, MatchPoseHistory records
  companions, and CatchReconstruction resolves/hides/copies all bodies. Score and
  input ownership remain four-player contracts. Replay means the victim's instant
  tag reconstruction, as clarified by the owner.
- Yasmin/Cheska: live cinematic playback starts at authored 9.18 s, just after
  the heart; retained duration is 2.72 s. Body, effects, shot, grade, post-process
  and handback use the retained clock. Authored tables/full clips are preserved.
  Shared timing requires protocol 189 on host and clients.
- Opening skip: one small fast-forward control and numeric vote count below it.
  The yellow WAITING pill is gone; the old local ring/hold path is suppressed
  while the shared match opening owns the vote. Existing input bindings remain.
- Throw landing hint: its ring projects onto actual nearby support, follows steps
  and slopes, avoids floating ledge bridges, uses pixel-derived stroke width,
  and has depth bias against floor flicker. Ballistic prediction is unchanged.
- Ultimate occlusion: all live bodies, ghosts, particles, callouts, runtime VFX
  and persistent skill visuals are hidden only for the ultimate camera draw.
  The authored stage is excluded. Hidden skill colliders do not redirect its
  camera. Exact renderer enabled/force-off and light flags restore in finally
  and disposal; colliders, effects, statuses and lifetimes stay active.
- Additional reproduced client error from the owner's log: a blindfold frame
  could precede its victim's new movement epoch and throw in AheadOf. It now
  hides while awaiting that exact binding, preserves the host lifetime, and
  draws again when the matching epoch arrives.
- Grounding diagnostics additionally record companions separately from the
  unchanged four-player actor list, allowing actual host/client doll motion QA.

## Native evidence

Evidence root: Logs/multiplayer-fixes-20261104/. Failed attempts are retained.
Current passing evidence is selected below; older failing XML is not current status.

- terrain-state-after.xml: 11/11 EditMode, including body codec, invalid subject
  bounds, local/dense support, stepped/sloped marker projection, timing and protocol.
- focused-play-before-card-fix.xml: the opening/skip native case passed after
  its capture/canvas lookup repair. That mixed run had three other failures,
  superseded by the passing evidence below. The one-control image was inspected.
- focused-play-after.xml: 8 passing native cases: actual doll tag replay,
  raised-ground real flight comparison, actual local marker show/hide, full client
  doll pose receipt/interpolation, client doll flair, Banaue next-round HUD,
  retained Yasmin performance/effect on Banaue, and live-effect render isolation.
- mixed-cohort-current-kits.xml: 1/1 native. Four current kits, one deadline,
  two successive cohorts, resource spending and no accumulated state.
- blindfold-epoch-after.xml: 2 relevant native cases passed: delayed-epoch
  suppression/rebinding, and owner/observer/spectator hydration without recasting.
- render-isolation-current-state.xml: 1/1 latest native render isolation,
  including immediate pre-render flag restoration and interrupted-scope disposal.

Twelve distinct exact PlayMode cases passed across these runs. No broad PlayMode
suite was run. Sources compiled in the actual Editor. Pictures under final/
were inspected; absolute-zero live freeze and the post-cinematic Banaue HUD passed.

The before cases reproduce companion pose rejection, lost companion flair/history,
old round-card suppression, live-effect occlusion, and the blindfold binding crash.
The repaired old fixtures now include SearedLeft in their packet, assert Yasmin's
real instant freeze instead of an active timer, and stage only currently aimed
ultimates instead of assuming the retired aimed OMEN behavior.

## Release checkpoint

No Unity job is running. Implementation and the targeted checks above are done.
Current agent is preparing a Windows candidate and actual local host/client checks,
then final publication/build verification. No requested implementation is delegated
to Claude. User is asleep; do not ask more questions or invoke paid services/agents.
Leave canonical checkout on main so the desktop latest-build helper remains usable.
The existing trailer/capture-harness branch is preserved.

Original ProjectSettings bytes are retained at source-before/ under the evidence
root until final cleanup. Native guards preserve the named test profile and shared
Editor input preferences. Restore task-owned settings differences before release.
A later result section will record native-player receipts and final publication.

Physical separate-PC/WAN/mobile play and every possible skill combination are
separate claims from this bounded Windows evidence. Final Claude handoff is for
review/refinement of completed work and should cite the final receipts.

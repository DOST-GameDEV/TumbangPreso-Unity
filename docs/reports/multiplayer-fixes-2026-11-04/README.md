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

## Built-player and integration evidence

Windows candidate f341ea52b built and launched through the canonical helper:
Logs/windows-build-20261105-005222/job.json. Exit 0, protocol 189, source/settings
preserved, responsive player window. The interim player was closed gracefully
before final packaging. Original and subsequent owner icon updates are preserved;
d82d2c150 was merged normally, and doll-tag-new-owner-icons.xml passed afterward.

Both actual local host/client runs used 80 ms one-way UDP delay, 10 ms jitter
and 1% packet loss, separate task profiles and matching immutable player artifacts.

- native-phaister/companion-motion-review.json: passed. Host/client doll horizontal
  spans 11.07/9.21 m; 102 matched samples; median gap 0.063 m, p95 0.319 m.
  No runtime exceptions, floating suspects, input changes or artifact changes.
  The original generic tool exit 1 expected staged victim copies that Phaister
  does not use. Its corrected criterion requires actual companion motion and
  bounded position agreement. Five unit tests accept delayed matching motion
  and refuse the original frozen-client bug, absent/wrong dolls and divergence.
  The original run/result remains preserved; no failed product assertion was removed.
- native-banaue-yasmin/acceptance-review.json and result.json: passed. Both players
  exited 0, no runtime exceptions or floating suspects, three matching staged
  victims at 0 m start gap, with a 4.2 m terrace victim. Both entered real play
  and the shared ultimate. Shared input and the packaged artifact stayed unchanged.

Final published source and launch are owned by the current windows-build receipt;
verify its launched status, SHA and protocol against main. The runtime code fixes
are in f341ea52b, with the later owner icon/feed integration tested separately.
Failed attempts and the source-before settings snapshot remain evidence until
final restoration/cleanup is verified. User profiles and authored source are preserved.

Physical separate-PC/WAN/mobile play and every possible skill combination remain
separate claims from this bounded Windows evidence. The final Claude handoff is
for review/refinement of completed fixes, with no required implementation left.

## First retained camera follow-up

Paired-player image review exposed one overlap: playback starts at 9.18 s, while
the overhead heart shot ends at 9.2 s. That partially retained shot could appear
on frame zero or be borrowed when another shot was blocked. The new predicate
keeps only whole shots beginning at/after the trim, and LastShot now reports the
actual selected fallback camera. Body timing, shared duration and protocol do
not change. first-retained-shot-before.xml reproduces camera 4 where 5+ is required.
first-retained-shot-after.xml passed 2/2. The actual frame-zero camera is now a
complete post-heart shot, with effect isolation/restoration and the Banaue real
freeze/footing/HUD assertions retained. Shared simulation timing and protocol stay
unchanged. The final delivery receipt must include this camera follow-up.

# Amihan feel pass: cutscene, skills and her light body

Owner, October 2 (this session): finish the Airburst checkpoint, *"thoroughly direct
the improvements and revisions in the cutscnes and skills of amihan"*, *"thoroughly
refine animations"*, then light footsteps and a floating run, with every verb
considered ([light-body.md](light-body.md)). Amihan only; no mechanics, protocol or
other heroes.

## Critique of the F3 films (normal speed and body only first)

Frames were read from the retained F3 runs (`Codex/work/tump-amihan1002`), the
committed clips, and offline witness sheets of the real glb
(`tools/preview_amihan_clips.py`, `author_ultimate_intros.py --preview`).

| # | Where | What the film shows | Change |
|---|---|---|---|
| 1 | Live fan, court view | Mint edges and chevrons nearly vanish on the light plaza tiles; the meter is unreadable before the release | Floor strokes ink-weighted in her darker greens (dark rim round a mint line), edges 0.16 to 0.26 m and chevrons 0.16 to 0.24 m, meter starts at 0.5 instead of 0.34 |
| 2 | Live fan, contact honesty | A thicker edge centred on the limit would draw half of itself outside the fan | Edges set half their width inside, so the outer ink IS the 30 degree contact line; chevron arms stop shorter of them |
| 3 | Release, court view | The kasikus sigil grew to 6 m diamonds behind her, outside the fan: a false area | Sigil kept under her feet, at most about 1.3 m |
| 4 | Release, body only | Torso pitched 26 degrees, so her large head dived over both arms; from behind and above it read as a bow | Upright lunge step, chin up, both palms driven out wide at chest height, hands part on the follow-through (release still on 1.5 s) |
| 5 | Cutscene CALL | At 45 degrees her short arm never left the coat | Flung out to 72 degrees, still under the shoulder so the held shoe clears the head; the open arms widen too |
| 6 | Cutscene GATHER | The close-up cut her head off at eye level | Look at her chest, slightly wider lens: cupped hands low, whole face above |
| 7 | Cutscene AIM | First frames were a wall of the back of her head | Opens further out and higher; the lane still fills the frame |
| 8 | Cutscene stage | Calle Crisologo houses filmed as black slabs against a bright sky | Lime plaster, narra timber, clay roofs and glowing capiz, one step darker than her |
| 9 | Drift (witness sheets) | Same 24 degree dive: her face left the frame for the whole carry, arms hidden from behind | A short flight: upright launch, chin up, lead hand reaching, slipper hand swept back, both legs trailing, then a catching foot |

Kept: Whirlwind (both arms sweep across at shoulder height and read from the side),
Featherfall (arms out to balance on the lift), the first-person Airburst hands (they
frame the centre target rather than cover it during the windup), the 3.6 s length,
the 1.5 s delay, the 60 degree fan and every contact rule.

## Native validation plan (recorded before the run)

Candidate: new detached worktree `Codex/work/tump-amihan1002f` at `6bae3bc45`, its
Library seeded from the idle `tump-amihan1002`, plus the frozen overlay in
`Logs/amihan-feel1002/overlay-inputs.json` (19 files). Profile `amihan-feel1002`,
through `tools/run_unity_job.py` (serial, one heavy job). Graphics on for PlayMode.

| Launch | Question | Expected | Stop condition | Retries |
|---|---|---|---|---|
| B: bake | Does the dash and storm bake rewrite exactly those two clips with their GUIDs? | Two `.anim` changed, metas identical, three other clips untouched | Any other asset changes | 0 |
| E: EditMode | Do the shipped storm clip and first-person path still release on 1.5 s; does her new gait calibrate; does `step_amihan` have a file and a caller? | `AmihanAirburstPresentationTests` 4/4, `MotionContinuityTests` gait cadence pass, `DeadFeatureAudit` live cues pass | Product failure is fixed, then rerun once | 1 tooling |
| P: PlayMode | Films and checks: light body, Airburst fx/body/Low, round-boundary cleanup, intro held-shoe audit, her three skills in a match | All selected cases pass with fresh XML; frames inspected | Product failure is fixed, then rerun once | 1 tooling |

Evidence limits stated up front: fixed-simulation films are not real-time
performance; one PC, no actual peers; assistant review is not human approval; the new
step sound is measured, not heard.

## Results

Candidate `Codex/work/tump-amihan1002f` at `6bae3bc45` plus the frozen overlay
([inputs](evidence/feel-overlay-inputs.json), revisions r2 and r3 recorded in it),
profile `amihan-feel1002`, every launch through `run_unity_job.py` (serial; admitted
with 4.6 to 4.7 GB free). [Receipts](evidence/feel-job-receipts.txt).

| Run | Result |
|---|---|
| B1 bake | Stopped by this session before admission (a 4000 + 2048 MB claim against about 4.7 GB free); no Unity started |
| B2 bake | Exit 0. Only `hero-amihan-dash.anim` and `hero-amihan-storm.anim` changed; metas and the other three clips untouched |
| E1 EditMode | 16/17. **Product failure:** the release's full drive landed at 1.533 s. The follow-through swung the arm's pitch back far enough that the hold's end tangent bent the held contact 1.44 degrees past its key, so the true deepest pose fell after the release. [XML](evidence/feel-e1-editmode-failed.xml). Gait cadence, continuity and the live cue audit (`step_amihan` has a file and a caller) passed |
| r2 fix, B3, E2 | Follow-through pitch kept near the drive (modelled dip 0.67 degrees). E2 4/4 |
| P1 PlayMode | 2/7. **Product failure:** in all three Airburst films the palm's forward peak was the handback (0.336 m), not the release. Measured on the glb, the wide release had thrown the palms out to the SIDES (right palm 0.21 m forward, left behind her): a wings-open shape that no longer pointed down the lane. **Fixture timing:** the light-body film charged its throw after two seconds of sprinting had emptied her stamina, so she was already trudging grounded. **Stale staging, not changed:** `FilmHerSkillsInAMatch` drifts her twice from 11.5 m out to about 1.5 m from the can while she holds a slipper, where Featherfall is refused by the retrieval rule; Drift itself passed and was filmed. Round-boundary cleanup and the introduction held-shoe audit passed. [XML](evidence/feel-p1-play-failed.xml) |
| r3 fix, B4, E3 | Release H: upright lunge, palms forward at chest height just outside the torso (0.49 and 0.42 m), hands opening wide only in the follow-through. Light-body fixture re-timed to two short sprints (its one fixture repair). E3 4/4 |
| P2 PlayMode | **4/4.** Airburst fx, body and Low: handback 139, release 184 (1.50 s), victim Whirled 184, palm peak 186 (0.486 m), outsider untouched, zero logged errors, cutscene clock frozen. Light body: standing 0.000, walking 0.020, sprinting 0.121 to 0.189 m, never above 0.190, jump carries 0.162, a throw charged from 0.171 m settles over 0.27 s, five `step_amihan` and no rubber steps. [XML](evidence/feel-p2-play.xml), [lift](evidence/feel-p2-light-body-lift.csv) |

Frames inspected from P1 (visuals valid there) and P2: the fan strokes now read as
dark green lines on Bayan Plaza's light tiles through the whole windup; after the
release no diamonds appear behind her; the call arm leaves her coat; GATHER keeps her
whole face above the glowing cupped hands; the Vigan houses read as plaster and timber
with lit windows; the AIM shot still opens on her hair lower left but yields to the
lane sooner. From behind and above, the palms are hidden by her head (measured, not
fixable with her arm length), so the lunge and the turn carry that view.

The baked clips match the candidate byte for byte; Drift re-baked identically twice.

## Still open

- Human taste on every item above, and listening to `step_amihan` in the game mix.
- Actual peers, real-time frame rate, other maps, refusal, disconnect and role changes.
- `FilmHerSkillsInAMatch` staging versus the retrieval rule (Featherfall from inside
  the box with a slipper), and baseline films of Featherfall and Whirlwind floor anchors.
- Her own tag reach and throw shapes, and the shared `land` thump on hard landings.
- Her name tag sits about at her head top (capsule 1.6 m plus 0.25 m against a 1.87 m
  body); check it against the float in a player view before changing shared code.

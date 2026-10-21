# Production plan

Owner authorization (October 2, local session): Amihan's direction, body and
first-person motion, effects and ultimate cutscene. No SFX creation,
replacement, mixing or modification anywhere. No gameplay, balance, networking,
reliability or loading changes. One coherent action at a time.

## Order of units

1. **Airburst** (this unit): live timing honesty, readable gather and release,
   and a cutscene that hands back into the real windup. Chosen first because it
   has concrete, verified defects (critique 1 to 4) and is the owner's named
   reference.
2. Drift, Featherfall and Whirlwind follow as separate claimed units, each with
   its own baseline. Nothing in them is touched by this unit.

## Owned paths for the Airburst unit

| Path | Change |
|---|---|
| `Assets/TumbangPreso/Runtime/Visual/HeroAbilityClips.Amihan.cs` | `BuildAmihanStorm` only: 1.5 s release |
| `Assets/TumbangPreso/Art/characters/amihan-motion/hero-amihan-storm.anim` | Re-baked in place; GUID `fc1f7e39...` kept so `person_amihan.asset` still ships it |
| `Assets/TumbangPreso/Editor/AmihanMotionAuthor.cs` | A storm-only bake entry, so the other four baked clips are not rewritten |
| `Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.cs` | `StormCallClip` only |
| `Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.CastGesture.cs` | `storm-call` entry only |
| `Assets/TumbangPreso/Runtime/Visual/AmihanVfx.cs` | `AmihanStormFan` only |
| `Assets/TumbangPreso/Runtime/Visual/HeroIntroductionScene.Amihan.cs` | Amihan's stage and effects |
| `tools/author_ultimate_intros.py` | `amihan()` only |
| `Assets/TumbangPreso/Resources/UltimateIntros/amihan.txt` | Regenerated from `amihan()` alone; other heroes' files untouched |
| `Assets/TumbangPreso/Tests/PlayMode/AmihanKitPlayProbe.AirburstFilm.cs` (new) | Evidence films and the windup interruption case |
| `Assets/TumbangPreso/Tests/AmihanAirburstPresentationTests.cs` (new) | EditMode guard: the shipped clip's release matches the rule |
| `docs/reports/amihan-presentation-2026-10-02/` | Research, plan, evidence |
| `docs/TODO.md`, `docs/ACTIVE_REWORK_LEDGER.md`, `docs/README.md` | Own entries only |

Not touched: `AmihanHazards.cs`, `AmihanHeroKit.cs`, `AmihanRules.cs`, the
shared phase, `HeroIntroductionScene.cs`, `UltimatePhaseView.cs`, audio cues,
other heroes, Paete and Phaister.

## Implementation steps

1. Author the live clip (`BuildAmihanStorm`) to the beat sheet; add the storm-only
   bake entry.
2. Retime `storm-call` (path and arm clip) to the same beats.
3. Rebuild `AmihanStormFan`: edges, six kasikus chevrons stepping inward on the
   beats, a draw-on segment at negative age for the cutscene, a simultaneous
   release, a fast standing chevron front inside the 30 degree edge, sigil and
   cotton, everything thinning by gather + 1.0 s. No new component types.
4. Rewrite `amihan()` (keys, shots, still, zero lift) and the scene partial:
   keep the Vigan stage, sky and kasikus; add inward threads, the cotton boll
   and the scene-owned fan (driven by `StepTo`, its `Update` disabled); remove
   the vortex, storm eye, lens streaks and release wall.
5. Preview the cutscene body offline (`--preview amihan`, with and without the
   stage sketch) and correct poses before any Unity run.
6. Add the EditMode guard and the film fixture.

## Validation (isolated, one heavy job at a time)

Candidate checkout: a detached worktree `C:/Users/matth/Documents/Codex/work/tump-amihan1002`,
created after the other session's active Hero reconnect job ends, seeded from an
existing Library if IO allows, named profile `amihan-airburst1002`, outputs under
its own `Logs/amihan-airburst1002/`. Inputs frozen per run by commit and hashes.

| Run | Question | Expected | Stop condition | Retries |
|---|---|---|---|---|
| R0 import | Does the candidate compile and import? | Unity compiles; no new errors | Compile errors are product failures to fix | 1 tooling retry |
| R1 baseline films | What does shipped Airburst look like (effects on, body only)? | Fresh frames; release lag visible; nonzero cases | Fixture fails for tooling reasons | 1 bounded fixture repair |
| R2 bake | Does the storm-only bake rewrite exactly one asset with its GUID? | One .anim changed, meta unchanged | Any other asset changes | 0 |
| R3 final | Films (effects on, body only, Low plus reduced effects), windup interruption, EditMode guard | All cases pass; frames inspected; release on beat | Product failure is fixed then rerun once | 1 |

Film views: her screen (cutscene overlay included), the court, an observer
standing outside the fan, and a victim inside it. Thirty frames per game second
with the film clock, so cutscene and windup run at true game speed. Clips are
stitched to MP4 with lead-in, the whole cutscene, windup, release, recovery and
about 2 s of aftermath, then a labelled slow-motion pass of the windup and
release. BEFORE and AFTER use the same fixture and staging.

## Evidence limits stated up front

Fixed-simulation films are not real-time performance. Native tests on one PC
are not actual peers. Assistant review is not human approval. No audio is
evaluated.

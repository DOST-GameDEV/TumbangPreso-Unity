# Nationals qualification report

- **Commit** `64718d3bbe4ec0a3cbe91880a271421c05274a8e`
- **Branch** `main`
- **Generated** 2026-09-04T22:16:13
- **Build target for every validation launch** `Win64`
- **Gate** standard pass
- ⚠️ **Working tree was DIRTY at report time**, 2 paths. A qualification is a claim about a commit; uncommitted edits mean the results describe something not in the history.

| Stage | Verdict | Detail |
|---|---|---|
| Core.Tests (engine-free rules) | PASS | 532 passed of 532. |
| Unity EditMode | PASS | total 376, passed 376, failed 0, skipped 0. 376 passed of 376. |
| PlayMode, isolated groups, pass 1 of 2 | **FAIL** | total 175, passed 155, failed 11, skipped 9. 155 passed of 175 across 6 isolated groups, 63 of 70 fixtures. 11 failed. |
| Checks.RunAll (every editor check, one launch) | PASS | all checks passed in one launch |
| Source audits | PASS | all audits clean |
| Release artifact identity | PASS | identity consistent |

## Verdict: NOT QUALIFIED

A stage above is red or missing. **A green subset is not a release certification**: the full PlayMode suite has been 42 red and then 56 red on commits where every targeted run anybody had bothered with was green.

## Identity

- `NetSession.ProtocolVersion` = **23** (read from source, never from a document)
- Application version = **1.0.0**
- UGS project `dcf0831e-a5f4-43b4-832e-b687f13a3569`, organization `matthewtlabrador`
- Windows artifact `C:\Users\Matthew\Desktop\TumbangPreso-Unity\TumbangPreso.exe` written 2026-09-04T21:41:05, SHA `64718d3bbe4e`, protocol `23`
- Android artifact: **none on this machine**

### PlayMode, isolated groups, pass 1 of 2: 11 failing

- `[destroyer] TumbangPreso.PlayTests.InputSurfaceProbe.EveryScreenHasAFocusPathAndReachableTouchTargets`  screens a controller cannot walk, or where a press lands on the wrong control: Eskinita/ResultCanvas @ 16:9 720p (1280x720): a press at the centre of 'HUD/HudCanvas/ResultCanvas/Card/Button_NEXT MAP' lands on 'TouchControlsCanvas/LookArea' instead. Eskinita/ResultCanvas @ 16:9 720p (1280x720): a pre
- `[destroyer] TumbangPreso.PlayTests.InputSurfaceProbe.PhotographTheThumbLayerOverTheRealStreet`  UnityEngine.MissingReferenceException : The object of type 'UnityEngine.Camera' has been destroyed but you are still trying to access it. Your script should either check if it is null or you should not destroy the object.
- `[screens] TumbangPreso.PlayTests.AspectRatioProbes.TheCharacterScreenSurvivesEveryAspectRatio`  1 label(s) on the character screen are authored below MenuKit.MinReadableUnits (18). Do NOT lower the floor to make this green: docs/TODO.md § 126.13. Widen the box, cut the words, or register the exemption through MenuKit.Fit so it is counted rather than hidden: 'DoorCaption' ("build a character") 
- `[screens] TumbangPreso.PlayTests.CustomGameScreenProbe.EveryRowFitsItsBoxAtEveryShippedResolution`  16:9 720p custom game: 'Label' reading "No bots · open to anybody with the code" needs 306 px and was given 16. Expected: less than or equal to 17.0f But was: 306.0f
- `[screens] TumbangPreso.PlayTests.PaperPurityProbe.NothingOnTheInventoryDisappeared`  these front-end controls were on the screens before this pass and are not on them now, so the rebuild lost them. 🧑 asked for this by name: "it should have all the functions of old ui, make sure ntohing in old ui as functions get lost". docs/TODO.md § 133.5, and Logs/control-inventory.txt is the full
- `[screens] TumbangPreso.PlayTests.PhaseSurfaceLayoutProbe.TheTelemetryRowFitsItsBoxAtEveryShippedResolution`  16:9 720p settings/telemetry-row: 'MainMenuCanvas/SettingsPanel/Card/Margin/Layout/Scroll/Content/TelemetryRow/Label' reading "Share Anonymous Stats" needs 212 px and was given 100. MenuKit.Label sets Overflow, so it draws straight over its neighbour and nothing errors. Expected: less than or equal 
- `[screens] TumbangPreso.PlayTests.PlayerHubLayoutProbe.EveryTabFitsItsBoxAtEveryShippedResolution`  16:9 720p hub/PROFILE: 'Label' needs 584 units for "None of this is required and all of it is public on your career page." and was given 567. It does not wrap and does not shrink, so it draws over whatever is beside it. Expected: less than or equal to 567.880005f But was: 583.5f
- `[match] TumbangPreso.PlayTests.CarryTests.AHeldSlipperStaysOnTheArmThroughMovementAndAMissingAnchor`  a held slipper drifted 0.092 m from the hand while its carrier walked. The carry has to run in LateUpdate: Unity evaluates the Animator between Update and LateUpdate, so a bone read in Update is the PREVIOUS frame's pose and the slipper trails the hand by one frame of animation. Expected: less than 
- `[match] TumbangPreso.PlayTests.SteeringTests.AMovementAimedSeatTurnsToFaceItsDirection`  the seat drifted sideways while settling, so it is not standing on the floor this test built. See docs/TODO.md § 130.14. Expected: less than 0.00999999978f But was: 1.07999992f
- `[match] TumbangPreso.PlayTests.SteeringTests.MouseAimedMovementIsRelativeToTheBody`  the seat drifted sideways while settling, so it is not standing on the floor this test built. See docs/TODO.md § 130.14. Expected: less than 0.00999999978f But was: 1.07999992f
- `[match] TumbangPreso.PlayTests.SteeringTests.TheSteeringFrameByFrameIsWrittenOut`  the seat drifted sideways while settling, so it is not standing on the floor this test built. See docs/TODO.md § 130.14. Expected: less than 0.00999999978f But was: 1.07999992f

### Source audits

| Audit | Verdict | Summary |
|---|---|---|
| `audit_ability_authority.py` | OK | 49 effect call sites, 30 host-gated, 0 ungated on another body, 19 ungated on the caster |
| `audit_request_call_sites.py` | OK | 59 wire entry points, 0 unreachable. |
| `audit_wire_payloads.py` | OK | 61 named messages, 0 mismatched. |
| `audit_audio_reach.py` | OK | 42 call sites, 0 host-only |
| `audit_presentation_reach.py` | OK | 96 presentation call sites, 96 reachable by every peer, 0 HOST-ONLY |
| `audit_cue_relay.py` | OK | 48 NetCue call sites: 21 host-only, 1 suppressed, 25 inside a kit, 1 owner-driven, 0 UNGATED. |
| `audit_shader_stripping.py` | OK | 10 shaders looked up by name, 0 would be missing from a player build. |
| `audit_ability_stat_drift.py` | OK | 18 ability constructors across 13 files, 0 stat drift finding(s). |
| `audit_event_subscriptions.py` | OK | 85 subscriptions in Runtime/, 0 with no matching unsubscribe, 0 stale allowlist entries. |
| `audit_tournament_defaults.py` | OK | 8 tournament modifiers named, 8 read cases, 8 write cases, 0 finding(s). |
| `audit_gameplay_clocks.py` | OK | 16 wall-clock reads across Runtime/ and the core, 33 gameplay-critical files checked, 0 finding(s). |
| `audit_cue_audio.py` | findings (not gating) | 117 files, 11 flagged |

### Editor checks

```
ALL CHECKS, ONE LAUNCH

  OK    headless       Logs/headless-check.txt
  OK    arena          Logs/arena-check.txt
  OK    map geometry   Logs/map-geometry-check.txt
  OK    audio cues     Logs/audio-cue-check.txt
  OK    scene scripts  Logs/scene-script-check.txt
  OK    input surface  Logs/input-surface-check.txt
  OK    scene dependencies Logs/scene-dependency-check.txt
  OK    shader warmup  

RESULT: OK. All 8 checks passed in one launch.
```


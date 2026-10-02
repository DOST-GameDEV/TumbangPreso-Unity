# Airburst evidence

Every number below comes from a run's own files (copied under `evidence/`).
Candidate: detached worktree `Codex/work/tump-amihan1002` at `70370785e` plus
the owned overlay (`evidence/final3-overlay-inputs.json` holds the SHA-256 of
every overlaid file). Profile `amihan-airburst1002`. One heavy job at a time.

## Runs

| Run | Question | Result |
|---|---|---|
| B1 baseline, PlayMode | What does shipped Airburst do natively? | 0/5. Every case fails on a logged `NullReferenceException`: the shipped cutscene throws while it is built (`WindVfx.Motif` x3 on one root; `GeneratedMeshOwner` is `[DisallowMultipleComponent]`). Films stop at 33 frames. |
| B2 baseline retry (the one bounded retry), PlayMode | Film what players see today, logging but tolerating that exception | 1/4. Round-boundary cleanup passes. The three films record 330 frames each and fail on "The cutscene never came up". Owner frames 31 to 138 show a frozen court with no Amihan picture. |
| F0 storm bake, editor method | Does the storm-only bake rewrite exactly one asset? | Exit 0. Only `hero-amihan-storm.anim` changed (3.01 s to 2.07 s); `.meta` hash identical (GUID kept); the other four baked Amihan clips untouched. |
| F1 EditMode guard | Do the shipped clip and first-person path release on the rule? | 4/4 passed: roster entry ships the checked asset; full drive within one frame of 1.5 s and drawn back 0.25 s before; clip starts on the cutscene's last key; `storm-call` contact and shove key are 1.5 s. Not run against the baseline. |
| F2 final, PlayMode | Films, interruption, intro body study | 4/5. All films and the interruption pass; the intro study failed: 86 held-shoe vertices inside her head (new cutscene arm poses). Fixed by lowering four arm keys (offline clearance 0.205 m minimum across 0 to 3.6 s). |
| F3 final rerun after that fix, PlayMode | Same five cases | 5/5 passed, zero skips, 184.528 s, ended 2026-10-02 03:21:25 UTC; owned job exit 0. Final held-shoe/head audit: 0 intersecting vertices. |

## Timing (B2 shipped vs F2 new, same staging, 30 frames per game second)

| | Shipped (B2) | New (F2) |
|---|---|---|
| Cutscene overlay | never drawn; world frozen frames 31 to 138 | drawn frames 32 to 138, clock 88.780 at both ends |
| Handback, release | 139, 184 (1.50 s) | 139, 184 (1.50 s) |
| Right palm forward at handback + 0.33 s | 0.37 m (already pushed) | -0.08 m (cupped at hip) |
| Right palm at draw back (release - 0.17 s) | 0.38 m | -0.26 m |
| Peak drive frame | 191 (release + 7), clip shove still running at 214 | 185 (release + 1), 0.537 m |
| Victim Whirled / outsider | 184 / no | 184 / no |
| Fan on the court during windup (court view) | not visible (drawn under the tiles) | edges and chevrons visible, Low included |
| Fan gone after release | 1.67 s | 0.97 s |
| Cast action still playing after release | 1.50 s | 0.57 s |

## Clips (silent; audio not evaluated)

Each clip: a 2x2 grid (her screen, observer outside the fan, player inside the
fan, the court), two full normal-speed passes, then a labelled 0.4x slow-motion
section with original film time burned in. See `clips/`.

## Limits

Fixed-simulation films, not real-time performance or frame-rate evidence. One
PC; no actual peers, no Android, no physical controller. Bayan Plaza only. The
B2 observer camera sat inside a player's head; that view is blanked in the
BEFORE clip. F3 changes its position, but representative final frames still
contain a large nearby head in observer/victim views. Use the court view for
clearer fan inspection; observer visual readability is only partially evidenced. Assistant review is
not human approval. Clips are silent; no SFX were created, changed or judged.

## Publication checkpoint verification

The final rerun completed after the authoring session stopped. Its XML, three
330-frame timing tables and summaries, grounding audit, frozen overlay hashes
and terminal exit receipt are retained under `evidence/f3-*`. All thirteen
overlaid paths match the final candidate and recorded manifest byte for byte;
the separately baked storm clip also matches. `publication-inputs.json` records
the fourteen source, asset and test inputs. The storm `.meta` is unchanged.
No new native run was launched for this publication checkpoint.

F3 again shows handback139, release184 (45 frames, 1.50s), first victim Whirled184,
peak palm185, outsider untouched, zero logged errors and a frozen cutscene clock.
Each body/fx/Low variant has330 frames per view for four views. The body-only
film hides the live fan's renderers; the cutscene retains its authored effects.
The round-boundary case ends then advances the round; it checks cleanup three
frames later, not the stricter one-frame aspiration in acceptance.md.

This is a preserved Airburst implementation with scoped native evidence, not
completion of every acceptance line or all of Amihan. Acceptance checkboxes
remain conservative. Refusal/disconnect during the introduction, role changes,
all-map readability, actual player/peer checks, physical input and human review
are not qualified by these five cases. No SFX changes or audio evaluation.
The earlier F2 failure and both baseline failures remain in the report.

## Feel pass (later October 2)

The cutscene, Airburst, Drift and light-body revisions have their own run table,
retained failures and limits in [feel-pass.md](feel-pass.md). Evidence files are the
`evidence/feel-*` set; clips are the `*_p2.mp4` set in `clips/`.

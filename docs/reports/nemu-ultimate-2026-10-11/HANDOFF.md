# Nemu ultimate (LET THEM IN): handoff, 2026-10-15

Paste this file's path into a new chat to keep refining. Current state: **v14**, pushed on branch `ASTRAReworks`. The full
history, every owner quote and every film round is in [direction.md](direction.md) (sections 18 to 22 are this pass).

## What it is now

**Cutscene, 7.0 s** (`Assets/TumbangPreso/Runtime/Visual/HeroIntroductionScene.Nemu.cs`; all timing is the `Nm*` constants at
the top; her body poses and fallback shots are authored in `tools/author_ultimate_intros.py` `nemu()`, which writes
`Resources/UltimateIntros/nemu.txt` and `nemu-held.txt`; keep the two in step):

| Beat | Time | What |
|---|---|---|
| THE GATE | 0.00-0.80 | Low behind her, hands raised; a portal tears open over the court (no prayer, no cast laugh). |
| THE FIRST ONE | 0.80-2.50 | Low in front of the first player: one soul out of the gate, glides down and circles, heads follow; dives in at 2.25, eyes light 2.35. |
| THE HUNT | 2.50-5.30 | Beside the runner: the gate pours the rest out; the third is caught 3.35; the runner trips 3.50, rolls 3.85, plunged 4.30 (eyes light on entry 4.40), hauled up like a puppet 4.70. |
| THE STRINGS | 5.30-7.00 | Behind the runner: his head turns all the way round to stare (5.45), strings yank him up (5.95), he drifts at the lens and snaps nearer as the lights die to his eyes only (6.30), eyes flare (6.42), dusk for the hand-back. Glitch bursts throughout. |

**In play** (`Runtime/Visual/NemuWorld.cs`): while anyone is possessed every match camera takes the dusk (kept through the
cutscene's paused round); the gate hangs open over the court; 24 souls (12 with reduced effects) circle, a third round the
court and the rest spread over the whole map's playable bounds; they go back into the gate one at a time from 3 s to 13 s;
the last four and the gate go with the last leave.

**The possessed player's own screen** (`Assets/TumbangPreso/Shaders/PossessedVignette.shader`, driven by
`Runtime/UI/TumpHudEffects.cs`): a compass ring round the crosshair with a violet arc and two chevrons pointing the way the
ghost drags them; streaks flowing to that edge; their aura as dark smoke only on the pull's side. No ghost hands. Nothing
fades with grip; it only grows toward the pull.

**On the possessed body, for everyone** (`Runtime/Visual/PossessedPresence.cs`, `NemuAura.cs`): the aura, which leans and burns
hardest toward the pull (`NemuAura.State.Lean`), violet eyes the whole time (`NemuEyeGlow`).

**Shared pieces**: the gate `Runtime/Visual/NemuPortal.cs` + `Resources/Shaders/NemuPortal.shader` (textured tunnel, grab-pass
twist); the souls and their five faces `Runtime/Visual/NemuSouls.cs` (`Expression`: Shock, Mischief, Wail, Rage, Smirk);
the screen pass `Resources/Shaders/NemuInk.shader` (dusk `_Eerie`, blackout `_Black`, impact frame, focus lines, `_Glitch`).
The floor cracks (`NemuCrackField`, `NemuCrack.shader`) were deleted in v10. Other maps: the cutscene stage stands on the
caster's own floor when the court lookup disagrees (`HeroIntroductionScene.cs`), and the lens stops in front of the map
(`NmClearLens`).

**Sound drafts**: `tools/build_nemu_ult_audio.py` writes `docs/reports/nemu-ultimate-2026-10-11/audio-drafts/` (theme timed to
the beats by its constants near the bottom; keep them in step with `Nm*`). Not in `Resources/Sfx` until the owner approves.

## How to film

Unity only through the guarded runner, in the background, never editing `.cs` while it runs. Environment:
`TUMP_NEMU_FILM=1 TUMP_NEMU_FILM_LITE=1 TUMP_NEMU_FILM_FRAMES=390 TUMP_EVIDENCE=Logs/nemu-lti/rNN`, then:

```bash
python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile presentation-validation-20260921 -runTests -testPlatform PlayMode -testFilter TumbangPreso.PlayTests.NemuPrusisyonFilm -testResults Logs/nemu-lti/rNN.xml -logFile Logs/nemu-lti/rNN.log
```

* `TUMP_NEMU_FILM_LITE=1`: her screen and the possessed screen only (what the owner wants to review). Leave it off for all
  four views; `TUMP_NEMU_FILM_OWNER_ONLY=1` for her screen only; frames default to 24 s.
* `TUMP_NEMU_FILM_MAP`: BayanPlaza (default), Eskinita, IlalimNgTulay, LagoonCove, Kanto, SaBubong, Arena. The owner wants films
  on Bayan Plaza.
* Stitch with `tools/stitch_ability_film.py <film dir>/nemu-prusisyon out.mp4 --title ... --view owner "HER SCREEN" --view
  victimscreen "POSSESSED PLAYER'S SCREEN" --sfx-dir docs/reports/nemu-ultimate-2026-10-11/audio-drafts`. Compress under 30 MB
  for the phone (`ffmpeg -crf 30`).
* Gates: the film test (cutscene under 7.2 s, round clock frozen) and `NemuPrusisyonTests;NemuPossessionMovementTests` (8/8).
* If a run is stopped early, restore the profile snapshot in `Logs/profile-preservation-*` (the runner's restore did not run).
* Python writes on this machine sometimes fail with `OSError 22`: write to `path.tmp` and `os.replace`, or retry.

## Owner rules for this ult (do not regress)

5 to 7 s; black and one violet; medium and wide shots; the story must be readable (slow enough, one thing at a time); people
move like people until possessed, then wrong; eyes violet from the moment the soul enters and the whole possession; no ghost
drawn on the possessed (aura and eyes only); no floor cracks; souls not cluttered; nothing fades with grip.

## Open

* The owner's verdict on v14 and on the sound draft.
* The strings in THE STRINGS read faintly (they run back toward her, behind the lens).
* Her own screen shows about one bright frame at the hand-back before the dusk.
* Not filmed: Ilalim ng Tulay, Sa Bubong, Lagoon Cove, Kanto (Arena and Eskinita pass).
* Real networked peers on protocol 160; `NemuKitContractProbe` still fails 21/38 on retired mechanics.

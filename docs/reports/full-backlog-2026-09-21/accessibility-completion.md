# Accessibility completion, task127.3

The former missing comfort/readability options are implemented locally. Defaults
preserve the existing controls and presentation. The black-outline rule was
published separately as74ac6e9a98afbba301eca298eb65c7317a59ed72.

## Implemented behavior

- Persisted first-person FOV75..110 with original95default and finite validation.
  Opt-in toggle sprint and can restore; menu/role/round/possession boundaries clear
  latches. Pickup/shove remain press actions. Restore can be cancelled or completed
  without holding the button. The real check also fixed solo handover to a bot seat
  that had no PlayerInputReader. Existing input backends/network protocol remain.
- HUD size100..120%; larger text enlarges whole anchored HUD groups without cropping
  the canvas. Settings reading text grows20% and controls reflow below labels in the
  existing scroll view. Save/discard previews restore the original layout, including
  the existing touch row minimum. Training, match chat and replay captions share the
  scale. Ability reference enlarges text while retaining all three columns.
- High-contrast HUD uses white text, black supporting plates and retained explicit
  seat/role words. Hidden text behind controller glyphs remains hidden. Meaningful
  icons/identity marks are retained. It is an opt-in presentation preference.
- Reduced effects use35%burst density and60%looping status density, retaining emitters,
  shape, colour, motion, ability zones and game rules. Density restores when switched
  off. Peripheral flashes and stun vignette coverage are reduced.
- English announcer captions use HUMAN.md Table A meanings for the8delivered IDs,
  follow the same priority/cooldown gate, and work with announcer volume muted.
  Muted captions do not play silent sources or duck music. Missing VO is not invented.
- An8m same-frame/greyscale comparison showed the old taya ring hidden under the
  character's feet. Only that annotation now uses an8-sided ring with1.95capsule
  radius, versus the unchanged attacker's1.375disc. The open front/angular edge
  survives greyscale. Existing people, rigs, gameplay colliders and art are unchanged.

## Evidence and limits

Core toggle6/6 passed. Unity live sprint and FOV/discard passed; real restore
start/cancel/finish passed6.311s after its fixture respected can protection and
exposed the actual missing-reader handover bug. Layout/discard2/2 passed5.598s;
legacy/default/bounds9/9 passed.0789s. Reduced particles passed; muted actual
announcer/caption and current role frame passed6.212s. Wider role marker capture
passed3.648s and its greyscale image was inspected. Profile guards preserved data.

The seven-hero ability referencev3 uses the actual UI builder and asserts each
hero's real three names during capture;21descriptions passed5.691s. V1 falsely
captured the local Zack kit for every filename because live Hud.Tick replaced
staged text. Its receipt is retained but is NOT seven-hero proof. V2's attempted
Hud disable also hid its canvas. V3 isolates this layout check correctly. The
latest DEV refinement fits large reference height to the longest real description;
its final v4layout check passed6.504s with all7actual hero-name assertions.
Rafi/Phaister compact references inspected. Earlier failed evidence is retained.

Windows internal v56 built1211MB/85s,guard269227d5d75b. It includes the comfort
features and precedes the final dynamic reference-height refinement. It is an
internal DIRTY-stamped build, not a clean release or whole-project qualification.
Native spectator v56 passed: held-item hiding/release/re-equip, manual flight,
bookmarks, tactical pause and actual replay at960x720/1680x720; shared input/profile
data preserved. Its earlier v55 failure photographed
a watched actor who was RETRIEVING, with empty hands: another bot had disarmed the
held-item fixture. The diagnostic now re-establishes held ownership and identifies
further interference rather than falsely diagnosing a duplicate held renderer.

Task127.3 remains open until expanded overlay/native integration evidence is recorded.
The implementation and focused Unity checks are complete; remaining integration
includes actual native large settings/owner HUD, training/chat and replay labels.
Owner acceptance, physical device certification and final whole-source qualification
are separate from this implementation evidence. Continue all remaining TODO items.

## Native v57 closing evidence and the shared reduced-effects link, 2026-09-23

Native Windows v57 (guard 2aa2d43a9ec6, built from the validation workspace at 7c028dda plus the
recorded accessibility inputs) ran the bounded `--accessibility-only` route and passed all 15
stages: accessibility settings through the real preparation route, native save and discard, the
large owner HUD with a muted-announcer caption in Hero play, the larger training card after the
match-chat display check, spectator startup, autopilot and manual flight, real POV, held-item
release and replacement, bookmark and tactical pause, a replay buffer filled from real camera
renders at 960x720 and 1680x720, and flight POV restoration. Receipts:
`accessibility-evidence/native-v57-result.json` and `native-v57-runner-result.json`; frames:
`native-v57-owner-large-wide.png`, `native-v57-settings-comfort-4x3.png`,
`native-v57-training-large-wide.png`, `native-v57-spectator-replay-wide.png`.

Two findings from that run, recorded rather than hidden:

- The player process exited with 0xC0000005 during shutdown, after `[OwnerUiReview] PASS` and
  `CodeReloadManager destroyed`. Two of the recorded native runner results carry this code; the
  verdict is unaffected, the crash is not explained. Tracked in TODO.
- In `native-v57-owner-large-wide.png` the injected match-chat line is clipped to "LOCA..." at
  the larger HUD size. The uncommitted `HudReadingLayout.RebasePlacement` change (chat adopts its
  final corner after construction) targets this placement and postdates v57; re-check it in the
  next native build before calling it fixed.

The TODO 134.10 reduced-effects contract is now linked in source: one
`GameSettings.EffectiveFlashIntensity` and `EffectiveCameraShake` (the player's slider times 0.25
when Reduce visual effects is on, the saved slider unchanged) drive body, replay, can-contact,
elemental burst, sky-event, HUD edge and score-row flashes and all camera shake, ground rumble and
impact punch. Reduced effects also skips the optional micro-hitstop (and releases one already
running), holds ultimate introduction cameras steady and stops the ultimate card slide. Default
play is unchanged. Focused EditMode 19/19 (`Logs/visual-p0-editmode-v1.xml`, including
`ReducedEffectsQuartersFlashesAndShakeWithoutRewritingTheSliders` and
`ReducedEffectsSkipsTheOptionalImpactPause`) and Core 615/615. Native confirmation of the link
rides the next VISUAL-1 build.

# Ultimate performances: progress and evidence

One hero at a time (owner). Each row says what was built, what was actually looked at, and what is
still unverified. Research: [research.md](research.md). Plan: [plan.md](plan.md).

## Machine limits (cloud Ubuntu, 2026-09-24)

- Unity 6000.5.8f1 installed at `/opt/tump/unity`, but **no licence can be activated here**
  (`No valid Unity Editor license found`; the account has no `com.unity.editor.headless`
  entitlement). No native editor, PlayMode, render or build ran on this machine.
- **Compile check that did run:** a Roslyn semantic check of every `Runtime` source (and the
  changed PlayMode test files) against Unity 6000.5.8's own managed DLLs and the exact package
  versions in `Packages/packages-lock.json` (netcode 2.13.1, inputsystem 1.20.0, services,
  transport, ugui, collections, mathematics, burst). Baseline: 5 known errors, all
  `InputSystemUIInputModule` (an input-system UI assembly this check does not build). A planted
  error in a new file was caught with the right messages, so the check sees method bodies.
- **Pose check that did run:** `tools/intro_pose_preview.py` skins the real `team-<hero>.glb` in
  Unity space (glTFast's X mirror, `Quaternion.Euler` order) from the authored shots. Silhouette
  and framing only; no toon shader, outline or stage effects. Every sheet is labelled.
- **Owed to the Windows machine:** `UltimateIntroductionProbe` (all seven heroes, clip length,
  grounding net of authored lift, framing outside close shots), `SharedUltimatePhaseTests`
  (cohort length, release timing, blocked-shot fallback, reduced view), a native capture of each
  performance from the caster, an observer and a spectator, and the reduced-motion version.

## Shared plumbing (done, compile-checked, not natively run)

- `UltimatePerformance` reads `Resources/UltimateIntros/<hero>.txt` written by
  `tools/author_ultimate_intros.py`: length, raw bone keys, punches, lift, shots (with `close`
  and `fit` flags), the locked reduced-motion shot and the voice cue time.
- `SharedUltimatePhase.Duration` is now the longest accepted hero's length, derived on host and
  peers from the same commits. `NetSession.ProtocolVersion` 51 to 52.
- `UltimatePhaseView`: handoff 0.4 s before the shared boundary, return over the last 0.12 s,
  every authored shot checked against walls (mirror, borrow a clear shot, or the card), reduced
  motion/effects now get the performance from one locked shot, faded in.
- `HeroIntroductionScene` split into plumbing plus one partial file per hero. A shorter hero in a
  longer cohort holds its last full moment and exits with the shared boundary.
- The hero's own voice line can play inside the performance; the live cast then skips it on that
  peer (`HeroAbility.IntroductionVoiced`).

## Per hero

| Hero | State | Length | Evidence |
|---|---|---|---|
| Phaister | **New performance** (sly hat tip, held-in chuckle, laugh that lifts her 0.58 m with the five laugh syllables shaking her chest, eclipse drawn forward, the claim, a soft landing). Stage: night falls with the laugh, stars, the moon rises, the moon serpent coils round it and swallows it to a violet corona, laugh stars on each "ha", a levitation shadow, her WardCircle claims the ground. Laugh voice moved inside the laugh. | 4.2 s | `previews/phaister_v1.png` to `v5_shots.png` (v1 to v4 are the rejected iterations: shot A too tight, brim turning into a slab with the head thrown back, raised hands hidden inside the brim on chibi arms; all fixed). Stage effects NOT yet seen rendered. |
| Sean | Retained 2.8 s baseline, transcribed key for key | 2.8 s | next |
| Zack | Retained baseline | 2.8 s | |
| Nemu | Retained baseline (Kuro fit reveal kept) | 2.8 s | |
| Dante | Retained baseline | 2.8 s | |
| Cheska | Retained baseline (held-slipper variant kept) | 2.8 s | |
| Rafi | Retained baseline | 2.8 s | |

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
| Sean | **New performance**: plant and shoulder roll, cupped hands at the chest with the head bowed over them while a parol frame of five fire sticks lays itself stick by stick (his lantern craft), the star fills with flame, the head snaps up and holds a breath, the coil with arms swept back, the rise onto his toes. Stage: dusk wall with a glowing horizon band that dims on the held breath, six paper-lantern stars drifting up behind him, coil flames at his feet, sparks racing up on the rise. | 3.4 s | `previews/sean_before_witness.png` (the old one barely moves), `sean_v1_*` (shot B too tight, coil unreadable from the front), `sean_v2_shots.png` (side shot for the coil). Stage effects NOT yet seen rendered. |
| Zack | **New performance**, still the shortest: hip cocked with a head tilt, a spark flicked off the fingertip, one lazy finger up (a diagonal, clear of his hair) that the storm answers with a far bolt, then he sights the shot down his arm while a gold line runs out along it, snaps it down (a bolt drops behind him) and shrugs. Stage: a storm rolling over a roofdeck skyline (his Pasig condo), two single-frame flashes only, none in reduced effects. The old intro's hand covered his face. | 2.8 s | `previews/zack_before_witness.png`, `zack_v1` (finger hidden by hair), `zack_v2` (over-the-shoulder shot hid the arm behind his head), `zack_v3`/`v4` (profile along the arm, room ahead for the line). Stage effects NOT yet seen rendered. |
| Nemu | **New performance**: gazing at nothing with hands behind her back, Kuro nudges her, she turns and offers a hand he nuzzles, then opens it and looks straight down the lens while ink seeps up from the road and climbs the stage, his eyes open in the dark, he swells behind her (retained rage form and fit reveal) as she stays calm, and she points him ahead. | 3.8 s | `previews/nemu_before_witness.png`, `nemu_v1` (head turns went the wrong way: +yaw turns these rigs to THEIR right, the old code comment says left; Phaister's glance fixed with it, `phaister_v6_glance.png`), `nemu_v2_shots.png`. Kuro is not in the preview tool; stage NOT yet seen rendered. |
| Dante | **New performance**: plants wide and unhurried, gets under a weight with a strain tremble, stands into it and pushes two stone slabs apart overhead (held, trembling), loads both fists over his right shoulder, stamps forward. Stage: a dust haze settles, a ridge lifts on the horizon, two opaque stone slabs shoulder up out of the road and part with his push, molten seams split forward on the stamp and dust kicks up. | 3.8 s | `previews/dante_before_witness.png`, `dante_v1_shots.png` (every beat read on the first sheet). Stage NOT yet seen rendered. |
| Cheska | Retained baseline (held-slipper variant kept) | 2.8 s | |
| Rafi | Retained baseline | 2.8 s | |

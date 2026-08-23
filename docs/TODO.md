# TODO: Tumbang Preso Unity

Open work, ordered by what is worth doing next. Each entry says what is wrong, where it lives,
and what "done" looks like, so nobody has to re-derive it.

**Check this before inventing a task, and update it in the same commit as the work.** Finished
items move to **Closed** at the bottom with one line on how they were verified.

Read [`VISION.md`](VISION.md) first if you have not. Several entries here only make sense
against the readability budget in its § 2.

---

## 1 · Peer rematch voting across the wire

**The last genuine PARTIAL row in the ledger, and the only one.**

`match_result.gd`'s rematch is a VOTE in a networked match. Here the button acts locally, so
four peers can each think a rematch is or is not happening. Single-player rematch works.

**Needs:** an RPC pair (a peer votes, the host broadcasts the tally), the tally drawn on the
result card, and the same "counts peers, not characters" rule `ReadyGate` already uses, since
bot-filled seats cannot vote.

**Where.** `Assets/TumbangPreso/Runtime/MatchResult.cs`,
`Assets/TumbangPreso/Runtime/Net/MatchRpc.cs`, and `ReadyGate.cs` for the pattern to copy.

⚠️ **This cannot be finished honestly without two real processes on a LAN**, which has never
been run. Write it, cover it in `RuntimeLayerTests` the way reconnect is covered, and say
plainly in the handoff that the wire half is simulated.

---

## Closed

- **Dante 3D Model Stray Geometry Fix.** ✅ 2026-08-23. Removed 1,340 stray vertex and triangle
  elements (islands 959-992) from `team-dante.glb` head mesh that formed an asymmetrical floating horn/spike
  protruding through the temple and rear of the character's head. Cleaned binary buffers and re-indexed
  mesh to restore symmetrical head bounds `[-0.195, 0.188]`.

- **In-Game Ability HUD Slimdown & First-Person Hand Clearance.** ✅ 2026-08-23. Redesigned `HeroDeck`
  in `Hud.cs` into a slim, minimalist 240x68 dark glass panel anchored at `y = 10` (down from 592x122 at `y = 24`).
  Replaced cramped 3-line text wrapping with centered high-DPI vector SDF glyphs, corner key chips (`[Q]`, `[F]`, `[X]`),
  and bold centered cooldown countdowns (`4.2s`). Moved `ReadyObjective` to top-center (`y = -210`) and
  `ReadyPrompt` to `y = 96` so game text never obstructs first-person hands.

- **Character Select Ability Ribbon & Tile Polish.** ✅ 2026-08-23. Polished `ConvertedCharacterSelect.cs`
  to style selected ability tiles with a crisp gold/accent glowing border over dark slate glass rather than
  a solid orange background fill. Expanded ability details card to an uncluttered 2-line tactical readout.

- **Ability VFX footprints, procedural particles & UI overhaul.** ✅ 2026-08-23. Calibrated all
  hazard footprints (Cheska Permafrost 2.3m / Barricade 3.2m / Nova 4.6m, Dante Stomp 2.4m / Fissure
  5.5m, Nemu Void 3.2m, Sean Supernova 4.8m, Zack Thunderstrike 4.5m). Introduced `AbilityVfx.cs`
  procedural ParticleSystem generators for ice bursts, magma eruptions, void wisps, and electric arcs.
  Overhauled `ComicPopup.cs` with comic font (Darumadrop One), ink outline layers, and punchy bounce.
  Redesigned `AbilityIcons.cs` with 128px high-DPI procedural vector glyphs and modern tactical shapes.
  Overhauled UI theming across Character Select, HUD Deck, and Inspect Panel, replacing solid bright
  orange tiles with sleek dark glass plates (`rgba(16, 22, 34, 0.90)`) with glowing white/accent glyphs.
  Redesigned Character Select Hero Loadout into a Valorant-style horizontal ability ribbon with an
  interactive details card, eliminating button collisions. Rewrote all ability descriptions across all 5
  hero kits into intuitive, action-driven tactical instructions (`ACTIVATE`, `DEPLOY`, `SLAM`, `PHASE`, `SURGE`).
  Fixed font blurriness by increasing dynamic TTF raster size to 32 and removing unnecessary outline overhead.

- **The 8 PARTIAL rows in `docs/Port_Ledger.md`.** ✅ 2026-08-23, audited against the code
  rather than against the rows. **Seven of the eight were stale bookkeeping**: the work had
  landed and the row was never updated. The audit table is in the ledger's status summary.
  Two pieces were genuinely missing and were written: the music **intensity lift** in the last
  15 s of a round, and the **duck-trigger table** that drops the bed under the countdown, the
  round end, the win and the score award. One row remains genuinely partial and is § 1 above.


- **Load every resource on the BH Studios loading screen.** ✅ 2026-08-23. The preload covered
  the roster, audio and the MAIN MENU scene, and then the arena, its materials, the baked UI
  sprites and the hero kits were all still cold when Play was pressed. It now also warms both
  maps through their assets, every `GodotTheme` box, every ability glyph, the input asset with
  the player's rebinds, and all five kits. `SplashScreen.PreloadGameAssets`.

- **Plan the whole keymap and put throw on left click.** ✅ 2026-08-23. Throw always WAS on
  left click; Grab was on it too, which is why it did not behave like it. There were four live
  collisions in total (left click carried Throw and Grab, E carried Grab, Lunge and Skill 1,
  Q carried Throw and Skill 2). Every action now owns exactly one control:
  left click throws or punches, E is the contextual pick up / shove / reset, right click
  lunges, Q and F are the skills, X is the ultimate, Tab holds the ability panel open.
  `InputMapAndAbilityTests` asserts no control is shared and that throw is on left click.

- **Redesign the skill UI.** ✅ 2026-08-23. Cards are an icon tile with the bound key on a chip
  in the corner; cooldowns drain a smooth bar and the ultimate fills a notched one, so the two
  quantities can no longer be confused; the deck moved out from under the practice prompt; key
  labels come from the live bindings. Descriptions moved off the HUD entirely into a
  hold-to-read panel, and character select now shows every power with its icon, its kind and
  its sentence.

- **Ultimate charging during the ready screen.** ✅ 2026-08-23, and the requirement changed
  mid-flight. Charge now PERSISTS across rounds and is frozen whenever the round clock is not
  running; during the warm-up and the between-round buffer the ultimate is free to cast off a
  practice counter, so it can be rehearsed without spending the meter or earning one by
  waiting. Cooldowns still run in practice, deliberately.

- **Organise the settings controls into groups.** ✅ 2026-08-23. Four headed sections
  (Movement, Playing the game, Hero powers, Round and screen) instead of fourteen unlabelled
  rows. `SettingsGroupsCoverEveryActionExactlyOnce` asserts nothing can fall out of the panel.

- **Hero Strike unretrieved-slipper penalty variance.** ✅ 2026-08-23. Two causes, both
  measured. The probe was unseeded, so the same build measured 110 and then 467 penalties on
  consecutive runs either side of its own 200 ceiling; it is seeded now. And bots walked
  straight through hero hazards on the way to a tsinelas, so `HazardMap` and
  `AIController.AvoidHazards` steer around them. Hero Strike now measures 77 throws, 77
  retrievals, 182 skill uses, 21 ultimates and **1** unretrieved-slipper penalty in a match.
  ⚠️ The avoidance is capped at 3 m until § 1 lands; see § 1.2.

- **The stun frost is very strong.** ✅ 2026-08-23, reduced. Reach 0.36 to 0.24 screen heights,
  body alpha 0.36 to 0.30. Both opposite edges spend the reach, so at 0.36 the clear strip left
  in the middle was 0.28 of the screen height for a five second stun. Still worth a look in a
  real match: § 1.5.

- **The preview idle pose vs the Godot reference.** No need. The character preview was reworked
  in a separate pass; the arms-crossed mismatch in `ModelPreview.PlayIdle` is not being chased.

# Pullback, bare hands and low tripwire, 2026-10-25

Owner direction: Backtrack should read as a body being pulled into its old
location, like Ekko. Remove the retired magnets from first person. Lower the
wire and replace the strange poles with anchors that explain the connection,
using Cypher as a reference without requiring walls. Make original electric SFX
auditions. The owner authorized commits on main.

## Motion references actually viewed

The official videos were played in the browser and sampled during normal
playback. Both temporary reference tabs were closed after viewing.

- [Riot's Ekko page](https://www.leagueoflegends.com/en-us/champions/ekko/),
  [Chronobreak demonstration](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0245/ability_0245_R1.mp4).
  Observed the lead-in and the return around 4.5 to 6.2 s: the past echo makes
  the destination legible, the real body departs its current pose, several body
  afterimages trace the curved route and collapse into the old location, then
  the arrival resolves. Take the body motion and readable collapse. Keep Zack's
  gold palette, 3-second history and held slipper. Do not take healing, damage,
  invulnerability, blue/pink effects or a large damaging arrival sphere.
- [Riot's Cypher page](https://playvalorant.com/en-us/agents/cypher/),
  [Trapwire demonstration](https://cmsassets.rgpub.io/sanity/files/dsfx7636/game_data/aab21b75eb43f0e8cc9c0b816cb4877ae868b9fd.mp4).
  Viewed placement, target contact and the victim view through 17 s. The small
  emitter is a device attached to a surface, the line is under tension and the
  stronger pulse answers contact. Take the clear emitter/line relationship,
  tension and contact punctuation. Zack instead uses two bolted floor cleats
  with short yokes, insulated reels and live sockets. No tall concrete poles,
  wall requirement, stealth, reveal, delayed tether or copied assets.

## Implementation beats

- Backtrack: a 0.42-second render-only body pull through the recorded path,
  sampled poses playing backward, three trailing afterframes, a waist-height
  gold ribbon and an arrival catch. The actual accepted teleport and its
  ownership/status/slipper behavior remain unchanged. The first-person eye
  follows the same pull; the normal body is revealed again on the catch or
  interruption. Recorded input can resume normally while the presentation
  merges into the current body position at the end.
- Hands: remove the old magnet runtime. Bare arms gain only short current arcs
  on the forearms while powered or casting. No coins, caps, keys or floating
  metal. Historical authoring assets are not loaded by the replacement.
- Wire: sockets at 0.28 m, middle at 0.24 m, contact reach 0.40 m including the
  small feet/body allowance. A timed jump now clears it. This deliberately
  changes the old above-jump rule, so protocol 183 rejects the old semantics.
  The two compact floor anchors face into the span. Overcharged still doubles
  the line and Zaps each attacker once.

## Original sound auditions

`tools/build_zack_refinement_sfx.py` uses deterministic local synthesis without
samples or voices. The pull is a rising inward current with dry tightening
contacts; the landing is a separate short electrical catch. The wire has a
clasp/whip/pluck, a quiet mains-family current, a short broken buzz on contact
and a descending twang when it snaps. Stereo remains mono-compatible and peaks
stay below clipping. The sounds are mixed into review exports only. Nothing is
registered in Resources or enabled in the game's skill-sound switch until the
owner chooses it by ear.

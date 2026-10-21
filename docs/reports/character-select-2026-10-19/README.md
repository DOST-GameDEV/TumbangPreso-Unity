# Character select rework, 2026-10-19

The request: fix character select, study how Valorant, Overwatch and other games do it, and keep
TUMP's own identity. This file records what was wrong (measured on in-engine captures), what the
references actually do, how each idea was translated into this game, and what is still open.

Screen: `Assets/TumbangPreso/Runtime/UI/Hub/HubCharacterSelect.cs`.
Captures: `MatchArrivalFlowTests.CharacterSelectionReviewCaptures` writes
`Logs/shots-native-ui/CharSelect-<version>-*.png`. `v1` is the screen before this pass and `v8`
the accepted one (section 5 has the versions between). The ones referenced here are copied into
`evidence/`.

## 1. What was wrong

Taken from the owner's screenshot (NEMU, Hero Strike, a small window) and the `v1` captures at
1600x680, 1920x1080, 1280x720, 960x540, 1280x960, 2560x1080 and 877x482.

| # | Fault | Evidence | Cause |
|---|---|---|---|
| 1 | The ability's cooldown line drew over its sentence ("25 s cooldown" over "Hide a ghost", "35 s cooldown" over "Imbue your held slipper"). | Owner screenshot; `CharSelect-v1-locked-1600x680.png` (Dante, ATTACKING) | `CrispUiText` draws small text at up to twice the canvas scale, but layout measures it at the native scale. A legacy font is generated at a whole pixel size (28 units at a 0.446 scale is 12.49 px, generated at 12), so the native measurement is about 4 per cent narrower than the drawn text. "ATTACKING · PROJECTILE" measured one line and drew two. |
| 2 | The hero floats above the chalk ring and its shadow. Nemu stood about a head above her own ring at every shape. | `CharSelect-v1-nemu-*.png` | The ring and shadow were placed 40 units off the stage's bottom edge. `ModelPreview` aims at 54 per cent of the subject's own height, so a shorter hero's feet are drawn higher up the frame, and the panel aspect moves them again. |
| 3 | The "shadow" under the ring read as an empty text field. | all `v1` | It was a 420 by 46 rounded sticker at 55 per cent ink, the same shape as a form field. |
| 4 | Three hatched cells at the bottom of the Hero Strike grid. They look like locked heroes and lead nowhere. | `CharSelect-v1-nemu-1600x680.png` | The grid was a fixed 3x4 for a roster of nine. |
| 5 | Two highlighted tiles: the focus ring on Dante, the persimmon pick on Nemu. | `CharSelect-v1-nemu-1600x680.png` | No `FirstFocus`, so focus landed on the first tile in reading order. |
| 6 | After LOCK IN every tile, including the one you picked, turned hatched. | `CharSelect-v1-locked-1600x680.png` | Every cell was disabled, and disabled is hatched in the hub's state language. |
| 7 | A faint chalk line ran across the hero's waist and an old ring sat beside the seat cards. | `CharSelect-v1-nemu-1600x680.png` | The asphalt texture's random "faded court", which seed 51 put across the stage. |
| 8 | The hero shared the screen with a 380 by 610 dark ability box of nearly the same weight, mostly empty under a three-line sentence. | all `v1` | Layout. |
| 9 | An empty seat with no bot was labelled BOT. | source | `DrawSeats` fell back to "BOT" for every unoccupied seat; custom rooms can now start with chosen empty seats (`eec7970b0`). |
| 10 | In the untimed lobby route your card read LOCKED, carried over from the lobby's ready flag. | `CharSelect-v1-classic-1600x680.png` | The state word did not know which route it was on. |
| 11 | A second timed pick screen in one session opened with your card reading LOCKED beside a SELECT you had not pressed. | `CharSelect-v7-larger-1280x720.png` (the old screen had the same rule) | Your own lock was read from the lobby's ready flag, which outlives the screen that set it. On a timed screen it is now this screen's own lock, which is what the host's start test already read. |

## 2. What the references do

Sources read this session are linked. Rows marked *familiar* describe long-standing, widely
documented behaviour of those screens that was not re-inspected in this session.

| Pattern | Seen in | Source |
|---|---|---|
| The pick stands large in the middle of the screen; the name is the biggest word. | Valorant, Overwatch, Apex, Marvel Rivals | *familiar*; `docs/reports/ui-hud-review-2026-09-23/research.md` |
| Clicking a face picks it at once; a separate LOCK IN commits. Teammates see your current pick before you lock. | Valorant, League | *familiar* |
| Picking IS committing until the clock ends. | Overwatch | *familiar* |
| The commit button sits directly under the grid it commits. One filled primary per screen. | Valorant, Overwatch | `research.md` finding 7 |
| The clock is a plain big number near the decision; Overwatch writes its job beside it ("ASSEMBLE YOUR TEAM: 19"). | Valorant, Overwatch | `research.md` finding 4 |
| The team is a strip of cards: portrait, name, and whether they have locked. | Valorant (left column), Overwatch (top row) | *familiar* |
| Names stay visible without hovering; the lineup was rearranged because it was getting crowded. | Overwatch | [Bailey Kalesti, Overwatch UI](https://www.baileykalesti.com/overwatch) |
| What the pick DOES is one fixed block: ability icons with their keys, the focused one's sentence. | Valorant, Apex | `research.md` finding 3; *familiar* |
| Decorative elements that did not serve a purpose were removed for legibility; higher contrast, simpler shapes, a clearer call to action. | Valorant 5.08 | [Riot, Preview the future of VALORANT's interface](https://playvalorant.com/en-us/news/game-updates/preview-the-future-of-valorant-s-interface/) |
| If the clock runs out, you play what you had selected. | Apex | [EA Help, Apex Legends characters](https://help.ea.com/help/apex-legends/apex-legends/apex-legends-characters-abilities-guide) |
| Team information (Team-Ups) is checked during selection, not after. | Marvel Rivals | [Epic Games Store, Marvel Rivals tips](https://store.epicgames.com/news/marvel-rivals-tips-to-help-you-win-matches) |
| Each player has a physical token they put on the portrait they choose; per-player panels and CPU settings live in the player's own panel. | Super Smash Bros. | [SmashWiki, Character selection screen](https://www.ssbwiki.com/CSS) |
| A lock-in is a moment (animation, voice line), not just a state change. | Valorant, Overwatch, Apex | *familiar* |

What does not transfer: role-grouped rosters (TUMP heroes have no Tank/Damage/Support roles,
and Hero Strike allows duplicates), duplicate blocking, bans, voice barks, and the blue-grey
chrome all three shooters use (`CLAUDE.md` section 6.4).

## 3. How it was translated, and where TUMP's own identity comes in

| Reference idea | What TUMP does instead |
|---|---|
| Pick in the middle, info in one column, commit under the grid | The hero now stands centre stage. Name, kit row and the focused ability sit in one left column with no box. The grid sits directly on SELECT, bottom right. |
| The agent stands on a stage | The hero stands in the **can's chalk circle on the street at night**: the circle and a soft ink shadow are measured onto the drawn feet every frame (`ModelPreview.TryGroundViewport`), and every hero stands on one floor line (`StandOnFloor`). The street light's pool follows the circle. |
| Team strip with hover and lock states | Seat cards: a face still being chosen is drawn faint, a locked one solid. Bots show the bot mark (their hero is decided at install, so no face is invented). Empty seats read OPEN. |
| Smash's token on a portrait / the lock-in moment | **Locking in puts your tsinelas down on your seat card**, the brand's own slipper slapped onto your portrait the way a player leaves a slipper at the line to say they are in, and the chalk circle is re-chalked in gold. Only your card gets it: the tsinelas means "this one" and stops meaning anything when it repeats (`BrandMarks.Mark`). |
| Marvel Rivals' "check the team now" | Your seat says **TAYA IN ROUND n**, chalked on the road over it. It is the game's own word and its own rule (`MatchRules.DefenderSlotFor`: seat N defends round N + 1), fixed before the match, and it tells a role-kit player which power they will need first. |
| Ability keys beside the icons | The cost line reads the live binding for the device in hand (`Hud.KeyLabelFor`), "Press E  ·  25 s cooldown", and says no key on a phone. |
| Overwatch's always-visible names | Kept as the owner already asked: roster tiles are faces only, the name is said once, big, in the column. |
| Valorant's legibility pass | The dark ability box, the pill shadow, the dead grid cells and the stray chalk court are gone. |

Unchanged on purpose: the pick rules and messages (`SelectLobbyPickServerRpc`, the ready tally,
host-only start), the 30 s clock and its persimmon last five seconds, SELECT and LOCKED IN, the
unowned-hero hatch online, and no way out of a timed screen.

## 4. What changed in code

- `CrispUiText`: when the native and the crisp generation wrap a label onto different numbers of
  lines, it draws at the native scale, so the picture matches the layout. Labels that agree keep
  the finer glyphs. This is the root cause of fault 1 and applies to every hub label.
- `ModelPreview.StandOnFloor` and `TryGroundViewport`: one floor line for every person, and the
  drawn position of that floor for whatever wants to stand something on it. Sizes are unchanged.
- `HubSurface.FadedCourt`: the night court leaves out the random faded chalk court.
- `HubCharacterSelect`: the layout above; grid sized to the roster; `FirstFocus` on the picked
  face; locked state fades the other faces and keeps yours lit; kind and cost are two one-line
  labels stacked by measured height and re-stacked when any of them outgrows its box.
- `MatchArrivalFlowTests`: the review captures, now with a picture check on every shot (each
  ability line drawn inside its own box and not overlapping the next, and the lowest drawn row of
  the hero, its toes, inside the chalk circle's outline), and the dead-cell check.
- `ModelPreviewTests.ClassicCharacterSelectDrawsTheGodotCastAndBackdrop` looked for the retired
  "Floor" sticker as proof of the warm stage; it now looks for the chalk circle and the shadow.

## 5. Results

The review captures went through five versions before they were accepted; each one's fault
is recorded because two of them were real findings rather than tuning.

| Version | What it showed | What changed |
|---|---|---|
| `v1` | The screen before this pass (section 1). | |
| `v2` | New layout. The floor check failed at 1280x720 (toes 0.065 of the stage below the ring's centre against a 0.05 tolerance). | The check tested the wrong thing: the lowest drawn row is the toes, which stand in front of the floor point. It now asks the real question, whether the soles are inside the ring's outline. |
| `v3` | Feet inside the ring at seven shapes for Nemu and Dante; the LOCKED shot had Dante's toes on the ring's front edge. | `LOCK IN` re-applied the framing mid-idle, and a skinned mesh's local bounds follow the animation, so the floor was re-measured above the soles. The floor is now the posed box's bottom, LOCK IN no longer re-frames, and focus follows the pick. |
| `v4` | Still 0.01 short on the LOCKED shot without any re-frame from this screen. | Every canvas resize re-frames `ModelPreview`, sampling a different idle pose each time. `StandOnFloor` now measures the floor and the framed height once per subject. The ring is drawn 0.55 of the stage height wide and further forward, measured against Dante (toes 0.06 below his floor point) and Nemu (0.04). Your own card stops fading your face. |
| `v5` | Passed, 12 captures, every brief shape with the bounds pass and the picture check. | The lock-in stamp read as a paperclip at 64 units, so it became the brand's own tsinelas (`tsinelas_mark.png`) on your card only. |
| `v6` | The whole `MatchArrivalFlowTests` fixture: 4 passed, 6 failed. Five failures were the occupied LAN port (section 6). The sixth was this pass's own regression: with Larger text, Paete's role line wrapped to 104 units in a 59-unit box. | The role line has its own 840-unit box, fitted to one line. |
| `v7` | 3 of 3 passed (all heroes' kits at normal and Larger text, the review captures, the sidegrade case). The tsinelas covered the L of LOCKED, and fault 11 showed up. | Stamp moved to the portrait's outer corner; own lock read from this screen. |
| `v8` | **Accepted.** 3 of 3 passed (`evidence/run-v8.xml`): `CharacterSelectionExplainsItsKitWithoutPausingTheClock` (every Hero Strike hero and every kit slot at normal and Larger/High contrast text, the five brief shapes with the bounds pass, the clock running while reading), `CharacterSelectionReviewCaptures` (12 shots, each with the picture check), and `ClosedSidegradesShowTheAuthoredSkillDespiteASavedUnlockedAlternate`. | |

Accepted pictures, all in `evidence/`: `CharSelect-v8-nemu-1600x680.png` (the owner's window
shape), `-nemu-1920x1080`, `-nemu-1280x960` (4:3), `-nemu-877x482` (the owner's screenshot size),
`-dante-1600x680` (the overlap reproducer), `-locked-1600x680`, `-larger-1280x720` and
`-classic-1600x680`. `owner-report-nemu.png` is the screenshot that started this.

Floor measurements from `v5` (fraction of the stage height; the ring's half height is about 0.08):

| Shot | Lowest drawn row (toes) | Ring centre |
|---|---|---|
| Nemu 1600x680 / 1920x1080 / 1280x720 / 960x540 | 0.090 / 0.093 / 0.094 / 0.101 | 0.099 / 0.101 / 0.099 / 0.100 |
| Nemu 1280x960 / 2560x1080 / 877x482 | 0.100 / 0.096 / 0.097 | 0.099 / 0.099 / 0.103 |
| Dante 1600x680, before and after LOCK IN | 0.059 / 0.059 | 0.099 / 0.108 |
| Dante Larger text 1280x720 | 0.056 | 0.099 |
| Classic (Maring) 1600x680 / 1280x960 | 0.071 / 0.067 | 0.128 / 0.123 |

For comparison, `v1` at 1600x680 drew Nemu's toes at 0.26 of its 740-unit stage with the ring's
centre at 0.12 (read off the capture: toes at pixel row 440, stage bottom at 560, scale 0.63).

### The room-hosting tests, run once the port was free

An open Unity Editor held UDP 8910 (`LobbySession.DefaultPort`) for most of this session, so the
tests that host a LAN room first failed on "Failed to bind UDP socket" before reaching the
screen (`evidence/run-v6-whole-fixture.xml`). With the port freed, the selection set (the whole
`MatchArrivalFlowTests` fixture, `HubFlowTests.PaeteAndSeanKeepTheirRelativeSizeInShopAndCharacterSelect`
and `ModelPreviewTests.ClassicCharacterSelectDrawsTheGodotCastAndBackdrop`, 12 tests) was run on
this branch merged with `player-room-depth-and-bots` at `368b625f9`, and the failures were run
again on `368b625f9` alone, without any of this pass:

| Test | Target `368b625f9` alone | Target plus this pass |
|---|---|---|
| The three start flows (queued, custom with vote, custom with chosen court) | not run | **pass** |
| `OnlineUnownedHeroDimsButRemainsInspectableWithoutAServiceCall` | not run | **pass** |
| The kit, review-capture, sidegrade, legacy-ready and leave-during-intro cases | not run | **pass** |
| `CustomRoomWithBotsOffRequiresFourHumanSeats` | fails, "Expected: False But was: True" | fails, same message |
| `HubFlowTests.PaeteAndSeanKeepTheirRelativeSizeInShopAndCharacterSelect` | fails, "The shop's HEROES door did not open the hero stage" | fails, same message |
| `ModelPreviewTests.ClassicCharacterSelectDrawsTheGodotCastAndBackdrop` | fails, expects `character-male-f(Clone)`, finds `bayan-redesign(Clone)` | fails, same message |

9 of 12 pass with this pass merged (`evidence/run-merged-lan.xml`), and the 3 that fail fail
identically without it (`evidence/run-target-baseline.xml`). They are inherited and recorded in
`docs/TODO.md` beside UX-1.5. Two are expectations left behind by later work (Berto became
`bayan-redesign` in `bd44c197b`, 2026-10-09; `eec7970b0`, 2026-10-17, let a host start a room
with empty seats, which the bots-off test still forbids). The shop door may be a real fault.
Because the HERO screen check fails before it reaches character select, and the Classic check
fails before it reaches the chalk circle, this pass's Paete sizes and its updated Classic
assertion are not exercised by those two until they are repaired.

**Repaired the same day under UX-1.5a** (`docs/TODO_Archive.md`): all three pass
(`evidence/ux15a-run5-accepted.xml`), so the chalk circle and contact shadow on the Classic screen
and the Paete, Sean and Cheska shares on this screen now run. On this screen Paete drew 0.832 and
0.834 against Sean's 0.799 and 0.794 (1600x680 and 2340x1080), and stayed above Sean across a whole
idle (`evidence/ux15a-run4-home-fix-idle-spread.xml`). The repair also fixed HOME resetting a live
room to the mode card's defaults, which had made the room in run 3 Classic
(`evidence/ux15a-run3-classic-card-reset.xml`).

### Still open
- A multi-peer lock-in check is still open (`docs/TODO.md` UX-1.5); this pass ran offline only.
- The ability glyphs on the kit row are placeholders like every icon in the build (`AGENTS.md`,
  2026-09-19). Their size and meaning were kept; they were not polished.
- The lock-in moment has no sound beyond the existing `ui_valid` and no hero animation; a
  per-hero lock-in pose would need animation work outside this screen.
- Not checked on a phone or with a pad in hand. The focus path (FirstFocus on the picked face,
  focus returned to it after lock-in) and the no-key cost line on touch were built for both.

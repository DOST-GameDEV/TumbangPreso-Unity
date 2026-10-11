# Nemu ultimate: PRUSISYON (direction, 2026-10-11)

Research: [research.md](research.md). Method: `docs/HERO_KIT_METHOD.md`. Stable IDs are kept:
`nemu`, `nemu_ultimate`, `AbilityGlyph.NemuSeanceVoid`. This replaces KURO: HAUNT! and the unpitched
LIGHTS OUT cutscene that arrived with QoLUpdates. Nemu's three basic skills are unchanged; they are
future work. Kuro's model, his first-person sleeve view and his basic-skill jobs are unchanged.

## 0. One sentence

**Nemu hushes the world grey, and the dead she is always half-listening to file past in procession
and step into every player.**

## 1. The kit concept: a family of ghosts (Gekko rule)

Each ghost is one character with one silhouette and one job. They share one accent: the light they
carry (candle amber). Kuro is still the pet and still does the basic skills. The procession is the
ultimate's cast.

| Ghost | Silhouette | Job | Where |
|---|---|---|---|
| Kuro | the small round shadow with a fading tail | her pet: sit, fetch, catch | basics (unchanged) |
| Lola | hunched, shawl (alampay) over the head, rosary swinging | leads the procession, sets the pace | cutscene |
| Sakristan | small, square cassock, a hand bell held high | rings the beat the dead walk to | cutscene |
| Kandila souls | child-sized sheet ghosts, two stubby hands round a candle | the possessors: one rides each player | cutscene and play |
| White Lady | very tall, faceless veil, long hair, no feet | the scale of the parade; looks at the lens last | cutscene |
| Paru-paro | black moths | the souls' messengers; the near layer at the lens and round the possessed | cutscene and play |

All are blocky, typed part by part, flat-faced like the cast (two ink eyes, no mouth unless a beat
needs one). Bodies are pale lilac grey, translucent, never pure white; eyes are her violet; flames
are candle amber.

## 2. Mechanics (numbers live in `Core.NemuRules`; every one is asserted by `Core.Tests`)

**PRUSISYON** (`nemu_ultimate`, 15 points, unchanged cost). After the cutscene every other player is
**POSSESSED** for 15 s of round time (owner's number). Nemu is never possessed by her own cast.

### 2.1 The contest (the Jockey rule)

The ghost does not take the stick. It adds its own pull to whatever the player asks for, and the sum
is clamped to a full walk:

`wish = clamp1(input + grip x pull(t) x lure(t))`

| Phase | Pull | What it feels like |
|---|---|---|
| Settle, first 0.6 s | ramps 0 to 0.42 | the rider finds its seat (the cutscene already showed it entering) |
| Drift | 0.42 | hands-off you walk at 42 % where it wants; fighting it straight on you still make 58 % of your speed |
| Tell, 0.5 s before each surge | 0.20 | it inhales: the pull slackens, the rider rears and its candle flares. The read |
| Surge, 0.75 s | 1.05 | it lunges: hands-off you are walked at full speed; fighting straight on you are HELD (5 % backward), never thrown |
| Leave, last 1.2 s | ramps to 0 | it climbs out |

Six surges per possession, typed per seat so four victims never lurch on the same frame.

**Grip** (the skill expression): starts at 1.0. While the player pushes against the pull (input
at least half a stick and pointing more than 110 degrees away from it) grip falls 0.12 per second, to
a floor of 0.45. Otherwise it recovers 0.05 per second. A player who fights the whole time ends
with a ghost that pulls at less than half strength: steady fighting nets 81 % speed and a surge nets
53 %. Grip is local to the body's own simulating peer (like the stick itself) and never decides an
outcome on the host.

### 2.2 Where it pulls (the lure, the caster interaction)

The ghosts always pull toward the worst place for their host, and that is always good for Nemu:

- **An attacker** is pulled toward the taya. If Nemu is the taya, her prey walks to her; if she is
  attacking, her rivals walk into the tag.
- **The taya** is pulled away from the can. If Nemu is attacking, the can is left open for her.
- No taya on the court: toward the can.

The direction sways up to 28 degrees round the lure (a slow seeded sine), and straightens during a
surge. Everything is a pure function of seat, elapsed status time and replicated positions
(`NemuRules.PossessionPull`), so every peer draws the rider leaning the same way and the victim can
predict it.

### 2.3 What it never does (fairness)

- Never a stun: throwing, picking up, skills, jumping, shoving, sprinting and stamina all work.
- Never the camera: the player's view is theirs; only the picture is treated.
- Never a takeover: at full grip, pushing against a surge holds you in place.
- Never stacking: a second cast refreshes to 15 s (`Max`), never adds.
- Stunned, tagged, frozen, rooted or feared bodies feel no pull (the existing status wins).
- Removable by cleanse and refused by status immunity (Geo shield and Carapace are counterplay).
- Round end, reset and rejoin clear it like every other status.

### 2.4 Bots

Bots press the same buttons, so the same contest applies to them and their goal-seeking already
pushes against the pull. Nemu's bot casts PRUSISYON when at least two opponents are on court and the
round has more than eight seconds left.

## 3. Network

- `StatusKind.Possessed = 14`, appended. `SyncUnit` appends `PossessedLeft` after `ZappedLeft`
  (bounded 0 to 15, finite). The protocol is bumped because the wire and the ultimate's semantics
  change. Peers with the Haunt cannot join.
- The host applies POSSESSED to every eligible body when the shared ultimate activates. Clients only
  receive it. The body's own peer computes the pull from the replicated clock and positions.
- The old Haunt chase is removed. `FamiliarEffect` stays on the wire for compatibility and is
  ignored for the ultimate (`ReceiveHaunt` returns false; Kuro never devours).
- Replay: statuses already ride the recorded unit state where present; the rider is presentation
  read from `IsPossessed`, so a replay that carries the status draws it.

## 4. The cutscene: 5.8 s, three shots, one travelling light (owner: *"below 6 seconds"*)

The travelling thing is **candlelight**, moving left to right across the screen in every shot.

| Time | Shot | Picture | Sound | Reference |
|---|---|---|---|---|
| 0.00 | A THE HUSH (close, straight on, slow push in) | a black moth crosses the lens left to right; Nemu is gazing off, Kuro bobbing at her shoulder | room tone | Castorice moths, open on the element |
| 0.15 | A | her eyes come round to the lens | | her canon look |
| 0.40 | A | finger to her lips: *shh* (punch) | a breath | Arlecchino's hush |
| 0.50 | A | THE GREY: a ring of grey sweeps out from her feet over the court and up the walls; the grade drains to 18 % saturation | everything ducks to near silence | Acheron's monochrome |
| 0.55 | A | | one agunyas toll | the motif |
| 0.70 | A | Kuro flinches and hides behind her head | | character beat: even her ghost is scared |
| 0.85 | A | a tiny candle flame kindles at her fingertip | a match hiss | the accent arrives |
| 1.05 | A | the flame drifts off screen right | | the travelling light starts |
| 1.20 | B THE PROCESSION (wide, low, trucking left to right) | the flame lands in Lola's candle; the dead rise out of a grey mist bank on the left and file across behind Nemu, in step | footsteps without feet: a soft shuffle on the beat | Pom Poko lockstep, prusisyon |
| 1.55, 2.05, 2.55 | B | the Sakristan's bell rings; every candle bobs on the beat | hand bell | |
| 1.3 to 2.9 | B | the White Lady glides at the back, a head above the line | a low choir hum swells | scale contrast |
| 2.95 | B | EVERY head turns to the lens at once; every flame flares (punch) | the hum cuts dead | Acheron's held beat |
| 3.30 | C THE ENTERING (one continuous move) | every candle snuffs together; a smoke thread rises off each; near dark, only violet eyes | a single breath out | |
| 3.42 | C | the souls burst into amber-tailed wisps streaming at the real targets; the camera rides the lead stream out from behind Nemu | rushing whispers, panned with the stream | Paete's TAKE rule: follow it to the people |
| 4.15 | C | the wisps hit: every target jolts (shock clip), a puff of moths bursts off each | thumps, staggered | |
| 4.30 to 5.00 | C | a Kandila soul re-forms on each target's shoulders, hands over their eyes; heads loll; the camera swings wide | | the Jockey silhouette |
| 5.25 | C | every rider's candle relights at once; every target's eyes glint amber | the toll again, softer | the motif |
| 5.55 to 5.80 | C | ink closes in from the frame edges to black | | hand-back out of the dark |

Nobody else on court: the wisps dive into the court at three typed spots and no rider is shown. No
target is invented. Play picks up with the riders already seated (never shown twice).

Grade: 1/1 until 0.45; to brightness 0.80, saturation 0.18 by 1.0; a dip to 0.45 at the snuff; back
to 0.80/0.18; the iris finishes it.

## 5. In play: VFX that says what is happening

| Question a player has | Answer on screen | Who sees it |
|---|---|---|
| Who is possessed? | a Kandila soul riding their shoulders, hands over their eyes, two black moths circling them, the POSSESSED icon overhead | everyone |
| Which way are they being dragged? | the rider leans and reaches that way; a short trail of faint grey footprints appears on the court ahead of them in the pull direction | everyone |
| Which way am I being dragged? | ghost hands reach in from THAT edge of my screen | the victim |
| Is a surge coming? | the rider rears back and its candle flares; the footprints brighten; my screen's hands tighten; an inhaled whisper | everyone (the whisper is the victim's) |
| Is it surging now? | the rider thrusts its arms forward with a moth streak; the hands yank; a breath push and a heartbeat | everyone / victim |
| Am I winning? | as grip falls, the rider flickers and its candle gutters; my screen's hands slip back and fade | everyone / victim |
| When does it end? | the icon's timer; the last 1.2 s the rider climbs out, its candle snuffs and a smoke thread rises | everyone |

The victim's screen also goes grey (saturation 0.35, brightness 0.86) under a breathing ink
vignette. Reduced effects keeps every shape and drops flicker and flashes. Effects family rules:

1. Nothing appears from empty air: souls rise from the mist, re-form out of their own wisps, and
   leave by climbing out and snuffing.
2. One accent: candle amber. Her violet only in eyes. Bodies pale lilac grey. Never white.
3. Small on the court: a rider is a body's size; the footprint trail is 1.2 m. The can, chalk and
   every player stay readable.
4. Everything is posed from a clock (the scene clock in the cutscene, the status clock in play).

## 6. Sound (`tools/build_nemu_audio.py`, numpy, seeded)

Instruments: `toll` (agunyas bell, inharmonic partials, long tail), `bell` (hand bell), `hum` (a
low choir of detuned formants), `breath` (shaped noise in/out), `shuffle`, `whisper`, `snuff`,
`heart`. Cues: `sfx_ult_theme_nemu` (the cutscene, one file timed to section 4), `sfx_possess_enter`,
`sfx_possess_exit`, `sfx_nemu_possess_tell`, `sfx_nemu_possess_surge`, and the victim's loop
`sfx_nemu_possessed_loop`. No voice.

## 7. Files

Core: `NemuRules.cs` (+ tests), `StatusRules.cs` (append). Runtime: `NemuHeroKit.cs` (the ultimate),
`CharacterMotor.Status.cs`/`CharacterMotor.cs` (status and contest), `Net/MatchRpc.cs` and
`NetSession.cs` (wire), `UI/StatusIcons.cs`, `Visual/PossessedPresentation.cs` (rider, moths,
footprints), `Visual/PossessedScreen.cs` (victim HUD), `Visual/HeroIntroductionScene.Nemu.cs` (+ a
procession partial), `tools/author_ultimate_intros.py` (`nemu()`), `AIController.cs`, audio builder.
Docs: TODO entry, `SKILL_NETWORK_CONTRACT.md`, `Hero_Strike_Balance.md` row, this file.

## 8. Acceptance (what is evidence and what is not)

- `dotnet test` for every number and the pull function (resisting holds against a surge, hands-off
  drifts, grip floor, lure for taya/attacker/no-taya, deterministic across calls).
- A PlayMode check on a real map: cast, every other body POSSESSED for 15 s, Nemu not, a hands-off
  body drifts toward the lure, a resisting body holds, a stunned body does not move, cleanse clears.
- The cutscene sampled at every beat, its length below 6 s, the clock frozen.
- Film of the caster's screen and a possessed player's screen. Compilation is not acceptance.

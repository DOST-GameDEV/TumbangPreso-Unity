# Can the taya tag you right now? (2026-10-23)

Owner: "improve understandability of the game … shit like (pedeng mataya, di pede mataya etc) intuitive dapat",
"maybe use pacman as reference (whenever ghosts are vulnerable and not?)", "we need to make it more obvious kasi
whenever the defender can taya someone and when they cant", "thoroughly think abt how to execute it without making
game more complicated", "maybe kapag pede mataya nagkakasomething sila? color change or smthhh idkkk", and
"think abt how it should look like too for spectators, and other players … its fine if they all see diff shit, what
matters is they can comprehend what can happen and whats happening better".

No rule changes. This makes the rule that already exists visible.

## The rule, as the code has it

`CharacterMotor.IsTaggable()` says whether an attacker's state exposes them: the round is live, they are not
tag-immune (`AbilitySystem.IsImmuneToTags`), not aloft (Amihan's Updraft), and either Vulnerable (taggable anywhere,
5 s, `StatusRules.VulnerableSeconds`) or holding their slipper inside the box. A tag only lands if the can is also
standing (`RoundDirector.TryResolveTag` checks `Lata.IsUpright`).

So the player-facing states are:

| State | When | What it means |
|---|---|---|
| SAFE | In the box without a slipper, or outside the box | The taya cannot get you. |
| TAGGABLE | In the box holding your slipper, can standing | One touch from the taya and you are caught. |
| TAGGABLE ANYWHERE | Vulnerable, can standing | Same, and leaving the box does not help. |
| CAN DOWN | The can is knocked over | Nobody can be tagged until the taya stands it back up. |

## What the game showed before this pass, and what was wrong

| Cue | Who saw it | Can-aware? |
|---|---|---|
| Pulsing Defense-blue rim on the body (`CharacterVisual.Readability`) | Taya only | **No** |
| Four ink brackets replacing the nameplate ring (`CharacterNameplate.Catchable`) | Taya only | **No** |
| VULNERABLE row in the status stack (`StatusStack`) | That attacker | **No** |
| Three blue arcs on the ground (`TayaIdentity`) | Everyone | Yes |
| Blue screen-edge frame, orange for the taya while the can is down (`TumpHudEffects`) | That player | Yes |
| Chalk arms and points to the nearest exit (`CourtBoundaryPresentation`) | Everyone / that player | Yes |
| Taya reach reticle lights on a catchable target in the cone (`TumpMatchReadout.Reticle`) | Taya | Yes |
| Taya ring breaks into grey dashes, headband dims, while the can is down (`TayaIdentity`) | Everyone | Yes |

Three faults, read off the code and the review frame `Logs/taya-identity/state-taggable.png`:

1. **Two answers to one question.** The rim, the brackets and the VULNERABLE row asked `IsTaggable()` alone, so
   while the can was down the taya was told "catchable" about someone a tag could not land on, and an attacker read
   VULNERABLE while they were free. Everything else asked `IsTaggable() && Lata.IsUpright`.
2. **Nobody but the taya saw it on the body.** From an attacker's or a spectator's camera a taggable attacker
   looked exactly like a safe one, except for a thin arc around their feet that the body mostly covers and that is
   the same blue as the taya's own ring. Nobody could learn the rule by watching.
3. **No warning before a change.** Vulnerable runs out and the can gets stood back up with no warning, so the
   state flipped the instant it happened.

## What other games do

- **Pac-Man.** The whole ghost changes (blue, a scared face), every ghost the same way, and it blinks blue and white
  just before it turns back. Only the exception is marked: a normal ghost carries no "dangerous" badge. The blink
  is how a player learns the timer without reading a number.
- **Mario Kart star, Smash invincibility.** Whoever is in a special state flashes on the body, everyone sees it,
  and the flashing changes rhythm near the end.
- **Overwatch Discord Orb, Valorant Vulnerable, Dead by Daylight Exposed.** A debuff that makes someone easier to
  kill goes on that body for everyone who can act on it, with a matching icon on that player's own HUD. Colour
  always comes with a shape or a motion.

What carries over: put the signal **on the body**, make it the **same for every viewer**, mark **only the exception**,
and **blink before it changes**.

## The design: one function, one fill on the body

### One function for "can be tagged right now"

`RoundDirector.CanBeTaggedNow(victim)` = round live, `victim.IsTaggable()`, can standing. `TryResolveTag` asks it, and
so does every cue. A cue can no longer disagree with the tag.

### The body fills with the colour, on every screen

Owner, later the same day: "they get slowly covered by a color highlight everytime theyre taggable", "it climbs up
their body really quick".

A taggable attacker fills with the cue colour **from the feet up**. The fill line climbs to over the head in 0.22 s,
with a bright band riding the line while it moves. Below the line, the body takes the colour (keeping its own light
and shade so the face still reads), the rim lights, and the black ink outline turns the colour and thickens to 2.2x.
Once full, it pulses. When they become safe it drains back down in 0.3 s. It shows on **every** gameplay camera: the
taya, the other attackers, spectators, and the attacker's own body when the camera shows it. The three arcs under them
stay, now outside the seat ring, which used to draw over them.

The default colour is the taya's blue: Defense blue already means "the taya can get you" (the screen frame, the arcs),
so filling with it says "the taya can claim this one". Offense orange already means "the attackers are free".

### The fill level says how close a flip is

The fill is a level, not a switch. Everything reads it, so the Pac-Man warning comes without a blink:

- **Taggable now:** full.
- **The can is being stood up:** an attacker who will be taggable the moment it stands (in the box with their slipper,
  or Vulnerable) fills **with the taya's restore channel**, so the colour climbs as the danger returns and reaches the
  head the instant the can is up. This is the moment an attacker most needs to know to get out.
- **Vulnerable running out** (the last second, and only if that will make them safe): it drains with the time left.
- **Safe:** empty.

The channel is 1.3 to 1.8 s, so the warning is short and honest: it starts when the restore really starts.

### Safe is the default look

Most of the time three attackers are safe; marking all three would bury four silhouettes in a 14 m box (VISION
section 2, the readability budget). Pac-Man marks only the frightened ghosts. Safe is what an unmarked body means, and
the change into it is visible: the colour drains down, the arcs spin open, and leaving the box with a slipper already
puffs chalk (`CourtEscapePuff`).

### What each viewer sees

| | Taya | Another attacker | That attacker | Spectator |
|---|---|---|---|---|
| SAFE | Plain body | Plain body | Plain body; no frame | Plain body |
| TAGGABLE | Full colour, arcs, nameplate brackets, reach reticle lights in range | Full colour, arcs | Full colour (if their body is in view), frame, VULNERABLE row, nearest exit on the chalk | Full colour, arcs |
| TAGGABLE ANYWHERE | As TAGGABLE, plus the moonlight already on Vulnerable bodies; drains in the last second | Same | Same, frame fades with it | Same |
| CAN DOWN | Nobody coloured; own ring grey and broken, band dim, orange frame | Nobody coloured; taya's ring broken | No frame, no VULNERABLE row | Nobody coloured; taya's ring broken |
| CAN BEING STOOD UP | Exposed attackers fill as the restore collar fills | Same | Frame rises with the channel | Same |

Everyone sees the same thing on the bodies. Only the private extras differ (the frame, the row, the exit point, the
brackets and the reticle), and each is about that viewer's own situation.

### The colour is a setting

Owner: "make it changable in settings too with diff colors". Accessibility, **Taggable colour**: Taya blue (default),
Magenta, Yellow, Cyan, White (`Settings.TagColours`). It recolours the fill, the outline, the arcs, the taya's
nameplate brackets and the attacker's frame. Like the slipper highlight it is local and never replicated: each player
picks their own. Red and orange are left out, because orange already means "the can is down, the attackers are free".
There is no Off; `WorldCueProfile.TayaTarget` stays the authoring switch.

### Accessibility and comfort

- `WorldCueProfile.TayaTarget` still scales the fill, the arcs and the brackets; at 0 they are off.
- Colour is never alone: the fill climbs and pulses (motion), the outline thickens and the arcs bracket the feet (shape).
- Reduced UI motion: no climb, no pulse. The fill jumps to its level.
- Nothing flashes: the fill is a level, not a blink.

### Networked play

Every input already reaches every peer: position and `HoldingSlipper`, `VulnerableLeft`, ability immunity, `IsAloft`,
`Lata.IsUpright`, and the restore ratio (`LataClockPresentation` reads the host's 10 Hz snapshot on clients). The cue
decides nothing and changes nothing on the wire; the colour setting is local.

## How the look was refined

Each round was judged on frames from the taya's first-person view, an attacker's and a spectator's (`Logs/tag-readability`).

1. The taya-only rim, opened to every camera: on flat block faces a rim lights only thin edges, and from a spectator's
   distance a taggable runner looked the same as a safe one.
2. Whole-body tint (42 %) plus a blue, thicker outline: visible side by side, but skin went pink rather than blue.
3. Tint 68 %, outline 2.2x, and the arcs moved outside the seat ring (at the same radius the ring drew over them).
4. The owner's climbing fill replaced the blink: the level now carries the warning, and the change itself is animated.

## Not changed

- No rule, timing or balance. Bots still read `IsTaggable()` alone in their own decisions (`AIController`); that is
  behaviour and is left as it is.
- `CourtBoundaryPresentation.IsThreatened` already asked the same question and keeps its signature for recorded
  replays.

## Evidence

Filled in after the review: test names and results, frames per state per viewer, the build and its videos.

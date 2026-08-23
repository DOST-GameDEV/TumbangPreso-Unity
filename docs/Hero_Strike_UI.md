# Hero Strike UI: the design, and why each rule exists

The look of Hero Strike's HUD, its hold-to-read tray and its character select ribbon. Written
2026-08-23 after the first pass shipped a palette that fought the game's own brand.

`docs/Art_Direction.md` § 1 is the law above this file. Where the two disagree, that one wins.

---

## 1 · What went wrong, so it is not repeated

The first pass built the hero UI from a **slate blue glass** look copied out of a modern hero
shooter: `rgba(16, 22, 34, 0.90)` plates, `rgba(61, 82, 112, 0.60)` rims, near-white glyphs.

🧑 2026-08-23: *"i lowk dont get why we use light blue and shit in some parts of ui, it doesnt
really look good with brown"*. Correct, and it is a rule violation rather than a matter of
taste. Two separate faults:

**Fault 1: seventeen colours were named outside `UiTheme`.** `Art_Direction.md` § 1 ends with
*"`ui_theme.gd` is the only place a colour is named. Read it, never restate it."* The hero UI
restated seventeen `new Color(...)` literals inline across `Hud.cs`,
`AbilityInspectPanel.cs` and `ConvertedCharacterSelect.cs`. A colour that is not in the palette
file cannot be checked against the palette, so nobody noticed the whole hero layer had drifted
into a different hue family from the rest of the game.

**Fault 2: the hue family was the cold one.** The brand is the wood set: `WoodDark #1d0e06`,
`WoodDeep #31190b`, `WoodEdge #8b5227`, `Cream #f5e6c8`, `Amber #ffba00`. Brown, tan, gold,
cream. The slate blue plates sit on the opposite side of the wheel from every one of them, so
the hero deck read as a panel from a different game pasted over this one. It looked worst
exactly where it mattered: against the wooden scoreboard and clock, which are on screen at the
same time.

**The correction is not "make it browner".** It is: the hero UI has no palette of its own. Its
chrome comes from the wood set, its accents come from the five hero constants, and both live
in `UiTheme`.

---

## 2 · The chrome, and the constants that are all of it

### ⚠️⚠️ Corrected once already: the wood set was wrong here too

The first fix for the slate blue swung the whole hero layer onto the **wood set**, and that was
wrong as well. 🧑, looking at it beside an Overwatch frame: *"the brown shit looks ugly. kinda
wanted just the icons like in overwatchh or something"*.

**The lesson is that menu chrome and combat chrome are different jobs.** A menu panel is
FURNITURE: it is the thing you are looking at, it can be opaque, and the painted wood is the
brand. A combat overlay is a WINDOW: the thing you are looking at is the court behind it, so its
job is to disappear and let the glyph on it read. Every shooter in this lineage lands on the same
answer, which is a translucent near-black with a bright rim.

So the plates are near-black at 0.55 to 0.72 alpha: warm enough not to read as a foreign object
beside the wooden scoreboard, neutral enough to carry no hue of their own.
`TheHeroChromeCarriesNoHueOfItsOwn` asserts the SATURATION rather than an exact value, because
that is the property that actually matters and it is what the slate blue failed.

**And the deck has no container at all.** Three floating squares say "these are a group" for
free; a plate says the same thing and costs a slab of furniture across the bottom of the frame.

⚠️ **Removing that plate is what broke the HUD once already.** `new GameObject("HeroDeck")` had
been getting its `RectTransform` for free from the `Image` on the next line. Deleting the plate
deleted the rect, `Hud.Build` threw out of `Awake`, and the scoreboard, the deck and the
crosshair all vanished at once. `HudBuildTests` covers it now.

### The constants

Named in `UiTheme` under HERO STRIKE CHROME. Nothing here is invented at a use site:

| Constant | Value | Where it is used |
|---|---|---|
| `HeroPlate` | near-black at 0.72 | The inspect tray and the objective banner |
| `HeroPlateRaised` | near-black at 0.55 | A single ability tile |
| `HeroPlateSunk` | black at 0.55 | The meter groove inside a tile |
| `HeroRim` | `Cream` at 0.26 | A resting border |
| `HeroRimLit` | `Cream` at 0.95 | A border on a power that is UP |
| `HeroGlyphOn` | `Cream` at 1.00 | A glyph that is available |
| `HeroGlyphOff` | `Cream` at 0.20 | A glyph that is not |
| `HeroNumber` | `Cream` at 0.96 | The countdown and the charge figure |

⚠️ **The glyph is CREAM, not pure white.** Cream is the brand's own paper colour and it keeps
the deck related to the wooden scoreboard sharing the screen with it, at no cost to contrast:
on a plate this dark, cream and white are the same read.

⚠️ **Unavailable is CREAM AT LOW ALPHA, not a grey-blue tint.** The old
`rgba(179, 191, 217, 0.25)` was a cool grey, which is a second hue family arriving through the
back door on the one state the player looks at most (a skill on cooldown). Dropping alpha on
the colour that is already there cannot introduce a hue.

⚠️ **There is no seventh chrome colour and adding one is the failure mode.** Every state below
is drawn from these plus the hero accent. A state that "needs its own colour" is a state
that has not been designed.

---

## 3 · The five hero accents, and the one law they had been breaking

`Art_Direction.md` § 1: **orange `#f87020` means OFFENSE, blue `#0080e8` means DEFENCE, and
nothing else in the frame may sit near those two hues.** They track the role, which rotates
every round, so they are the only two colours a player must READ rather than merely see.

Two of the five hero accents were sitting on top of them:

| Hero | Old accent | Hue | Role hue it collided with |
|---|---|---|---|
| Dante | `#ff6d00` | 26 | `Offense` at 22, four degrees away |
| Cheska | `#00e5ff` | 187 | `Defense` at 207, twenty degrees away |

Dante's is the serious one. His accent was a saturated orange fill placed next to other
saturated orange fills that mean "this player is an attacker".

### The set that ships

| Hero | Identity | `Hero*` | `Hero*Bright` | Hue | Nearest role hue |
|---|---|---|---|---|---|
| Sean | Solar Striker | `#ff3355` | `#ff8fa3` | 349 | 27 from Offense |
| Zack | Thunder Speedster | `#e8f53a` | `#f6ffa0` | 66 | 44 from Offense |
| Dante | Earth Juggernaut | `#3fa65c` | `#8fe0a0` | 136 | 71 from Defence |
| Cheska | Frost Control | `#5fe8d0` | `#b8fff2` | 170 | 37 from Defence |
| Nemu | Void Disruptor | `#b44dff` | `#dfaaff` | 275 | 68 from Defence |

Smallest gap between any two heroes is Dante and Cheska at 34 degrees, and they are further
separated by lightness: jade at L 45 against mint at L 64. Every other pair is 70 degrees or
more apart, which is the spacing a 60 px tile rim needs.

### ⚠️ Dante is GREEN, and that is the one decision here worth arguing with

His kit is magma: a stomp that cracks lava, a carapace of molten armour, a fissure. Orange is
the obvious accent and it is the one colour he cannot have.

The resolution is that **his accent is the colour of the crust, not of the melt.** The rim, the
tile and the reticle are basalt jade; the fissure light, the ember particles and the magma core
inside them stay hot orange. Dark green stone glowing orange in the cracks is a real look and a
better one than a flat orange plate, and his ultimate already builds basalt pillars at
`#47332a`, so the stone half of his fiction was on screen before this.

If this is ever reverted, it is one line in `UiTheme` and one row in this table, but the thing
it must be reverted TO is not `#ff6d00`. That value is unavailable.

---

## 4 · The three states, and why none of them is a word

An ability is in exactly one of three states and the player has to know which one at a glance,
mid-fight, without reading. The deck therefore encodes state in **rim, glyph and one number**,
never in a label.

| State | Rim | Glyph | Centre | Meter |
|---|---|---|---|---|
| **Ready** | hero accent, full | `HeroGlyphOn` | nothing | full, hero accent |
| **Cooling** | `HeroRim` | `HeroGlyphOff` | seconds, `Amber`, bold | draining, `Amber` |
| **Active** | hero accent, breathing | `HeroGlyphOn` | seconds, `Highlight` | duration ratio |

⚠️ **READY PRINTS NOTHING IN THE CENTRE.** The state a player is in most of the time must be
the quietest one on screen. A tile that says "READY" is a tile shouting at somebody who already
knew.

⚠️ **THE COOLDOWN NUMBER AND THE COOLDOWN METER SAY THE SAME THING ON PURPOSE.** They are read
at different distances: the meter is peripheral vision ("nearly back"), the number is a glance
("1.8, I can wait"). Removing either was tried and the tile got worse, not cleaner.

---

## 5 · Motion, which is a signal and not decoration

Only three things in the hero UI move, and each one means something:

1. **The ready pop.** The frame an ability leaves cooldown, its tile scales 1.00 to 1.12 and
   back over 0.18 s. This is the single most requested feedback in the whole system and it did
   not exist: a player had no way to know a skill was back except by watching a number they
   were not looking at.
2. **The ultimate breath.** A slow 1.4 s pulse on the rim while the ultimate is ready, and
   nothing while it is charging. A charging meter that also pulses is a meter that has taught
   the player to ignore pulsing.
3. **The cast answer.** See § 6.

Everything else is static. Three moving things get noticed; nine do not.

---

## 6 · The cast answer, which is the whole anti-clunk fix

⚠️⚠️ **A PRESS THAT WAS REFUSED USED TO LOOK EXACTLY LIKE A PRESS THAT WORKED.** This is what
"clunky" was. The player presses Q on cooldown, the game does nothing at all, and the only
honest reading available to them is that the input was dropped. It was not: the ability was
down, and the UI simply never said so.

Every press of a hero key now gets an answer inside one frame:

| Outcome | Answer |
|---|---|
| Cast succeeded | tile flashes to the hero accent for 0.14 s, ground telegraph confirms where it landed |
| Refused, on cooldown | tile flashes `Danger` for 0.12 s, `ui_error` at low volume |
| Refused, not enough charge | same, on the ultimate tile |
| Could not act yet (stunned, mid-recovery) | **nothing, because the press is not refused, it is buffered.** See § 7 |

---

## 7 · Buffering, and the rule for it

⚠️⚠️ **A HERO KEY PRESSED WHILE THE PLAYER CANNOT ACT IS HELD FOR 0.30 s, NOT DROPPED.**
`HeroAbilitySystem.Update` used to return before it had even looked at the intent table when
`CanAct()` was false, so a skill pressed during a stun, a stagger or a shove recovery was eaten
outright. In a game where the taya's tag is a five second stun, that is most of the presses a
player makes under pressure.

The window is 0.30 s because it is long enough to cover a stagger (0.20 to 0.35 s is the range
the shove and the trail pulses apply) and short enough that a press made a second ago cannot
come out on its own later, which is the failure buffering usually introduces.

⚠️ **THE BUFFER DOES NOT SURVIVE A REFUSAL.** A cast refused for cooldown or charge is answered
and cleared. Only "cannot act right now" is held. Holding a cooldown refusal would mean the
ability fires the instant it comes up, seconds after the player asked, at a moment they did not
choose.

---

## 8 · Telegraphs tell the truth

Every ground-placed power carries `TelegraphRadius` and `TelegraphRange` on the ability itself,
and the reticle reads them.

⚠️⚠️ **THE NUMBERS USED TO BE INVENTED IN THE HUD.** `HeroAbilitySystem.UpdateReticle` drew
7.5 m for any ultimate, 5.0 m for any first skill and 3.5 m for any second, and offset the ring
forward only when the kit happened to be Cheska's. So Dante's 2.4 m stomp drew a 5.0 m ring,
Nemu's 3.2 m void drew 7.5 m and landed 3.5 m in front of where the ring was shown. A telegraph
that lies is worse than no telegraph, because the player believes it once.

They live on the ability for the same reason `Glyph` does: a lookup table keyed by ability id is
a second place to forget, and a new hero would compile and run with three wrong rings.

---

## 9 · Layout, and the arithmetic that has to hold

The deck is **214 x 78** at `y = 14`, bottom centre, with no plate behind it. Each card is a
60 px square tile with its key in plain text underneath, outside the tile.

```
width = padding_left + padding_right + (cards - 1) * spacing + sum(card_widths)
214   = 0            + 0             + 2 * 11                + (64 + 64 + 64)
```

⚠️ **The key goes UNDER the tile, not in a chip inside it.** 🧑: *"i want the keybind for the
icons to show too"*, sent with a crop in which the corner chips were three illegible smudges.
They were 22 x 15 with 13 pt type inside a tile only 60 px across, competing with the glyph for
the same square. A key is read once, while learning, and then never again: it has to be legible
and it must cost the icon no room.

⚠️ **CHANGE A CARD WIDTH AND THIS LINE MOVES WITH IT.** A `HorizontalLayoutGroup` will happily
lay three cards out past the edge of a rect that no longer fits them, and the overflow lands
under the first-person hands where it is least visible and most annoying.

⚠️ **NOTHING ELSE MAY ENTER THE BOTTOM 90 PIXELS OR THE MIDDLE THIRD.** The middle third is the
viewmodel hands, the crosshair and the lata. `ReadyObjective` sits at `y = -210` from the top
and `ReadyPrompt` at `y = 96` from the bottom for exactly this reason, and both were moved
there after covering the hands.

---

## 10 · What the inspect tray is for, and what the deck is for

The deck carries **only what is true right now**: which power, which key, is it up. Every
sentence lives in the hold-to-read tray.

🧑 2026-08-23: *"games like valorant overwatch league etc dont clog their screen with text, to
see how abilities work they usually click a button and then let go when they dont wanna see it
anymore"*.

The split is enforced by the type: `HeroAbility.Summary` is one short line for the character
select ribbon, `HeroAbility.Description` is the full tactical sentence for the tray. The
character select card used to draw the full sentence into a 46 px box with
`VerticalWrapMode.Truncate`, so four of the fifteen powers described themselves in a sentence
that stopped mid-word.

---

## 11 · What broke while building this, and what now covers it

Three faults, all found by looking rather than by testing, and each one now has a guard.

**The branch did not compile.** `Hud.BuildAbilityCard` was missing its closing brace at HEAD
and `InputBinding.ToHumanReadableString` was reached as an extension method in a file with no
`using UnityEngine.InputSystem`. Nothing on the branch had ever run.

**Every particle emitter threw an engine assert.** `AddComponent<ParticleSystem>()` comes back
already playing and writing `main.duration` to a playing system is not supported. All four
generators in `AbilityVfx` set `duration` on the line after the component was added, so every
hero ability that spawned particles asserted. It went unnoticed because it is a LOG assert: the
effect still played and only the PlayMode runner treats an unexpected log line as a failure.
`AbilityVfx.Quiesce` stops each emitter before configuring it.

**Deleting the deck's background plate took the whole HUD down.** See § 2. The lesson worth
keeping is not about `RectTransform`: it is that **an exception in a Build method abandons every
widget after it**, so one missing type argument presented as three unrelated bugs (an empty
scoreboard, a missing deck, no crosshair) and none of them pointed at the deck. It was found in
`Player.log` after `tools/shoot_player.ps1`, which was the only check in the project that looks
at a built HUD at all. 95 EditMode and 55 PlayMode tests passed over the top of it.

⚠️ **That is why the player capture is part of the workflow and not a nicety.** EditMode does not
run `Awake`; PlayMode does not construct the in-match HUD. The .exe is the only place this
screen exists.
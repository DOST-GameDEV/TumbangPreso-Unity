# CAST-1: hold to preview, release to cast, and a cancel on every device

Owner, 2026-09-26: *"ALSO improve skill cast and skill cast indicator?"*, *"Make it easier for ppl to
understand and visualize HOW and where their skills will be cast if they HOLD"*, *"check out marvel
rivals for skill cast indicator they have a really good one"*, *"do more research on ur own but they
have really good skillshot indicator and cancel and think abt other shit that should go with it"*,
with two screenshots of Groot's wall in Marvel Rivals, then *"all characters have shityt preview and
cancel cast rn thoroughly think abt implementation of it later too"*, *"and ui for the cancel shit
idk"*, *"U figure it ALL out"*.

## 0. What the owner's screenshots show

Groot placing a wall in Marvel Rivals: a full-size translucent GHOST of the actual wall in the
hero's green, drawn as bright edges over a dim fill, standing exactly where it will grow, with
three prompts floating beside it: `Build` (left mouse), `Cancel` (right mouse), `LSHIFT Adjust
Angle`. A red marker on the floor shows the anchor point. The body keeps moving while aiming.

## 1. Research

| Game | What it does | Taken |
|---|---|---|
| Marvel Rivals (owner's screenshots; general knowledge) | a ghost of the real construct at the aimed spot, confirm and cancel prompts, rotate on a key; skills with a travel path show the path | the GHOST (the real shape at real size), the three prompts, the rotate |
| Valorant ([Barrier Orb, Valorant wiki](https://valorant.fandom.com/wiki/Barrier_Orb)) | while Sage's wall is equipped her crosshair becomes a placement indicator; recast rotates 90 degrees, alt fire gives full rotation control | a rotate on one button, and the indicator REPLACING the reticle so there is one thing to read |
| League of Legends ([Quick cast, LoL wiki](https://leagueoflegends.fandom.com/wiki/Quick_cast)) | three modes: normal (press, click), quick cast, quick cast WITH INDICATOR (hold shows the shape, release fires); range ring round the caster | our hold-to-aim is exactly "quick cast with indicator"; add the caster's RANGE RING, and let a setting pick instant cast per ability later |
| Wild Rift ([Yahoo guide](https://sg.news.yahoo.com/how-to-wild-rift-controls-button-layout-settings-pro-134837982.html)) | drag a skill button out to aim; drag onto a CANCEL button near the skills to cancel; option: releasing outside the range cancels; a drag dead zone | the touch answer: drag to aim, a cancel target that appears only while aiming, a dead zone |
| Overwatch (general knowledge) | Symmetra's teleporter and Mei's wall show a ghost; invalid placements turn red and refuse | a RED invalid state (out of the arena, inside a wall) that refuses to cast on release instead of firing somewhere else |

## 2. What is wrong today (read from source)

- `HeroAbility.AimByHolding` grows the reach with HOLD TIME (min to max over 0.55 s) along the
  facing. Where it lands is decided by how long the finger was down, which nobody can judge.
- The preview is a flat ring (`GroundReticle`, one style per hero), the same shape for a wall, a
  zone and a blink, and the yellow version the owner already called bad (SKILLUI-1).
- There is no cancel. Letting go casts; the only way out is to hold until a ceiling that fires.
- Abilities that are not hold-to-aim (dashes, cones, throws, lobs, the ultimates) show nothing
  before they fire.

## 3. The design

### 3.1 Aim with the crosshair, not with time

Every aimed ability places its anchor where the camera's centre ray meets the ground, clamped
between its min and max range (X and Z clamped independently to the playable square, `CLAUDE.md`
§ 4). FPP and TPP both have a crosshair. The pad's right stick moves the camera exactly as it does
now, so it aims the same way. Hold time stops mattering; the ramp is deleted.

### 3.2 Every ability gets a preview SHAPE that matches what it makes

A new component `CastPreview` (Visual) draws one of these, in the hero's hue, as bright edges over a
dim fill (the direction baseline's value ladder), at real size, updated every frame while held:

| Shape | Used by | Drawn as |
|---|---|---|
| **Ghost** | constructs: Glacial Wall, Bakya Bloom, Kuro Guard spot, Thorn Harvest ring, Paete's sentry, Barrier | the real model (or its footprint mesh) at the anchor, translucent, edges lit, rotating with the aim |
| **Zone** | Cold Feet, Higop, Terrify haunt | the exact footprint outline plus a faint inner pattern (frost, a swirl, smoke) |
| **Arc wall** | Glacial Wall, Whirlwind front | the arc itself at the aimed spot and facing |
| **Path** | dashes and leaps: Quick Dash, Liana Leap, Shadow Blink, Flame Rush | a ground strip from the feet to the end point, the end marked with the body's silhouette where it will stand |
| **Lob** | thrown things: Bakya seed, Boulder, Voodoo doll, Frostbite and every loaded throw | the flight arc (dotted) and the landing ring where it will land, from the real arc solver |
| **Cone / fan** | Vulnerable curse, Storm Surge | the cone edges out to range |
| **Self radius** | Absolute Zero, Earthquake, Seismic stamp, Thorn Harvest reach | a ring round the caster at the true radius |

Plus, always while aiming: the caster's **range ring** (faint, where the anchor may go) and a line
from the hand to the anchor.

### 3.3 Invalid is red and does not cast

Out of range is clamped (never red). Red only when the spot is illegal: inside geometry, off the
court, a blink into a wall, a construct that would overlap the can's spot. Releasing on red cancels
with a short buzz, a shake of the ghost and the prompt "CAN'T PLACE THERE"; no cooldown is spent.

### 3.4 Cancel, on every device (`CLAUDE.md` 4a)

| Device | Cast | Cancel | Rotate (constructs only) |
|---|---|---|---|
| Mouse and keyboard | release the ability key | right mouse (the throw-aim button is the same button; while an ability is aiming it cancels instead) | mouse wheel, 15 degrees a notch (`Adjust Angle`) |
| Controller | release the ability button | B (East) while aiming; B stops being "back" only while aiming, `MenuNav` is untouched | right shoulder taps 45 degrees; hold and use the right stick for free angle |
| Touch | lift the finger off the dragged skill button | drag onto the CANCEL target that appears above the skill cluster while aiming (Wild Rift), or lift inside the button's dead zone | a second finger twists, or a small rotate arrow button beside the ghost |

A cancel costs nothing (no cooldown, no charge) and plays a soft "unclick". The rebind screen gets
`AbilityCancel` and `AbilityRotate` rows (a new `ScreenInputCatalogue` / verb entry with a pad path
and a thumb target; `InputAssetSync.Regenerate`). Prompts read the live binding through
`Rebinding.DisplayNameFor`, never a literal.

### 3.5 The prompts (the "ui for the cancel shit")

While aiming, three small prompts sit under the crosshair in the in-game UI style (black
`UiTheme.InGameOutline`, at least 28 canvas units): `[key] CAST`, `[key] CANCEL`, and for
constructs `[key] ROTATE`, each with the live binding glyph for the device last used. No sentence.
The skill's tile on the deck lifts and glows while it is being aimed, so the player also sees WHICH
skill is held. On touch the CANCEL target is an X in a circle, 144 units, above the skill cluster,
that lights red when the finger is over it. Prompts fade after the player has cast the same skill
five times (a setting can keep them).

### 3.6 What else goes with it

- **Enemies see less.** Opponents see nothing of a held aim (no free information), except the
  ultimates that already telegraph on purpose (Storm Surge's fan, Higop's cast).
- **The body tells it.** While aiming, the caster holds the ability's TELL pose (the first beat of
  the six) as an upper-body layer, so others read "she is about to do something" without seeing
  where.
- **Sound.** A quiet held tone per hero while aiming (the inhale), the cast transient on release,
  the unclick on cancel.
- **Bots** never need the preview; they keep aiming by the rules they use now.
- **Accessibility.** Reduced motion keeps the preview static (no pulse); a colour-blind setting
  swaps red invalid for a hatched pattern.
- **Instant cast option later.** A per-ability setting (League's quick cast) can skip the hold.
- **Replays and spectators** see the preview of the player they follow (it is the POV).

## 4. Implementation plan

1. `HeroAbility`: replace the time ramp with `AimShape` (enum above) and `AimAnchor(ctx)` from the
   camera ray; keep `AimMinRange` / `AimMaxRange`; add `CanPlaceAt(point)` per ability (default
   true); `RotationDegrees` for constructs. `HeldSecondsOnCast` stays for anything that still
   wants it.
2. `HeroAbilitySystem`: an `Aiming` state with Cast / Cancel / Rotate; cancel consumes nothing;
   the aimed pose (anchor, rotation) is what `Activate` receives and what goes on the wire with the
   cast request (the host validates it, as it already validates range).
3. Input: `AbilityCancel`, `AbilityRotate` verbs with pad and thumb answers; touch drag-to-aim and
   the cancel target in the touch layer; the prompts in the HUD.
4. `CastPreview`: the seven shapes; each ability names its shape and size from the same constants
   the effect uses (one number, never two).
5. Every ability on every hero moved onto it, one at a time, and photographed on the owner's window
   shape, a pad and a phone.
6. `GroundReticle` is kept for bots' footprints and removed from the player's own aim.

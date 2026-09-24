# Skill performances: research and per-skill plan (SKILL-FX-1)

Owner, 2026-09-24: *"refine all their skills VFX SFX and animation and everything too. THOROUGHLY
research how other games that are good in roblox and or actual games like valorant or overwatch make
skill effects and try to author one that works in our world"*, *"i really want phaister's ult to be
improve its cast/animation is awkward and ugly and circle doesnt look that amazing its js a purple
empty circle"*, *"thoroughly plan how to make each skill WAY better"*.

Order: Phaister's live Grand Coven first (the named complaint), then one skill at a time as listed.
Every skill keeps its gameplay (radius, timing, authority, cost); this is presentation.

## 1 · Research: what makes a skill effect read and satisfy

Sources read this session: Riot's League of Legends VFX Style Guide (2017, public PDF,
[nexus article](https://nexus.leagueoflegends.com/en-us/2017/10/dev-leagues-vfx-style-guide/),
[summary of its ten rules](https://www.vfxapprentice.com/blog/10-league-of-legends-vfx-design-tips));
Riot's [VALORANT Shaders and Gameplay Clarity](https://technology.riotgames.com/news/valorant-shaders-and-gameplay-clarity)
(already in `reports/visual-research-2026-09-23/findings.md`); Jujutsu Shenanigans' domain rules
([Sportskeeda](https://www.sportskeeda.com/roblox-news/jujutsu-shenanigans-0-2-domain-guide));
the Roblox battlegrounds VFX market (asset listings and artist portfolios, e.g. BuiltByBit's anime
VFX packs) for what that audience expects; Overwatch from general knowledge of shipped play,
labelled as such. No video was watched on this machine.

| Principle | Source | What it means for TUMP |
|---|---|---|
| **One primary shape, secondary shapes support it** ("immediate clarity with minimal visual noise") | Riot style guide tips 1, 7 | Each skill gets ONE silhouette you can name from across the court (a pentagram stamp, a wall, a wave). Everything else is smaller, dimmer and later. |
| **Visual impact matches gameplay impact**; ultimates visibly exceed basics | Riot tip 2 | Casts < skills < ultimates in size, contrast and duration. A small skill with a huge flash lies. |
| **Value range carries focus; avoid 0/100 % value and saturation** | Riot tips 3, 5 | Dark core + bright edge beats a bright flat fill. The "empty purple circle" is the failure of this rule: everything mid-value purple on grey road, no darks, no brights. |
| **Illumination: glow conveys power, direction, duration** | Riot tip 4 | Lines that matter get a bright core and a soft wider underlay; that doubles their read at distance without widening the footprint. |
| **Anticipation, impact, processing pause, dissipation**; "if your FX feel long they are way too long" | Riot tips 9, 10; timing slide | Every skill: a tell before (so the other three can react), a sharp peak, then a quick fade. Lingering zones say their remaining time by shrinking or dimming, not by staying the same. |
| **The AoE edge must be exact and visible from any view** | Riot (hitbox accuracy); VALORANT smokes' hard geometric edge | Any zone with a gameplay boundary gets a boundary you can read edge-on in first person: a raised rim or low curtain, not only a floor line. |
| **The space is transformed; a gesture starts it** | Jujutsu Shenanigans domains (hand sign, dome with the technique written into it) | Big zones (Grand Coven, Devouring Seance, Permafrost) read as a PLACE that changed, with a wall or ceiling, not a decal. |
| **Layered anime impact language**: a one-frame flash, a shock ring, debris, speed lines, lingering smoke | Roblox battlegrounds VFX packs and front-page games (Blox Fruits, Combat Warriors, per artist portfolios) | TUMP can borrow the LAYERING (each layer a different job and lifetime), drawn in our blocky, ink-outlined shapes, never the anime textures. |
| **Silhouette and role shape language**; heroes' ability colours are theirs; enemy readability wins | Overwatch (general knowledge) | Each hero keeps one colour family and one shape family across all three powers; their powers never borrow another hero's shapes. |

Our own standing rules that bind all of this (`AGENTS.md`, `docs/VISION.md`, `Art_Direction.md`):
no whiteout or strobe; can, slipper, players and chalk readable on Low and during overlaps; no
killing/horror; blocky cute world; no generic shared rings/bursts; reduced effects keeps the
information and drops flashes.

## 2 · Phaister, GRAND COVEN (live part), FIRST

**Owner's complaint:** the cast is awkward and ugly; the circle "doesnt look that amazing its js a
purple empty circle".

**Diagnosis from source** (`HeroHazards.SpawnGrandCovenEclipse`, `CovenCircleBuild`,
`author_hero_action.py` `hero-phaister-eclipse`):
- The circle is seven thin rules (collar widths 0.05, alpha 0.45 to 0.72) plus ticks, script,
  medallions and three sigils, ALL in the same mid purple family, lying flat on the road. From eye
  height at 5 to 10 m, perspective squeezes a 5 m ring of thin lines into a faint ellipse, the fine
  detail aliases away, and nothing is darker or brighter than anything else: exactly an "empty
  purple circle". Riot rules 1, 3 and 4 all fail.
- Only 6 floating glyphs; nothing stands up, so edge-on it has no height at all.
- The cast clip raises both arms to -166 (straight overhead, which on her short arms disappears
  inside the hat brim, measured in the intro sheets) and then folds her forward 34 degrees at the
  punch, which pitches the brim at the viewer: the "black slab" fault the Hex pass already fixed.

**Plan:**
1. **Cast body, rebuilt** (glb table): continue from where the new introduction ends (one hand up,
   one pointing down). She DRAWS the circle: the pointing hand sweeps round in front of her while
   her chest turns with it (0 to 0.9 s), the raised hand keeps time; then both hands come up
   forward, palms up, lifting the curtain (0.9 to 1.4 s, hands in front of the brim where they
   read); at the close (1.55 s, the ritual's own `RitualBuildSeconds`) a sharp downward clench,
   both arms out and down, head level (never pitched down); a held beat; recovery. Timing stays the
   ability's.
2. **The circle, rebuilt for value and height**, same radius and build stages:
   - a **shadow pool**: the road inside darkens to a low-alpha deep violet, so the lines have a
     dark ground to glow against (value range, rule 3). Darkness, not paint: the lata and players
     stay bright on it.
   - **bold primary rules**: outer, middle and inner rings get a bright core and a wider soft
     underlay (illumination, rule 4); the rest stay fine as secondary detail.
   - **a curtain**: a low luminous wall rising from the rim (about 0.9 m, fading upward), so the
     boundary a cursed player must escape is visible edge-on from any view (VALORANT's hard edge,
     Jujutsu's dome).
   - **motion**: the three sigil layers counter-rotate; more floating glyphs (the owner asked for
     about twenty, distinct) orbit and rise.
   - **the curse beat**: each repeated curse sends a bright ring from the centre to the rim and
     flares the curtain, so the zone's rhythm is visible (the ability already calls `Pulse`).
   - **close and dissipation**: when the outer ring closes, one bright snap (off in reduced
     effects); at the end the curtain sinks into the road and rings fade inner-first.
3. **First person** (`ViewmodelArms.CastGesture`, `coven-eclipse`): the hand sweeps across the
   bottom of the screen drawing, then both hands rise into view palms up, then press down.
4. **Sound**: keep the coven cast and eclipse toll; add a rising tone under the build that stops
   dead on the close (generated with the existing seeded synth, a new slot).

## 3 · Every other skill, one at a time

Each row: what it must SAY, the body, first person, the primary shape plus supporting layers, the
timing arc and the sound. "Now" is what source shows; everything will be checked on a pose sheet and
a stage sketch, and natively on Windows.

### Sean (fire, FORWARD, lantern maker)
- **FLAME RUSH**: says "a straight committed line". Body: a low sprinter's lean, arms raked back.
  Primary: the fire trail as a clean ribbon of flame tongues along the ground; secondary: embers
  kicked up behind, a scorched edge that fades first at the tail. Tell: a heat shimmer at his feet
  for the first frames. Sound: a whoosh that pitches up with speed.
- **IGNITION CANNON**: says "this throw is loaded". Body: chambering the slipper at the shoulder with
  the free hand cupped over it (his lantern gesture again, small). Primary: the slipper glowing like
  a parol ember with a short flame tail in flight; the landing blast a star-shaped burst (his five
  points) rather than a round one. Sound: a tick of ignition on load, a dull thump on landing.
- **SUPERNOVA (live)**: the leap and slam. Primary: the crater as a five-point scorch star with a
  bright rim, flames standing on the points; secondary: a ring of ash thrown out; hit-stop on
  landing (`HitFeel`). Dissipation: the flames burn down point by point.

### Phaister (magic, ritual, the setup)
- **HEX**: says "a binding placed here". Body: a precise stamp with one hand, head level (brim rule).
  Primary: the WardCircle stamp, with a short standing ring of writing that rises and settles;
  stumbles inside flash the stamp once per tick. Sound: a hissed syllable plus a lock click.
- **SHADOW BLINK**: says "gone, and there". Body: collapse inward then open (retained). Primary: the
  vertical Rift at both ends, with a violet ink smear connecting them for one frame; the shove at
  departure as a puff of torn paper-like shadow flakes. Sound: an inhaled whoosh, a pop on arrival.
- **GRAND COVEN**: section 2.

### Zack (electric, SIDEWAYS, casual)
- **BOLT SPRINT**: says "faster, and do not follow". Body: skater's lateral push cycle (retained).
  Primary: the shock trail as zig-zag lines crackling low on the road; secondary: sparks off his
  skates each push. Sound: a buzzing loop that tightens with speed.
- **MAGNET**: says "come back". Body: off hand aims, receiving arm pulls in (retained). Primary: a
  taut gold line from hand to slipper that snaps shorter as it flies home; the charged next throw
  with a crackle tail. Sound: a rising "zzip" and a clack on catch.
- **THUNDERSTRIKE (live)**: primary: the bolt as one thick jagged column that holds for three frames,
  a scorched rune of cracks at the ring; secondary: arcs that crawl out to the ring's edge, stunned
  players buzzing. One flash only; reduced effects keeps the column without the flash.

### Nemu and Kuro (spirit, looks distracted)
- **PHANTOM VEIL**: says "she is half here". Body: a light float forward, arms trailing. Primary: a
  translucent trailing afterimage of her silhouette (two or three echoes) rather than a generic aura.
  Sound: a breathy rising tone.
- **ASTRAL HIJACK**: says "she is looking through Kuro". Body: eyes closed, hands folded, sway; Kuro's
  eyes glow. Primary: a thin ink thread linking her to Kuro; the recast pull as the thread reeling
  her in. Sound: a bell-like tone.
- **DEVOURING SEANCE (live)**: primary: giant Kuro's maw and the pull, with ink streaks flowing
  inward along the ground toward it (showing the pull direction); slippers visibly sliding in.

### Dante (earth, DOWN, planted)
- **SEISMIC STOMP**: primary: a cracked stone plate that lifts in pieces around him (Upheaval),
  dust ring; hit-stop; secondary: pebbles. Sound: a deep thud with a crack on top.
- **DEMONIC CARAPACE**: says "cannot be moved". Body: a double-arm flex and roar (retained). Primary:
  stone plates snapping onto his shoulders and forearms one by one with molten seams between; a
  subtle heavy stride dust while it lasts; plates crumble at the end. Sound: stone clacks per plate.
- **TITAN FISSURE (live)**: primary: the crack opening forward in stages with slabs heaving up along
  it (matching the new introduction's seams); launched players thrown with dust.

### Cheska (ice, NOWHERE, precise)
- **PERMAFROST SHEET**: primary: the ice sheet as angular glassy plates with a bright edge line
  (friction boundary readable edge-on), frost breath over it; players on it leave skid streaks.
- **ICE BARRICADE**: primary: three pillars rising with a crackle, faceted, bright edges; a frost
  line on the ground where she drew it. Sound: rising crystal creak and a lock.
- **GLACIAL NOVA (live)**: primary: a ring of spikes bursting out along the ground to the radius
  (angular, like the introduction's spire ring), frozen players encased; slippers blown outward with
  frost trails.

### Rafi (water, the tease)
- **CROSSCURRENT**: primary: a narrow ribbon of water with a visible flow direction along the aimed
  line; the bent slipper gets a wake. Sound: a slap and a swish.
- **MIRRORWAKE**: primary: a watery echo of Rafi that retraces his route, glassy and rippling, and
  splashes into droplets on its feint. Sound: a gurgle echo of his own steps.
- **BREAKWATER (live)**: primary: the low wave as a curling crest travelling forward with foam, the
  same wave shape the introduction raises; a wet sheen left behind that dries.

## 4 · How each is verified here and owed

- Body: pose sheets of the glb cast clip (the preview tool will sample glb animations).
- Stage/VFX: a sketch of the main shapes in the same tool, plus source review.
- Native: `AbilityShowcaseProbe`, `CastAndMotionReel`, `GameplayActionShots` on Windows, and a
  look in a live match. None can run on the cloud machine (no Unity licence).

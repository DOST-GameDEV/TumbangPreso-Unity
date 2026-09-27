# Phaister's overhaul: the plan, 2026-09-27

Owner, 2026-09-27: *"rename her shit too hahah it sucks ass"*, *"it needs really great presentation VFx ANIIMATION SFX AND
DIRECTING"*, *"think abt her personality too in making her cutscenes and vfx"*, *"thoroughly refine existing animation
effects and models and vfx of her skills"*. Method: `docs/HERO_KIT_METHOD.md`. Brief:
`ArtSource/phaister/kit-20260927/design-brief.md`. Research: [research.md](research.md).

**The one sentence for her kit: a stage magician whose tricks happen to be curses; every ability is a small show with a
setup, a reveal and a bow.** Playful, pleased with herself, never cruel (LORE.md).

## 0. What is there today (audited from the code, 2026-09-27)

| Ability | Today | Verdict |
|---|---|---|
| SHADOW BLINK | a torn shadow sheet at the start (`HeroHazards.SpawnShadowRift`), falling glyphs at the end (`PhaisterArrivalSeal`), the old `hero-phaister-blink` clip (a kick), `sfx_blink_arrive` | two unrelated effects; nothing travels between them; no personality |
| CURSE: DISORIENTED | a first-pass modelled doll (`build_rework_props.py`, block fallback) spinning on an arc; lands with `sfx_blink_arrive`, the BLINK's sound; cast clip `hero-phaister-hex` from the retired Hex | shares a clip and a sound with other abilities; the method forbids both |
| CURSE: VULNERABLE | `VoodooConeFlash`, a flat fan that scales in over 0.15 s and vanishes at 0.6 s; the SAME `hero-phaister-hex` clip and `cast-hex` first-person action | invisible after 0.6 s, so nobody can tell who is Vulnerable |
| HIGOP | a placeholder dark sphere with a ring (`VoodooBlackHole`); cast clip `hero-phaister-eclipse` from the retired Eclipse; the cutscene is still the retired Grand Coven ritual | the owner's *"make blackhole really cool"* is not met anywhere |

Mechanics stay as built (`Core.VoodooRules`, ABILITY-2); question 9 asks about the numbers that were never reviewed.

## 1. Names (the owner asked; ids unchanged)

Paete's final names were English with one Filipino anchor (BAKYA BLOOM), so hers follow that shape.

| Slot | Today | Proposed | Why | Alternative |
|---|---|---|---|---|
| Signature | SHADOW BLINK | **VANISHING ACT** | the oldest trick on a stage: now you see her, now you do not | CURTAIN CALL |
| Attacking | CURSE: DISORIENTED | **MANIKA MISCHIEF** | *manika*, a doll; the doll takes the victim's look and scrambles what they see | PUPPET PANIC |
| Defending | CURSE: VULNERABLE | **SPOTLIGHT PIN** | she pins them in the spotlight, where everyone can catch them | CENTRE STAGE |
| Ultimate | HIGOP | **BAKUNAWA'S BITE** | her Capul childhood story: the serpent that swallows the moon, performed as her greatest trick | THE GRAND FINALE |

## 2. The effect family (her rules, written before any effect)

1. **Props are solid, toon-lit geometry** in her palette (black, violet, magenta, gold, bone for the moon and pin heads).
   Only the spotlight, the moon, her sigils and the hole's rim glow. Never white.
2. **Every effect comes out of her props and is put away again**: pulled from her sleeve or hat, folded, switched off,
   deflated, swallowed. Nothing fades a solid by alpha.
3. **Every payoff is a REVEAL**: a beat of gold glints and a presenting gesture, a different gesture per ability.
4. **The spotlight is her one light**: violet body, warm gold edge, drawn as edges on the ground, never a fill over the can.
5. **Power sits in a small point; an area is drawn by what moves toward it** and a ring on the ground (Zarya).
6. **Readable in the 14 m box**: footprints 1.2 to 2.3 m except the ultimate; the can, the chalk and every player stay
   visible on Low.
7. **Typed by hand, part by part**, in `tools/build_phaister_props.py`: the rag doll, the pin, the lamp, the curtain, the
   moon, the serpent.

**Her sound instruments** (`tools/build_phaister_audio.py`, numpy, seeded; each cue a transient, a body and a tail, no two
cues sharing a recipe): `snare` (a stage roll), `sting` (a two-note ta-da), `swish` (cloth), `zip` (thread drawn fast),
`tick` (a pin's bright metal), `lamp` (a stage lamp's clunk and hum), `musicbox` (a warped tine melody), `pluck` (thread
tightening, pitch rising), `hiss` (cloth hiss for the serpent), `gulp`; her recorded laugh stays the one voice.

## 3. The six beats per ability

### 3.1 VANISHING ACT (signature; hold to aim, release, she is there; whoever she left is shoved)

| Beat | Body (everyone) | First person | Effect | Sound |
|---|---|---|---|---|
| Tell (while aiming) | she takes her robe's hem in one hand, chin up | her hand lifts the hem into view | the aim ring becomes a SPOTLIGHT: a violet cone from 2.2 m, gold-edged circle on the court where she will arrive | a lamp hum under the aim |
| Release | a swirl: she sweeps the hem up over her head | the hem sweeps across the screen | a curtain of violet cloth drops round her from a ring at 1.7 m; a snap; the curtain falls EMPTY and its ring of cloth whips out (the shove, 2.5 m) | `swish`, the snap |
| Travel | (gone) | a black frame for two frames, a stitch line racing ahead | a seam of stitched thread zips along the court from her start to the spot (0.12 s) | `zip` |
| Contact (arrival) | a curtain rises out of the court round the spot and is pulled up and away; she is there, mid hat tip | the curtain lifts off the screen | the spotlight blazes for 0.2 s, gold glints | `sting` |
| Linger | she finishes the hat tip, strut resumes | | glints drift down through the cone | |
| Dissipate | | | the spotlight irises shut to a point; the dropped curtain at the start folds and sinks into the seam | a lamp clunk off |

### 3.2 MANIKA MISCHIEF (attacking; a thrown doll; the first player within 1.2 m is Disoriented 4 s)

| Beat | Body | First person | Effect | Sound |
|---|---|---|---|---|
| Tell | she pulls a rag doll from her sleeve and whispers to it | the doll held up to the lens, her other hand cupped beside it | | a giggle whisper |
| Release | an underhand juggler's toss with a spin, and a wink | the toss arcs away from the hand | a single thread stays tied between her finger and the doll | `swish` small |
| Travel | | the thread pays out from her fingers | the doll tumbles on its arc, the thread trailing back to her | `pluck`, rising as the thread pays out |
| Contact | | | the doll grabs the victim's shoulder and TAKES THEIR LOOK (it copies their colours, question 4); a pop of loose stitches | a soft cloth thump, a `musicbox` phrase that detunes |
| Linger (4 s) | she tugs the thread once (a puppeteer's twitch) | a tug in the fingers | a tiny ring of three doll-copies of the victim circles their head; the hallucinations on their screen (built) | the music box loops, detuning |
| Dissipate | | | the doll's stitches pop and it deflates to a flat cloth that sinks | a cloth sigh |
| Miss | | | the doll lands, sits up, shrugs, flops flat and sinks | a cloth thump, one sad tine |

### 3.3 SPOTLIGHT PIN (defending; a 60 degree, 7 m cone; every attacker in it is Vulnerable 5 s)

| Beat | Body | First person | Effect | Sound |
|---|---|---|---|---|
| Tell | she draws a long pin out of her hat and spins it between her fingers like a baton | the pin twirls in her hand | | `tick` twice |
| Release | a conductor's point with the pin along her aim | the pin points down the cone | a fan of three spotlight beams sweeps across the cone from above, drawn as edges on the court | `lamp` clunk x3, fast |
| Contact | | | each attacker hit: a beam locks onto them from 3 m up; a gold pin sigil stabs into the court at their feet | `tick` on each |
| Linger (5 s) | she presents the lit victim with an open palm | | the spotlight FOLLOWS the Vulnerable player (everyone can read who is exposed, question 5) | a lamp hum on them |
| Dissipate | | | the lamp flickers and cools (violet to a dull orange filament), the pin sigil pops out | a clunk off, a cooling tick |

### 3.4 BAKUNAWA'S BITE (ultimate; 2.2 s slow cast, a hole at an aimed spot up to 8 m; 5 s pull within 7.5 m; no escape)

**Live:**
- The hole is an ECLIPSE: a small black disc (0.6 m) hung at 1.1 m over the spot, with a hot magenta corona. A stitched
  cloth serpent circles the rim as the accretion ring, its jaws open round the disc.
- The area is a ring drawn on the court at 7.5 m and thread arcs, dust and loose pins spiralling inward; the court dimples
  toward the centre. No dome.
- Every other body and every slipper that is not hers is dragged in (the owner's rule: they can push away for a moment,
  then are sucked back); caught players bob at the rim, struggling. Slippers whistle and vanish into the disc.
- The end: the serpent snaps its jaws shut on the disc (a gulp), its coils tighten to a point, a thump releases everyone at
  the rim, and a small moon pops back OUT of the serpent's mouth and floats up (the trick undone, her open finish,
  question 8).
- Victim view: screen edges stretch toward the hole; a low hum rises as they near it.

**The cutscene (direction before building, method section 6).**

One sentence: **"She shows them the moon, and her serpent swallows it."** The travelling thing is the MOON, always moving
left to right across the frame, from her hand, to the serpent's mouth, down onto the court as the eclipse.

| Shot | Time (6.5 s, question 7) | Picture | Camera | Sound |
|---|---|---|---|---|
| 1 SETUP | 0.0 to 2.0 | opens on pins and loose thread already raining through a spotlight cone (the element in the air); she steps into the light, tips her hat INTO the lens, pulls a sewn moon out from under the brim and holds it up | a slow push in from a mid shot to her waist | a snare roll, a cymbal-less sting as the moon appears (the motif) |
| 2 THE TRICK | 2.0 to 4.4 | power surges: she rises off the court, robe, hair and sleeves whipping UPWARD, pins rising in a slow ring; a stitched serpent pours out of her ring of thread and coils up round her; she lets the moon go and it drifts left to right into the serpent's open jaws; her eyes OPEN (the one held close-up, 0.5 s); the jaws close: eclipse, the frame goes to her dark with only the magenta corona | low angle, looking up past her feet (the Kafka detail shot: pins raining round her shoes), then a cut to the close-up of her face | a long rising whine, cloth whipping, `hiss`, the `gulp` cut dead into silence |
| 3 THE REVEAL | 4.4 to 6.5 | she flicks her fingers and the eclipse drops onto the aimed spot; the camera rides it down and out to the REAL targets (staged copies, their own clips) as they are yanked off their feet, shocked, and dragged in; it swings wide and settles with every target bobbing at the rim and Phaister behind, landing into a curtsey | one continuous move, never meeting a body (method 6, rules 3 to 5) | her laugh, the hole's drone, a slipper whistle per slipper, the final `sting` on the curtsey |

The density pass (method 6) on every beat, in her colours: glints on each reveal, shockwave rings where the eclipse lands,
rays behind her as she rises, thread ribbons spiralling round her, rising pins as motes, a curtain of streaks round the
hole, the frame's edges sinking into her violet dark while the power is up. Play hands back at the cutscene's end state,
never showing the catch twice.

## 4. Files this touches

- **New**: `tools/build_phaister_props.py` (doll, pin, lamp, curtain, moon, serpent glbs), `tools/build_phaister_audio.py`,
  `Runtime/Visual/PhaisterVanishingAct.cs`, `PhaisterManika.cs`, `PhaisterSpotlight.cs`, `PhaisterBakunawa.cs`
  (replacing `VoodooVfx.cs` bodies; the gameplay classes keep their host logic), `HeroAbilityClips.Phaister.cs` + a
  motion author (four clips, no shared clip), `HeroIntroductionScene.Phaister.cs` rewritten + `PhaisterBurst.cs`,
  `PhaisterKitPlayProbe` (films her screen, the court, a caught player) and a rejoin probe for the hole and the spotlight.
- **Changed**: `PhaisterHeroKit.cs` (names, descriptions, cast actions, cues), `ViewmodelArms` (four first-person
  actions), `CharacterAnimator` action map, `tools/author_ultimate_intros.py` `phaister()`, `HeroGlyphs` and
  `tools/build_ability_icons.py` (four icons), `HeroLines.cs`, `AudioCues`, `WorldEffectSnapshot` kinds for the spotlight,
  `docs/TODO.md` (a HERO entry with the method's checklist).
- **Retired**: the Hex, Eclipse and Coven clips and cutscene once nothing references them.

## 5. Order of work

1. Owner's answers (section 6), recorded in section 7.
2. Names and descriptions in the kit and icons (small, ships first).
3. Props in the typed builder; turnarounds of each prop beside her.
4. The four body clips and four first-person actions; filmstrips.
5. Effects per ability, then sound per ability, filmed in a match and sent.
6. The cutscene: storyboard with `--preview`, then built, filmed on her screen with the clock measured.
7. Density pass, loudness table; film; send; act on the verdict.
8. Bots, rejoin probe, full verification, build.

## 6. Open questions (ONE batch, 2026-09-27)

1. **Names**: VANISHING ACT, MANIKA MISCHIEF, SPOTLIGHT PIN, BAKUNAWA'S BITE? Or the alternatives, or your own.
2. **Her own model**: keep it as it is (she is the look the cast is being restyled toward), or refine her too?
3. **The teleport's vehicle**: a stage curtain (drops round her, rises round her at the spot, proposed) or the old plan's
   shadow ribbon along the ground?
4. **The doll**: on a hit it copies the victim's look (their colours), so it becomes a voodoo doll OF them. Yes?
5. **Spotlight Pin**: the spotlight follows each Vulnerable player for the whole 5 s so everyone can read who is exposed.
   Too loud, or good?
6. **The ultimate's serpent**: a stitched cloth serpent circling the hole and swallowing the moon (proposed), or a pure
   black hole with no creature?
7. **Cutscene length**: use the whole 6.5 s cap, since you asked for her to cast it really slowly?
8. **The ending of the live hole**: the moon pops back out of the serpent after it collapses (her bow). Keep or cut?
9. **Numbers set on 2026-09-26 and never reviewed**: doll 32 s cooldown, 12 m/s, 10 m; cone 32 s, 60 degrees, 7 m;
   ultimate 15 points, 2.2 s cast, 5 s pull, 8 m range, 7.5 m radius. Keep them?
10. **Voice**: her existing laugh stays the only voice (sourced voices must be human recordings). Want new lines recorded
    by someone on the team, or keep it as is?

## 7. The owner's answers

(to be recorded here: answer, his words, what it becomes)

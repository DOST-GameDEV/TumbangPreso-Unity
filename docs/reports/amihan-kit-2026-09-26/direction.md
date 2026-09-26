# AIRBURST: the ultimate's cutscene, directed before it is built (2026-09-26, cloud)

Owner: *"fot cutscene thoroughly direct it and use genshin impact and other game ULT cutscene animations as reference for
direction, what happens as welll as vfx sfx animation etc"*. Method: [HERO_KIT_METHOD.md](../../HERO_KIT_METHOD.md) section 6.
References: [research.md](research.md) section 2 (this pass, frame-stepped) and the first pass's rules
([../amihan-kit-2026-09-25/research.md](../amihan-kit-2026-09-25/research.md) section 3). Not Paete's look: his cutscene is a
light travelling DOWN into the ground and a tree crawling UP; hers is a breath travelling OUT and a whole sky leaning one way.

## 0. What was wrong with the 3.6 s version (v1, 2026-09-25)

It had the four beats (who, intent, gather, release) and a nice idea (she blows cotton off her palm: Vigan's Binatbatan).
Against the method it fails four ways: **four shots with four ideas** (Paete's v3 lesson: five cuts, five ideas, *"ur
direction of the entire cutscene sucks"*); **nothing travels through it** (the cotton is blown in shot B and forgotten);
**it ends on the power leaving** (the wall of wind recedes down an empty court; Paete's v6 lesson: *"this felt liek a weak
ending"*); and **the air is empty** between beats (Paete's v6: *"A LOT MORE VFX"*).

## 1. One sentence

**She breathes the monsoon off her palm, the kasikus whirl winds it round her until the whole sky leans her way, and she
lays it down across the court on everyone standing in it.**

## 2. The one thing that travels: her breath, as cotton

A tuft of cotton, blown off her palm, unravels into the first wind lines (the Binatbatan idea kept). That same cotton and
those same lines are what spiral round her in the whirl, and what ride the wall of wind out to the targets. It always moves
**left to right across the screen**, so every cut continues the one before.

## 3. Three shots, one idea each (6.2 s, under the 6.5 s cap)

| Shot | Time | Idea | Camera | What happens |
|---|---|---|---|---|
| **BREATH** | 0.00 to 1.70 | the wind is hers | medium to close, a slow push in on her face and right palm from her right, slightly below eye level | Frame 0: cotton and abel threads are ALREADY streaming across the lens left to right (Venti, Kazuha). She is impatient (her lore: she hates waiting): a hand on her hip, a look straight down the lens. She catches one tuft out of the air, holds it at her lips, and blows. The tuft leaves her palm and unravels into three thin wind lines that exit frame right. The world starts to grade down behind her. |
| **WHIRL** | 1.70 to 3.60 | the sky answers her | cut to low and wide from her left, circling right and rising, ending looking UP at her against the whirl | The wind lines come back in from the left (continuity) and spiral round her. She turns twice through her whole body (her motion language is the spiral). The kasikus whirl (graduated rectangles radiating from a centre, the binakol pattern) blooms on the court under her, then a second, larger kasikus opens in the air behind and above her head. At 2.90 a ring flash: she RISES 0.45 m and hangs in the air (Xianyun) over the turning pattern, arms opening, cotton bolls bursting open round her. Vertical streaks rise out of the court round her (power). The frame's edges sink into her deep green (the veil). |
| **THE TAKE** | 3.60 to 6.20 | everyone in the fan is caught | over her right shoulder, then ONE continuous move: ride the leading edge out down the fan to the nearest real target, pass beside them, swing wide to show all of them, settle back behind her with every target in frame | She lands, draws the storm back to her right side (3.60 to 3.95), and drives both palms forward (3.95, the punch). A ring of giant crescents sweeps out from her (Venti's release) and the fan's leading edge rushes down the court carrying the cotton. It reaches each real target in turn: they are hit by the grip of it (a shock frame, cloth and hair whipped TOWARD her, a lean against it) and then turn and try to run out of it (the panicked flee every rig carries). The last frame: her braced in the push at the apex of the fan, the fan's edges on the court, every target inside it straining. |

## 4. The ending, and why the blast itself is not in the cutscene

The method says end on the ultimate landing on the real targets, and never show it twice. Airburst is the one ultimate with
a **dodge window after the cast** (*"After a 2.5 second delay"*), and the shared phase starts at the commit
(`SharedUltimatePhase`), so the cutscene plays BEFORE that window. If the cutscene showed them blown to the edge, play would
then either show it again (forbidden) or skip the window the owner asked for. So what lands in the cutscene is **the storm's
grip**: the air seizing every real target inside the fan, and them starting to fight their way out. Play picks up exactly
there: she is braced (the live clip's first pose), the fan is on the court, the air inside it streams toward her, and the
players really do have 2.5 s to get out. Then the wall leaves, live, once. **Asked of the owner in the handoff**: if he would
rather see them blown away inside the cutscene, the delay moves before the cast (a plumbing change to the phase, flagged).

- **The real targets.** Exactly the players inside the fan by `AmihanStorm`'s own rule (35 degree half-angle, 40 m) on the
  committed aim, copied with `MatchPoseHistory.Track.Clone`. Nobody in the fan means nobody on screen; never an invented one.
- **Their clips.** Clips every rig already carries: a flung-open shock frame, then `feared-flee` (the panicked run with an arm
  over the head and a glance back, `HeroAbilityClips.Status`), sampled onto the copies and blended by weight.
- **The camera never meets a body**: the ride ends 2.4 m beside the nearest target, 50 degrees round from the line the edge
  travels, on the side the swing goes (method section 6, rule 4).

## 5. The beat table (times first; everything hangs on them)

| t (s) | Picture | Sound | Answers |
|---|---|---|---|
| 0.00 | cotton and threads already crossing the lens L to R; she stands hand on hip, looking at us | cut-in accent: a dry breathy swell cut dead | Venti/Kazuha: element already in the air |
| 0.20 | the impatient hip shift (punch) | a cloth flick | her personality |
| 0.62 | she catches the tuft (punch), holds it at her lips | a soft catch | the one close-up (first pass rule 2) |
| 1.05 | she blows (punch); the tuft unravels into three lines that exit frame right | a long breath that becomes wind | Binatbatan; the travelling thing leaves |
| 1.70 | CUT: low wide from her left; the lines come in from the left and circle her | the wind theme's bed enters | continuity L to R |
| 1.85 to 2.60 | she turns twice through her body; the kasikus blooms on the court under her | two whooshes timed to the turns; the loom motif (a struck wooden shuttle, twice) as the pattern draws | her spiral; one emblem |
| 2.60 | the second kasikus opens in the air behind her, above her head | the loom motif, higher | the emblem where the power is |
| 2.90 | RING FLASH; she rises 0.45 m and hangs; bolls burst open round her; vertical streaks rise | a rising rush drawn in to 2.90, a bright bloom on it | Xianyun's rise, vertical light is power |
| 3.60 | CUT: over her right shoulder; she lands and draws the storm back to her right | the swell drawn in (inhale) | Kazuha's charge |
| 3.95 | the PUSH (punch); a ring of giant crescents sweeps out; the leading edge leaves | the swell cut dead into a deep wall of air with a crack | Venti's release, a stroke for the strike |
| 4.10 to 5.40 | the camera rides the edge down the fan; each target is hit by the grip (shock) and turns to flee | a thump of air per body, the rush panning with the camera | Kazuha's targets flung; Paete v7's THE TAKE |
| 5.40 to 6.20 | the swing wide and back; settle behind her, every target in frame, straining in the fan | the rush sustained, a tail left open into the live gather's pressure drone | the ending is the effect on people |

## 6. The density pass (her kinds, her rows; typed per beat in `HeroIntroductionScene.AmihanBurst.cs`)

| Kind | Hers | On which beats |
|---|---|---|
| Glints | a four-pointed star, cream (`fff1d6`) with a wind-green edge; they pop along the cotton's path | the catch, the blow, each turn, the rise, the push, each target hit |
| Shockwave rings | thin bright rings racing out low over the court; a flat flash under the push | the landing at 3.60, the push, each target hit |
| Rays | a slow sunburst of pale green behind her head as she hangs | 2.90 to 3.60 |
| Ribbons | wind ribbons spiralling up round her, three at different radii | the whirl |
| Rising motes | cotton wisps rising off the court round her | the whirl and the hang |
| A pillar | one column of pale green light under her at the rise (never white; checked against the sky) | 2.90 to 3.40 |
| A curtain | thin streaks rising in a ring round her as she hangs | 2.90 to 3.60 |
| A veil | the frame's edges sink into her deep green while the storm is up | 1.70 to 5.40 |
| The grade | the backdrop steps back (dim and desaturate), back to full for the take so the court and the targets read | 0.60 to 3.60 |
| The near layer | cotton and abel threads drifting past the lens in every shot, placed on the lens line | all |

Colours are hers only: the wind body `a6ec84`, the core line `f4ffe9`, her ink `2f6b2a`, the warm accent cream `fff1d6` and
her brooch gold `f2c14e`. Never white, never blue (hue 100 is yellow-green by design).

## 7. What got faster, what is held, and why

- **Faster**: the WHO beat (0.62 s became the first 0.62 of the BREATH shot, not a shot of its own): she is the setup.
- **Held**: the blow (0.45 s) and the hang (0.70 s): the two images that say who she is and what she can do.
- **New time**: THE TAKE, 2.6 s, bought from the old separate WHO and INTENT shots and by running to 6.2 s.

## 8. Play picks up from the end state

Live, `AmihanStorm` starts with its fan already laid (it is the cutscene's last picture), the air in it streaming toward her,
and `hero-amihan-storm` starts in the push pose the cutscene ends on. The release at the end of the 2.5 s is the only time
anyone is blown away.

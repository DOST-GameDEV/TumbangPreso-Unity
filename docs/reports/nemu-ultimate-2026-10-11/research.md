# Nemu ultimate research, 2026-10-11

Owner brief (2026-10-11): an eerie parade of ghosts. The world becomes grey, muted and
unnerving, then Nemu summons many ghosts that enter every player. POSSESSED for 15 gameplay
seconds. Players keep control, but ghosts contest their steering, inspired by resisting a Left 4
Dead 2 Jockey. Difficult and haunted, never a full takeover or a hard stun. Possessed players get
ghost/darkness HUD effects, an unsettling screen treatment and sound. The broader kit is Gekko-like:
distinct ghost companions with their own identities and jobs. Follow-ups the same day: *"research
genshin, honkai and other games' ultimate cutscenes too ... heavily analyze vfx"*, *"add a lot of
cool vfx that communicates whatever is happening in the screen well"*, *"pls limit ult to below 6
seconds"*.

Method: `docs/HERO_KIT_METHOD.md` section 2. Footage was stepped frame by frame in the built-in
browser (the video element pinned, paused and seeked, a screenshot every 1.2 to 1.5 s). Rules are
extracted, not looks. Each reference has what it does, what TUMP takes, and what TUMP does not take.

## 1. Mechanics references

### Left 4 Dead 2, the Jockey ride

Footage: "Jockey Rides Rochelle", 2:26 to 2:36 (third-person survivor view while ridden).

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| The rider steers the survivor; the survivor keeps their own stick and can hold against the pull, which slows the ride but rarely stops it | A contested steering sum: the player's wish plus the ghost's pull, clamped. Pushing straight against the pull holds ground; doing nothing walks you where the ghost wants | The rider choosing the direction freely. Here the direction is a fixed, readable rule (toward danger), so every peer and the victim can predict it |
| The ride is readable from outside: the rider sits on the head, hands over the face, legs scrambling | A visible rider on every possessed player's shoulders, hands over the eyes, leaning the way it pulls | Gore, the scream loop, the forced third-person camera (the victim's camera stays theirs) |
| The ride ends only by rescue | A fixed 15 s with grip that weakens while you fight it (skill expression without an escape button) | Teammate rescue: Tumbang Preso attackers score individually |

### Valorant, Gekko

Footage: "Gekko Abilities Gameplay In-Game", 0:05 to 1:30 (Wingman, Mosh Pit, Dizzy, Thrash).

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| Four creatures, four silhouettes, four jobs (a winged planter, a round splitting blob, a flier that blinds, a lunging axolotl) | Nemu's ghosts are a FAMILY of distinct characters, each with one job and one silhouette. Kuro stays the pet; the procession's souls are new characters | Neon colour, the street-art styling, a creature that the caster pilots in first person |
| One shared accent ties them together; each is cast from the hand and returns to it | The candle flame is the family accent: every soul carries its light | Reclaimable globules (no economy change) |

## 2. Cutscene references

### Honkai: Star Rail, Acheron ultimate (4K showcase, 0.6 to 14.5 s)

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| The world drops to monochrome; ONE colour survives (red petals, sword, eyes) | The hush: everything goes grey, only candlelight stays warm | Red. Ours is candle amber with Nemu's violet in the eyes |
| A held stillness (a slow walk, a pause) before the strike | A held beat before the entering: every soul turns its head to the lens at once, then the candles snuff | The black sun and the mirror floor (not ours, and too large for a 14 m court) |
| Petals drifting at the lens in every shot | A near layer of black moths (see Castorice) | The radial crack finale |

### Honkai: Star Rail, Castorice ultimate (0:01 to 0:25)

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| A summoned death dragon, violet butterflies as souls everywhere | Butterflies as the soul motif, which is ALSO a Filipino belief: a black moth or butterfly in the house is a dead relative visiting (paru-parong itim). Our near layer is black moths | The giant dragon, the beam, the saturation of the finale |

### Genshin Impact, Arlecchino burst (0.5 to 11.5 s) and Hu Tao burst

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| Arlecchino opens CLOSE on her face, a hand to her mouth, eyes glowing in the dark; then the stage goes black and red | Nemu opens close: a finger to her lips, *shh*. The hush gesture IS the cast. It fits her (quiet, distracted, already knows) | Blood moon, the crescent, the scythe |
| Hu Tao: ghost-shaped flames ride the swing for under 1.5 s | Ghosts that are made of the same light as their candles when they stream | Fire, the length |

### Pom Poko, the night parade (0:25 to 1:25)

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| A line of identical-but-different figures dancing in lockstep reads as uncanny | The procession walks in step; candles bob on the same beat; heads turn together | The giant skeleton's size (one tall figure is enough) |
| One giant among many small figures gives the parade scale | The White Lady, very tall, at the back of the line | Japanese yokai designs |
| Paper lanterns and hitodama (soul fires) drift in the air | Candle flames that keep burning after they leave their hands | Lanterns on poles |

## 3. Filipino grounding

- **Prusisyon**: the religious procession every town knows: a slow file of people with candles, an
  older woman with a rosary at the front, a bell. Ours is the procession of the dead.
- **Undas**: All Souls, candles on every grave, the night the dead come home.
- **Agunyas**: the slow church bell for the dead. One toll is the cutscene's first sound.
- **Paru-parong itim**: a black moth or butterfly in the house is a visiting soul.
- **Dumaguete and Siquijor**: Nemu's home faces Siquijor across the strait, the island of spirits
  and healers. Her canon line "Looks distracted. Already knows your next move." becomes: she is
  half-listening to the dead all the time. The ultimate is the moment she lets them in.

## 4. Shared cutscene vocabulary (applied, `ultimate-performances-2026-09-24/research.md` section 4)

Open on the element already in the air (the first moth is at the lens on frame 0), the backdrop
steps back (the grey grade), particles ride the motion (flames ride the procession, wisps ride the
streams), one emblem (a ring of candle flames round her), vertical light for power (thin smoke
threads rising off every snuffed candle), a near layer at the lens (moths), a sound motif (the
agunyas bell), saturated hero colour never white (candle amber and her violet; ghost bodies are a
pale lilac grey, never pure white).

## 5. What the previous Nemu research rejected, and why this overrides it

`docs/reports/hero-reference-footage-2026-10-02/nemu.md` rejected "puppet possession" and a
"possessed-camera vignette" for the old Haunt. The 2026-10-11 owner brief asks for possession and
for darkness HUD effects explicitly, so those rejections are superseded for the ultimate only. Its
other rule still holds: the camera stays under the player's control.

## 6. Round two, 2026-10-13: the owner rejected preview v1

The owner on preview v1: *"this ult cutscene sucks wtf is that"*, *"everything about it sucks"*, *"tangina nung procession
cringe na"*, *"fuck that name and idea"*, *"i dont like hwo the ghosts js sit inside on the ppl"*, *"hell i hate how the
ghosts look"*, *"i wanted the ghost to go inside the ppl tapos may parang aura nalang sila that theyre nasasapian"*,
*"HINDI UNG KITA UNG GHHOST IS IN THEM"* (it must NOT show a ghost sitting in them). Then: *"make a research and
throoughly think abt the plan first"*.

Footage was stepped in the built-in browser by drawing the paused `<video>` into a canvas at typed times (the pane could
not screenshot while the app window was behind another one), a contact sheet per reference.

### Valorant, Fade, NIGHTFALL (first person, 1 to 31 s)

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| A wall of black fills the whole frame as it passes the lens; inside it, line art drawn in light: faces, eyes, coiling tendrils | The dead are not solid figures. They are black smoke with their faces and claws drawn in violet light inside it. Many of them read as a mass of faces and hands, not a row of dolls | Cyan, the tattoo line style, a full-screen wipe on every player's screen |
| After it passes, a dim haze of the same colour hangs in the air | The court stays dark while they are in the players | The deafen and decay mechanics |

### League of Legends, Viego, Sovereign's Domination (the possession, 45 to 56 s, cropped)

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| A black pool with a glowing rim and a column of mist marks the body he is about to take | Where a soul is about to go in, its smoke is already gathering round the body | The target reticle |
| He dives in as a smoky streak; one bright burst of mist on entry | The entry is a dive and a single burst at the chest, then the soul is gone from view | Green, the beam |
| The possessed body keeps its OWN colours and shape, with a tint, a thin glow and one emblem over the head | **Possessed = the player, plus an aura.** No ghost figure is ever drawn on or in them after the entry. Their own silhouette stays readable | The crown |

### Honkai: Star Rail, Black Swan, Bliss of Otherworld's Embrace (0.5 to 14.5 s)

| What it does | What TUMP takes | What TUMP does not take |
|---|---|---|
| Opens on a black silhouette cut-in; the close shot is her face lit violet from below in a dark frame | Purple reads because the frame is near-black with ONE hot violet light, never because the frame is grey. v1's grey grade (saturation 0.38) is what turned violet to glass | The tarot cards and the stained glass |
| The payoff: the space round the enemies erupts in dark violet grasping hands and shards | Grasping hands of smoke are the most readable "something is coming for you" picture | Magenta and pink |

### What these overturn in v1

1. Ghosts as lathed translucent bodies (jelly bells): replaced by smoke with faces and claws drawn in light (Fade).
2. The procession, its cast (Lola, Sakristan, White Lady, Kandila souls), the candle: cut (owner).
3. The grey whole-frame grade: replaced by darkness with one hot violet (Black Swan).
4. A ghost-shaped sheet round the possessed: replaced by an aura of the player's own silhouette (Viego).
5. The pitch-black ending: cut. The ending holds on the players fighting it (owner, 2026-10-13).

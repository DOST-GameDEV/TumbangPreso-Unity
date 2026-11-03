# Gameplay trailer: animation and cutscene prompts

2026-11-03. Written for whoever captures the trailer's non-gameplay shots; the trailer's owner
reviews each clip. Shot numbers and times follow [the trailer draft](TRAILER_DRAFT.md).

## How to use

There are 19 prompts: one for the shared text cards, then one for each of the 18 non-gameplay shots in [draft 1 of the trailer flow](TRAILER_DRAFT.md). Each prompt is written for a Claude Code session in the TumbangPreso-Unity repo and makes one clip that drops straight onto the beat grid. Paste the shared rules block first, then the shot's prompt.

The grid comes from the trailer track (reference copy: `ArtSource/trailer/tump-trailer-reference.mp3`): 160 BPM, first downbeat at 0.03 s, so at 60 fps a beat is 22.5 frames, a bar 90 frames and a half bar 45. Frame numbers in each prompt count from the clip's first frame, with the handles excluded.

Gameplay shots (67% of the trailer) are not here. They are shot from the replay viewer as described in `docs/TRAILER_DRAFT.md`.

### Shared rules (paste before every prompt)

```
You are making one clip for the TUMP gameplay trailer. Read AGENTS.md, then docs/TRAILER_DRAFT.md
for the shot list and the beat grid, and docs/TRAILER_PROMPTS.md for the other prompts.
Until they are merged, both docs live on branch claude/gameplay-trailer-audio-draft-7831b9.
The track: ArtSource/trailer/tump-trailer-reference.mp3 is a reference copy for timing checks;
the master WAV stays with the trailer's owner and is never retimed. 160 BPM, beat 0.375 s,
bar 1.5 s, first downbeat at 0.03 s.

Output: 1920x1080 at 60 fps, ProRes 422 or H.264 at 40 Mbps or more, named shot-NN.mov in a
"TUMP Trailer" folder outside the repository, with 12 extra frames before and after the stated
length. Never commit footage, stills or stems.
Picture only unless the prompt asks for sound; keep any game audio as a separate WAV stem.
Use the game's own models, maps, effects and camera code. Capture what the game already does;
do not change gameplay, kits, networking or authored art to get a shot. Paete's and Phaister's
kits and art are protected. Human voice recordings only. No blue in any UI or text.
Text uses Darumadrop One, chalk white #F3F0E7 with the black in-game outline. UI hidden unless asked.
No full or broad PlayMode test runs. Do not build a player in a worktree.
If a beat cannot be hit exactly, get within 2 frames and say so. If something blocks the shot,
stop and report what blocks it with a still frame, rather than inventing a substitute.
Where a prompt says to ask, ask the trailer's owner before going on.
Finish with: file path, length in frames, the frame where each sync point lands, and one still.
```

## Text cards (all shots)

One pack of 31 alpha overlays the editor lays over the footage, so the cards stay one style from start to finish.

```
Make the trailer's text cards as overlays with alpha (ProRes 4444), 60 fps, one file per card,
saved to ~/Movies/TUMP Trailer/cards/NN-<slug>.mov. Each card's length equals its shot's length.

Look: Darumadrop One (the game's display face, see docs/FONT_USAGE.md), chalk white #F3F0E7,
a thick black outline matching UiTheme.InGameOutline, and a 4 px hard black drop shadow straight down.
No blue, no gradients, no glow.

Motion, locked to the beat: in on frame 0 at 135% scale and 20% opacity, settling to 100%
by frame 13 with one small overshoot. Hold. No out animation; the next cut removes it.
On one-beat shots (23 frames or fewer) skip the overshoot and land by frame 6.

Three layouts:
1. CENTRE: one or two lines centred, cap height about 12% of frame height.
   Use for rule cards, counts, map names on shot 26, ultimate names and the logo shot.
2. HERO: the hero name large at lower left (cap height 10%), the skill name under it at 45% size.
   A short underline in the hero's UiTheme colour slides in from the left over frames 4 to 12.
3. MAP: shots 21 to 25 use a lower-left map name at 6% cap height, no underline.

Cards (shot, in-time, layout, text):
6 0:06.03 CENTRE THROW | 7 0:07.53 CENTRE KNOCK IT DOWN | 8 0:09.03 CENTRE RUN BACK IN
9 0:10.53 CENTRE DON'T GET CAUGHT (appears on frame 45, the 11.28 contact)
11 DANTE/BOULDER | 12 YASMIN/GLACIAL WALL | 13 ILYAS/WATER WALL | 14 RAGO/MAGMA FIST
15 ZACK/BACKTRACK | 16 NEMU/FETCHER | 17 PHAISTER/TELEPORT | 18 AMIHAN/FEATHERFALL | 19 PAETE/LIANA LEAP (all HERO)
20 CENTRE 9 HEROES (appears on frame 68, the 26.66 hit)
21 TONDO | 22 ERMITA | 23 SITANGKAI | 24 BANAUE | 25 BGC (all MAP)
26 CENTRE 6 STREETS | 27 CENTRE 4 PLAYERS | 28 CENTRE 1 TAYA
35 MAYON AWAKENS | 36 ABSOLUTE ZERO | 37 AIRBURST | 38 MAKILING'S EMBRACE
39 OVERCLOCK | 40 LET THEM IN | 41 VOODOO DOLL | 42 CONTINENTAL DRIFT | 43 BAHA (all CENTRE)
Check hero, skill and ultimate names against the kits in Assets/TumbangPreso/Runtime/Abilities
before rendering; the displayed names win over this list.
Also export one 3-second contact sheet showing every card on a mid-grey frame.
```

## Cold open and title shots

The cold open's gameplay shots (2 to 4, 6 to 9) use one Tondo match at dusk with RAGO as taya, so shot 5 must match that match.

### Shot 1: studio badge (0:00.00, 59 frames)

```
Shot 1, 0:00.00 to 0:00.98, 59 frames. Black, with the studio badge.
Render the studio badge from SplashScreen.StudioIntro on pure black, centred, at 60% of its
menu size. It fades in over frames 0 to 12, holds, and is gone by frame 53, leaving
6 frames of black before the smash cut on the 0.98 drum hit. If the trailer's owner prefers no badge,
deliver 59 frames of black and say so. No sound.
```

### Shot 5: the taya taunts (0:04.53, 90 frames)

```
Shot 5, 0:04.53 to 0:06.03, 90 frames. Character animation.
On TONDO (scene Eskinita) at dusk, RAGO is taya with the red TAYA plate showing, standing
beside the can on its chalk circle. He plays the COME AT ME emote (Emotes.cs, id "tpose") at
the camera; if that reads as a T-pose rather than a taunt, use HA HA ("crouch") and say why.
Camera: medium shot from the throw line at chest height, lens about 35 mm equivalent.
Frames 0 to 15 hold still. Frames 16 to 89 push in slowly to a medium close-up (the pad
enters the track at 4.8 s). The emote's strongest pose must land on frame 68 (the 5.66 beat):
start the emote early or late to get it there; never speed it up.
The three attackers stay out of frame, and their slippers sit on the line in the foreground.
Shoot from a saved replay with the free camera so the take is repeatable.
```

### Shot 10: "Taya!" in the black (0:11.62, 25 frames)

```
Shot 10, 0:11.62 to 0:12.03, 25 frames. Black plus one voice line.
Picture: 25 frames of black.
Sound: the kid's recorded "Taya!" from SidewalkLife.KidTaya (the clip TondoFilm uses).
Export it as a 48 kHz WAV, trimmed so the consonant starts on frame 2 and the word ends before
frame 25; place it on a timeline against the trailer WAV at 11.62 s and check it against
ArtSource/trailer/tump-trailer-reference.mp3 so it is still clear over the music at about -6 dB under the music's peak. Do not pitch, stretch or replace
the recording; it is a human voice and must stay one.
Deliver shot-10.mov (black) and shot-10-voice.wav, and report the level you set.
```

## UI motion shots

All four are screen captures of real game UI, with UI shown. Stage them in a local match or with editor capture data; never through new runtime code.

### Shot 20: nine heroes (0:25.53, 90 frames)

```
Shot 20, 0:25.53 to 0:27.03, 90 frames. UI motion, hero select.
Find the hero select screen the game shows today (inspect BrandCharacterSelection,
ConvertedCharacterSelect and TumpPickerView to see which one is live) and capture it full
screen with all nine heroes visible in Roster.HeroPeople order: DANTE, YASMIN, RAGO, ZACK,
NEMU, PHAISTER, ILYAS, AMIHAN, PAETE.
Action: the cursor moves across the roster and the four seats lock picks on frames 0, 23, 45
and 68 (the beats), in this order: RAGO, YASMIN, ZACK, PAETE. Each lock plays the screen's own
select animation; do not add one. On frame 68 the 9 HEROES card lands, so leave the centre of
the frame readable from frame 60 on.
If the screen cannot show all nine at 1080p without scrolling, capture at 1440p and report it.
```

### Shot 27: four seats fill (0:36.03, 90 frames)

```
Shot 27, 0:36.03 to 0:37.53, 90 frames. UI motion, online room.
Capture the current room screen (LobbyCast, LobbyNameplates, LobbyChrome) as it fills.
The four seats fill one per beat: frames 0, 23, 45 and 68. Use four readable player names
with no real people's names or accounts: TALA, BUNSO, KUYA, ATE. Each seat shows its hero:
RAGO, YASMIN, ZACK, PAETE (the same four as shot 20).
Stage it with local or editor-only data. Do not connect to UGS, Lobby or Relay, and do not
change networking code. Chat stays empty. Leave the frame centre clear from frame 0 for the
4 PLAYERS card.
```

### Shot 28: the taya rotates (0:37.53, 90 frames)

```
Shot 28, 0:37.53 to 0:39.03, 90 frames. UI motion, taya rotation.
Show the red TAYA plate passing from player to player, the way the taya rotates each round
((round - 1) % 4). The four players are the same as shot 27.
First look for a real game view that already shows this: the round intermission, the break
card or the match bar (TumpMatchReadout.MatchBar, TumpMatchReadout.Board). If one exists,
capture four consecutive rounds' handover and retime the cut so each hop lands on frames
0, 23, 45 and 68. If none exists, compose the shot in an editor-only capture scene from the
real nameplate and TAYA plate prefabs, four players in a row, the plate hopping one seat per
beat with the game's own plate animation. Report which route you took.
The TAYA red is UiTheme.Defense (#E8203A). Leave room at the top for the 1 TAYA card.
```

### Shot 33: the overtake (0:45.03, 90 frames)

```
Shot 33, 0:45.03 to 0:46.53, 90 frames. UI motion, scoreboard.
Capture the in-match score board's overtake, where the score plates trade rows
(TumpMatchReadout.Board, commit ae36b27f7). Frame it tight on the board, scaled up cleanly;
render at 4K and crop rather than enlarging a 1080p capture.
The swap must start on frame 23 (the 45.41 beat). Set up the scores so the overtake comes
from a +100 knockdown: second place at 640 passes first place at 700. Show the +100 pop if
the board shows it. The rest of the HUD stays hidden.
If the existing PlayMode test TumpNativeHudTests drives this animation, you may run that one
test by its full name once to stage the state; no other tests.
```

## Ultimate cutscenes

Nine prompts, one per hero. Shots 35 to 38 are a full bar (90 frames); 39 to 43 are half bars (45 frames). Each runs on a different map from its neighbour so the fast run never repeats a backdrop. Every ultimate is captured live, because ultimate visuals in replay are still open (TAG-PHASE-1031).

Each prompt asks for the full cast to be recorded first, from the cast to the hand-back, then cut to the window. The authored film (UltimatePhaseView, HeroIntroductionScene) supplies the hero moment; the in-world effect supplies the payoff.

### Shot 35: RAGO, MAYON AWAKENS (0:48.03, 90 frames)

```
Shot 35, 0:48.03 to 0:49.53, 90 frames. Ultimate cutscene, the trailer's peak downbeat.
Map: PHILIPPINE ARENA. RAGO is an attacker; three bot rivals stand within 9 m of the spot.
Record the whole cast live from RAGO's screen and from a spectator view, cast to hand-back.
The volcano bursts on frame 0: no wind-up inside this shot (shot 34 carries the wind-up).
Frames 0 to 30: the burst throws all three rivals out, readable from a wide low angle.
Frame 67 (the 49.15 beat): the first lava bomb launches toward the nearest rival.
RAGO stays visible and Molten beside the volcano throughout. Pick the angle that reads best;
if the authored film's own shot of the burst is stronger, use it and say which shot it was.
```

### Shot 36: YASMIN, ABSOLUTE ZERO (0:49.53, 90 frames)

```
Shot 36, 0:49.53 to 0:51.03, 90 frames. Ultimate cutscene.
Map: BGC (scene Kanto). YASMIN casts; three rivals mid-run across the court, one of them
carrying a slipper back in, so the freeze stops a retrieval mid-stride.
Frames 0 to 21: YASMIN's cast pose from her authored film, or the cold wave leaving her.
Frame 22 (the 49.90 hit): every rival is Frozen in the same frame. Hold the frozen tableau;
slow push in from frame 22 to 89. Do not show the thaw or Chilled; the cut comes first.
Use a three-quarter high angle so all three frozen bodies read at once.
```

### Shot 37: AMIHAN, AIRBURST (0:51.03, 90 frames)

```
Shot 37, 0:51.03 to 0:52.53, 90 frames. Ultimate cutscene.
Map: SITANGKAI (scene LagoonCove), over the stilt lagoon court. AMIHAN casts with all three
rivals in front of her and two loose slippers on the court.
Frames 0 to 44: the monsoon builds; her authored film's release pose lands by frame 30.
Frame 45 (the 51.78 hit): the rivals leave the ground, Whirled, and the slippers fly toward
the arena edge. Frames 45 to 89: follow them outward; at least one body and one slipper
should cross the frame edge before the cut.
Shoot from behind and above AMIHAN so the fan of wind reads left to right.
```

### Shot 38: PAETE, MAKILING'S EMBRACE (0:52.53, 90 frames)

```
Shot 38, 0:52.53 to 0:54.03, 90 frames. Ultimate cutscene. Paete's kit and art are protected:
capture only, no changes to his kit, tree, rig or materials.
Map: BANAUE (scene Payo) at the terraces. PAETE places the guardian tree; three rivals within 9 m.
Frames 0 to 21: the tree rises.
Frame 22 (the 52.90 beat): the pull starts and all three rivals slide toward the tree.
Frame 67 (the 53.65 beat): the roots close and the rivals are Rooted.
Use a wide shot that holds the tree and all three rivals. If the authored film's shots are
replaced by the end of its first shot (the known UltimatePhaseView blocked-shot issue),
report which shots were blocked rather than working around it in code.
```

### Shot 39: ZACK, OVERCLOCK (0:54.03, 45 frames)

```
Shot 39, 0:54.03 to 0:54.78, 45 frames. Ultimate cutscene, the first half-bar cut.
Map: BGC (scene Kanto), night if the map has it. ZACK is Overclocked and holds the ultimate
to zap a rival within 9 m. Keep the approved electrical-outline look (commit 308fb345f);
do not bring back the removed hand zigzag.
Frame 0: ZACK already Overclocked, both hands raised. Frame 22 (the 54.40 beat): the zap
connects and the rival is Zapped. Tight two-shot, ZACK in the foreground.
```

### Shot 40: NEMU, LET THEM IN (0:54.78, 45 frames)

```
Shot 40, 0:54.78 to 0:55.53, 45 frames. Ultimate cutscene.
Map: TONDO (scene Eskinita) at dusk. NEMU casts LET THEM IN; three rivals in frame.
Frame 0: the possession lands on all three in the same frame, using the game's own
Possessed look (CharacterMotor.Possession). Frame 22 (the 55.15 beat): their steering is
visibly dragged; all three lurch the same way toward danger. NEMU small in the background,
still. Low angle down the alley.
```

### Shot 41: PHAISTER, VOODOO DOLL (0:55.53, 45 frames)

```
Shot 41, 0:55.53 to 0:56.28, 45 frames. Ultimate cutscene. Phaister's kit and art are
protected: capture only.
Map: ERMITA (scene IlalimNgTulay), under the viaduct. PHAISTER casts VOODOO DOLL.
Frames 0 to 21: the doll lies still beside her. Frame 22 (the 55.90 beat): it wakes and
stands as its own fighter, holding its own slipper. End on the doll and PHAISTER side by
side facing camera. Medium shot at doll height.
```

### Shot 42: DANTE, CONTINENTAL DRIFT (0:56.28, 45 frames)

```
Shot 42, 0:56.28 to 0:57.03, 45 frames. Ultimate cutscene.
Map: PHILIPPINE ARENA. DANTE slams the court; three rivals down the lane in front of him.
Frame 0: the slam. Frames 0 to 44: the five bands of earth erupt down the lane toward the
camera, one band about every 9 frames, and knock the rivals away. Camera low at the far end
of the lane looking back at DANTE, so the bands come at the lens. Cut before MOUNTAIN FORM.
```

### Shot 43: ILYAS, BAHA (0:57.03, 45 frames)

```
Shot 43, 0:57.03 to 0:57.78, 45 frames. Ultimate cutscene, landing on a strong hit.
Map: SITANGKAI (scene LagoonCove). ILYAS casts BAHA; loose slippers on the court, two
rivals grounded in the flood's path.
The 0.8 s warning happens before this shot. Frame 0 (the 57.03 hit): the flood front
reaches the camera side of the court. Frames 0 to 44: it carries the loose slippers and
nudges each rival once. Camera at ankle height so the front fills the lower half of frame.
```

## Slam and logo

The trailer ends on the slipper hitting the can at 1:00.72, so these two shots must cut together frame-tight with shot 46, the slow-motion throw.

### Shot 47: contact flash (1:00.72, 5 frames)

```
Shot 47, 1:00.72 to 1:00.80, 5 frames. The final hit.
Frame 0: the contact frame of shot 46's slipper meeting the can (same match, same replay,
same free camera, 0.25x), so the cut is invisible. Frames 1 to 4: pure white #FFFFFF.
Deliver both the 5-frame shot and the single contact frame as a PNG. Also deliver a
can-hit SFX stem from the game's own knockdown sound, placed so its transient lands on
frame 0; the editor mixes it about 12 dB under the music's final slam.
```

### Shot 48: logo and key art (1:00.80, 139 frames)

```
Shot 48, 1:00.80 to 1:03.12, 139 frames. Logo over key art; the track rings out to silence
by about 1:01.8.
Key art plate: the nine heroes in Roster.HeroPeople order, rendered in engine with their
current models and materials, standing in a loose arc around the can on its chalk circle
on TONDO at dusk, each holding their slipper, RAGO in the taya's place by the can. Use
each hero's own pose from their HOME film or emote set where one exists; do not repose
Paete or Phaister beyond their authored poses. Render a 4K still and a 139-frame plate with
a very slow push in.
Logo: use the owner's logo from ArtSource/ui/owner-brand-2026-09-13/ (logo.jpg and the logo
sheet) or the extracted main-logo.png, whichever is the newest owner version; ask if unsure.
Do not redraw it. Frame 0: it is already on screen at 110% scale over a white frame that
fades to the plate by frame 8; it settles to 100% by frame 10.
Frame 42 (1:01.50): the call to action fades in under the logo over 12 frames. The text is
not decided yet: render it with the placeholder COMING SOON and as a separate alpha layer.
Optional post-logo stinger, frames 72 to 138, only if it reads: the can wobbles once and
tips over off its chalk circle, with one can clink at the moment it lands. Deliver the shot
with and without the stinger.
```

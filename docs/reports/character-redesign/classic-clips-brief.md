# Classic cast: the action clips, redone for the redesigned models

Written 2026-10-07. One agent per character. Read this whole file, then your character's
`tools/author_character_redesign_<id>_clips.py` from top to bottom (its header says who this person is, what
their arms can and cannot do, and how the pose helpers work), then `docs/reports/character-redesign/classic-brief.md`
for the cast's rules.

## What is wrong today

Each Classic redesign has five clips of its own (`idle`, `walk`, `sprint`, `jump`, `fall`) and 28 clips copied
untouched from the old shared rig. The copied ones were made for a body with no elbow: on the new model they play
with a dead straight forearm, they barely move, and all twelve people do them identically. See
`Logs/character-redesign-classic/clips/bayan_old1.png` and `bayan_old2.png` for what that looks like: the "throw"
is an arm held out that twitches, the "punch" is a stiff chop, yes and no are a small nod and a small shake.

## What to make

Thirteen clips, added to `CLIPS` at the bottom of your clips script (a name in `CLIPS` replaces the copied clip
of that name when `tools/author_character_redesign_<id>.py` writes the glb; nothing else has to change):

| Clip | What the game plays it for | Shape it must keep |
|---|---|---|
| `holding-right` | carrying the tsinelas (slipper) in the right hand | loops; a held pose with a little life |
| `holding-right-shoot` | THE THROW of the tsinelas at the can | starts from and returns to the holding pose |
| `pick-up` | picking the tsinelas up off the ground; also the "bow" emote and "grab" | down and back up to standing |
| `attack-melee-right` | the taya's tag: a reach or jab with the right hand | from standing, back to standing |
| `attack-melee-left` | the shove, led by the LEFT arm | from standing, back to standing |
| `interact-right` | generic right-hand gesture (fallback for several verbs) | from standing, back to standing |
| `interact-left` | the same with the left | from standing, back to standing |
| `slide` | the retrieval slide along the ground | from standing, back to standing |
| `crouch` | out of breath; also the "victory" emote slot today | ENDS in the pose and holds its last frame |
| `sit` | the sit emote | ENDS sitting and holds its last frame |
| `die` | knocked down | ENDS on the ground and holds its last frame |
| `emote-yes` | yes, and "ready" | loops |
| `emote-no` | no | loops |

**Hard rules, because the game's timing hangs off these clips:**

1. KEEP EACH CLIP'S LENGTH exactly as it is in the glb today (read them out of the file first and write them at
   the top of your work). KEEP THE TIME OF THE KEY BEAT: when the old throw's arm is furthest forward is when the
   slipper leaves; when the old pick-up is lowest is when the slipper is taken; when the old tag is furthest out
   is when it lands. Measure those times from the old clip before you replace it.
   ⚠️ CORRECTION, later on 2026-10-07 (checked in `CharacterAnimator.cs`): five of the thirteen are only looked
   at, never timed against, and the old ones are a sixth of a second long, which leaves no room to move. THESE
   FIVE MAY BE LONGER:
   - `holding-right`: a loop of 1.6 to 2.4 s. A held carry with life in it: breath, a shift of weight, a small
     thing this person does with a slipper in their hand. First frame equals last.
   - `crouch`: 1.4 to 2.0 s of this person out of breath. It is played two ways (looped while fatigued, and
     held on its last frame as an emote), so the first and last frames must both be the full bent-over pose.
   - `sit`: up to 0.8 s of getting down, ending seated; it holds its last frame.
   - `emote-yes`, `emote-no`: loops of 0.8 to 1.4 s.
   The other eight keep their exact lengths and key beats.
   ⚠️ ALSO: in the game the THROW and the TAG are posed by code over the clip (`CharacterAnimator.ThrowBody.cs`,
   `CharacterAnimator.TagBody.cs`): the torso, the head and both UPPER arms are overwritten while they play, and
   your clip keeps the root, the legs and the two FOREARMS. So in `holding-right-shoot` and `attack-melee-right`
   put the character into the root and legs (the squash, the step, the hop), and keep each forearm's rotation
   something that still looks right on an upper arm pointing somewhere else: a moderate bend that opens toward
   straight at the key beat, no large twists.
2. Looping clips: the last frame equals the first. One-shots that return: first and last frame are the neutral
   stand your `idle` starts from (or the holding pose, for the throw), so they blend in and out.
3. Only the nine bones: `root`, `torso`, `head`, `arm-left`, `arm-right`, `forearm-left`, `forearm-right`,
   `leg-left`, `leg-right`. Squash and stretch on `root` scale and forearm stretch are allowed, as in your five
   clips. The slipper rides the right forearm's fist in the game, so the right fist has to be somewhere a
   slipper can be seen in `holding-right` (not buried in the body, not behind the back).
4. The game lays some poses OVER these clips (`CharacterAnimator.TagBody.cs` over the tag,
   `CharacterAnimator.LocomotionArms.cs` over walking arms). Skim them so your tag does not fight the overlay:
   keep the tag's legs mild.

**What makes it good:**

- Every clip is THIS PERSON doing it. Bayan throws like a man who clears the street; Lola Pacing throws like a
  grandmother who has thrown slippers at children for sixty years; Jun-Jun barely wakes up for it. Use the
  character notes already at the top of your clips script and the person's roster line. No two people in the
  cast should share a throw, a yes or a no.
- Cartoony and lively: anticipation before the action, a fast action, an overshoot, a settle. Squash on the wind
  up, stretch on the release. Arms bend at the elbow; they are never a straight stick unless that IS the pose
  (the end of a throw).
- Readable from behind and from 10 m, because that is where the player's camera is. Big silhouettes.
- `emote-yes` and `emote-no` are gestures with the arms and the body (a thumbs up, a fist pump, a wagging
  finger hand, crossed forearms where the body allows), not a nod and a shake only.
- `crouch` today doubles as "out of breath": hands on knees, heaving, in this person's way.
- `die` is a comic knock-down, not a death: this is a street game.

## How to work

- You own ONLY: `tools/author_character_redesign_<id>_clips.py`, the glb it writes
  (`Assets/TumbangPreso/Art/CharacterRedesign/<id>/`), and your stills under
  `Logs/character-redesign-classic/clips/<id>/`. Do not edit the model, the textures, the five existing clips,
  any C#, or any other character's files. Keep scratch files in a folder named for your character.
- Do NOT run Unity. The owner's editor is open on this folder. Blender only.
- Build: `& "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe" -b --python tools/author_character_redesign_<id>.py`
- Look: `& "C:\Program Files\Blender Foundation\Blender 5.0\blender.exe" -b --python tools/render_character_redesign_classic_clips.py -- <id> [clip ...] [--at 0,0.2,0.4,0.6,0.8,1]`
  then `py tools/sheet_character_redesign_classic_clips.py <id> <sheetname> [clip ...]`, and OPEN THE SHEET.
- This shell refuses compound commands: one plain command per call, PowerShell, `py` for Python.
- Look at every clip you write, from the front quarter and the side, at five or more times through it, and be
  critical: does the forearm pass through the head, chest or the other arm; is the pose readable as the thing it
  is meant to be without its label; is it this person or anybody. Fix and look again. Expect three or four
  rounds per clip.
- No em dashes anywhere, in code comments or in your report. Do not mention tools or assistants in comments.
- Do not commit.

## Lessons from the first three (added 2026-10-07, read before you pose anything)

- THIRTEEN DIFFERENT SILHOUETTES. The first Bayan set had carry, tag, reach and yes all reading as "right fist
  held out in front". Before posing, write one line per clip saying where the fists are at its key beat, and
  check no two lines match. Carry is a hold (cocked by the shoulder, or weighed at the hip); tag is a long
  straight reach; yes and no use height and width the others do not.
- A GESTURE FILLS ITS LOOP. The first `emote-yes` was a fist up for one frame in eight and a man standing
  still for the rest. In `emote-yes` and `emote-no` the gesture is on screen for at least two thirds of the
  loop: hold the peak, repeat it, and keep the body moving between. Never pass through a T-pose.
- Render with `--back` too. The player sees these from behind.
- THE KEYS SHEET is what the owner is shown: one large still per clip at its peak. Write
  `Logs/character-redesign-classic/clips/<id>/keys.json` mapping each clip name to the percent through it where
  its pose peaks (render a still at exactly those percents with `--at`), then run
  `py tools/sheet_character_redesign_classic_keys.py <id>` and `... <id> back`, and LOOK at both. If two tiles
  could swap labels, you are not done.

## Report back

The final sheet paths, the measured old lengths and key-beat times and your new ones, one line per clip on what
the person does, and an honest list of what still looks wrong.

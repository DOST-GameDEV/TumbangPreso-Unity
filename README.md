# TUMP trailer shots

This branch holds only the finished video clips for the TUMP gameplay trailer, for Paul's edit.
Nothing else from the game is here. Shot numbers and times follow `docs/TRAILER_DRAFT.md` and
`docs/TRAILER_PROMPTS.md` (draft 1, 160 BPM grid) on the branch `claude/gameplay-trailer-audio-draft-7831b9`.

New shots are added here as they are finished.

## How to get the clips

The videos are stored with Git LFS, so install it once (`git lfs install`) before cloning.

```bash
git clone --branch trailer-shots --single-branch https://github.com/DOST-GameDEV/TumbangPreso-Unity.git tump-trailer-shots
```

`--single-branch` matters: it downloads only these clips, not the whole game. To pick up shots added later:

```bash
git pull
```

To grab a single clip without git, open it on GitHub (switch the branch menu to `trailer-shots`) and press
the download button.

## Format

Every shot clip: 1920x1080, 60 fps, ProRes 422 HQ, picture only (no sound).
Each has 12 extra frames before and after its stated length (handles). Frame numbers below count from the
shot's first frame with the handles excluded, so shot frame 0 is the 13th frame of the file.

The text cards in `cards/` are ProRes 4444 with alpha, each as long as its shot.

## What each shot has

| Shot | Title | Trailer time | Length | File |
|---|---|---|---|---|
| 01 | Studio badge | 0:00.00 | 59 frames | `shot-01.mov` |
| 05 | The taya taunts | 0:04.53 | 90 frames | `shot-05.mov` |
| 35 | RAGO, MAYON AWAKENS | 0:48.03 | 90 frames | `shot-35.mov` |
| 36 | YASMIN, ABSOLUTE ZERO | 0:49.53 | 90 frames | `shot-36.mov` |
| 37 | AMIHAN, AIRBURST | 0:51.03 | 90 frames | `shot-37.mov` |
| 48 | Logo and key art | 1:00.80 | 139 frames | `shot-48.mov`, `shot-48-plate.mov`, `shot-48-cta.mov` |

**01, Studio badge.** The BH studios mark in white on black, at 60% of its menu size. It fades in over
frames 0 to 12 and is gone by frame 53.

**05, The taya taunts.** RAGO as taya beside the can in a live Tondo match, doing his COME AT ME taunt
at the lens. The arms-wide pose lands on frame 68 (the 5.66 beat). The camera holds still to frame 15,
then pushes in from medium to medium close-up. No slippers and no UI text in frame.

**35, RAGO, MAYON AWAKENS.** Live on the PHILIPPINE ARENA. The volcano bursts on frame 0 and throws all
three rivals out across frames 0 to 30, with motion blur. RAGO stands Molten beside the mountain the
whole time. Low wide angle, slow push in.
Differs from the prompt: the game launches the first lava bomb on frame 36, not 67. The burst was kept
on frame 0, the downbeat.

**36, YASMIN, ABSOLUTE ZERO.** Live on BGC. Frames 0 to 21 are the end of her cutscene: the ice spikes,
her cast pose, then the hand-back. On frame 22 all three rivals freeze in the same frame, and it cuts to
a high three-quarter wide that pushes in slowly on the frozen group. No thaw is shown.
Note: the game pauses under the cutscene, so the rivals are frozen mid-stride rather than mid-run.

**37, AMIHAN, AIRBURST.** Live on SITANGKAI. Frames 0 to 44 are the end of her cutscene: the monsoon
builds and she lands her release pose. Frame 45 is the hit: it cuts to a high wide from behind her left
shoulder, so the wind fan sweeps left to right. All three rivals are thrown, two of them off the stilt
court into the lagoon, and both loose slippers fly toward the edge.

**48, Logo and key art.** The nine heroes around the can on Tondo at dusk, each with their slipper.
- `shot-48.mov`: plate, logo and COMING SOON together.
- `shot-48-plate.mov`: the plate only.
- `shot-48-cta.mov`: COMING SOON alone, with alpha.

**cards/.** All 31 text cards, named `NN-<slug>.mov` after the shot they go over, plus
`contact-sheet.mov` showing them all. ILYAS's underline is his own colour, a deep indigo.

## Not here yet

- 10, "Taya!" in the black: blocked. The kid's "Taya!" in the game is a synthesized sound, not a real
  recording, so it needs a recorded voice first.
- 20, 27, 28, 33: UI captures, still to do.
- 38 to 43: the other ultimates, being recorded one at a time.
- 47: the contact flash. It depends on shot 46's replay.

# Bring It Home: silent picture handoff

Current master: `Logs/trailer/bring-it-home/trailer-v01.mp4`.
Quick review copy: `Logs/trailer/bring-it-home/trailer-v01_share.mp4`.
Source branch: `trailer/pandaigdig-plan`. Consult current Git HEAD.

120.000 seconds, 3600 frames, 1920x1080, 30fps non-drop, H.264, BT.709 limited
range, zero audio streams. Full decode and exact frame count passed for both
master and 720p review copy. Master SHA256:
`4ec35b24d3226c0191f8410cdfa3a9d1f4fd33594cbdfe55c688b0229ee5d1ad`.

0-20 original cinema; 20-88 native gameplay; 88-112 original cinema; 112-120
TUMP end graphics. Material boundaries are enforced by the compositor. Opening,
ending and neighborhood performances are newly directed. Existing models and
clips are materials; old map/hero films and game cutscenes are not inserted.

The final image review used127 ordered decoded frames, both sides of cut
boundaries and dense throw/contact/pickup/escape samples. One late wide gameplay
shot was replaced by a closer same-event angle and rechecked. The owner's monitor
was kept free of UI playback. Normal-speed human viewing/creative acceptance and
all sound remain with the owner/team; encoding checks do not certify emotion.

## Portable editing folder

Build with `python tools/trailer_handoff.py trailer-v01` after publishing source.
It writes `Logs/trailer/bring-it-home/delivery/TUMP_BringItHome_trailer-v01/` and
its ZIP. START_HERE.md explains editing. The folder includes the viewing movies,
selected encoded clips, relative-path OTIO timeline, CMX EDL, exact frame/source
manifest, original directors/tools, native event/replay evidence and sound guide.

The packager verifies timeline duration/relinking, actually runs the delivered
rebuild helper from a different working directory, compares video bitstreams,
checks file hashes and tests ZIP CRCs. PORTABILITY_CHECK.json and SHA256SUMS.json
inside the package record the result. A package is complete only when the command
finishes successfully. No paid service, Drive upload or marketing publication.

`Logs/trailer/bring-it-home/edits/trailer-v01/TRAILER_SYNC.csv` has 161 event,
retrieval, exit and edit cues at 30 fps. SOUND_BRIEF.md has the arc and a command to
attach the team's final mix while copying the picture stream. No assistant-made
music, SFX, voice or temporary score is included.

## Regenerate and resume

Use the full repository and Unity 6000.5.8f1 for image regeneration. The accepted
brief, PICTURE_EDL.json and PRODUCTION_CHECKPOINT.md preserve the direction. Tools
use Python 3, Pillow/numpy and ffmpeg/ffprobe. OTIO 0.18.1 is used only to author the
portable edit project; it was installed in ignored work/python-libs. The portable
picture rebuild requires only Python 3 and ffmpeg, with no Unity or account.

Final cinema: opening-v12-master 600 frames; ending-v12-master 540;
neighborhood-v03-master 180. Each passed 1/1. The four native sources are listed in
GAMEPLAY_SHOTS.md. QualitySettings was restored exactly after every owned Unity
run. Preserve the five pre-existing owner-dirty paths and unmerged source.

Next human action: watch the silent picture, then create/mix the team's sound.
Picture changes need a new encoded witness, regenerated cues and a refreshed
portable bundle. Do not restart rendering merely because an old checkpoint says
production was active. Retired raw drafts retain essential failure evidence but
cannot be re-encoded as complete takes. Current final source takes are intact.

# Trailer animatic: Acts 1 and 2

Owner, 2026-10-27: start the animatic with **The Throw** and **Our World**; refine until
satisfied; it must be very well animated; anyone on any PC with their own Claude must be able
to continue. The team writes the music; Claude decides where voices go.
Concept: [TRAILER_PLAN](TRAILER_PLAN.md). Working method for any Claude:
[tump-trailer skill](../../.claude/skills/tump-trailer/SKILL.md).

## Pick it up on another PC

1. `git fetch` and check out branch `trailer/pandaigdig-plan`.
2. Install: the Unity Editor named in `ProjectSettings/ProjectVersion.txt`, Python 3 with
   `pip install numpy Pillow`, and `ffmpeg` on PATH.
3. Render the shots (about 5 minutes, graphics required, one Unity job at a time):
   ```
   python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile trailer-1027 -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.Trailer.TrailerRecording.Street;TumbangPreso.PlayTests.Trailer.TrailerRecording.Arena" -testResults Logs/trailer/r.xml -logFile Logs/trailer/r.log
   ```
4. Cut it: `py -3 tools/trailer_animatic.py --version N` (next free number; see the log below). It also writes
   `animatic_vN_share.mp4`, a small 720p copy to send for review (the 1080p master is about 200 MB).
   Review frames per shot with `py -3 tools/trailer_sheet.py street` (or `arena`).
5. Read the iteration log, change the weakest shot, repeat. Write your round into the log and push.

Frames and videos live under `Logs/` (gitignored); they are rebuilt from source, so only
code and docs travel through git.

## Files

| File | What it owns |
|---|---|
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerStreetFilm.cs` | Tondo shots: can, kid, throw, elder, rack, tv, archive, vhs |
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerArenaFilm.cs` | Philippine Arena shots: spin, crane, crack |
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerBody.cs` | Acting: `Pose`, `Track` (keys, easing, overshoot), `TrailerBody` (bones, hands) |
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerRecording.cs` | Renders each film to PNG frames, 24 fps, 1920x1080, no UI |
| `tools/trailer_animatic.py` | The edit (EDL), looks, cards, sound cues, mix |
| `Runtime/Map/MapFilm.Picture.cs` | `OwnsLight` hook: off for every match film, on only for trailer films |

The trailer code sits in the PlayMode test assembly, so it never ships in the game.

## Voice or music: decisions

| Moment | Decision | Why |
|---|---|---|
| Cold open on the street | **No music**, only street sound (rooster, kids, a far tricycle) | Silence makes the stadium hit land harder |
| Kid shouts "Taya!" | **Voice**, the existing recorded kid line | Real, short, instantly Filipino; the only voice in Acts 1 and 2 |
| Match cut to the stadium | **Music hit + crowd roar** | First downbeat of the score |
| Title cards and Our World | **Music only** | Cards carry the words; a narrator would explain what the pictures already show |
| Archive and VHS | Music + texture sounds | Period flavour without words |
| Sari-sari TV | Music + tinny TV crowd, then the barangay erupts | The crowd is the voice |

Commentator lines start in Acts 3 and 4. Animatic music is **TEMP** (`ost_menu.mp3`), for timing only.

## Music sync points for the composer

Times are for the latest version; the edit script prints every segment's start when it runs.

| Moment | Music should |
|---|---|
| Street, kid, throw | Nothing (ambience only) |
| Match cut to the stadium | **Huge downbeat** |
| Can cracks off its mark | Accent, then cut to black |
| First title card | Nostalgic theme begins |
| Archive, VHS | Theme continues, gentle |
| City and street details | Groove builds slightly |
| PANDAIGDIG ident | Lift |
| Sari-sari TV eruption | Swell into the eruption |

## Iteration log

| Version | Changes | Still weak / next |
|---|---|---|
| v1 | First cut. Train, jeepney and laughing shots are placeholders from the map films (marked on screen) | Stiff arm-pointing acting; taya kid blocked the lens; rack slippers oversized; TV push ended inside Totoy; arena crack lost the can; daylight not golden |
| v2 | New acting system (`TrailerBody`): keyed full-body poses with elbows, anticipation, whip and overshoot, breathing idles, staggered jumps. Throw uses the game's charge/contact/follow shapes and a speed ramp into slow motion. Mang Kanor walks in, sets the crooked can upright, pats the kid. Golden-hour light; blue hour for the TV. Fixed framing issues from v1. New side-on wind-up angle cut into the can POV flight. Sent to owner as `animatic_v2_share.mp4` | Can shows its back label in kid and throw; TV push ends behind an adult's hair; archive lift hard to read; train, jeepney and laughing shots still placeholders |

## Owner notes

| Date | Note |
|---|---|
| 2026-10-27 | Start the animatic; focus on The Throw and Our World; refine until satisfied; very well animated; pipeline must be portable |

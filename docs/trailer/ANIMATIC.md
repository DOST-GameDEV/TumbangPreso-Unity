# Trailer animatic: Acts 1 and 2, "Lolo's Tsinelas"

Owner, 2026-10-27: start the animatic with **The Throw** and **Our World**; refine until
satisfied; very well animated; show the real game; tell a story; anyone on any PC with their
own Claude must be able to continue. The team writes the music; Claude decides where voices go.
Story and energy curve: [STORY](STORY.md). Whole trailer: [TRAILER_PLAN](TRAILER_PLAN.md).
Working method for any Claude: [tump-trailer skill](../../.claude/skills/tump-trailer/SKILL.md).

**Current version: v4** (49 s), `Logs/trailer/animatic_v4_share.mp4`, sent to the owner.

## Pick it up on another PC

1. `git fetch` and check out branch `trailer/pandaigdig-plan`.
2. Install: the Unity Editor named in `ProjectSettings/ProjectVersion.txt`, Python 3 with
   `pip install numpy Pillow`, and `ffmpeg` on PATH.
3. Record real gameplay (about 25 minutes; graphics required; one Unity job at a time):
   ```
   python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile trailer-1027 -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.Trailer.TrailerGameplay.Street" -testResults Logs/trailer/g.xml -logFile Logs/trailer/g.log
   ```
   Bots are not perfectly deterministic: on a new machine the knock-downs land on different frames.
   Re-pick the `("game", ...)` frame numbers in the EDL from `Logs/trailer/gameplay/street2/telemetry.tsv`
   (`can_up` 1 to 0 is a knock-down) and contact sheets of the three views.
4. Render the story shots (about 12 minutes; Arena first because the TV shows its frame):
   ```
   python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile trailer-1027 -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.Trailer.TrailerRecording.Arena;TumbangPreso.PlayTests.Trailer.TrailerRecording.Street" -testResults Logs/trailer/r.xml -logFile Logs/trailer/r.log
   ```
5. Cut: `py -3 tools/trailer_animatic.py --version N` writes the 1080p master and `animatic_vN_share.mp4`
   (send this one; the master is about 200 MB). Review shots with `py -3 tools/trailer_sheet.py street`.

Frames and videos live under `Logs/` (gitignored) and are rebuilt from source.

## Files

| File | What it owns |
|---|---|
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerGameplay.cs` | Real match with bots as the story's kids; fpv, line and side cameras; telemetry |
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerStreetFilm.cs` | 18 Tondo story shots (laugh, gift, sole, y1974, y1996, jeep, display, statue, crane, lineup, breath, windup, throw, payoff, lolo, tv, tvscreen) |
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerArenaFilm.cs` | Arena: hit (cause), then reveal with Rago (effect) |
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerBody.cs` | Acting: `Pose`, `Track`, `TrailerBody` |
| `Assets/TumbangPreso/Tests/PlayMode/Trailer/TrailerRecording.cs` | Renders the story films to PNG frames, 24 fps, 1920x1080, no UI |
| `tools/trailer_animatic.py` | The edit: EDL by beat id (A to P from STORY.md), looks, cards, sound cues, mix |
| `tools/trailer_sheet.py` | Contact sheets per shot |
| `Runtime/Map/MapFilm.Picture.cs` | `OwnsLight` hook: off for every match film, on only for trailer films |

The trailer code sits in the PlayMode test assembly, so it never ships in the game.

## Voice or music: decisions

| Moment | Decision | Why |
|---|---|---|
| Black open | **Voice**: the recorded kid's "Taya!" | The game's own sound as the hook; the only voice in Acts 1 and 2 |
| Real match, laugh | Street ambience and game effects, no music | Fun and real; leaves room for the score to enter |
| The gift onward | **Music** (theme enters) | The emotional turn |
| The breath | **Silence** | Stopdown before the throw |
| Arena hit | Can crack, crowd erupt, the big downbeat | Release |
| Night TV | Tinny TV crowd, then the barangay erupts | The crowd is the voice |

Commentator lines start in Acts 3 and 4. Animatic music is **TEMP** (`ost_menu`, `ost_match`).

## Music sync points for the composer (v4 times)

| Time | Beat | Music should |
|---|---|---|
| 0:00 to 0:05.5 | "Taya!", real match | Nothing, or light playful percussion |
| 0:05.5 | The laugh | Still nothing |
| 0:07.2 | The gift | **Theme enters**, solo, warm |
| 0:12.8 to 0:18.4 | 1974, 1996 | Theme grows, gentle |
| 0:18.4 to 0:23.8 | The world (jeep, display, monument, crane) | Groove builds, cut on each shot |
| 0:23.8 to 0:27.9 | Line-up, his real first-person view | Build, drum roll |
| 0:27.9 to 0:31.75 | Breath, wind-up, flight | **Silence**, then one breath |
| 0:32.07 | Arena hit (0.32 s into the shot) | **Huge downbeat** |
| 0:33.15 to 0:40.85 | Rago reveal, street payoff, Lolo | Full score, sustained |
| 0:40.85 | Card | Hold |
| 0:45.45 | Screen flashes the hit; 0:46.35 barangay erupts | Swell and hit |
| 0:48.55 | Black | Stop, into Act 3 |

## Iteration log

| Version | Changes | Still weak / next |
|---|---|---|
| v1 | First cut; placeholders from map films | Stiff acting, framing faults, daylight |
| v2 | `TrailerBody` acting system, golden hour, side-on wind-up | Owner: no story, no real gameplay, borrowed footage, hit after the crane, energy all over the place, ugly font |
| v3 | Rebuilt around STORY.md ("Lolo's Tsinelas"): real recorded gameplay (round 1 Totoy as taya, round 2 his first person); 18 new story shots; cause then effect in the Arena; aspect ratio carries meaning; Paalalabas font; the world shots built new (RAGO 7 jeepney, display, monument, crane) | Grade too red; small card text |
| v4 | Neutral grade on story shots; three-line card; pacing tightened (gift, 1974, line-up, reveal, TV) | Acts 3 to 6 not built; real score and recorded VO needed; 1974 newsreel could be clearer; gameplay picks are machine-specific |

## Owner notes

| Date | Note |
|---|---|
| 2026-10-27 | Start the animatic; focus on The Throw and Our World; refine until satisfied; very well animated; pipeline must be portable |
| 2026-10-27 | v2: "wtf is this" (rack); font "panget"; no story; game not shown; reused cutscenes; the zoom out before the can hit has bad flow; energy all over the place; think through every aspect |
| 2026-10-27 | Send with a self-contained handoff in chat, in case of changes |

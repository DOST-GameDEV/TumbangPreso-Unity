# Local match replays

The owner's October 7 request is a saved, explorable match rather than a fixed
camera movie or a three-highlight shortlist. Standard and Custom matches use
the same local recording path. Career reward eligibility does not control saves.

Open **Menu → Replays** to browse recordings. The same screen displays the save
folder, opens it in the operating system, and accepts another absolute folder
path. Changing the destination affects future recordings and the folder being
browsed; it does not move or delete footage already saved elsewhere. The default
is `Replays` inside the current local profile directory. Named validation profiles
have separate destinations and preferences.

The viewer provides a seek bar, pause/play, five-second backward/forward jumps,
separate Slower/Faster buttons (0.25×–4×), reset to1×, start/end jumps,
20Hz pose stepping, previous/next round, exact-time entry, free camera and P1–P4 follow. Right mouse steers the camera; WASD
flies, Q/E changes height, and Shift speeds movement. Space toggles playback,
arrow keys seek, comma/period step while paused, H hides/shows the interface,
and Escape returns to Home. Enter a time as seconds or MM:SS with optional decimal
seconds, then press GO. Invalid/out-of-range times leave the current position
unchanged. Typing a time does not trigger playback hotkeys. Hide UI
for clean gameplay footage captured with the owner's preferred video recorder.
Replay data itself is not an MP4 export.

## Data and lifetime

`LocalReplayRecorder` consumes the existing actual pose/world history. It detaches
three-second segments on the main thread, then compresses and writes managed
values on one background task chain. It keeps the existing bounded pose ring,
with at most two outstanding writes. It does not retain a whole match in RAM or
resimulate inputs. The viewer retains the current segment, one prepared next
segment and one pending disk read. It starts lookahead while drawing the current
segment, then installs prepared data at the timeline edge. Loading-induced
seam pauses are not treated as normal playback.

Each `TUMP-<date>-<unique-id>` folder contains `manifest.json` and numbered `.tps`
segments, with optional numbered `.scene.json` sidecars for recorded map state.
Sidecars capture actual BGC traffic poses, visibility and signal material-slot
colour/emission, Arena crowd response and pool swimmer/wake state. New Arena
recordings also contain optional `.fx.gz` sidecars with the actual rendered
effect quads, atlas cells, colours and camera-facing geometry at every active
render frame, including empty transitions. Playback uses the original material
and atlas with an owned mesh; it does not run the live effect pool.
These sidecars are independently hashed, bounded and
validated before use; older files without them still load. A segment and its
sidecar are committed atomically before their manifest entry. SHA-256,
identity/window validation, bounded decoding and a local-only short-tail decoder
prevent damaged data from becoming plausible footage. The existing network
highlight decoder and wire version remain unchanged.

A seek supersedes an earlier read without installing its stale view or error.
Only the requested segment can report a read failure; prefetch failures wait
until that segment is selected. Closing releases owned views and cached data,
and any unfinished managed read cannot recreate the closed viewer.

The manifest preserves match identity, map, mode, custom rules, time offsets and
rounds. Only active gameplay is on the replay timeline; loading, ready countdowns
and intermissions are not video recordings. A completed capture is distinguished
from an interrupted or incomplete one. Recording failure never awards a result
or silently labels missing footage as a complete match. Changing scenes flushes
the final available segment. A process crash can leave the most recent uncommitted
segment absent, while earlier committed segments remain browseable.

Playback loads map geometry through a replay-only `MatchInstaller` route. It
creates no live players, bots, match runner or score/reward flow. Recorded render
copies, lighting, fields, props and cues are drawn at the chosen replay time.
Current compatible game art is required; changed or missing recorded art fails
with an explanation. Keep the tournament build alongside its replay folders
when preserving footage across future map or character revisions.

## Acceptance

Native storage, natural short Custom capture, two-round Hero field/seek,
all-nine-hero/familiar catalog binding, same-menu folder changes, actual viewer
controls, 1080p/720p UI fit and continuous-segment audio checks pass. Compilation
is separate from input/device, full-effect fidelity and packaged acceptance.
Evidence and any reproduced failures belong in the dated laptop report, not in
an assertion that the entire game is tournament ready. Real peer recordings, full effect/prop transitions, dynamic map scenery timing,
long-session performance and physical viewer controls remain separate acceptance.

Replay shader animation uses recorded scaled time since the map loaded during the replay
camera render. Ordinary authored shader time, shader assets and shared globals
are restored afterward, including on failure. The replay adapter leaves the
ordinary game's material appearance unchanged. Seek and pause apply to water,
hologram, frost-band and supported spirit shader motion as well as poses.
Actual Arena crowd response uniforms and authored pool swimmer/wake parameters
are now saved in optional sidecars, applied at replay time, and restored afterward.
Traffic pose/property overrides are also scoped to that render; they do not call
road clocks, routes, hits or activate scene objects.
Closing a segment captures a detached current body, prop, field and scene endpoint
when the final rendered frames extend beyond the last scheduled pose sample.
The shared catch-history ring remains unchanged. Final effects belong inside the
saved clip window, rather than being accepted outside it or discarded. A small
tail immediately after a periodic segment flush follows the same path.

The current viewer reconstructs recorded gameplay and supported effects; ambient
scenery is supplied by the compatible map, with the traffic timeline stored in
new sidecars. Arena pool snapshots and their actual-material render copies have
focused same-camera checks, with saved short flashes and free-camera facing checks.
Full visual parity is not yet qualified: drones, ambient animals, remaining
transient state and full kit/map coverage need further recording and
same-camera/time verification. Older recordings cannot gain scene or effects state that was
never saved. Default-material shader comparisons do not prove all live ability,
particle or map states, and native Editor checks do not establish packaged parity.
Capture size depends on actual rendered frames and visible effect density.
The uncapped Editor's short-match measurements are evidence for that run, not a
promise about tournament storage or long-session performance.

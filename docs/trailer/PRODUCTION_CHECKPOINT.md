# Trailer production checkpoint

Branch `trailer/pandaigdig-plan`. Published checkpoints include ea7c927f1; consult current HEAD. Inspect
HEAD/status and [live runner state](PRODUCTION_JOB.json) before continuing.
Source changes after that commit are task-owned unless excluded below. Exact
frozen source hashes and fresh native XML belong to each take's job.json.

## Boundaries

- Background only overnight. Owner confirmed monitor now stays off. No UI playback,
  browser previews, window activation or input injection. No power settings changed.
- Verify actual Unity.exe command/PID. ONE job. Never edit Assets during a run.
- Team authors ALL sound. Silent picture: 0-20 original cinema, 20-88 native
  gameplay, 88-112 original cinema, 112-120 actual TUMP end graphics.
- No old map/ultimate films or game cutscenes in cinematic slots. No HUD/markers.
- No paid service, new agents/chats, resets, clean or force push.
- Preserve owner dirt: SceneryTextureIdentities.asset, tump_logo.png.meta,
  cloud-bank-a.png.meta, GraphicsSettings.asset and ProjectSettings.asset.
- QualitySettings baseline SHA256
  b12f8817b7355d4844c2a7cb0a057982de075131ae223850bc5b51244ad6b191.
  run_trailer_job.py restores exact pre-run bytes after its guard terminates.

## Current material and fixes

- Qualified Classic: classic-v03-world-master, 35 s, 1080p30, 1/1 pass.
- Qualified Kanto: hero-street-v01-master, 45 s, 1080p30, 1/1 pass.
  Both have external_visibility_scope=true and actual replay archives. Old
  classic-v02/hero-arena-v01 inherited ShadowsOnly; do not select those takes.
- Need qualified hero-arena 35 s and defender 30 s, same CLI kinds.
- Current cinema freezes/hides only live Stage geometry and dresses a constant
  flat competition deck at stage Y+2.5. Stadium bowl stays visible. Opening-v10
  predates this change. Render a NEW paired opening/ending.
- TrailerBody measures palm/fingertip geometry from actual forearm weights and
  bindposes. Rago forearm is .326 m, not old .162 m proxy. Pickup contact is now
  within about .013 m in inspected probes. Compress pose keeps feet planted.
- TrailerPropGrip measures true long X axis and model pivot. Flying motion follows
  mass center. Latest reaction IK lowered/outward to (-.85,.85,.2) relative to
  grounded Rago. Needs final image confirmation.
- ending-v09-present-probe proves stable set/pickup, but reaction prop obscured.
  Latest lowered reaction pose is newer than that probe.
- Street payoff v01 had foreground occlusion/stair staging. Latest source moves
  action six meters onto flat street, friends behind/right, uses explicit local
  BakeMesh(false) grounding and measured prop grip.
- neighborhood-v02-draft: 180 frames, 1/1 pass; framing/grounding improved in
  ordered samples. Completed Editor shutdown lingered; verified parent/importer
  stopped after complete frames, passing XML and final shutdown marker. Guard
  restored settings. Receipt is in its take folder.
- opening-v11-stage-probe: 19 samples, 1/1 pass and normal process exit. Stable
  stadium set, visible held slipper, center-based can contact and restore reviewed.

## ONE next action

Render opening-v12-master, ending-v10-master and neighborhood-v03-master at
1080p30 using frozen current source. Then qualified hero-arena 35 s and defender
30 s, select gameplay and finish edit. Consult live job JSON before launch.

## Tools and delivery

`python tools/run_trailer_job.py KIND --tag FRESH --width 1920 --height 1080 --fps 30 --seconds N`
Kinds: probe, opening, ending, ending-probe, neighborhood, classic, hero-arena,
hero-street, defender. Native seconds flag ignored by fixed-length cinema.

`tools/trailer_review.py` creates frame evidence and optional silent movies.
`tools/trailer_cut.py --tag trailer-v01` reads PICTURE_EDL.json (still to author),
enforces source/block boundaries and writes 3600-frame silent 709 master + share
copy. Title-only mode passed: actual TUMP logo and Paalalabas font. No empty BH logo.
`tools/trailer_sync.py TAG` derives sound cue sheet from encoded picture witness.
Verify visual contact times against retained frames before handoff.

Need final 120-second picture, source/edit inputs and frame sync handoff. Replay
data is real native match evidence; do not promise full future visual parity.
Media: Logs/trailer/bring-it-home (ignored). No Drive uploads. Keep one clear final
and essential failure evidence. Old opening-v03/v04/v05 raw pruned with receipt;
movies and critical frames remain. Temporary HTTP server/browser tab closed.
Do not reopen overnight.

Hourly heartbeat tump-trailer-overnight-continuation ACTIVE. Pause after complete
final picture/source/sync delivery. Push coherent source checkpoints.

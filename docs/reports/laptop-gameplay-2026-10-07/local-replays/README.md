# Full local match replays — laptop, October 7

Status: focused feature implementation and native checks pass. No whole-tournament or
Desktop/current-package claim. PC retains network/history/caught-camera ownership.

Original behavior retained only three short in-memory highlights (`Capacity=3`),
with no local full-match library or explorable viewer. The new lane adds automatic
local recording, including Custom, a same-menu save-folder display/open/change,
full active-gameplay timeline, pause/play, rewind/seek, speed, free camera and
player follow. It reuses recorded poses/props/fields and never replays game inputs.

## Evidence so far

- Direct Roslyn five-assembly compilation passes on frozen native-base120d plus
  replay overlays; 1,484 C# input hashes and no drift. This is not native gameplay.
- Native1 (Unity54268): storage multi-round/custom/short-tail/wire-minimum and
  corruption controls PASS; interrupted/folder preservation PASS. Natural Custom
  match completed, but Windows manifest replacement failed. Source13 prefs,
  four profile originals and all21,333 source inputs restored. Original failed
  source/results remain. Reader sharing and transient rename handling corrected.
- Native2 (43472): actual single ReadyGate starts a naturally completed30s Custom
  match; committed10 segments/29.9798s. Standalone readback then exposes runtime
  HandAnchor missing from the catalog prefab. Only that proven non-rendering node
  is reconstructed; changed real bones still fail. Disk controls remain PASS.
  The earlier fixture manually began beside ReadyGate and produced two IDs; that
  fixture error is preserved and corrected rather than called a game defect.
- Native3 (56808): a newly added AudioListener lookup had one ambiguous Object
  compile error. Fixed with UnityEngine.Object qualification; zero gameplay
  cases ran, all source/preferences restored. This failed attempt is retained.
- Native4 (63388): read-only reuse of the actual native2 saved Custom replay PASS
  through actual Timeline/button listeners, seek17/rewind2, pause/play,2x/follow,
  free render-camera movement and clean UI. No live CharacterMotor/SliceRunner.
  Actual960x720 world frames inspected, all21,333 source inputs/prefs restored.
- Native5 (58740): 10 actual cases,9PASS/1FAIL. Natural Custom capture/readback,
  storage controls, actual-widget readback, archive disable/rebind/recovery and
  both normal/interrupted live visibility restoration PASS. Hero two-round test
  finds last-pose-window loss across ring reset. Recorder now flushes on round/
  intermission events before LateUpdate clears history. Runner expected9 was a
  classification count error; raw XML actually contains10. Do not erase it.
- Native6 (55728): Hero actual IceSheet disk/seek and both round tails PASS;
  all3 archive lifetime controls PASS. Native1080 UI catches help-line height41
  versus rectangle34; corrected height46 and separated toolbar rows.
- Native7 (59580): actual viewer controls and native1080/720 UI content/action
  bounds PASS. All-hero reference fixture calls ApplyModel before Awake and fails
  its material-block initialization; corrected fixture to follow real lifecycle.
- Native8 (65052): all9 current Hero/Familiar catalog record/encode/decode/bind/
  render paths PASS, actual controls/1080+720 UI and focused5s timeline nav PASS.
  This does not establish every hero ability's complete footage fidelity.
- Native9 (65360): actual Home hamburger REPLAYS route, displayed save folder,
  same-screen destination change and library1080/720 UI PASS. Viewer controls,
  quarter-speed label, outlined status and1080/720 toolbar PASS.
- Native10 (54984): actual playing AudioSource survives continuous segments and
  the actual controller's automatic boundary load; an explicit seek stops it.
  Actual controls/native UI regression PASS. This is functional audio-state
  evidence, not a listening/mix-quality judgment.

## Scope and remaining limits

Only active gameplay is recorded; ready/loading/intermission movies are excluded.
Existing local formats and the network clip decoder minimum/wire14 are preserved.
The disk writer is bounded and asynchronous; partial/error recordings are labeled.
Corruption does not become plausible footage. Folder changes preserve old files.
Use the tournament build with its saved folders when preserving footage for later
map/art revisions. This implementation is replay data, not direct MP4 encoding.

Current native gameplay render captures are960x720. The1080/720 UI captures
composite that world image to qualify UI layout/type, not1080 world/GPU performance.
Renderer follows window resolution (up to3840 wide) and current MSAA; a physical
window/device/performance matrix remains. Full ability/prop transition fidelity,
exact dynamic map scenery timing, long sessions and packaged/real-peer recordings
need their exact remaining checks. Ambient scenery comes from the compatible map,
not a saved full map simulation. Do not call traffic/wildlife timeline-exact. Native scripted controls do not establish physical device or
whole-network acceptance. LAN remains unavailable after the owner moved networks.
The report preserves raw evidence with -text attributes and SHA-256 receipts;
private profile before/after content remains outside Git. The worker is a validation
identity, not the shipping Desktop build; its stale Git stamp is not a tested-tip
claim. Current exact frozen source is in each qualified-source.json.gz.

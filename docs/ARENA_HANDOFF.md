# The Arena map: HANDOFF (written 2026-10-05)

Read this first. It is the current state of the Arena map (TODO ARENA-1) and stands alone.
Detail lives in [ARENA_MAP_BRIEF](ARENA_MAP_BRIEF.md) (gameplay design and every system, sections
ARENA-1.1 to 1.10) and [ARENA_ART_BRIEF](ARENA_ART_BRIEF.md) (the look, the numbers, the kit rules).
The pipeline lessons come from [ILALIM_REWORK_GUIDE](ILALIM_REWORK_GUIDE.md)'s HANDOFF block.

## What the map is

A STADIUM FLOATING IN THE NIGHT SKY OVER A NEON CITY. A saucer hull carries a round stadium: a
hover stage of round platforms over an open shaft (radius 40) in the middle of a turf field, a full
lower bowl, four upper stands with open corners, a canopy with floodlights over each, a centre-hung
scoreboard, a screen in each corner, about 26,000 spectators as animated sprites. Beyond it: 23
towers placed by sightline from the can, giant hologram ads (with PC Express), a tethered slipper
mascot balloon. The stage REARRANGES between rounds through five round layouts (plaza, tore, krus,
hukay, entablado) in an 8 s show. A player who falls is caught by the rescue drone SAGIP, hauled
back and frozen with the tag's 5 s. Scene: `Assets/TumbangPreso/Scenes/Maps/Arena.unity`, last in
`SceneFlow.Maps` (index 6), protocol 146.

## Branch and rules

- Worktree `.claude/worktrees/kanto-blender-assembly-909442`, branch `QoLUpdates`. PUSHED at
  `aaa66cb2d`. Everything after it is LOCAL ONLY (the character shading setting, the docs, the
  Dante prototype `a883bbe70`, and all of the Arena). Push only when the owner asks.
- Never commit the owner's files: `Resources/UI/composition-redesign/*.png.meta`,
  `ProjectSettings/ProjectAuditorSettings.asset`, `TimeManager.asset`, `QualitySettings.asset`,
  Unity's churn under `Resources/UI/input/xelu/**`, `Art/IlalimRebuild/Materials/prop_glass__*.mat`,
  `Art/LagoonCove/Materials/court_chalk.mat`. `ArtSource/arena/*.blend` and `kits/` are NOT
  committed (the scripts rebuild them); `ArtSource/arena/brand/tump_stamp_owner.png` is.
- No em dashes anywhere. Commit trailer: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- The character redesign (Dante) is ANOTHER session, in worktree `dante-character-redesign-873f79`
  with its own Unity editor: never touch that process or `Art/CharacterRedesign`.
- **Unity.** The owner usually has the editor OPEN on this worktree and plays the map there. While
  it is open: no batch Unity, C# edits recompile live (keep them compiling), new art needs a scene
  rebuild that only he can run (**Tumbang Preso > Sample Map > Build Arena**) or that needs the
  editor closed. Batch (editor closed): `py -3 tools/run_unity_guarded.py -batchmode -force-d3d11
  -tp-profile arena ...`, one run at a time, NEVER `--help` (it opens an editor), never kill a
  Unity process. Check which worktree an editor has open by its command line before assuming.
- **Compile check without Unity:** `bash <scratchpad>/cc_int.sh TumbangPreso.<Assembly> [new .cs
  files]` (Core, Runtime, Editor, PlayTests, in that order; the C:/ form of the path). The script
  lives in an old session's scratchpad (`.../410a99e0-.../scratchpad/cc_int.sh`); if it is gone,
  the Ilalim guide's HANDOFF says how to rebuild it from `Library/Bee/artifacts/*.dag/*.rsp`.
  Shaders only compile in Unity.
- Work is done by parallel background agents with separate file scopes, each told the rules above;
  the coordinator inspects their pictures before showing the owner. Agents cannot hear or see the
  game: say plainly what is unverified.

## The pipeline

1. **Kits in Blender 5.0, headless**: `tools/author_arena_<kit>.py` with
   `tools/author_arena_textures_<kit>.py`, kits `bowl`, `roof`, `hull`, `city`, `stage`, `holo`
   (shared numbers and mesh helpers in `tools/arena_kit.py`). Each writes
   `ArtSource/arena/kits/<kit>.blend`, textures to `Assets/TumbangPreso/Art/Arena/Textures/`
   (`arena_<kit>_<surface>.png` and `_emit.png`), review pictures to `Logs/arena/<kit>/`.
   The crowd's sprite atlas is `tools/author_arena_crowd.py`.
2. **Layout data**: `tools/author_arena_layouts.py` writes `tools/arena_layouts.json` (five round
   layouts; disc, ring, arc, ramp pieces; pads; pickups; `catchY`). The stage kit builds the art
   pieces `stage_<layout>_<id>` from it; `ArenaStageMesh` builds the colliders from it.
3. **Assembly and export**: `blender -b --python tools/author_arena_assembly.py -- --version=vN`,
   then `blender -b ArtSource/arena/arena_assembly.blend --python tools/export_arena_unity.py`
   (writes `Art/Arena/Models/`, `Stage/`, `Props/` and `arena_layout.json`; Blender (x, y, z) is
   Unity (x, z, y); it reads its output back and must end `EXPORT_OK`).
4. **Unity**: `ArenaSceneBuilder.Run` (menu Build Arena) writes the scene; `ArenaArtPlacer.Rules`
   is the ONE material table (shader `TumbangPreso/ArenaPainted` with an emission map;
   `ArenaGlow` for unlit light). `ArenaSceneBuilder.RunReview` renders stills to
   `Logs/arena/unity/vN/` (the editor review does NOT run bloom: judge glow from play).
5. **Probes** (PlayMode, batch): `ArenaStageProbe` (floor, reach, fall), `ArenaMatchProbe` (a bot
   match through layout changes), `ArenaPerfProbe`, `ArenaBalloonProbe`, `ArenaBoundsProbe`; output
   in `Logs/arena/unity/`. They were last RUN before the deeper fall, the updraft, the 18 s
   halftime, the balloon and the off-stage slipper rule: their expectations were updated by hand
   and have not been run since.

## What is in the game, system by system (files under `Assets/TumbangPreso/`)

| System | Where | State |
|---|---|---|
| Layout rotation, hologram preview, colliders | `Runtime/Map/ArenaStage.cs`, `ArenaStageMesh.cs` | Ran in Unity (probe passed before later changes). Layout derived from match id and round; nothing sent. |
| The break's show (alarm, move with locks and shake, reveal), drones lifting players, break camera | `ArenaShow.cs`, `ArenaBreakCamera.cs`, `HalftimePresentation.cs` | Played by the owner. The dim was removed at his request. Ordinary break 8 s; HALFTIME is 18 s (10 s package, then the show), code only, unplayed. |
| The fall | `ArenaFallRecovery.cs`, `CharacterMotor.EdgeRecovery.cs` (kind Drone), `MatchRpc.MoveFloorY`, `KillPlane.Height` | Catch at y -22. UPDRAFT in the shaft (gravity 8, terminal 3 to 10 m/s, 3.3 s to the catch), camera and aim stay the player's for the first 10 m. Paete's vine hauls a falling body to a deck within 9 m (`PaeteVine.MapCatch`, `CharacterMotor.BeginHaul`). Code only since the slow-fall change. |
| Rescue drone SAGIP and its comic beam | `ArenaDrone.cs`, stage kit | Exported; in the scene after the owner's rebuild; look unconfirmed by him. |
| Pads and pickup | `JumpPad.cs` (restyled per map), `ArenaSpeedPad.cs`, `ArenaStaminaPickup.cs`, `CharacterMotor.BeginSpeedBoost` | Probe passed. The pickup's taken state is inferred on clients (no message yet). |
| Bots | `AIController.EdgeSense` (edge probing) | 6 falls in 110 bot-minutes in probes. |
| Off-stage slippers | `ArenaFallRecovery` | A slipper with no stage under it returns to the nearest standable point after 1.6 s. Code only. |
| Balloon easter egg | `ArenaBalloon.cs`, `Net/MatchRpc.ArenaBalloon.cs` (ONE new host message), holo kit | A 90 per cent charge throw within 10 degrees of the balloon strikes it; five hits pop it; re-inflates next round. Code only. |
| Crowd (sprites) | `ArenaCrowd.cs`, `Shaders/ArenaCrowd.shader`, `Editor/MapKit/ArenaCrowdBuilder.cs`, `tools/arena_rows.json` | About 25,900 quads, 12 draw calls. In game. |
| Effects, haze, searchlights, show spots, glare | `ArenaFx.cs`, `ArenaAmbience.cs`, `ArenaGlare.cs`, `ArenaPainted.shader` (haze globals) | In game. The spots' ORIGINAL beams were restored at the owner's request; camera glare added on top. |
| Clouds | `BlockyClouds` with the Arena row's own bands in `WorldLookProfile` | Code only. |
| Glow | `WorldLookProfile.MapLook.Bloom/BloomThreshold` (Arena: 0.16 above 1.9), `ColourGrade.BrightLook.cs`, `PostAntiAlias.cs` | A map's own glow shows under every lighting style and keeps HDR under MSAA. The owner called 0.32/1.7 "a bit too much"; 0.16/1.9 is unconfirmed. Off on the Low graphics tier. |
| Holograms and ads | holo kit, `ArenaHoloMotion.cs`, `ArenaHoloAuthor.cs` | Flat ad columns (owner: not tubes), globe, line figures, PC Express from the project's own artwork. Column heads now fade by vertex alpha: NEEDS A SCENE REBUILD. |
| Sound | `ArenaCrowdAudio.cs`, `tools/synth_arena_*_sfx.py`, `Runtime/Audio/AudioCues.cs` | See "In flight" below. |
| Map card, preview, look row | `Resources/UI/map-cards/Arena.png`, `SceneFlow.MapRegistry`, `WorldLookProfile` | Done. |

Shared code changed for this map (each gated or defaulted so other maps are as before, by reading,
partly by test): `MatchHost.SeatOnFloor` (a real bug fix for every map: it stopped on the body's
own capsule), `CameraRig`, `SpectatorCamera`, `MapCameraRange` (far plane 1300 m in play),
`WorldOutline.SetFade`, `JumpPad`, `Slipper.HostFinishMapRecoveryAt`, `Carrier` (one call),
`MatchRpc` (move floor, the balloon message), `KillPlane`, `PaeteHazards/PaeteHeroKit`,
`CharacterMotor` (speed boost, map fall rule, haul), `BlockyClouds`/`BlockyCloud.shader`,
`TumpRoundSwapView.CourtBreak`, `RecordedWorldView`, `VoiceDirector` (captions), `PostAntiAlias`.

## IN FLIGHT when this was written (three background agents; check what landed)

1. **Real crowd recordings.** The owner rejected the synthesized crowd ("just sounds like noise",
   no chants, no reactions heard) and APPROVED downloading 28 named CC0 files (about 80 MB) from
   Freesound's preview CDN and Wikimedia Commons. Raw files go to
   `C:/Users/StarX/.cache/tump-audio/crowd-src/` (outside the repo); provenance in
   `Resources/Sfx/ARENA_CROWD_SOURCES.md` and `tools/arena_crowd_sources.json`; builder
   `tools/build_arena_crowd_from_recordings.py`; cues keep their names (`sfx_arena_crowd_*`,
   `sfx_arena_chant_*`). The three chants of the game's own words (TUM-BANG PRE-SO, TA-YA, TUM-BA)
   are to be removed from rotation until the TEAM RECORDS them. The same agent makes reactions
   audible (levels, lazy clip loading, chances) and gives a self-save its own cheer. Nobody can
   listen: the owner must audition, flagged cuts first.
2. **Announcer voice clone.** The announcer is a teammate who AGREED to cloning; the owner approved
   the Chatterbox model (MIT) locally. Environment at `C:/Users/StarX/.cache/tump-voice/venv`
   (outside the repo). Script `tools/clone_announcer_lines.py`, lines in
   `tools/arena_announcer_lines.json`, takes to `Resources/Vo/vo_<id>_<n>.wav`, candidates in
   `Logs/arena/voice/`, PA versions baked by `py -3 tools/synth_arena_crowd_sfx.py --no-vo --only
   pa`, and every generated file labelled AI-cloned in `docs/HUMAN.md` and
   `Resources/Vo/AI_CLONED_LINES.md`. `--no-vo` must stay: the coordinator chose not to grain the
   real takes into the crowd.
3. **Match-start cinematic** (owner's request with a Blue Lock frame): tunnel, walk out into the
   floodlights' glare with the crowd drowning out, the bowl revealed and the roar, the screens
   flickering through the four characters and landing on the TAYA, then the arena gets built. New
   files (`Runtime/Map/ArenaIntro.cs` and helpers), a probe `ArenaIntroProbe.cs`; it must not edit
   `ArenaCrowdAudio.cs`, `VoiceDirector.cs` or `AudioCues.cs`.

If their work is in the tree but uncommitted, read it, compile-check, and commit it; if an agent
died half way (a session restart killed three earlier), restart it from its files.

## OPEN, in the order to take them

1. Land and commit the three agents above; tell the owner what needs a scene rebuild.
2. **Close-editor checks.** Nothing since `8d8d10fd2` has been run in batch: rebuild the scene, run
   all five probes plus the intro probe, render in play, and look. Then the tests most likely
   touched (the list in the brief's ARENA-1.5 report: MatchLoadingReadiness, MapExperience,
   CurrentPauseExit, the Ilalim probes).
3. **Known faults not yet fixed:** the taya's chalk box floats over the gaps on tore, entablado and
   hukay; replays do not record the drone, and show the crowd, screens and balloon in their present
   state; a held slipper lost in a fall still returns to the owner's mark after 8 s (the owner was
   asked whether he wants the fast rule); three RooftopRecoveryProbe cases fail and whether an
   Arena change caused it is not established; NationalsHardeningTests wants `AIController.EdgeSense`
   in its switch list; the pickup's taken state has no message.
4. **Owner decisions pending:** the glow at 0.16/1.9; the balloon (he chose option A, the seated
   mascot; the rotating slipper HOLOGRAM is still in the scene to compare); whether the eight soft
   discs at the spot beams' ends come back; the dense downtown from the air (open sky fell from 171
   to 75 degrees when the towers were enlarged at his request); real recordings of the game's own
   chants and of the nine arena announcer lines if the clone is not good enough; whether to push.
5. **Never done:** an online match with a second player and a late joiner (layout, carry, balloon,
   deep fall, 18 s halftime); a friend's weaker PC (from the can: 0.8 to 1.7 M triangles, about
   850 to 1030 set-pass calls before the holograms and effects were added); LODs (none; the
   candidates are the bowl's seat profile, the roof's truss steel, the hull body).
6. Ilalim's open list is unchanged (the Ilalim guide's HANDOFF): not played as a full match since
   the court moved; friends' frame rate unmeasured.

## The owner's direction, in his words (so it is not relitigated)

- Scale and concept: "i want a large arena similar to a soccer or football or baseball stadium. i
  need the map large"; "concept is floating arena in the sky with cyberpunk-esque aesthetic"; the
  ROUND layout ("i liked the previous layout") in the night stadium's colours ("more
  bluelock/rocketleague stadium in aesthetic", "night time"); "wanna incorporate our logo too"
  (the stamp drawing on the turf "one or 2" colours).
- Craft: "need you to be more critical of the work"; real connected meshes, no overlapping faces,
  nothing floating; "i need you to think more about sightlines"; textures in "the same
  artistic/illustrated artstyle".
- Play: "map transformation is so dull, theres no emphasis on it. could also use some screenshake";
  "also the screen goes dim when the platform switches" (so: no dim); "falling off threshold is too
  high, you need to fall further"; "the fall effect is too fast ... it doesnt even have much of a
  time window to let me clutch back up"; "halftime replay is interfereing with the transformation
  animation".
- Dressing: "buildings should also be more visible, we should also add a distance haze effect for
  outside the arena, moving spotlights, use the clouds we have in the other maps too"; gigantic
  hologram ads "should include pc express"; "the ad columns should be straight flat, not tubes";
  "can we make the ads have a more natural top edge"; the balloon "should be animated" with the
  throw easter egg, "do it enough times and itll pop", and "i like A".
- Light and sound: "look into emmissives for the arena so things are glowy", shown "under all
  lighting styles", then "i think the bloom is a bit too much"; "the old spotlight beam was good, i
  mean i wanted a camera glare when iit was pointed at you"; "drone design and ufo effect needs to
  be more stylized"; "there should be reverbey crowd cheers, chants, and an announcer"; of the
  synthesized crowd: "just sounds like noise and not actual crowd cheers"; "you could probably
  clone the voice of the current announcer".
- The opening: "the players walk out onto a field, they walk into the bright glare of the stadium
  lights alongside a drowning out of the cheers before the glare disappears and the crowd cheers get
  louder. a screen billboard flikers through all the characters and then lands on the person
  playing taya to display whos defending.. then the arena gets built".

## Things that cost time (do not repeat)

- The first stadium tries were boxes stacked together, with coplanar faces; the owner saw it at
  once. Every ring-shaped thing is now one closed lathed mesh, and the stage kit runs a joint and
  height checker on every build (it must print zero).
- Towers were placed for the aerial picture and were invisible from the can. `rim_elevation` in
  `tools/arena_kit.py` and the kits' SIGHT reports exist for this.
- The synthesized crowd was a waste of a round: real recordings were the answer.
- A feature was misread twice from a short remark (the spotlights; "get rid of it" earlier): when a
  remark could mean keep-and-add or replace, ask or do the smaller thing.
- Bloom looked absent to the owner while present in probes: his MSAA setting turned HDR off on the
  game camera. Check `settings.json` under `AppData/LocalLow/BH Studios/Tumbang Preso` before
  theorising about what he sees.
- Agents sharing a scratchpad overwrote each other's patch files, and an agent treated a
  coordinator message as untrusted until it was resent: name scratch files per agent, and expect to
  confirm.
- A concurrent commit can sweep up another agent's half-finished files: commit by path while agents
  are running.

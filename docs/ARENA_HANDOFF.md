# The Arena map: HANDOFF (written 2026-10-05, brought up to date 2026-10-06 when the work paused)

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
`SceneFlow.Maps` (index 6), protocol 152 (it was 146 before the merge with ASTRAReworks, which had used 146 to 151).

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

## STATE AT THE PAUSE, 2026-10-06 (read this before anything below it)

The owner paused map work here ("we'll take a pause from map making for now") to work on the
character redesigns' first-person hand effects. The three jobs that were in flight on 2026-10-05
are all resolved, the branch was merged with ASTRAReworks and the redesigns, and it was played
online once.

**Branch.** `QoLUpdates`, pushed at `0d47e76df`; one commit after it is local
(`d06fa5125`, the screens' replay trigger). It CONTAINS all of `origin/ASTRAReworks` as of
2026-10-05 (merge `36877565f`) and the character redesigns (merge `f6f79319a`). ASTRAReworks itself
was NOT pushed to: the owner chose to push QoLUpdates only. **Protocol 153.**

**What happened to the three jobs.**
- Crowd from real CC0 recordings: landed (`05ddd36d1`). The owner has not said whether it reacts
  properly since the mixing fault below was fixed.
- Announcer voice clone: CANCELLED by the owner ("i plan to just replace it"; and of the Filipino
  lines: "why is it in tagalog? just make it english lines.. like Lata is down"). The cloned takes
  are deleted. The lines are English now and wired (`tools/arena_announcer_lines.json`,
  `ArenaCrowdAudio.Lines`, `VoiceDirector.CaptionFor`); he will record them himself as
  `Resources/Vo/vo_<id>_1.wav`, then `py -3 tools/synth_arena_crowd_sfx.py --no-vo --only pa`.
  `tools/clone_announcer_lines.py` is still in the tree, untracked and dead.
- Match-start cinematic: landed and then reworked heavily with him over a dozen rounds (next list).

**Built since, in the order he asked.**
- THE OPENING (`ArenaIntro.cs`, `ArenaIntroScreen.cs`): 3 s of black with heartbeats; the picture
  comes up on the cast already walking, in TWO FILES, in each character's own walk clip with
  bounce, sway and a hop into the lights; the tunnel's glare builds with the walk and the white
  lands as they reach the mouth; the bowl; the screens roll the cast and land on the taya; the
  camera cuts back to the taya under a spot and draws back as the files open into ONE LINE
  ABREAST, the walk crossfading into the idle; then the stage is built. About 24 s full, 15 s
  short (every opening after the first in a session). The game's UI is hidden for it.
- GLARE (`ArenaGlare.cs`): a painted starburst over the screen in two turning layers, added as
  light (`Resources/Shaders/ArenaLensFlare.shader`), for the show spots, the opening's light and
  the floodlight banks; in the opening it dims behind the walkers' heads, with a pool of light on
  the tunnel floor and shafts in its air.
- THE TAYA'S BOX IS ROUND ON THIS MAP, sized per layout (`Core.Confinement.Use`,
  `ArenaStage.BoxRadiusFor`: plaza 6.4, tore 8.6, krus 7.9, hukay 9.3, entablado 7.5), its chalk a
  circle drawn only where there is deck. Every radius keeps 1.6 m clear of jump pads and 1.2 m of
  speed pads. Every other map keeps the square of 7.
- THE BIG SCREENS IN PLAY (`ArenaScreens.cs`): a card on all eight for the can going down, a tag,
  a fall, a block, a near miss; and sometimes a replay of a recorded moment
  (`RecordedWorldView` drawn into the card). The card is drawn once to a texture and shown through
  `Resources/Shaders/ArenaScreen.shader` (a line-by-line wave).
- Sounds of their own for the jump pad, speed pad, boost and stamina orb
  (`tools/synth_arena_pad_sfx.py`).
- Character AO: a wider bias and "Full" at 0.6 of what it was, against stripes on the redesigns.
- Replay copies no longer inherit the live body's hit flash (they were white in the tag replay).

**Two bugs worth knowing, both mine and both found only by running.**
- `Mathf.SmoothStep(from, to, t)` is NOT a shader's `smoothstep(edge0, edge1, x)`. The glare's and
  the crowd's code used it with edges first: every lamp's glare came out at zero (four redraws of
  the look could never have shown), and the crowd's roar layer never fell under half. Use a local
  `Ramp`. This was found by writing a trace to a file from the owner's own play session.
- Online, the host could stamp a match id of its own before the transport was ready, and every
  client took the transport's: the host played one layout and the others another. The layouts now
  read `MatchRpc.PresentationMatchId` online (`ArenaStage.MatchId`).

**Still paused on 2026-10-08** (the session spent 10-07 and 10-08 on Paete's kit: the leap's swing, and every ability's plant remodelled, painted and given new effects; `docs/CHARACTER_REDESIGN_DANTE.md` 15.11). What follows was written on 10-07 and still holds.

**Still paused on 2026-10-07.** This session spent 10-06 and 10-07 on the redesigned heroes' first-person hands and on
Paete's ability rework instead (all of it recorded in `docs/CHARACTER_REDESIGN_DANTE.md` 15.9 and 15.10, all
uncommitted on this same branch). Nothing below has moved. Two things from that work touch this map and want a look
when it resumes: the first-person hands are now shaded by a ray to the sun (`ViewmodelArms.WorldShade.cs`: under the
Arena's roof and floodlights, check they are not left dark or bright), and the view model is drawn pulled toward the
eye (`Framing.DepthPull`).

## OPEN when map work resumes, in order

1. **Unverified by anyone but the owner's eye, and some not even that.** No batch probe has run
   since `8d8d10fd2`. `ArenaIntroProbe` does not work in batch (it yields `WaitForEndOfFrame`,
   which batch mode never fires, and then hangs): fix it first. Then rebuild the scene and run all
   six probes. The rule tests (`ConfinementTests` and whatever asserts the box) have not been run
   against the round box.
2. **Waiting on the owner's word:** whether the screens' replays now appear (the console prints
   `[ArenaScreens] replay: ...` with the reason for each one that did not play); whether the crowd
   reacts; whether the glare with the can down is right; Paete's first-person hands (put back on
   the OLD model's arms through `RosterEntryAsset.ArmModel`; the cut from a redesign's `arm-`
   bone misses the forearm and hand, and Rafi is cut the same way); whether the stage matched for
   every player after the id fix; one teammate whose build kept the old models.
3. **Online, played once and not again since the fixes:** the layout id fix, the round box
   (protocol 153), pad sounds on other peers, a client's opening (each peer starts it when its own
   loading ends; one host stamp would make it frame-equal and is his to approve).
4. **Known faults not yet fixed:** replays do not record the drone; a held slipper lost in a fall
   returns after 8 s; three RooftopRecoveryProbe cases; the pickup's taken state has no message;
   `NationalsHardeningTests` still names five older switches unrelated to this map
   (`AudioCues.SkillSfxOn`, `GameLaunch.TrainingRange`, `KantoTraffic.Covered`, two in
   `SidewalkLife`). Tore leaves the taya only 0.6 m of ring because its jump pads sit mid-ring:
   more needs the pads moved (layout data and a scene rebuild).
5. **Never done:** a weaker PC; LODs; the game's own word chants and the English announcer lines
   recorded by the team.
6. Not this map's, but in this worktree: a first-person arms rework for the redesigns is
   UNCOMMITTED here (new `dante-redesign-fpv-arms.*`, `FpvNaturalArmProbe.cs`, rebuilt
   `RosterArms/*`, changes to `ViewmodelArms.cs`, `GaitStyles.cs`, `RosterBookBuilder.cs`,
   `ViewmodelArmAuthor.cs`, `person_paete.asset`). It belongs to the redesign work: never sweep it
   into an Arena commit.

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

- An agent's probe was committed without ever having been run, and failed the first time it was
  (the opening's). A probe is not evidence until it has run once.
- Four redraws of an effect that was never being drawn. When the owner says "nothing" twice, stop
  tuning and find out whether the code runs at all: write what it decides to a file he can
  produce by playing, and read that.
- A commit run in the same command as its compile check went in with a compile error while the
  owner's editor was live. Check, read the result, then commit.
- "Fetch" is not "pull": a teammate who only fetched kept the old build. Give the three commands
  and the commit hash to look for.
- An uncommitted other session's work in the same worktree (the redesign's) was committed at the
  owner's word before its own handoff was read; the handoff warned about some of those files.
  Read the other session's handoff first, then commit.

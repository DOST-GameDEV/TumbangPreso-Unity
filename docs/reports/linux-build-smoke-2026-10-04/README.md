# Internal Linux build and real-player smoke check

Validated runtime source **70a600a1657438399caa02de1aa8db88df0033d8**, Unity6000.5.8f1,
Linux x86_64 Mono/OpenGL core llvmpipe. This is an internal validation artifact,
not a Windows release, current paired-peer qualification, or tournament sign-off.
Later contributor Paete-flight, arrival-FPP and online-room-publication fixes
were reviewed and integrated separately after this artifact; they are excluded.

## Build recovery and result

- First graphics-enabled build: exit247/197.9s; actual cgroup OOM rose5→6,
  kills4→5. No completed artifact.
- Cached trim retry: exit247/1142.7s; OOM6→7, kills5→6, during native plugin/Burst
  generation. Original failure logs and partial-output inventory retained.
- Graphics-free Editor retry, using the existing GameBuilder's documented
  `-batchmode -nographics` route plus its unused-scan trim flag: **exit0/75.3s**.
  BuildPlayer reported28s and2420MiB;254files,2,538,026,926bytes including debug
  information. Peak process-tree RSS4,903,555,072bytes; no new OOM.

The last run reuses warmed import/compilation caches. The elapsed-time difference
is not a controlled graphics-only performance comparison. Player graphics APIs,
all12 enabled scenes, shader collection, authored-animation checks, scene-script
checks, stripping and quality settings were retained. The generated collection
contains76shaders/139variants and no unresolved Shader.Find names. Burst native
library exists. All25 settings files restored byte-for-byte after each run;
only ProjectSettings.asset was observed changed in the frozen input manifest.

The private validation project is not a Git checkout. Its honest build stamp is
no-sha/treeUnknown/protocol148; external source/input manifests identify the
validated source. It is deliberately not relabelled as a shipping release.
Executable/runtime/core/Burst hashes and byte counts are in artifact-verification.

## Actual player path

Fresh named cloud70aBoot profile and separate XDG config/data/cache directories.
No owner profile was used. The actual built player ran with graphics enabled:

1. Cold startup → loading screen → sign-up/login screen.
2. Guest → supplied title art → click-to-continue → Home.
3. Game mode → Practice → Training → Eskinita world/HUD/FPP.
4. Desktop W input visibly moved the camera/player; exact speed was not measured.
5. Escape opened training controls; Leave returned Home.
6. Menu Quit Game exited normally: **exit0/371.3s**, no new OOM.

Actual screen pixels were inspected. Retained captures show training pause and
returned Home at1364×1024. The default native-resolution path superseded the
1280×720 launch preference. Basilio's training name, dante portrait, green outfit
and earth icons agree; a preliminary identity suspicion was discarded after
inspecting the real dante/zack portrait assets. No speculative fix was made.
Bound-window synthetic X11 inputs showed no effect; supported desktop pointer
and keyboard input worked. This is not evidence of an in-game input defect.

## Limits / known environment failures

- FMOD found no output device and ran silently; no listening acceptance.
- Unity Wire WebSocket connections were refused in this cloud environment;
  online services and multiplayer are not qualified by this offline smoke.
- Existing PLAYTEST_TOPUP displays999999. This is not a real wallet award receipt
  or a public-release approval of that setting.
- No claim of all-map player coverage, Cinder actual-player visual acceptance,
  exact red/cyan foliage resolution, GPU performance, or human approval.
- Earlier editor/map captures remain separate evidence. This smoke establishes
  a real packaged startup, offline map entry, movement/pause/return and clean exit.

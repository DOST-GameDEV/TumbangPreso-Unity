# Current cloud player refresh

The complete12-scene Linux player builds successfully with protocol100,2076MB.
Unity6000.5.8f1, source8613d3c7 synchronized into the isolated83136899 checkout.
All15,364tracked Assets/Packages/ProjectSettings inputs were hash-inventoried;
233candidate differences were synchronized from source before building.
Embedded identity honestly says83136899, dirty, protocol100. The separate source
manifest pins the overlay; this is not a clean83136899 or clean8613d3c7 build.
[Receipts and executable/runtime hashes](cloud-player100-checks/).

## Resource diagnosis

The cloud has an8GiB cgroup budget. Graphical Editor builds crossed the memory
guard. One warm retry still crossed it. Sequential-import and idle-worker policy
experiments did not remove the build's active importer memory; one abrupt spike
caused a new OOM before the observer stopped the process. Original private Editor
settings were restored. Those failures are preserved, not counted as passes.

The successful build uses -batchmode -nographics, one job worker and the same
production gates. A build does not need the software graphics-device allocations
that native rendering checks require. All scenes/assets/renderer targets remain;
no quality reduction or GI rebake. It finishes in57.55s overall,26s BuildPipeline,
without another OOM or guard stop. Subsequent player checks still use real OpenGL.
[Unity6.5 command reference](https://docs.unity.com/en-us/engine/6000.5/manual/unity-editor/command-line-arguments/editor).

Only the obsolete generated private protocol97 player was removed for disk space,
after its254-file size/hash inventory was retained. Source assets, profiles, logs
and captures remain. No Desktop build was replaced.

## Player execution

The fresh graphical player boots and displays the current HOME with correct
colours. The old halftime UI automation route fails waiting for a UI state after
HOME, before qualifying halftime; it is not a gameplay pass. No new OOM occurs.
The built-player Hero bot run reaches round8 but stops at its existing1100-second
probe limit with42.98seconds left; this is NOT natural full-match completion.
Native keyboard Ready starts play. Escape and nested Settings pause actual offline
simulation; time116.6562 and round-left64.70425 stay fixed for more than142wall
seconds. Resume restores movement/clock. No settings values were changed.

Seven real boundaries are observed: six ordinary breaks start near5seconds and
halftime near10seconds. Each holds simulation, input and one captured frame.
The actual retained halftime replay is visibly played and round5 resumes.
The run records165throws,26skill uses,6ultimates,46can knocks,116tags and zero
idle penalties. Requested30fps is NOT achieved: software rendering measures3.4fps,
including the manual pause. This is not a hardware performance claim.

Only connection-refused Unity Services socket exceptions appear in this offline
cloud run; services remain unqualified. There is no further OOM or guard stop.
A visible spectator issue needs correction: ordinary break captures lack the
next-taya/standings card. Source confirms NativeSpectator disables RoleSwapCard,
leaving the spectator frozen without that context. Do not count its layout passed.
The all-bots spectator camera does not establish human-control or hardware behavior.
Actual peers and Windows/mobile compatibility are separate, still unqualified.

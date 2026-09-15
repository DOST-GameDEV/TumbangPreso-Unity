# Rematch loads the announced next court

The result board updated SelectedMap, but the old rematch path only restarted
MatchDirector inside the current scene. A focused regression reproduced
SelectedMap=BayanPlaza while the actual scene remained Eskinita. Earlier native
checks had proved match restart without checking this map transition.

The host now selects and synchronizes the map before sending the existing rematch
command. Both host and clients load that arena through SceneFlow.StartMatch.
Map synchronization updates session state directly, including while no lobby UI
exists. Invalid map indices and votes after a closed host result board are ignored.
The existing protocol37 message formats are retained; use the corrected build on
all demo peers.

The opt-in peer driver recognizes each new ReadyGate and checks that the rematch
round actually becomes active. Its explicit short-review settings are separate
from normal8-round defaults. The protected wrapper can run the same real vote,
reload and ready sequence in Classic and Hero Strike.

Editor evidence: the baseline failed as expected, then three focused checks passed:
the chosen-scene regression and both mode result/rematch routes. Restoration
receipts8e8fd580d508 and1561afb18868. Native qualification now passes:

- Classic and Hero Strike each completed a real short match, submitted peer votes,
  loaded BayanPlaza on both processes and started the new first round through its
  ready gate. Structural hashes agree within each run. Profiles/shared input were
  preserved. Reports are sampled at different times, so scores are not identical.
- The ordinary native UI driver also passed its actual-next-scene assertion. Its
  rematch capture now shows BayanPlaza. This run overlapped peer checks and is not
  a clean performance measurement.
- Three focused recovery-input cases passed: keyboard, gamepad and touch handling
  at30/60/144 cadence settings over trip and six stagger types. These use synthetic
  devices/callbacks through real readers, not physical device certification.
  Restorationcad297ecf815.

Candidate build: Builds/demo-2026-09-16-v4/TumbangPreso.exe, Runtime SHA256
53565E945341C50C3AB8FA8ABA7F33DA8076D93B19E5980FCADE1877B92B5E7C.
Build1081MB/46s, restorationb9db7207d491. Previous candidates are preserved with
their old-map-rematch limitation recorded.

# Real Windows host-loss retirement

Status: **FIRST ACTUAL RUN PASS**, no retry, repair, runtime edit or new build. Runner session20612 exited0.

This unit checks the existing host-loss retirement in frozen Windows candidate1002j. The prior [host-loss-lifetime proof](../host-loss-lifetime/README.md) passed six controlled native cases but did not use a real transport failure. No runtime, build, qualification input, hero or private asset changes are part of this unit.

The frozen player is `C:/Users/matth/Documents/Codex/work/tump-competition-release1002/Builds/competition-candidate1002j/TumbangPreso.exe`: source `d28770a25504c8f6a069b429d8384af0ae4ff557`, protocol132, Runtime SHA256 `d75db25d1c5e6a1eb18fee96cf03cd2b56e674e6a5912b8f38ada6522258e37e`. The classified artifact receipt passed; full runtime hash and StreamingAssets identity were also read from disk during preparation.

`tools/run_host_loss.py` uses the existing GPU admission, profile/input preservation, installed custom-rule parser, completed-arrival observer and final NetStateReport. One direct loopback pair uses unique owned profiles, ordinary lobby host/join, custom one-round/30-second rules and no AllBots. Both existing observer receipts must prove their owned PIDs, live arena, two human-origin seats and zero completion/record events before the runner stops only its captured host process. Pre-kill receipts are saved separately. The client report is scheduled at60 seconds, the host at90, with a90-second scenario ceiling and separately bounded cleanup. The host must be killed with at least15 seconds before the client report.

The terminal gates require normal client exit, its final report in MatchSetup with round0/inactive, exact protocol and frozen source prefix, a raw `HostLost: ABANDONED at round 1 of 1` log, and no fabricated MatchEnded/RecordReady events or record identity. An auto-hosted lobby is acceptable only with the old round inactive. Shared input restoration, exact owned profile-seed restoration, unchanged Runtime, all captured processes retired and lease removal must verify.

The final report does not expose MatchInProgress or the raw round clock; this unit will not claim direct observation of those fields. It also makes no human input, WAN, full tournament, physical feel or AllBots claim. Existing completed-arrival receipt `phase=live` after loss is historical admission evidence; its scenario `passed` flag is not this unit's verdict.

Preparation checks: four engine-free acceptance tests passed; Python compilation passed. These controls reject active/missing-activity auto-host reports, unproven host kills, foreign source identity, fabricated completion and foreign/non-live initial receipts. No player or real registry mutation occurred during these checks.

The one real loopback run verified both owned players in Eskinita, live, with two human-origin seats and zero completion/record events. Host PID25496 was abruptly stopped at20.719 seconds; client PID26120 had run17.453 seconds at that point. Its transport subsequently logged `[Abandon] HostLost: ABANDONED at round 1 of 1; this peer may no longer resolve anything`.

At its60.0-second sample the surviving client reported `map: MatchSetup`, `round: 0`, `round active: False`, `networked: False` and the frozen identity/protocol132. Its post-loss observer retained zero MatchEnded/RecordReady events and an empty record identity. The client exited0; total scenario time was66.609 seconds. The observer's historical `phase: live` and `passed: false` belong to the interrupted completed-arrival scenario, not this host-loss verdict.

The [result receipt](first/result.json) confirms input restoration, exact profile-seed restoration, unchanged Runtime, both owned processes retired and lease removal. After termination, a direct process lookup found neither PID, and the resource pool contained zero active leases. No browser/preview/server helpers were opened by this unit. [Evidence hashes](evidence-hashes.json) verify the ten raw files copied from the untouched first output to this report.

This strengthens the controlled native6/6 host-loss proof with one actual Windows/UTP process-loss route. It does not add a runtime fix, establish repetition/intermittency coverage or qualify a full tournament/WAN session.

The existing `net_matrix.py` CLIENT_TERMINAL evaluator initially rejected only CLIENT+activeTrue and accepted HOST+activeTrue or HOST+missingActivity. A direct engine-free call reproduced that weakness. Root separately shipped its correction `f8ca7b72c` and input preservation `ba71fc043` before this launch; this unit did not overwrite that file.

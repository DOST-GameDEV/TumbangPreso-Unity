# Online room publication and route correction

Relay hosting previously reported success before its UGS Lobby registration completed. A failed registration left a listening room with an undiscoverable custom code. It also broadcast a direct LAN address even though its transport was configured for Relay, so LAN-first code resolution could choose the wrong route.

StartRelayHost now awaits registration before reporting success, stops its own transport on registration failure and presents the service error. Relay hosts no longer advertise a direct LAN endpoint. Direct LAN hosting is unchanged.

## Evidence

Source baseline 0b16a2e98, Unity 6000.5.8f1, actual non-batch PlayMode with isolated named profiles. Live original publication, indexed custom-code lookup and the shipping public query passed one case. Failure-original reproduced two causes: failed publication returned true and delayed publication returned before the code existed. Topology-original reproduced Relay direct advertising while the real LAN advertisement control passed. Final candidate-isolated passed the same three causes plus the LAN and real online publication/code/browser controls: 5/5, no skips, exit 0, PID4852/session33477.

The first candidate used an overlength UGS profile name and its teardown waited on a shutdown-intent flag after stopping an already stopped transport; it was interrupted and is not a pass. The next attempt exposed fixture dispatches surviving across the persistent NetSession, producing false failures in the later live controls. Cleanup now clears only the fixture dispatches and waits on actual listening state. Both failed attempts are retained. Production was not weakened to accommodate them.

All runs are terminal. Shared input preferences, editor input preferences, QualitySettings and isolated settings were restored. The final run froze 19,257 inputs; 202 task-owned GUI importer rewrites were preserved then restored exactly, with no remaining input deltas. Pre-existing ProjectAuditorSettings dirt was preserved. Full local logs and frozen maps remain under Logs/server-publication1004; this report stores compact service lines and native XML/results with exact hashes.

This confirms the repaired hosting failures and one-machine live UGS registration/discovery. It does not prove a second client can join, reproduce the QA tester's exact failure or fix owner-reported rubber-banding. Those remain open for a matching-artifact paired online test. QoLUpdates was compared read-only; no code was copied or run.

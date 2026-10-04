# Manual readiness chat focus: pending acceptance

The source candidate adds the existing `LobbyChat.AnyTyping` check before `ReadyGate` accepts a new manual ReadyUp press. Automatic readiness and previously submitted votes retain their earlier retry paths. The shipped keyboard binding is F. `LobbyChat` does not disable the Player action map, while `ReadyGate` reads it directly rather than through `PlayerInputReader`'s chat gate.

**This candidate is not natively qualified. No final product run was performed.** The four-case fixture shared a serial run with ten rebind and four buffer cases; results below refer only to readiness, and the complete original 18-case XML files are retained unchanged.

- First baseline, session 85263: readiness **2 passed / 2 failed**. The pending-vote and automatic-readiness controls passed. Both manual-key cases failed their fixture assertion that the synthetic key reached the action, before the production acceptance check. These are not evidence of the intended product defect.
- One bounded fixture repair: use a temporary cloned InputSettings with dynamic player updates enabled in EditMode. Repaired baseline, session 84619: readiness **0 passed / 4 failed during SetUp** with a destroyed InputSettings reference. No readiness behavior was evaluated in this run.
- No additional readiness retry or acceptance claim. Both recorded jobs completed profile/input preservation and released their leases.

The installed Input System source explains both fixture faults. `InputManager.defaultUpdateType` selects Editor updates outside PlayMode, and `InputActionState.NotifyControlStateChanged` ignores Editor updates. The repair then met an ownership rule in `InputManager.settings`: replacing settings destroys the previous object when it has `HideAndDontSave`. Consequently retaining that original reference was insufficient; restoration skipped the destroyed original and the fixture destroyed the current clone, leaving later cases with a destroyed settings object. This diagnosis is source-inspected, not proof of a repaired fixture.

Original and repaired fixture snapshots, input/protected manifests, raw XML, guard/job receipts and per-readiness case results are retained here. The production candidate remains separate from qualified work. Source-level chat gating is plausible, but physical input, actual chat-widget focus and gameplay acceptance remain unproven. No transport, SDK, live service, authored content or original settings asset was intentionally changed.

The not-yet-run emote fixture reused the temporary-settings setup before this second failure was known and must not be launched unchanged. Its production candidate is also pending. Do not present compilation or the two unaffected readiness controls as completion of either fix.

The two new fixture source/meta pairs were moved byte-for-byte out of the main Assets test suite into `Logs/ready-chat1002-inputs/retired` and `Logs/emote-chat1002-inputs/retired`. `retired-moves.json` records exact original/destination paths and matching hashes. ReadyGate and EmoteWheel product candidates remain unstaged and unqualified; no source candidate was reset or deleted. A future useful acceptance approach can be chosen within the existing engineering authorization, but this fixture repair loop is finished.

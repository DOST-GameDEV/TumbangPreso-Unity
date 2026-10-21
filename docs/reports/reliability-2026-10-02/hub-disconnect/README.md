# Hub disconnect cancellation, October 2

Host loss returned the hub HOME and stopped its transport without retiring the room request, native join or active matchmaking search. A delayed join failure could then set `SceneFlow.Networked` false after the player chose another online route, and a queue could keep joining after its visible plate had been cleared.

`ConvertedMatchSetup.HubDisconnected` now calls the existing `LeaveRoom` cleanup before its unchanged HOME/toast actions. This advances the room request generation, cancels/closes the native join, cancels an active queue, stops the session and clears room state. No new cancellation framework, message, rule or scene route was added.

The native EditMode fixture invokes the actual disconnect callback and existing native join connection boundary using dormant Unity UI components and a controlled delayed task. The queue case uses the real `Matchmaker.Cancel` path with an active, disabled component in Joining state. No transport, rendered hub, scene load, account, live SDK or service action is involved.

- Original product baseline: **3/3 causal failures**, native 0.2629147 seconds. The join token remained uncancelled, the delayed failure overwrote the next route's network flag, and the queue stayed Joining instead of Cancelled.
- Candidate: **3/3 passed**, native 0.2414014 seconds, identical fixture. Zero fixture/tool repairs or retries.
- Both serial guarded jobs completed preservation and released their leases. All 1,465 protected source/settings hashes and all three candidate file hashes remained unchanged; no Unity process remained at completion.

Baseline source: `e8de3702119fb1a831c17f5e7bd0ebbf838ad71d`. Owned paths are `Assets/TumbangPreso/Runtime/UI/ConvertedMatchSetup.Hub.cs`, `Assets/TumbangPreso/Tests/HubDisconnectCancellationTests.cs`, and its new valid metadata. The fixture restores the existing network singleton, route/room state and queue-watch fields. Other source, private art/settings, Cinder/Amihan and completed-arrival probe work was preserved.

Qualification used `tump-feedback-0930`, profile `hub-disconnect1002`, the main guarded serial CPU runner, 1536 MB job budget / 2048 MB reserve and 450-second timeout. Baseline session 30167: guard 13200 / Unity 22732. Final session 13223: guard 17576 / Unity 3340. Raw XML, both job/guard receipts, inputs and protected hashes are retained here; full Editor logs and source snapshots remain under the checkout `Logs/hub-disconnect1002*` folders. Private profile contents are excluded.

This proves cancellation and late-callback ownership at the current hub handler. It does not claim physical UI input, rendered layout, a real transport loss, WAN recovery or live matchmaking acceptance.

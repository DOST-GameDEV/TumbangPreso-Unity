# QA-04 lobby title lifecycle

Source trace and focused validation, 2026-09-27. Do not infer physical OS typing from a test that dispatches uGUI events.

**Later validation:** the two new title-domain tests passed in the graphics-enabled
[QA2 EditMode receipt](checks/qa2-edit-title-career-repair1.xml). They cover 24-character
LAN propagation and single-line/literal sanitization with nickname rules unchanged.
The physical typing claim and real host/joiner delivery remain open.

The later graphics-enabled [QA-04 PlayMode receipt](checks/qa04-title-input1.xml) passed 4/4 named cases, 0 failed/skipped, Unity exit 0, 14.860 s. Its host-field case raycasted the actual visible HOST input plate, sent pointer-down/click through `ExecuteEvents`, verified focus, rejected an append to a full 24-character prefill, then used `InputField.ProcessEvent` to replace it with `QA04 ROOM EDITED`. CREATE opened a local LAN room; `NetSession.RoomTitle`, beacon `HostName` and the visible lobby heading all matched. The [edited field](native/qa2/qa04-host-name-edited-960x540.png) and [created lobby](native/qa2/qa04-host-name-created-960x540.png) captures were visually checked at 960x540 with no title clipping. This is engine-event input and local propagation, not physical keyboard/IME proof or real host-to-joiner delivery. Minimum monitored free disk was 7,980,523,520 bytes; no watchdog.

## Value path

`HubHost.Build` constructs `LobbyName` with a 24-character uGUI `InputField` and a prefill that can itself reach that limit (`UI/Hub/HubCustom.cs:46-55`, `UI/Hub/HubForms.cs:100-138`). `Create` snapshots the trimmed current text before awaiting `HostRoom` (`HubCustom.cs:106-117`). `HostRoom` calls `LeaveRoom` first, then assigns `NetSession.RoomTitle`, and only then awaits LAN or Relay start (`UI/ConvertedMatchSetup.Hub.cs:132-158`). This order does not erase the new title: `LeaveRoom` clears settings *before* the assignment, `NetSession.EnsureStoppedAsync`/`StopCurrentTransport` does not clear them, and both start paths use `LocalLobbyName`, which prefers nonblank `RoomTitle` (`Net/NetSession.cs:44-62,689-778,1102-1184,1300-1398`). The room-request generation check makes an old completion return without clearing a newer title. Failure and explicit leave do clear it, appropriately.

The host lobby heading reads `RoomTitle` directly (`UI/ConvertedMatchSetup.Hub.cs:205-218`, `UI/Hub/HubCustom.cs:473-481`). Relay creation receives `LocalLobbyName` for both UGS Lobby name and `HostName` data; later count updates do not rewrite those name fields (`Net/ServerQuery.cs:461-521,543-601`). No source-grounded async title overwrite was found.

## Definite LAN mismatch

The LAN host beacon writes `LocalLobbyName` into its existing payload, but `LanBeacon.TryParsePayload` clips the received title to `Balance.PlayerNameMax` and calls `GameSettings.SanitiseName`, which clips it again (`Net/LanBeacon.cs:335,562-570`, `Settings/GameSettings.cs:541-556`). `Balance.PlayerNameMax` is **14** (`Packages/com.tumbangpreso.core/Runtime/Balance.cs:677-678`) while the room field accepts **24**. Thus `ROOM-1234567890123456789` can show in the host lobby but becomes its first 14 characters in a LAN browser; changing only a suffix beyond character 14 is invisible there. The current parser test uses `BongBong Host` (13 characters), so it cannot catch this (`Tests/LobbyAndSettingsTests.cs:591-604`). The account-name test's 14-character rule is about player nicknames, not custom room titles (`Core.Tests/AccountRulesTests.cs:228-236`). This is a definite propagation defect; it does not establish why the tester could not type into the original field.

## Bounded fix and remaining proof

No separate room-title limit/sanitizer existed. The approved source patch adds a room-specific 24-character single-line sanitizer in `Settings/GameSettings.cs`, leaving `SanitiseName` and the 14-character player-name policy unchanged. `NetSession.RoomTitle` now normalizes on assignment so host, LAN and Relay consume the same value; `HubHost` references the shared limit without changing its layout. `LanBeacon.TryParsePayload` uses the room sanitizer instead of the player-name cap. LAN magic, field order, separator handling and protocol version are unchanged. Controls/newlines are removed and surrounding whitespace trimmed; markup stays literal rather than being interpreted (both `HubKit.Text` and `OwnerUiLayout.Text` have `supportRichText = false`). New `LobbyAndSettingsTests` cover an exact 24-character title, a changed suffix after character 14, separators, control/line-break/overlong input, null/whitespace and unchanged nickname cap. The account-test comment was corrected without changing its assertion. The targeted EditMode title-domain cases passed 2/2 in the QA2 receipt above.

The PlayMode route check above exercises the actual GraphicRaycaster and event-driven replacement, with a local LAN host in an isolated named profile and no UGS. Physical OS keyboard/IME input remains the final QA-04 question. No production focus fix is justified from current evidence.

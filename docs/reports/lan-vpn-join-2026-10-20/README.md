# LAN over Hamachi: "stuck on joining"

TODO entry: LAN-VPN-JOIN-1020. QoLUpdates `8c4164b2a`, protocol 174.

## The report

JOIN GAME, source DEDICATED (LAN), one row (MAP UNKNOWN, 1 / 4); after JOIN the line read
"Joining 25.17.144.85:8910..." and did not change. The reporting PC is the joiner: its Hamachi adapter
is 25.16.130.116, both of its networks (Hamachi and Ethernet) are Windows Public, and its firewall has
an inbound Block rule for the Unity Editor on Public. The host did not answer ICMP, which Public
blocks by default, so that proves nothing either way.

## What the code does (read-only audit of the join path)

- The host listens on `0.0.0.0:8910`; the beacon is an outbound broadcast on 8911 to 255.255.255.255
  and each adapter's directed broadcast, Hamachi's included. A host whose firewall drops inbound 8910
  on its VPN adapter is therefore listed and cannot be joined.
- An unanswered join gives up after about 24 s of transport attempts (2000 ms x 12) or the seat
  deadline 2 s later. The line did not move in that time, and the failure then read
  "Could not reach <address>.  (disconnected: [Disconnect Event]...)".
- A host on another build refuses with "Game version mismatch (network protocol N)", which reached the
  screen behind "Could not reach".
- `HubJoin.Join` was `async void` without a catch, so a throw before the attempt's own catch left the
  screen busy with the line frozen for good.
- The beacon carries no protocol and no map; MAP UNKNOWN is a constant for every LAN row.
- Source audits run unchanged (`audit_request_call_sites`, `audit_wire_payloads`, `audit_wire_finite`,
  `audit_event_subscriptions`): their findings are pre-existing and none concern LAN, the beacon or
  approval.

## The change

`Core.JoinFailureRules` writes both lines. While waiting: "Joining <target>... N s", and after 8 s on an
address join "No answer yet: check the host's firewall." On failure: "No answer from <target>. The host's
firewall may be blocking the game.", "That host runs another version (protocol N, yours M). Use the
same build.", "That room is full.", "The host closed the room.", or the host's own sentence; anything
else keeps the old headline with its detail. `LobbyJoinPanel` (typed address and LAN match) and the
legacy `ConvertedMultiplayerSetup` use it. `HubJoin.Join` catches, always clears its busy state and
refreshes its line each second. No wire change.

## Verification

- Core.Tests `JoinFailureRulesTests` 11/11; full Core 767/767.
- `join-fixtures-final.xml`: `HubJoinWaitTests` 1/1 on the real hub JOIN screen with the real transport
  and an unanswered loopback port (counted wait, the hint after 8 s, "No answer from 127.0.0.1:...",
  the screen still JOIN and usable, no transport left), `TumpNativeJoinTests` 9/9.
- `join-fixtures-first-run.xml`: the same plus `PlayerHubFriendJoinHandlerTests` 2/2; its one failure is
  `TumpNativeJoinTests`' unreachable case still asserting the old wording ("timed out", "reach" or
  "failed") against the new "No answer from ..." line. That assertion now requires the new line and the
  absence of the `[Disconnect` envelope.

## Not verified

No real join over Hamachi: the host PC's firewall and build are not reachable from here. The beacon
still cannot say a host is on another build before JOIN is pressed (a wire change, left open).
Superseded by the beacon v3 section below.

## Beacon v3, the shared-subnet address and REFRESH

Branch `claude/lan-beacon-v3` off QoLUpdates `ef248c215`, protocol 174 (unchanged).

### The change

- `LanBeacon.MagicV3` (`tumbang-preso-lan3`): v2's fields, then `NetSession.ProtocolVersion` and the
  selected map id (`SceneFlow.SelectedMap`, refreshed every frame by `NetSession.RefreshBeaconCounts`,
  sanitised to `[A-Za-z0-9_-]`, at most 48), then the room name, still last. v1 and v2 still parse with
  protocol 0 ("unsaid") and stay joinable. A truncated v3 packet is refused; an unreadable protocol is
  unsaid.
- A host sends v3 then v2 each interval and in each discovery reply. A v3 reader merges a host's two
  copies on its beacon id and keeps the v3 protocol and map when the v2 copy arrives.
- `LanEntry.DifferentVersion` (`Core.JoinFailureRules.ProtocolDiffers`: said and not ours) makes a row
  neither joinable nor watchable; it sorts after joinable rooms.
- JOIN GAME (`HubJoin`): the MAP column shows the map's display name (`HubRoom.MapName`; a map this build
  lacks shows its own id, an unsaid map UNKNOWN), and a different-protocol row shows DIFFERENT VERSION
  where JOIN would be, with nothing to press. The online rows use `HubRoom.MapName` too, which no longer
  falls back to TONDO with a warning once a second for a map this build lacks. The native join card greys
  the row and says "Different version"; the legacy card's state reads DIFFERENT VERSION. A join by code
  that resolves to a v3 LAN host on another protocol refuses before connecting with approval's own
  sentence (`JoinFailureRules.VersionLine`).
- `LanBeacon.AddressPreference`: 5 shared non-VPN, 4 shared VPN range, 3 other, 2 VPN range not shared,
  1 link-local, 0 unreadable or loopback; VPN ranges are 25.0.0.0/8 (Hamachi) and 26.0.0.0/8 (Radmin).
  This machine's subnets are read at most every 5 s. A tie keeps the address already shown: before, a host
  heard on a LAN and on Hamachi (both scored 2) took whichever copy came last, so its row's address swapped
  every packet.
- REFRESH on the JOIN list (owner, 2026-10-20: "add a refresh button on server list to refresh the
  servers"): on the LAN list it drops every row and asks again on the next frame (`LanBeacon.Refresh`), the
  empty list reading "Looking for rooms on your network..."; on the online list it brings the UGS query
  forward (`ServerQuery.RefreshNow`, never doubling one in flight). It reads REFRESHING... and ignores
  presses for 1.5 s. A sticker button at the list heading's right: mouse, pad focus path and touch reach
  it; hidden for JOIN BY CODE.

### No protocol bump

The beacon is UDP 8911 outside Netcode. Nothing a connected peer exchanges changed and approval is
untouched. `tools/audit_wire_payloads.py` reads only `MatchRpc.cs` (not touched here); it exits 1 on the
same pre-existing `MatchRpc` mismatches as before (`ReqLunge`, `ReqPunch`, `ReqShove`, `ReqSlide`,
`SyncUnit` and others) and names nothing in the beacon. A build from before v3 ignores the new magic and
still lists the host from its v2 copy, so same-protocol peers from different commits keep finding each
other. Recorded in [the network contract](../../SKILL_NETWORK_CONTRACT.md#lan-beacon-v3-2026-10-20-no-protocol-step).

### Verification

All Unity launches through `tools/run_unity_guarded.py` with profile `lan-beacon-v3-1020`; counts read from
the XML.

- Core.Tests 769/769 (`JoinFailureRulesTests` 13/13: `ProtocolDiffers` says nothing for 0, and the beacon's
  `VersionLine` is the refusal's own sentence).
- [EditMode 152/152](beacon-v3/editmode-final.xml): `LanBeaconV3Tests` 12/12 (v3 round trip with a pipe in the
  name, v2 and v1 still parsed and joinable, either neighbouring protocol not joinable or watchable, map names,
  map id sanitising, unreadable protocol and truncated v3, the v2 copy keeping v3's protocol and map, shared
  LAN over shared Hamachi over unshared LAN, one host on Hamachi and LAN joined over the LAN, a tie keeping
  the shown address, `Refresh` emptying and re-asking); `LanReceiveOwnershipTests` 10/10 (a discovery reply
  is now v3 then v2, both on one beacon id); `LanBrowserIdentityTests`, `LanListenerStartTests`,
  `LobbyAndSettingsTests`, `HubLobbySeatControlsTests`, `ChatAndLobbyChromeTests` (protocol tripwire, still 174).
- [PlayMode final 4/5](beacon-v3/playmode-final.xml): `HubJoinLanVersionTests` 1/1 on the real hub JOIN screen
  with the real `NetSession` beacon on UDP 8911. Three hosts are real UDP senders on this machine, unicast to
  127.0.0.1 (never broadcast to the network): another protocol (v3 + v2), ours (v3 + v2), and v2 only. The
  other-protocol row reads DIFFERENT VERSION with no Selectable, sorts after joinable rooms and keeps its
  protocol through its v2 copy; ours reads BGC with JOIN; the v2 host reads UNKNOWN with JOIN. REFRESH empties
  the list in the same frame, reads REFRESHING... and "Looking for rooms on your network...", the hosts
  return, the button comes back after 1.5 s and is on the pad's navigation; it is hidden for JOIN BY CODE and
  on the online list brings the UGS query forward. `HubJoinWaitTests` 1/1, `UiEdgeQualityReview` 2/2 (JOIN
  at 1600x900 and 2560x1440 with every control in frame).
- The one failure, `UiBrowserFeedbackReview`, expects "Finding public rooms..." on opening JOIN and read
  "Online services are unavailable": it [passes alone](beacon-v3/ui-browser-feedback-alone.xml) (1/1) and fails
  right after `HubJoinWaitTests`, whose teardown leaves `NetSession`'s online browser running with that message
  (UGS sign-in is disabled in batch mode). `HubJoinLanVersionTests` stops browsing in its teardown. Recorded in
  the TODO entry; not changed here.
- [First PlayMode run, 18/20](beacon-v3/playmode-first-run.xml): the same order failure, and a real one this
  change then fixed: `TumpUiCapture` measured the two-line DIFFERENT VERSION tag 76 units tall in a 72-unit box,
  so the tag is 84 tall (the row is 92). An earlier EditMode copy of the screen checks could not build
  `HubJoin` at all (`HubScenery.SprayTitle` calls `Destroy`, which edit mode refuses); those checks live in
  the PlayMode test instead.

### Pictures

Rendered by `HubJoinLanVersionTests` through `TumpUiCapture` (action bounds and text clipping checked).

- `beacon-v3/join-lan-v2-1920x1080.png`, `-1600x680.png` (the owner's short wide window), `-1280x960.png`: the
  three rows and REFRESH over the JOIN column.
- `beacon-v3/join-lan-v2-refreshing-1600x680.png`: the frame after REFRESH.
- `beacon-v3/join-lan-v1-1920x1080.png`, `-1600x680.png`: the first placement, REFRESH in the panel's corner
  under its tape at 16:9 and 400 units right of the rows on the wide window; why it moved.

### Not verified

No second build on another protocol has been run against this one on a real network; the PlayMode check
sends the packets such a host sends, from this machine. No real Hamachi pair (the TODO's host-firewall item).
Pad and touch reach REFRESH through the hub's existing focus path and sticker hit area; no physical pad or
phone was used.

# Removing the cause: the host's firewall rule

## What Windows does

Read unelevated from this PC's registry (678 rules, active profile Public, both networks Public):
Windows' first-run prompt writes `TCP Query User{guid}<path>` and `UDP Query User{guid}<path>` rules with
the path lower-cased, one per ticked profile, and an explicit BLOCK rule for an unticked one. The Unity
Editor here has Allow on Domain and Block on Public, so it cannot host a room over Hamachi, and an allow
rule added beside that block would change nothing: block wins.

## The change

- `Core.LanFirewallRules`: the one rule (`Tumbang Preso (LAN)`, inbound allow UDP, Private and Public, the
  exe's path) as netsh text; the read-only PowerShell probe; the parser for the registry rule grammar
  ([MS-GPFAS] 2.2.2.10) and the verdict (Open, NeedsRule, BlockedByRule with the exact block rule ids,
  BlockedByPolicy for shields up or an every-program block); the elevated helper script; the line.
- `installer/TumbangPreso.iss` + `tools/build_installer.py`: per-machine Inno Setup installer from a
  GameBuilder output folder. Admin once; adds the rule for `{app}\TumbangPreso.exe` (delete first, so a
  reinstall never doubles it); launches the game as the original user; uninstall deletes exactly that rule.
- `Net.LanFirewall` + `HubLobby`: on Windows, when the local player hosts a LAN room, the read runs off the
  main thread (and again on window focus and after a request). If this exe is not reachable on an active
  Private or Public network, one line by the address and ALLOW LAN PLAY (House mark, on the focus path, its
  own hit area). The press starts `powershell -EncodedCommand` with the "runas" verb: Windows' UAC prompt,
  then the helper removes the named block rules by exact id, runs the installer's netsh delete and add, and
  exits. The game never runs elevated. The script travels in the command line rather than a temp file, so
  nothing unelevated can rewrite it between the prompt and the run. "LAN play allowed" is only said after a
  fresh read agrees; a declined prompt says "Not changed". Hidden for Relay rooms, clients, non-Windows.

## Verification

- Core.Tests `LanFirewallRulesTests` 25/25; full Core 792/792.
- Live, read-only, on this PC (`TUMP_FIREWALL_LIVE`): Unity Editor `BlockedByRule`, checked Public, removable
  `{6C852D9C-85B4-4325-893A-58E63F4D643F}`, about 2 s; `C:\Users\Andrei\tp-astra\TumbangPreso.exe` (a copy
  allowed on Public by the prompt, path stored lower-case) `Open`.
- Read-only checks of the helper's pieces: `Get-NetFirewallRule -Name <escaped id> | Where Name -eq <id>` finds
  exactly one rule for a path-bearing prompt id and for the Editor's GUID id; `Start-Process -ArgumentList`
  hands the netsh text through verbatim (quotes and an apostrophe in the path, checked with `cmd /c echo`).
- EditMode `HubLobbyLanFirewallTests`: 10/10 with HubLobbySeatControlsTests 13/13 (23/23, D3D11, captures room_firewall_v1 and _policy); that run had the first, longer copy. The two-row copy and the 16-unit gap that followed are asserted in Core 792/792 but not re-run in Unity or re-rendered.

## Not verified here, and the steps

This PC's firewall was not changed, so neither the installer nor the helper has run for real. On a host PC:

1. Installer: install Inno Setup 6.3+, `python tools/build_installer.py --source <GameBuilder output>`, run
   the setup it prints. In `wf.msc` > Inbound Rules: `Tumbang Preso (LAN)`, Allow, UDP, Private and Public,
   program `C:\Program Files\Tumbang Preso\TumbangPreso.exe`. Host a LAN room; a player over Hamachi joins.
   Uninstall; the rule is gone and other rules are untouched.
2. Zip copy: run a copy that Windows' prompt allowed on Private only (or the Editor here), host a LAN room
   while Hamachi is connected. The room reads "Windows Firewall may block Hamachi or Radmin players." Press ALLOW LAN PLAY, answer Yes to UAC. The line goes and a toast reads "LAN play
   allowed in Windows Firewall."; `wf.msc` shows `Tumbang Preso (LAN)` for that exe and its Public block rule
   is gone. Pressing No instead leaves everything as it was and the line says "Not changed".
   `Player.log` has one `[LanFirewall]` line per change of verdict.

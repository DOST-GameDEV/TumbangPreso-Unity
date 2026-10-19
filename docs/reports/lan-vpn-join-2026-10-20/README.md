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

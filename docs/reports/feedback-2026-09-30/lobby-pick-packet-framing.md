# Lobby pick framing and match boundary

The lobby-pick receiver had unguarded integer/string reads and could rewrite
chosen seat fields during play. It now checks the complete four-int/UTF-16 frame
and existing optional custom/build tails before decoding. Both the message and
local RPC reject pick changes while Lobby.MatchInProgress. Normal selection
resumes in the lobby; the packet's claimed peer field still grants no ownership.

The Identify string skip helper is renamed generically; its checked-byte-count
algorithm is unchanged. No guessed string clamp, wire field, protocol119,
authority/hero retune, loading or authored asset change.

Six native structural baseline cases fail with Overflow/InvalidCast exceptions.
A separate actual listening-host baseline fails when selected character2 becomes0
during the match. Final EditMode8/8passes malformed/Unicode/full/legacy-tail and
position-restoration cases. Final listening-host1/1passes valid lobby selection,
sender-based ownership, remote/local match rejection and lobby-return changes.
Nine distinct final cases across two runs, not one9/9suite.

Source1182b764f with four owned source/test/meta overlays;684frozen inputs have
no drift and owned files match the tested candidate. New32hex script metadata
imports natively. Jobs14827/15746/32941/45207terminal; no fixture repair or
unchanged suite rerun. Guarded profiles/input preferences preserved.
[Receipts](lobby-pick-framing-checks).

This validates receiver framing and an actual socket-host selection path locally.
No fresh player/actual-peer/whole-match or all-message hardening claim. Earlier
117reconnect and116Haunt evidence retain their exact source boundaries.

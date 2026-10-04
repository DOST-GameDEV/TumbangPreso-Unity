# Retire a departed player's rematch map ballot

A player could cast a next-map ballot and leave the result screen. MatchRpc removes
that player from the lobby before notifying MatchResult.OnPeerLeft. The result
screen removed the departed player's rematch consent, but kept their map ballot.
With the remaining players abstaining, the absent player still chose the next
court. Their ballot could also defeat a connected player's map choice in a tie.

MatchResult.OnPeerLeft now retires map ballots whose seats have no current seated
peer before the existing rematch consent recount. A seat held for reconnect does
not count as a connected voter. Connected players keep their ballots. When the
table changes, the host publishes the corrected ballot table and redraws its map
choice. Existing rematch consent, denominator, start conditions, map rotation rules
and message formats are unchanged.

## Causal native check

Unity 6000.5.8f1 ran four focused EditMode cases through tools/run_unity_guarded.py
in the isolated tump-feedback-0930 checkout, using profile rematch-map-owner1002,
batchmode and nographics. There was no quit flag. Preparation succeeded and input
hashes were verified before each launch. Only MatchResult.cs and RuntimeLayerTests.cs
were copied for this unit.

The fixture uses real LobbySession.OpenLobby, Admit and Depart calls with transport
IDs 0, 7 and 19, then calls result.OnPeerLeft in the production router's ordering.
It exercises the actual ProjectedNextMap result method and SceneFlow.AdvanceMapRotation.
Four cases cover a remaining ballot or abstention, with the departed seat either
freed or held for reconnect. No UI build, scene load, transport or service is started.

- baseline.xml: 4 cases, 0 passed, 4 failed, 0 skipped. All failures were the actual
  result projection retaining departed map index 3 instead of ordinary rotation
  index 1 or connected-player ballot index 5.
- final.xml: the same four cases and unchanged fixture, 4 passed, 0 failed,
  0 skipped. Actual next-court selection followed rotation or the connected ballot.
  The departed rematch consent was removed, the denominator remained two current
  players, and the board remained visible without starting an unconsented rematch.
- No fixture repair, tooling retry or additional native run was used. The original
  failing XML and log are retained.

## Inputs and preservation

baseline-inputs.json and final-inputs.json retain the exact source and fixture
SHA256 values. The runtime candidate is
640EB354BD3003B7453BEF1B5C9D3B51A1E63E41832F72B900E624EFAE04E325.
The unchanged fixture is
7BDEF9550DA99100EFD6F1977157DBCD7CFC88FBBA40DC8339203B6F660AF497.
owned-input-check.json confirms main and qualification copies matched after testing.
protected-before-inputs.json and protected-after-check.json retain unchanged hashes
for the completed lobby panel, social fix and the four reserved account/lobby files.

Both guarded sessions completed and their Unity processes exited before the shared
validation slot was released. The runner preserved zero existing named-profile
files and restored one shared Editor input preference in each run. guard-receipt.json
records session identities and preservation snapshots. No resource intervention,
reset, broad copy or unrelated source edit occurred.

## Limits

These are native EditMode tests of real lobby bookkeeping and result/map decision
methods. The result board uses a small supplied canvas, and component lifecycle is
not the subject of this check. The transport event, corrected broadcast delivery,
rendered labels, actual peers and complete player rematch remain untested here.
There is no new player build or whole competition qualification claim.

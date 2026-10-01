# Bank Shot mechanics and bounded bank credit

## Implemented

Current proposed Wiki Bank Shot replaces legacy Magnet in Zack's attacking slot:
35second cooldown,8second held-shoe load,85percent velocity retention on its
first side-wall bank. Overclock gives that throw two powered, credited banks.
Further contacts lose credit under the existing throw-chain scoring route.
Release consumes the load without the old1.6x launch boost or electric stun.
Dropping/replacing the loaded shoe, expiry and round reset clear it. Recovery
ages the load without cast/resource replay and tolerates equipment arriving later.
Existing visuals, clips and cues are retained; no authored art was edited.

Corner bounds consume one bank, scaling the entire velocity once. Ceilings do
not consume a powered side-wall bank. The shared ground guide and host bot
prediction use the same retention/consumption rules. Bot prediction also carries
the completed bank count, avoiding a renewed first-spin-bank bonus after use.

## Evidence and failed attempts

Isolated Unity6000.5.8f1 Windows/D3D11, source0d7133471 plus listed owned overlays.
Two baseline failures reproduced MAGNET naming/resource/held-shoe refusal.
First final compilation stopped on CS0122: the fixture directly referenced an
internal helper. One tooling repair switched to the public attached guide.
The next native run passed8/10; both guide comparisons hit the test's own player,
which a landing-only guide deliberately excludes. This was a second fixture
correction beyond the planned single repair; it is recorded rather than hidden.
The actor was moved aside, preserving the .12m comparison. The final focused run
passed3/3: both normal/Overclock guide comparisons and real bot prediction after
two powered banks. No unchanged broad suite was repeated.

11 distinct native checks passed across these runs, not11/11 in one launch.
Final738input hashes show no drift. Raw XML, original manifests and compile
errors are preserved in [receipts](bank-shot-checks/result.json).

## Compatibility and limits

Protocol125; stable ability ID zack_skill2 retained. Existing slipper affinity
integer carries values6/7 for remaining powered banks; no new packet layout.
Timed-kit recovery carries the existing bounded personal clock, now at most8s.
Only the host decides flight, bank credit and scoring. No new peer/build,
Relay/loss/rejoin or physical controller approval is claimed. The existing
generic121rejoin proof does not qualify these changed mechanics.

Proposed35s/8s/85percent values are implemented initial targets, not human
balance approval. Quick Circuit and Closed Circuit remain legacy/unimplemented;
full Zack is open. Older Magnet/sidegrade presentation probes are historical
contracts and are not completion evidence for Bank Shot.

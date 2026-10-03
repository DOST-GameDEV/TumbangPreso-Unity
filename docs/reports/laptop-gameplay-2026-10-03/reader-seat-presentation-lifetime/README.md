# Retire obsolete reader input without erasing another controller's tell

PlayerInputReader withdrew local throw/lunge input and unconditionally cleared
Carrier/Combat presentation before checking whether it still owned the network
seat. Newly received tells on the old body were erased. The fix always retires
old local windup, while preserving newly received presentation after seat loss.
It clears an old local tell when no received refresh replaced it.

## Measured proof sequence

| Run | Cases and result | Guard ended UTC | Frozen files |
| --- | --- | --- | --- |
| Original6 | Two causal failures, four controls PASS | 2026-10-03T11:33:17Z | 3368 |
| First candidate6 | All 6 PASS | 2026-10-03T11:39:18Z | 3368 |
| Added self-application control on original | One PASS | 2026-10-03T11:54:03Z | 3370 |
| Added control only on first candidate | One FAIL: old local tell 0.0246228017 remained instead of -1 | 2026-10-03T11:54:33Z | 3370 |
| Refined final candidate7 | All 7 PASS | 2026-10-03T12:00:24Z | 3370 |

The original six exposed received throw power 0.56 and received lunge tell both
being erased to -1. Assertions proving old local windup retirement passed before
the received-cache assertions failed. Four controls required still-owned throw
and lunge tells to clear, old local throw tell to clear without a new received
sample, and public-host committed lunge contact/cooldown to remain intact.

The first candidate genuinely passed those six. A source-reviewed snapshot path
then exposed a missing control: BroadcastWorldSnapshot reapplies cached observed
values through public setters on the listen host, outside the local publication
method. The extra control recreates that getter-to-setter self-application while
host-owned, then supplies another local seat and disables the obsolete reader.
It passed the original and failed only the first candidate. The refined candidate
passed the complete seven. Existing six case bodies/meta were never changed;
there was no fixture repair or repeat of the original six.

Each guard is terminal with completed preservation and a released lease. Failed
runs exit 2; passing runs exit 0. Main audited all frozen input hashes unchanged
for every run, including exact runtime/fixture bytes. No build or readiness
claim is inferred from these consumer results.

## Narrow implementation

Reader passes its existing offline/local-seat predicate into named consumer
retirement primitives. Both consumers always clear old local windup. Obsolete
Carrier retirement avoids publishing an inactive cue for the old seat; it keeps
a received tell but clears a tell still produced by local input. Scalar origin
flags distinguish those states. Public nonlocal received setters reset the flag;
local publication marks it after any synchronous host self-application, and the
local spin write marks its input origin too. An active, already-local-origin
sample stays local when reapplied for a current host driver. Inactive samples
clear origin, and existing finite-value rejection remains.

All existing no-argument cancellation entry points remain, including private
CancelAll/CancelCharge used by reflection. Their default full-clear/publication
behavior is retained. Contact windows, spent cooldowns, press ownership, owner
numbers, resets, held shoes, finalized kits/art, bind/AI logic and Net/protocol
source are unchanged. No TTL or new framework is introduced.

## Qualification boundary

The fixtures supply a registered Classic body, can and owned shoe in an empty
scene. They create local windup using public intent and shipping consumer Update,
apply public observed samples, supply the current seat through INetProvider, and
trigger actual reader.enabled=false lifecycle cleanup. Both fresh-received causes
first check that old local pending input ends, so guarding all cancellation
cannot pass. The contact window is observed read-only after public HostResolveLunge.

Motor physics and automatic consumer callbacks are disabled; consumers are
stepped explicitly. The receiver/authority transition is supplied, with RPC
Instance null. The added Carrier control qualifies public cached-value host
self-application. Actual network delivery, complete installer/bot handoff,
natural motion/tagging, physical controls and operator/full-match behavior are
not qualified. Lunge self-application symmetry, host-bot provenance and actual
WorldSnapshot/transport integration are source-reviewed preservation only. No
frozen artifact inclusion is claimed. Existing role/press fixes remain intact.

## Exact source and raw provenance

Local source-only base is cc9693607f691586fb437f1cd49f9080f8d96798. All jobs use
Main's qualified logical overlay 2e9036241cf240ec45b1ffe4925330101313067b on older
workerGitBase 8e7cfc7feb4eee614d456347ccbb26ad962d1714. Each manifest retains 14
changed base paths and its full 3368/3370 file map. These incoming paths are not
agent-owned changes. Tested code comes from the actual files map, rather than
assuming the older worker Git HEAD was current.

The final manifest retains an inherited reason label saying only the added
control ran and the old six were not repeated. That descriptive label is stale
for the final run: its actual three-class filter, expected 7 and XML prove the
complete seven ran. Raw metadata is preserved byte-for-byte; no reason, XML,
receipt or measured result was rewritten.

| Runtime | Original working/native SHA256 | First candidate SHA256 | Final working/native SHA256 |
| --- | --- | --- | --- |
| PlayerInputReader | 5c1ec1b76087822521658c5a51ed3a60b0d4eec46e8ca58eaebd3ddf041aa837 | 51df828b5793697a2133f0dc4a71afca2595fd6bcbafc51bab41652c0d90c91a | 51df828b5793697a2133f0dc4a71afca2595fd6bcbafc51bab41652c0d90c91a |
| Carrier | de5a84a4f05625d938ad17ab43f9b177751cce4f0b8f0a4d210c77073f805b59 | 3ca65c964054240b2eb507b0406087e010f275f98e65cfb608c31ca842e0bc6c | 8437b5a48bed9bc7a6106a8de164000860797a4049f490b384b7dafe7c3bf8bb |
| CombatVerbs | 3f24f6139906d1f6220215fe2e0413dc061313f91ccc127c97a5af2dc296b65e | 461ab278f88bdd4465e49853174aaad740fd0983e02ee2ee4b8c1c73d19b85da | bf5fdb7cefccab5b05e3874fdd08c3d20a977ef268dfede10737d5673b996c91 |

Final normalized LF hashes: Reader 3c754919456729ccf8c5deeafc2534c6358bf8d0fa226ab9031c343db53ba186;
Carrier 781810a1235987c1d488ef63184fae7a8e4ae3fd812823cabaa23d09ebb08520;
Combat 2310720d40f8c6b48701d4703f9d24606b9c4c25e4bf9e339f717ca323bd0448.

Immutable fixtures/meta (SHA256):

- ReaderSeatPresentationLifetimeTests: 0568e5c2b4e4926fdce63a68e85feb71516a6809705bc870583bde5688c59bf7;
  meta 6b66aa3d4beafc3cec5e9d3c4637274c2c9c736a7b68df73a18082b7e61c1431.
- ReaderSeatLocalTellControlTests: 74a1f66e96d42dda37a2993c5c4c6386882568f0868f32d914894b11fe8224f6;
  meta 09037ed4abb7bb821a8b33a411e32ae6dd7760fb7ac18ca26db89a4b2f7e39ed.
- ReaderSeatSelfEchoControlTests: 44781521341d5349d0b4b5e1439d45947e85ab303269da099fc7fb9c3c994956;
  meta da7a3df5e77b29611219c5ec258ca8e0d58778d67b6074c016de0bcec3c26b79.

Filters are those three fully qualified classes under TumbangPreso.PlayTests.
Original6/v1six use the first two; the added-only runs use SelfEcho alone; final7
uses all three. Raw originals/v1/final are qa-d/Logs/reader-seat-original6,
reader-seat-candidate6, reader-seat-final7; echo original is qa-a/Logs/reader-echo-original1,
and echo v1 is qa-d/Logs/reader-echo-v1-only1. All 15 copied XML/receipts/full-source
manifests are byte-identical to raw and protected by report-local Git attributes.

- Original6: [XML](original-tests.xml), [receipt](original-job-receipt.json), [manifest](original-qualified-source.json).
- First6: [XML](candidate-v1-tests.xml), [receipt](candidate-v1-job-receipt.json), [manifest](candidate-v1-qualified-source.json).
- Added original1: [XML](echo-original-tests.xml), [receipt](echo-original-job-receipt.json), [manifest](echo-original-qualified-source.json).
- Added v1-only1: [XML](echo-v1-tests.xml), [receipt](echo-v1-job-receipt.json), [manifest](echo-v1-qualified-source.json).
- Final7: [XML](final-tests.xml), [receipt](final-job-receipt.json), [manifest](final-qualified-source.json).

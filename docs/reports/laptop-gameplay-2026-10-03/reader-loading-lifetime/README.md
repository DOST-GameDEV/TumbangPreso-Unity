# Reader action lifetime beneath the loading curtain

Date: 2026-10-03. Published reader correction `b4ab7679d`. This separate
four-case fixture is qualified: the original has two causal failures and two
passing controls, and the published correction passes all four on the first
candidate. Main verified exact reader/fixture hashes and terminal preservation
before publication. There is no additional product patch. Main's PC/user joint network
run on existing artifact1003e completed before this gate was scheduled; that
artifact's network result remains separate from these new gameplay corrections.

## Why this adds evidence

The qualified chat correction cancels input in a branch shared with loading.
The seven chat cases qualify typing context, while this fixture exercises the
actual public loading curtain and its `Visible` predicate. The old reader
cleared/committed intent beneath either curtain without retiring pending
consumers; a still-active old scene could therefore publish an unintended throw
or lunge while the loading surface appears.

`HubLoading.Begin(Eskinita, externallyLoaded: true)` constructs the real curtain
and starts its existing external-loader wait. The fixture performs no scene
load, so that coroutine waits until the test calls public `HubLoading.Cancel`.
No `_current` field or loading boolean is injected. Loading entry preconditions
verify a live actor, non-parked intent and no presentation input block, avoiding
a false success caused by an already cancelled actor.

## Frozen gate and stopping condition

Run exactly `TumbangPreso.PlayTests.ReaderLoadingLifetimeTests`, four cases.
Original exact Git reader SHA256 from `0368f56f1` (also unchanged in `fa21c8af4`):
`960d81ed43ad9e7578f61caeb1a8ac81b8214768ec830db17f678f05ef27dd2d`.
Published corrected working-copy reader SHA256:
`23ded6eb3a6e88b1b3e0c5a7b08dc9e3f75f8f4421629abbef98b5d97f37c169`.
Expected original: two causal failures and two passing controls.

- Real TouchInput throw windup, public curtain entry and actual reader/Carrier
  updates; the shoe remains held and its pending charge/tell retires.
- Real TouchInput lunge windup, public curtain entry and actual reader/Combat
  updates; no new contact window or cooldown is spent.
- Public HostResolveLunge then curtain entry; committed contact/cooldown remain.
- Hold throw beneath the curtain, cancel the curtain and step the next frame;
  release remains necessary and a fresh press works. The original reader already
  has this loading-exit latch, so this is a preservation control.

Fixture SHA256:
`097bd87dcdbd780b0e2b03fcd4da04b378f292ac1234d5be8ec87b26ce81b2c7`.
Metadata GUID: `9cd4ce29d0194616ab293b526483cbc5`.

Both hooks reset the world. The fixture cancels only its own curtain and
restores provider, launch, touch, stats and can state. Motor and shoe flight
updates are disabled to isolate pending consumers. No worker writes or native
launches have been performed by the implementation agent.

Main owns frozen original/candidate inputs, the focused jobs and preservation
audit. Stop at fresh exact four-case XML and a terminal guard receipt. Use the
same fixture against old and published readers; do not change assertions to
repair a failure. This qualifies loading-curtain/reader boundaries, not full
map loading, shader prewarm, transport, physical devices, screenshots or the
joint network player. No duplicate chat/tournament check is needed.

## Byte provenance clarification

The first preparation stopped on the expected original hash before worker
writes or Unity launch. The initially supplied `19ad7988...` was the earlier
working-copy byte hash recorded before the chat patch, not the canonical Git
blob. These files have mixed working-copy line endings. Main retains the exact
Git original rather than attempting to reconstruct an old newline mixture.
Git `0368f56f1` and `fa21c8af4` both contain 509 LF and no CRLF/BOM and hash to
`960d81ed...` above. Git corrected reader `b4ab7679d` hashes to
`8491f40782dbbcf8c38817f0d014597f4a134f10a814f41ddcd4f950cb4f036b`.
The frozen working candidate `23ded6eb...` has 471 CRLF among 512 line endings;
normalizing its CRLF to LF gives that exact corrected Git hash. This is a byte
identity clarification, not a source, fixture, assertion or native retry.

## Native results

Main's `qa-b/Logs/reader-loading-original4` uses the exact original Git reader
`960d81ed...`. Pending throw and lunge actually release beneath the public
curtain, producing both intended failures. Committed-contact preservation and
the existing held-exit release/fresh-press gate pass.

Main's `qa-c/Logs/reader-loading-candidate4` uses the frozen published working
reader `23ded6eb...`. All four unchanged named cases pass on the first native
candidate. No fixture/assertion repair or native retry occurred.
[Original XML](native-original/tests.xml) and [receipt](native-original/job-receipt.json)
retain exit2 and both failures; [candidate XML](native-candidate/tests.xml) and
[receipt](native-candidate/job-receipt.json) retain exit0 and four passes.
Both guards are terminal, preservation completed and no lease held.

This accepts the real curtain construction/visibility/cancellation API feeding
the existing reader and local consumers. It is separate from full map loading,
shader prewarm, physical device control, actual peers and artifact1003e's
joint network run.

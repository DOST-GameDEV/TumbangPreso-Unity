# Career flush stays with its captured account cache

Original neutral shipping core843af78b7db397480e6048b9c1651203388dda8d plus
fixturee23807d3a54d871ece19e3e5b2bad7924dcc99b6 produced3 causal failures and
3 passing controls. Candidatee92a760c8b6a8e53e699baeadfd08a8bba8142b0 with the
exact same fixture/meta passed6/6, zero skips. Combined checked candidate branch
competition-laptop-career-flush-candidate6-1004 is dc02c0bddcba4dccd9da7adaa473a15511958c76.
PC owns CareerStore source and final integration; this publication is evidence only.

An old delayed failure overwrote replacement-account status. A successful
acknowledgment whose Changed callback switched to an empty replacement cache
claimed Career saved for that new owner. With a nonempty replacement cache,
the old operation dispatched and drained its queue. Capturing the original cache
and fencing loop/final/catch fixes all three. Existing old-success refusal,
same-owner failure/retry record+witness and same-owner success controls pass.
Finally releases the busy flag in every case so legitimate later work can resume.

The neutral extraction is a shipping private core invoked by public FlushAsync,
which passes CloudCode.CallAsync. It preserves the original body and replaces
only its dispatch call with a per-invocation delegate, following the existing
report-helper architecture. Fixture uses the same core, dormant fake signed-in
account/store, valid records, TaskCompletionSource and observed Changed callbacks.
It verifies dispatch script/record/witness, an actually incomplete task, ownership
replacement before completion, status/queue/witness preservation and busy release.
No mutable global test hook, IL patching, authentication initialization, credentials
or real REST/service call was used. The older ce013 candidate was superseded and
never tested or integrated separately.

Both serial headless EditMode jobs ran on gamergmae Windows11/Unity6000.5.8f1,
CPU2048MiB/reserve1024MiB/300s, independent physical Library, validation identity
and profilevalidation-qa-a-be075dcdfa07. Guards ended2026-10-04T00:21:15Z and
00:26:32Z, exit2/0, terminal, preservation complete and leases free. All3436
declared hashes match after byte-exact QualitySettings restoration. Only
CareerStore.cs differs between maps; raw tested fixture/meta and runtime variants
are preserved. Metadata has mixed line endings; normalized Git GUID/code match,
and native original/candidate bytes are identical. No diagnostic or fixture retry.

This proves controlled native async ownership at the shipping core boundary.
It does not qualify live HTTP/authentication, real sign-in switching, actual
account UI, endpoint delivery, packaged players or two-machine saved results.

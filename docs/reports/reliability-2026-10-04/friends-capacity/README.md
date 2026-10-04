# Full friend lists keep acceptance consistent

Baseline: ce9d7dd74640d0f43d0af5067cfc89f3696cc49e, October 4.

The owner requested the full login/gameplay/Friends journey. The Friends UI has
a NAME#TAG search, SEND REQUEST and incoming ACCEPT/DECLINE actions. SocialStore
resolves the handle through player-account and sends the request through social.
Friendship is saved by the server in both accounts and adopted by the client.
Those source routes exist; deployed two-account and rendered operator acceptance
are not established by this report.

The actual social script accepted a request even when either account already
had 100 friends. Its normalizer silently trimmed the new row on that account
after pending rows were removed. The other account could retain a friendship
that was absent from the full account. Crossed requests had the same failure.

Accept now checks both capacities before either save. Crossed requests also check
the other account. A refusal leaves both pending rows and existing friends intact.
An already-present reciprocal friend does not need a second capacity slot.
No UI design, cloud schema, ordinary list limit or service deployment changes.

`node tools/test_social_flow.js` loads the actual exported social endpoint in
a VM with two local accounts and a fresh Cloud Save stand-in. Original: three
capacity failures and five normal-flow controls. Corrected: 8/8. Controls cover
request/accept/reload, mutual requests, duplicates, decline and removal. Capacity
cases prove zero writes and retained pending state when either side is full.
No SDK installation, live account, paid service or Unity/player launch. Zero
tooling retries. This is not concurrent-writer/transaction or deployment proof.

Remaining user-flow work includes live handle lookup/service credentials,
two-account accepted friendship/presence/invite/join, focused-field refresh and
input methods, login/offline/retry/navigation, normal gameplay/result/return,
actual startup/first-use performance and correct preload timing.

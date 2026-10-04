# Spectator footage stays with its match

The spectator pixel ring and polling baselines survived same-scene match
restarts. The consumer now checks director and presentation identity at update,
capture, replay request and score boundaries, retires old playing footage and
markers, and resets polling baselines. Same-match round footage remains. Pending
readback counts and generations are preserved; existing frame-membership checks
reject retired wrappers before image writes, even when textures are pooled.

The first original6 had two stale-footage failures and two identity-precondition
failures; first candidate6 passed4 and still failed two identity preconditions.
Both raw runs are retained and are not clean acceptance. Rapid local starts can
share UtcNow ticks. Local presentation identity now advances monotonically using
the same UTC/previous+1 policy already used by the network producer. Network
branch, wire and bookkeeping remain. Original identity3 fails2/passes1.

With that prerequisite corrected, the mixed cohort passes all three identity
cases, reproduces four old spectator causes and passes two spectator controls.
Its overall failure is expected and preserved. The final spectator candidate
passes6/6 with the same unchanged fixture and no sleeps. Independent reviews pass.

Nine unique native cases qualify these two source changes. The spectator cases
invoke actual StartMatch, score and replay methods on dormant objects using a
graphics Editor, but do not schedule real GPU readbacks or inspect rendered
playback. GPU completion, ordinary operator/player flow and actual network
acceptance remain separate. Every guard terminated, restored profiles/preferences
and released its lease; no live profiles or services were used.

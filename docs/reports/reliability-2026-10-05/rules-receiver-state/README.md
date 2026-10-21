# Host rules adoption belongs to the receiver

OnSyncRulesMsg validated the host payload then notified UI observers. Shared
SceneFlow rules changed only when a lobby view handled that event. Without that
view, valid four-round Hero or six-round Classic packets left the client at eight
rounds. The targeted receiver now adopts the clamped remote rules before notifying
views. Adoption preserves saved local preferences; the existing view refresh path
and host/shape guards stay intact. The wire format and protocol151 are unchanged.

Native receiver cases call the production handler with real serialized buffers
and no lobby view. Both valid-mode causes fail before the fix. All five cases pass
after it, including state-before-event order, unchanged saved preferences, foreign
sender rejection, empty-packet rejection and listen-host loopback rejection.
The fixture also restores its prior rule pin in teardown; case expectations did
not change. This is native receiver evidence, not a packaged reconnect/WAN claim.

Original PID11640 exited2; candidate PID22780 exited0. Parents are terminal.
19,292 protected inputs compared. Intentional differences are MatchRpc.cs and
fixture pin restoration. Generated importer files were retained and restored to
exact original bytes. Input/editor preferences, QualitySettings and fresh profiles
were restored. Pre-existing Auditor dirt and private cancelled wardrobe draft stay.

The earlier ordinary local pair on source97a313 verified arrival, one browser row,
correct LAN address and four-round result headers with equal full saved records.
That package predates this receiver-lifecycle correction.

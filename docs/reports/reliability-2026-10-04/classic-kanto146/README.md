# Actual Classic tournament eight rounds on Kanto

Same coherent source23168/protocol146 Windows player ran normal two-peer lobby
admission and real host map selection on Kanto. Tournament mode is Classic.
All eight90-second rounds ended naturally:130/50/3450/2545, seat2 won. Host and
client full saved record hashes match d27f0b9246d44696bdda5aa1e5c212336f7a75d8ef9ff78a8ef26e7bde4d09f9;
queues match histories, witnessc9f645728c718e75 matches and in-match markers clear.

Host19684 final1650 report: HOST/networked/protocol146/Classic/Kanto/round8
inactive. Normal exit0/input/profile restored and complete artifact hash recheck
passed. Client30144 final1500 report independently remains CLIENT/Classic/Kanto/
round8; normal exit/restoration/all258hash checks reported by laptop, raw pending
integration. Its HostLost callback happened after the report in own-quit teardown,
not during gameplay; it is not claimed as recovery or physical-disconnect proof.

The initial script was labelled Hero and seeded Hub2 but also passed-tp-tournament.
That pins Classic. Original expected-Hero assertions remain failed and retained;
actual-classic-classification.json validates the actual Classic default wire
separately. The record MapId is canonical sceneKanto, not lowercase shorthand.
This run does not qualify Hero. A separate true-Hero driver omits tournament
entirely and retains the default eight rounds without a custom short wire.
Human-origin seats were undriven with two ordinary bots; human-feel/device and
wider Relay/recovery/newer-source acceptance remain distinct.

# LAN address hint and duplicate room rows

The actual ordinary two-player LAN run showed a Npcap 169.254.38.78 host hint
instead of the active Ethernet 192.168.1.7 address, plus two identical room rows
for one host. ConvertedMatchSetup used the first non-loopback DNS address.
LanBeacon keyed rows by address/port despite receiving a process beacon identity.

The hint now prefers non-link-local IPv4, retaining link-local and loopback
fallbacks for machines without another address. Current beacons use identity/port
for row identity. Older beacons without identity keep address/port behavior.
One host's recently observed non-link-local endpoint outranks link-local or
loopback endpoints, while room name and occupancy continue updating. A preferred
endpoint that stops advertising expires after the existing four-second deadline;
no separate timer or network wire format was added.

Four native cases use actual DNS enumeration for the host hint plus real beacon
serialization/parsing and the production main-thread inbox for discovery. Before
the fix, the hint and duplicate-row cases fail; separate-host and older-beacon
controls pass. The same four cases pass after the fix. Fresh standalone browser
acceptance remains open, as does a timed preferred-endpoint retirement check.

Original PID22020 exited2; candidate PID22388 exited0. Both parents are terminal.
19,290 protected inputs compared; only the two intended production files differ.
Unity's generated importer metadata was retained then restored exactly. Shared
input/editor preferences, QualitySettings and isolated profiles were restored.
Pre-existing Auditor dirt and the cancelled private Yasmin wardrobe draft remain.

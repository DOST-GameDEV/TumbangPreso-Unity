# Desktop LAN port boundary candidate: laptop native qualification

The original parser accepted UDP ports65536,65537 and int.MaxValue in both
legacy and current advertisements. The transport stores ports as ushort, so
those values cannot identify the advertised endpoint without truncation.
The desktop's exact one-line upper-bound candidate passes12/12 native cases;
the unchanged original reproduces6causal failures with6valid controls passing.
Desktop owns production/test publication. Laptop changed only immutable worker
overlays and this separate evidence folder, leaving main production unchanged.

## Question and exact source

Can TryParsePayload reject values above ushort.MaxValue while preserving port1,
default8910 and65535, join code, host name and joinability for both wire versions?

Base61a650049b4949bda2c60d8df8991ff1e8705ed0 has the same relevant source as the
desktop packet's917342b22. Exact normalized LF SHA256 values match its supplied
packet, recorded in checked-source.json:

- Original LanBeacon.cs: dade218b3003b643ba3f41604ee0b5f6415f81ae34c30a60b1805daf78fe0c2d.
- Candidate LanBeacon.cs:204876dd92b89f703cf206837268857a81fdaec0d825937491c9c7be3705ec43.
- LobbyAndSettingsTests.cs with the12case overlay:
 0664186898a4a3995ba07eaf11d97eebc68a92a1f3f32553e51e2a38661cdcc0.

Candidate adds only `|| port > ushort.MaxValue` to the existing nonpositive/parse
refusal. No default port, transport, host, wire, profile or protocol change.
No new test metadata or assemblies. The two exact method filters are in packet.json.

## Native results

Laptop gamergmae/Windows11, Unity6000.5.8f1, isolated warm qa-a worker and named
validation profile. EditMode, actual Unity Test Framework XML, no skips.

| Run | Malformed legacy/current cases | Valid legacy/current controls |
|---|---|---|
| Original Unity10824/parent61965 | Six FAIL: expectedFalse, actualTrue | Six PASS |
| Candidate Unity43616/parent82557 | Six PASS | Six PASS |

Both parents are terminal; original exit2 and candidate exit0. Each protects
19316inputs and preserves/restores265generated metadata/Auditor changes, Quality
and13existing preference values. The warm baseline applied only one bounded
current LobbyJoinPanel update before freezing; unrelated passing cases were not
repeated. No assertion, filter or expected case count was changed after execution.

## Limits

This proves malformed-advertisement parsing and valid-port preservation under
native Unity. It does not establish that invalid ports caused the initial LAN
discovery delay, Request timeout, host-kick, quit fault or any actual peer failure.
The ordinary current-package online admission/Ready/leave/rejoin control is
separate evidence in ../current-online-lobby. No malformed UDP packets were sent
to the owner's PC or any external host. The qualified candidate still needs the
desktop's checked source commit and integration; this report alone is not a claim
that shipped production has changed.

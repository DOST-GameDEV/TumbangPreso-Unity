# Exact departure notice framing

PeerDeparture previously accepted trailing unread bytes after its bounded name,
then displayed the notice and consumed its sequence. A subsequent valid notice
with that sequence was suppressed. The receiver now requires the reader to end
exactly after the name, before committing sequence or presentation state.
The consumed NGO name envelope is preserved; sender, match, seat, reason,
freshness, name sanitation and transport cleanup rules remain unchanged.
No schema or protocol change; current compatibility115 remains.

Native EditMode baseline2/2fails for one-byte and64-byte suffixes after a consumed
eight-byte envelope. Final6/6passes: suffix rejection without sequence poisoning,
valid enveloped notice once, existing host/match/seat/reason/freshness controls,
name sanitation, intent envelope and new-transport cleanup.
Source661ed3bfd plus two owned overlays;666frozen inputs have no drift and both
files match the tested candidate. [Receipts](peer-departure-checks).

Two initial launches exited before project/tests with license error198 and no
XML. The earlier planning launch had accepted Unity Personal. After the owner
refreshed Hub, a separate baseline-v3 launch accepted Personal/Unlimited and ran
the actual failing cases. Final then passed without fixture changes. This was
process entitlement validation, not a missing Editor or proof of Hub logout.
All launches are terminal and guarded preferences/profiles are preserved.

This qualifies the real receiver locally, not a new player/actual-peer departure
journey. The previous passing114LAN pair remains its own older source evidence.

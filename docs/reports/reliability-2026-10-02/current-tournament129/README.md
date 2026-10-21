# Refreshed Windows player in Classic tournament context

Frozen committed overlays25fab6cab, protocol129, in imported isolated checkout.
Windows build succeeds2432MB/79s,12shipping scenes;14critical/changed hashes
unchanged. Runtime c838dd34d3037ae123bba4582075415c3c8b3e4f65411f3be569f8f0e95f34e0.
Own build stampaecc0ee2+dirty is retained; this is an internal integration player,
not pristine release certification. Includes the later queue/service/career,
preview-audio and replay optimizations omitted from the earlier player.

Existing protected-profile direct LAN route at1920x1080/D3D11/Balanced/60cap,
Kanto, -tp-tournament with allbots flag omitted. Both actual peers report Classic,
8round tournament preset, ruleset OK, modifiers none, active round2/taya1,
protocol129 and structural BEB31538. Ordinary bots move; peer-controlled seats
are not artificially driven. No physical-input or full8round-completion claim.

Raw existing evaluator result is FALSE: its only two faults require HeroStrike,
where this deliberately selected Classic. Original result.json is preserved.
observations.json checks the stated Classic scenario against actual reports;
it does not replace the generic verdict or claim a green automated matrix.
No unchanged rerun or unrelated evaluator/harness rewrite.

Host live histogram59.62FPS/max270.33ms; client59.77/max28.86ms on Ryzen52600/
RX6600/16GB. The long host frame remains an observed stall, not fixed or attributed.
No script or shader error found in these logs. Different150/163sreport instants
include client automatic quit and later host bot takeover; do not compare gameplay
hashes, scores, can state or travel as simultaneous equality. Client's later
HostLost/TransportShutdown log follows its deliberate automatic exit.

Profiles/shared input restored, task players/ports released, temporary runner
removed. Desktop build unchanged. This is current internal build, tournament-
preset and limited round-progress evidence, not full operator flow, all maps,
all effects, physical devices, Relay/WAN or whole tournament-readiness approval.

# Server travel limit follows the current movement rules

Baseline:73ef60af32e32a92c2c7bcc5e5e910fc18b656b0, October4.

Core IntegrityRules.Check uses Balance.DefenderRunSpeed7.5 to bound recorded
travel. The server sanityFault still used4.6*1.5, the retired6.9 speed. For a
30-second single-round record, Core accepts distances up to1800. The server
refused1656,1700 and1800. The1656 refusal also reflects JavaScript's floating
point result for the old multiplication. These are supplied boundary records,
not a claim that an operator match actually reached those distances.

The server now copies DefenderRunSpeed7.5 explicitly. No gameplay tuning,
record schema, witness digest, rating or other refusal rule changed.

## Focused evidence

The actual engine-free Core assembly built from this baseline was loaded through
PowerShell. IntegrityRules.Check accepted0,1000,1656,1700 and1800; it rejected1801
with ImpossibleTravel. Assembly SHA256:
`a40c7d4049e911fd3921ebedd5886790bc44fe2fb3b786283c2e95cbdb00417f`.
Its exact returned values are in [core-results.json](core-results.json).

`node tools/check_integrity_contract.js <core-results.json>` loads the actual
server source in a VM and compares all six records against these Core answers.
Original:3passed/3failed. Corrected:6passed/0failed. The existing
`node tools/check_digest_contract.js` also passed, preserving7b135cbb69492fa5.
No native job or external request was needed. Zero tooling retries.

For a fresh Core comparison, build Core/TumbangPreso.Core.csproj, then run
the retained [Core probe](check-core-travel.ps1) with its assembly path and pass
the resulting JSON to the Node check. The check without JSON still compares
the server boundary to the current Balance source.

This establishes local source parity only. Cloud Code deployment, SDK requests,
packaged result delivery and matching-machine acceptance remain open. The source
change has not been deployed to the remote service by this unit.

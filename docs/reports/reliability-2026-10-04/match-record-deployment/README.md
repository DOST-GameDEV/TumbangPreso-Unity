# Match-record production alignment

Production still ran v5 while the repository contained checked career fixes.
The exported v5 endpoint reproduced five behavioral failures: missing mastery
for Rafi, Amihan and Paete, unrecovered history after a failed write and rewards
being applied to an offline record. Four controls passed. The exported v6 passes
all nine cases. Its bot-rating functions pass four checks covering human-seat
weight, zero-weight confidence/season credit and reduced gains and losses.
The v5 export lacks those functions; those four failures establish missing
coverage rather than a live ranked-account experiment.

Published the existing canonical script as production v6 on October 4 at
07:31:24 UTC in the existing project. Fetched published code exactly matches
ugs/cloud-code/match-record.js after line-ending normalization. Seven endpoint
parameters are unchanged and v5 remains available for rollback. See deployment.json.
Travel checks pass all six boundaries against retained actual Core results.
Witness digest remains 7b135cbb69492fa5.

These tests execute the actual exported script with a local Cloud Save stand-in
or pure rating functions. Publication metadata is live service evidence. No new
live player submissions or rendered SDK acceptance are claimed.

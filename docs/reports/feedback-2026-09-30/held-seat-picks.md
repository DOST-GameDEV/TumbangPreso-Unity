# Reconnect retains match picks and takeover rating

LobbySession held only the departed token and seat. A genuine reclaim returned
the seat but lost character/can/slipper picks and the rating hint used for later
bot takeover. A value snapshot now retains those four fields, restores them only
for the matching token, and consumes the hold. Reset, new match, lobby return and
match end discard it. Account identity and handle trust are freshly verified;
they are not inherited from a departed transport.

Following the real path exposed a second issue: Identify immediately overwrote
retained choices with local preferences. SetArrivalPicks now keeps already chosen
match fields, initializes unknown backfill fields, and permits normal changes
after returning to the lobby. Cosmetics authorization uses the resolved server
character. Ordinary SetPicks/admin routes and hero mechanics remain unchanged.

## Evidence

The strengthened existing three-reconnect case fails before the fix: character2
becomes-1 on the first reclaim. Four native EditMode cases then pass: repeated
cycles, immutable departure snapshot/fresh account metadata, foreign-token
isolation and four boundary routes. Capture/reclaim/boundary functions are
unchanged in the later Identify addition; this evidence is reused.

A separate native listening-host case invokes actual HandleIdentify. Baseline
fails when retained character2 becomes0. Final1/1passes, keeping all three picks,
allowing changes after lobby return and initializing a fresh backfill.
Five distinct native cases across two candidates; not a single5/5run.

Both candidates use sourcef66cf0664 plus explicit owned overlays. Final673input
hashes have no drift; four owned source/test files match the final candidate.
Jobs6759/43206/99354/12624terminal; no fixture repair or repeated unchanged suite.
Guarded profiles/input preferences preserved. [Receipts](held-seat-checks).

No wire field, protocol117, scoring/account authority, loading or authored asset
change. This is native lobby bookkeeping plus actual handler evidence, not a
new two-process cold crash/rejoin or whole-world ability-recovery qualification.

# Scenery fingerprint component comparison

An opt-in trace records the existing fingerprint inputs when a model is first
hashed: mesh streams and indices, material CRC/shader/keywords and exact texture
identities. Serialization and validation are unchanged. Normal gameplay does
not emit the trace or compute the diagnostic component hashes.

Two independent Windows Unity editor processes pass their selected controls.
The actual saved aspin clone has 28 distinct component rows with identical
values across both processes. Its fingerprint is 0F756E3EABE235A1CDA03398CE6E18C5DC8580942067A6E7E08B5CE01D7937F8.
The retained player recording's value is D612662E2F7ACEAC6E4C00317E9126EFC0477859AFBA0381D7644CB64E0D311F.
Both editor inputs contain generated NearFade materials. This does not yet
identify the player/editor difference or justify changing provenance checks.

The first process also passes the actual animal/line pixel comparison. The
second repeats only the incompatible-recording control to compare a fresh
process. All 21,415 frozen inputs and preferences are restored after each.
Exact source bytes and raw receipt hashes are retained here.

Next collect the same Windows player components using a replacement of the
existing candidate folder. No new build folder or Desktop copy was created.

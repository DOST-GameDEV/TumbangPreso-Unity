# Bound cast preparation decoding

OnCastPreparationMsg directly decoded a fixed header, UTF16 hero string and
52-byte tail without preflighting available payload bytes. Truncated host
messages could throw from FastBufferReader rather than being rejected. Whole
player crash, corrupt transport traffic and live-peer impact are not established.

Candidate088ae2aee reuses SkipWireString to validate the complete frame and
restores the decode position before reading. Extra trailing bytes are rejected.
Payload layout, protocol141, host authority, kit behavior and timings are unchanged.

Original with the identical regression fixture is8f0b34054. Ten native cases
cover six truncated boundaries, trailing bytes, two full UTF16 frames and a
non-host empty-payload control. Source expects seven causal failures and three
controls, but no native outcome is claimed before the laptop runs them.
Runtime and test assemblies compiled against the installed Unity references.
Compilation does not establish imports, native decoding, player or peer acceptance.

Validation is assigned to the existing laptop chat while Claude owns PC Unity.
Candidate is pushed on competition-pc-cast-preparation-bounds1003 and is not
integrated into ASTRAReworks. Merge only after focused original/candidate evidence
and unchanged fixture hashes are inspected. No PC Unity run was started.

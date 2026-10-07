# Optional replay fixture default-suite behavior

The streaming controls require an actual saved replay supplied through
TUMP_REPLAY_STREAM. Their first version asserted that the environment variable
was present, producing four false failures in an ordinary suite without that
explicit fixture. Entry now uses Assert.Ignore with an actionable reason when
the fixture variable is absent. Present but invalid inputs still fail.

Native32 explicitly removes the variable and produces four Skipped cases with
the expected reason, zero failures. This is skip-path evidence, not four
gameplay passes. Active-fixture algorithms and the100ms timeline threshold are
unchanged; the four native30 actual streaming/seek/close passes remain valid.
All21389 source inputs,13 original Editor preferences and four original profile
files restore after terminal execution. Exact native source bytes and explicit
CRLF-only normalized source proof accompany the receipt.

Runtime behavior is unchanged. This test-only correction does not qualify a
new packaged build, wider visual fidelity or global frame pacing.

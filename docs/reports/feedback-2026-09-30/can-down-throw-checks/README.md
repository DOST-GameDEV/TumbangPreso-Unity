# Can-down throw rule

The current Feedback request replaces the older allow-down behavior: knockdown
cancels an ongoing charge, and charging/release wait until the lata is upright
and restoration protection has ended. Existing airborne throws are unchanged.

Core now requires upright lata. RoundDirector's shared start/maintain/release
gate includes actual can protection, so Carrier's existing cancellation clears
charge and broadcasts it. No new charge transport or altered launch power.
Protocol118 prevents peers with the earlier accepted-input rule joining this batch.

Baseline1native case reproduces the old allowed-down behavior. Final same case
passes half/full power, down-can cancellation to zero, no charge behind restore
barrier, fresh charge from zero afterward, successful release and caption/pulse
cleanup. Focused Core1case passes every independent refusal. Frozen inputs
unchanged; OOM11/kill6unchanged. No tooling retries.

No new actual-player or matching118peer qualification. The centre-dot request
remains separate; the charge case still uses the existing hollow reticle.

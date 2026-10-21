# Host-loss acceptance requires an explicitly inactive round

The matrix's terminal-client evaluator rejected a live round only while the
player still called itself CLIENT. A player that auto-hosted its lobby while old
simulation remained active passed; HOST with a missing active field also passed.
The evaluator now requires explicit round-active False after host loss, regardless
of the current network role. An absent field cannot supply terminal evidence.

Three engine-free cases use the actual evaluator. Original committed function:
2 causal false-positive failures and1 explicit-inactive control passed. Candidate:
3/3 pass; inactive HOST and CLIENT both remain accepted. The earlier input
preservation2 cases also passed with the final evaluator change (5/5 total).
No real registry, Editor, player, transport or profile was touched. Raw original
test counts/output accompany this report; source snapshot remains in local
Logs/net-matrix-terminal1002. This is acceptance-tool correctness, not a runtime
fix or evidence that host loss currently occurs in the game.

The legacy matrix's absent-host-report evidence still does not prove that the
planned kill happened. The separate bounded host-loss runner must record owned
live peer identity before the actual kill and its captured PID/exit afterward;
no historical matrix evidence is reclassified as a verified kill by this change.

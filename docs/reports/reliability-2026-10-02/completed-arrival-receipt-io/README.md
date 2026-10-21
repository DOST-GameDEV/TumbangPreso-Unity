# Completed-arrival observer IO cannot interrupt snapshot handling

The first1002i actual Windows peer run55528 timed out after a naturally completed
match. Its client log records File.WriteAllText throwing IOException from the
opt-in observer's Ended callback into MatchDirector.ApplySnapshot/MatchRpc. Both
observers read peer receipts concurrently; root also inspected live receipts.
The particular reader responsible is unproven. Original logs and failed receipts
remain unchanged in windows-candidate1002i/peer-first. This is a diagnostic fault;
ordinary shipping-mode transport and match completion are not implicated by it.

NetCompletedArrivalProbe reads now share write/delete access. Truncated or partial
JSON remains unavailable until a later observation, as before. Save catches IO
and access refusals, logs once per consecutive refusal and leaves the callback
running. Its existing update writes the latest receipt again; a successful write
rearms the warning. No gameplay, score, schema or protocol change. A locked final
receipt can still prevent evidence collection; no guaranteed-write claim is made.

Existing failed actual-player evidence supplies the causal original. One focused
EditMode candidate99649 passed exactly3/3 first run on Unity6000.5.8f1:

- A real owned FileShare.Read lock refused Save during the actual private Ended
  observer callback, but the callback retained its count and did not throw. A
  repeated Save produced no second warning; releasing the lock allowed recovery.
- Read succeeded while an owned shared read/write handle remained open.
- Ordinary end-observer writes published the latest event count.

The native fixture uses random owned temporary receipts and invokes the actual
observer methods. It does not reproduce MatchDirector's whole subscriber chain,
the network, terminal Finish/Quit or ordinary gameplay. Actual Windows peer
acceptance must use a new artifact containing this correction.

Preparation68414 exit0 copied exact3 source/fixture/meta. A concurrently retired
helper fixture/meta required ONE preparation coordination correction89101 before
native launch: retained bytes and hashes matched, every other input was unchanged,
and the original protected snapshot was preserved. No source/fixture/native repair
or retry. CPU1536MB plus2048MB reserve,450-second ceiling, EditMode/nographics,
profile completed-arrival-receipt-io1002. Guard terminal/restored/no lease.
Protected post82197 retained its strict failure: Unity created previously absent
NetCompletedArrivalProbe.cs.meta for the copied owned observer script. Separate
artifact classification passed: all18424 originally protected inputs unchanged,
exact3 MAIN/qualification source/fixture hashes match, and the sole added file is
valid32-hex script metadata. MAIN's committed metadata remains unchanged; no GUID
equality/scene-reference claim is made. Both metadata hashes and strict-result
hash are recorded. No file mutation, deletion, source repair or native retry was
used to classify it. Raw XML, input manifests and both post results accompany
this report. Full logs/manifests remain in local
Logs/completed-arrival-receipt-io1002. No browser or preview opened.

# Owner arms tolerate a missing roster catalog

RosterBook.Load explicitly supports a missing Resources/RosterBook by retaining
fallback appearance. ViewmodelArms.MatchCharacter dereferenced that nullable
result when a CharacterVisual already had a model, so CameraRig.Follow threw
instead of retaining the mode/index identity. UseRosterArms repeated the same
unchecked lookup for roster-authored arms. Null lists and unassigned art rows
also made the lookup throw.

Keep the existing ability/mode/index identity when the catalog is unavailable.
Inspect only a non-null book/list/row. A valid matching model still overrides
identity as before. No kit, authored model, animation, arm shape or timing change.

## Evidence

The reduced native owner-animation setup exposed the initial NullReference at
MatchCharacter. Adding missing production shaders was the one setup repair;
its next run reached the real nullable-catalog defect. A subsequent animation
capture remained unacceptable: the compile-heavy candidate hit the memory guard,
and its single cached retry timed out after120s with21 obstructed frames. Those
runs are retained locally; none is reported as passing visual acceptance.

A dedicated five-case PlayMode fixture then isolates the catalog behavior:
missing catalog with Zack and Rafi, null People, a null row, and a valid matching
model control. Original shipping source reproduced four failures and passed the
valid-model control at05:21:54UTC,0.5678787s, terminal exit2/no memory guard.
The candidate changes only ViewmodelArms.cs against the frozen original inputs.
Candidate five/five passed at05:24:21–22UTC,0.4806869s, zero skips,
terminal exit0/no guard. Peak tree2,560,131,072bytes and
cgroup6,879,678,464bytes. Both restored settings matched. Only the same two
isolated Mesh assets changed after execution; source/package/fixture hashes
matched. The original and candidate maps differ only in ViewmodelArms.cs.

The valid-model control exercises imported Rafi arms. The existing outline path
adds tangent data to those two isolated Mesh assets during the run. Their
post-run bytes are retained, and their exact original bytes are restored before
the candidate starts. No Main asset is changed. Source/package/fixture identity
and settings restoration are recorded separately from this mesh side effect.

These are native reduced-project failure-path checks, not a complete scene,
packaged player, visual-composition or actual-peer acceptance result. The
observed missing catalog is a controlled validation setup; no live player is
claimed to have lost its catalog.

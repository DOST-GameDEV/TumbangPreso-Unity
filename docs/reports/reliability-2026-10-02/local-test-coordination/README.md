# Local test coordination

Three implementation agents shipped code in separate scopes while native checks
ran sequentially. Parallel Editors are optional on this PC: Ryzen5 2600,
6cores/12threads,15.95GiB RAM. A measured scene job used2.08GiB working memory
and left1.57GiB physical free. Two heavy scene runs are not qualified.

The job runner defaults to SERIAL. Optional two-CPU overlap requires explicit
permission on BOTH jobs, validation-only worker identities/caches, noncolliding
profiles/ports and sufficient memory. GPU/build jobs and outside Editors/game
players are exclusive. Worker preparation does not launch Unity or reuse/delete
existing worker directories. Its separate company/product identity is necessary
because launch profiles alone leave Editor PlayerPrefs shared. The guard now
protects the actual identity's save root and three input preference keys; old
identity-unaware guards are rejected for validation workers. Workers cannot
build shipping players.

Local checks: guard18, job runner22, worker preparation10 pass (50 total).
A real admission probe found the outside Amihan Unity process and refused without
launching a guard/Editor or holding a lease. It also declared a deliberately
unavailable64GiB budget to ensure no launch if that process exited mid-probe.
This proves safe refusal, not simultaneous Editor throughput, successful worktree
activation, native worker-pref isolation or licensing capacity. No worker/checkouts,
Library clones, paid service, installation or native parallel experiment occurred.

Source/CLI details and the same limits are in docs/TESTING.md. The pool coordinates
cooperating launchers; it cannot prevent an unrelated raw launcher from starting.
The production goal remains competition readiness and actual game fixes.

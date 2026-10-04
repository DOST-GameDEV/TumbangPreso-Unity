# Editor build-scan cleanup resource experiment

Status: measured resource experiment, NO accepted build or artifact. Do not
interpret the opt-in cleanup candidate as a qualified fix for laptop build memory.

Candidatead0026b01918da1eff7e28f1bd246beb1c32134c changes only Editor/GameBuilder.cs:
batch-only -tp-build-trim-unused-assets runs GC, unloads unused assets while keeping
script/static roots, then GC after the saved shader scan and identity stamps.
The scan loads project materials before BuildPlayer. Full variant collection,
quality, Runtime and reserve remain intact. Default builds do not opt into it.

## Actual measurement

The opt-in boundary executed in Unity6000.5.8f1. Unity allocated bytes changed
1095992544 ->681176596 (395.60MiB lower), reserved bytes stayed1717899264,
elapsed437.851ms. This API measurement is separate from process-tree working sets.

The actual local headless RELEASE job started with6474MiB available,4096MiB
admission estimate plus1536MiB runtime reserve,600-second deadline, one worker
and GC helper, and no development/profiler flags. Among129 one-second samples,
job-tree working set peaked4964.11MiB and available physical memory reached1216MiB.
The guard measured1209MiB below1536, interrupted with125 at05:01:49Z, stopped
only its verified owned Editors12248/23764 and completed restoration. The wrapper
then completed its source audit. No executable exists.

The process-tree peak exceeds the earlier attempts' sampled peaks. Allocated
object reduction does not prove peak-working-set reduction or build success;
the pool's reserved bytes did not fall. No unchanged retry was launched.

After all jobs terminated, compiler-server32264 remained547MiB resident. Its
exact creation time, Unity6000.5 DotNetSdk/VBCSCompiler command and recorded job
descendant identity matched; its parent32796 was absent. With no Unity slot live,
only that owned orphan was stopped. The local cleanup receipt is retained;
no unrelated app or shared licensing process was stopped.

## Source and scope

Shipping8f7f304b543d93db71ed09f9fbaa4107fc365895 has the committed build-input
tree of candidatead0026. ALL210 preexisting dirty raw files remained unchanged,
including208 prior importer files and two preserved build-generated settings.
The new19,662 input map fresh-hashed216 changed/dirty files and reused19,446
unchanged hashes from the prior map whose full post-job audit was unchanged.
All19,662 remained unchanged in this job; quality restored byte exactly, guard
terminal/preservation complete/lease free. Session91916 is CLOSED.

The ten raw Git blobs include the tested builder, boundary measurement, map,
samples, summaries, audit, quality, guard and cleanup receipts. Current package,
two-machine lobby/match/saved results, recovery, physical operator, service flow
and preload/player-performance acceptance remain open. No PC native job or
authored quality reduction happened in this experiment.

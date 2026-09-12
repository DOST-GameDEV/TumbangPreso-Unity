# Build identity Git pipe deadlock

The corrected internalbuildV3 hung after StampBuildBranch. The owned UnityEditor
was waiting on git diff --name-only HEAD. The helper redirected stdout/stderr,
read stdout synchronously toend,and only then attempted its10secondtimeout.
It never read stderr. The samequery outside Unity completed with891stdoutbytes
and4131stderrbytes of LF/CRLF warnings;the error pipe filled while Unity waited
for stdoutEOF. This is a process-I/O deadlock,not a long shader compile.

Only verified owned Editor/worker/git descendants were stopped;the guarded runner
restored/hashverified25profile files (e25f265452db). Exactownership receipt and
rawquery outputs are in Logs/build-v3-owned-stopped-processes.json and
Logs/build-v3-git-stdout.txt/build-v3-git-stderr.txt. No game player launched.

The helper now drains both pipes asynchronously while waiting,and shares its
10secondbudget between processexit and completed drains. Failure remains Unknown,
never Clean. Focused regression emits64KBstderr before its stdout completion,
plus a failedgitcommand contract. Fresh tests2/2 passed in0.44s,Logs/build-git-process-v1.xml;profile2bd5a8b10985
restored25files. The original failing build remains unqualified.

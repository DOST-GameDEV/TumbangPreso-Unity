# Native hub editing refresh: original failures

Status: original reproduction checked; no candidate or fix pass yet.

On unchanged production UI53d6b7e09bd2a2077446269334d7873bb89052d1, actual
Profile and Friends InputFields enter editing and retain their draft text, then
lose focus after same-owner OnDataChanged refresh. Both causal tests fail with
isFocused=false. Tab change and account replacement correctly retire old editing:
two failures/two controls passed, four total, zero skips.

The PC owns Runtime/UI and is preparing the smallest supported candidate. Its
corrected fixture and metadata were copied raw-byte exactly from the prepared
report into isolated Tests/PlayMode; the mapping is in qualified-source.json.
One prelaunch correction opened the actual collapsed search group and queried
the independent Canvas. No original native rerun or test repair occurred.
An invalid placeholder preparation ref failed before archive/mutation/Unity;
its empty output directory was preserved. A separate local five-case fixture
6db11ee72 was preserved unrun when the PC fixture reservation arrived.

Local Unity6000.5.8f1 job18302 used qa-a's named isolated profile, graphics,
4096MiB budget plus1024MiB reserve, one worker and300-second deadline. All
3440 frozen inputs remained unchanged; quality restored byte exactly,
guard terminal/preservation complete/lease free at04:18:39Z. The ten raw blobs
include original XML, map, receipt, audits, tested UI sources and exact fixture.

The account is dormant and services are null; the actual Canvas/InputField route
is tested without live authentication, HTTP or profile writes. This does not
prove physical keyboard/controller/touch operation, deployed Friends exchange,
packaged gameplay, preload timing or tournament readiness. Candidate acceptance
will use this same fixture and metadata before the production fix is shipped.

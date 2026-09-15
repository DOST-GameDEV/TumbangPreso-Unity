# Native demo-loop baseline

The fresh internal Windows player passed cold startup/Guest, settings, normal and
reduced UI feedback, offline Classic, its actual result/rematch/leave flow, and
offline Hero Strike through its actual result and return. All14 captured views
are1366x768 windowed, verified from real screenshots rather than launch arguments.
Both modes' normal8-round defaults are asserted before the driver deliberately
selects short custom matches to rehearse endings. Normal defaults are unchanged.

Verified artifact: Builds/demo-2026-09-16-v2/TumbangPreso.exe.
Its runtime is based on91d1e2f3 plus the opt-in review-driver changes in this batch.
Runtime.dll timestamp2026-09-15T04:00:25.9277903Z; SHA256:
D161737889CC2048C24BCD8BFDFF02694875C99E2BDD4BC9DD083B6B90C0F3F7.
Build1082MB/58s, guarded profile restoration928761018576. The earlier v1 artifact
is retained separately. Neither overwrites the user's Desktop build.

Native runner result: exit0, reviewPassed=true, sharedInputUnchanged=true,
existingFilesRestored0, fresh named profile demo-native-loop-v2. No account creation
or external social action. Controls use real UI raycasts/callbacks and ReadyGate's
countdown. This proves these native routes, not physical mouse/controller handling,
successful performance of every player verb, a full default-length match or LAN.

The previous run reached Classic rematch but clicked Leave Match in the same frame
the new pause panel was created. Waiting for its first rendered frame before
synthetic interaction resolved that run; no production pause-menu workaround was
added. Failure receipts remain alongside the success. The driver now records hit
targets and a diagnostic image if a click is still blocked.

The old home scale test no longer described the actual design. Current home actions
change ink/underline while targets and labels stay at scale1. The native driver
now tests that behavior. It separately records the existing text-action preference
transition and then asserts stationary reduced-state hover/exit behavior. Final
startup stills wait for entry to finish. Motion media uses measured frame times;
sampled images are explicit, with no generated or interpolated frames.

An independent Editor hand-identity baseline ran in parallel and passed1/1, capturing
carrying/empty FPP and body views for all18people. Receiptc813a1cf6a22. This is not
an artistic acceptance: review continues. Zack's current block hands and yellow
cuffs match his body. Nemu's body still contains explicit stepped finger pieces
in build_nemu_voxel.py, while FPP is a block hand; this conflicts with the owner's
no-finger direction and is the next demonstrated visible correction. The current
monster/familiar form must remain unchanged. Deferred Inday framing stays deferred.

Because one Editor capture ran alongside the player, this is functional/art evidence,
not a clean performance measurement. Remaining hero/throw/recovery play, LAN,
unloaded-performance rehearsal and owner review stay open in the demo checklist.

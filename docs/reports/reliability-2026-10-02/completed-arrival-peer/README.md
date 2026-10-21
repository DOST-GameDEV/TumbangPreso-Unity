# Completed arrival through two actual Windows players

The short custom Hero match and cold completed arrival **passed** on the new
Windows candidate1002e. Initial players used the current native hub host/join
routes and existing automation readiness RPC, then naturally finished one
30-second round with two human-origin seats and two ordinary bots.

The same client process invoked the actual current ResultMainMenu action, returned
to HubHome and rejoined through public StartClientAsync/WaitForConnectionAsync.
Trusted seating loaded the new arena. Its scene handle changed, MatchEnded and
RecordReady counts each increased from one to two, and the actual result board
became visible. Host and client retained matchID `bae2163e17a74bc9a3400a8095a12f98`,
scores20/0/0/150 and winner3. The host stayed ended. The rejoined client was
observed at slot1, not spectating; this does not establish retained-seat recovery.

Session49462 exited0. Both owned players22276/9468 exited; input preferences and
fresh profile seeds were restored, Runtime bytes unchanged, lease released.
No AllBots, forced finish/score, autorematch, SDK or physical-input path was used.
Original JSON receipts and logs are retained, with hashes; private profile files
are excluded. This proves the short actual-peer completed-arrival path and board
visibility, not a full default match, physical click, career settlement or WAN.

Candidate source69e3414913c3bc582b9ce5b028239f65503e62d7, protocol130,
Runtime SHA256 `b0bfbbaec5d5c16041f90036e33b2abad41e8029e28ae4ca3e8ef7536f1ba6c0`.
Build succeeded2432MB/122seconds across12 scenes and completed guard restoration.
Of18845 frozen inputs,18843 were unchanged. The two changed inputs are the
GameBuilder.StampBuildIdentity outputs; both generated sources and the packaged
StreamingAssets copy match the frozen commit, protocol130 and Windows target.
The original strict receipt remains false for those two writes. A separate
artifact receipt records the validated generated-stamp classification. No unknown
source drift was ignored, and no build was repeated to repair this classification.

The release checkout retains importer/generated deltas and identifies its tree as
dirty. This is a frozen committed-source candidate with disclosed generated data,
not a pristine post-import checkout. The older1002b player and failed c/d build
receipts remain intact. Release logs live under `tump-competition-release1002`.

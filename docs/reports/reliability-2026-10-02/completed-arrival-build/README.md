# Completed-arrival observer build attempts

The opt-in observer has not yet passed native build or actual two-player acceptance.

- Candidate1002c at `066bb51eeb4e08ba3182955388c9e2b8715fa509` failed compilation:
  this installed Unity version rejects implicit `SceneHandle` to `int` conversion.
- Candidate1002d at `7b36c1e638007cf52f9e4ca3ed35ddd29bde93b5` used `GetRawData()`
  but still failed because the observer's receipt fields remained `int`.

Root's second change was incomplete. Reflection against the installed Unity
6000.5.8f1 `UnityEngine.CoreModule.dll` confirms `SceneHandle.GetRawData()` returns
`System.UInt64`; the observer now retains both raw scene handles in `ulong` fields.
No truncating cast or weaker same-scene assertion was added.

Both failed jobs terminated, completed preservation and released their leases.
They produced no qualified new player. The earlier candidate1002b is untouched.
Raw errors, plans, job receipts and guarded output are preserved beside this report;
complete Editor logs remain in the release checkout's separate candidate folders.

The isolated observer build retry is exhausted. The next build will integrate the
next independently qualified product batch, with a new output and frozen source.
Until that build and the bounded two-player scenario pass, completed-arrival claims
remain limited to the existing 12/12 native source tests.

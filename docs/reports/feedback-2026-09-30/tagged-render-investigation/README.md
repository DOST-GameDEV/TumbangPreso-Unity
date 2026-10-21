# Tagged rendering report: investigation remains open

New human screenshots show red/cyan fragments across textured foliage during
CAUGHT BY playback. No runtime fix has been made or claimed.

A full Eskinita native capture with actual replay, outline-disabled and all-post
controls did not reproduce the supplied foliage artifact. The scene's current
foliage is stylized NearFade geometry rather than the pictured textured cards.
Source/candidate scene, Garden and shader files match; stale candidate assets
were checked rather than assumed.

Kanto was selected as a plausible map match. Its load hit the unchanged memory
safety stop: the owned Editor reached3.46GB beside a1.17GB asset worker. No new
OOM/kill occurred. One bounded tooling retry attempted documented idle-worker
shutdown, but this Unity build rejected DesiredWorkerCount=0. No second tooling
retry, lower safety threshold bypass, unrelated process kill or asset deletion.
The failed diagnostic fixture was retained privately and removed from shipping
source; no broken test is added to ordinary qualification.

The precise missing report fact is the map shown. Ask in the same Feedback row,
preserve both screenshots and leave Done unchecked. Continue the separate new
player-highlight report while this reproduction path remains unresolved.

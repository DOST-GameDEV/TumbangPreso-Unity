# LAN receive callbacks stay with their initiating socket

OnReceive read the mutable current listener rather than the UdpClient which began
its asynchronous receive. A retired completion arriving after replacement could
operate on and rearm that new socket. The native causal case reproduced the old
callback consuming the replacement's first real datagram at its valid pre-arm
startup boundary.

BeginReceive now carries its initiating socket in AsyncState. The callback verifies
current ownership before handling/rearming, and a retired rearm failure cannot
change the replacement's Listening flag. Queued parsed entries carry socket owner
and the main-thread drain ignores retired entries, including an enqueue racing
StopAll's inbox clear. The current reference is volatile across socket/main threads.
Packet schema, room names, sorting and discovery intervals are unchanged. The
separately qualified startup-failure socket cleanup remains intact.

Three native EditMode cases use real owned loopback UDP sockets and completed
IAsyncResults. The replacement pre-arm boundary is deliberately held by reflection
(the same field/flag state StartListening publishes before BeginReceive). Controls
exercise normal two-packet parsing/rearm and completion after StopAll. Original
55558 passed2controls/1causal; candidate10754 passed3/3 with identical fixture/meta.
Zero tooling/fixture repairs or unrelated reruns. This is controlled lifecycle
acceptance with real I/O, not a probabilistic multithread stress or WiFi/WAN proof.

Unity6000.5.8f1 batch/nographics, serialized CPU1536MB+2048MB reserve,450s limit,
profile lan-receive-owner1002. Preparations terminal0 before launch. Original12526
protected hashes included LanBeacon; candidate explicitly makes that source owned
and verifies all12525 unrelated hashes unchanged. Exact3 candidate main/native
hashes match. Guards terminal/restored/no lease, owned sockets closed by teardown.
Queued entry owner protection is justified by the same retirement race in source;
there is no separate forced mid-decode race test or throughput/FPS claim.

Current Windows1002g/source248836d1e contains37 fixes and predates this later source
unit. No new standalone inclusion or whole competition-readiness claim.

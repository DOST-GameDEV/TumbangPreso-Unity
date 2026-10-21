# Failed LAN discovery startup retires its UDP socket

StartListening allocates a UdpClient before setting up its discovery bind/receive.
If binding port8911 fails, the catch set Listening=false but retained the partially
allocated socket. Retrying replaced that reference and left disposal to collection.
The catch now closes and clears its owned listener before returning the warning.
Discovery packets, timings, normal startup and room UI are unchanged.

Three native EditMode cases use actual local UDP sockets. An exclusive owned
8911 bind provokes the real public StartListening refusal and proves no listener
is retained. Controls release the blocker and retry successfully, and verify normal
StopAll closes its captured owned socket. No network fake/SDK/scene/input framework.
Original77407 passed2 controls/1causal retained-UdpClient failure; candidate16368
passed3/3 with identical fixture/meta. Zero fixture/tool repairs or unrelated reruns.

Unity6000.5.8f1 batch/nographics, CPU1536MB+2048MB reserve,450s ceiling,
profile lan-listener-start1002 and exclusive8911 job claim. Prep3555 terminal0
before launch; each candidate prep terminal0. All12523 protected qualification
hashes unchanged; exact3 owned inputs frozen/verified; guards restored/terminal/
leasefree. Source original/frozen manifests retained in MAIN Logs/lan-listener-start1002.

Accepts real Windows bind-failure cleanup, retry and stop behavior; does not
quantify memory/FPS or qualify WiFi/WAN discovery. Separate receive-callback
ownership suspicion remains unconfirmed and is not fixed by this unit. Current
Windows1002g predates this source; no new standalone inclusion claim.

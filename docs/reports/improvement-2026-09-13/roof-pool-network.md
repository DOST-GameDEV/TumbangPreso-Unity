# Current roof and pool across real processes

Internal Windows build succeeded in60s at30971b59, built2026-09-13 around21:54.
Path: Builds/RoofReview/TumbangPreso.exe. Runtime DLL SHA256:
0ef9a459ca3525f7f1f356ba76ef26c3ce31ea564513734bda540e7bdad22d74.
It includes the forward breaststroke, separate sculling, current pool/rails,
ambient life and resident details. It predates later UI geometry commits.
The Desktop player was not rebuilt or replaced.

Three actual player processes ran each scenario, with individual named profiles.
The harness restores those profiles and leaves the main user profile untouched.

| Scenario | Result | Important observations |
|---|---|---|
| Classic pool, normal link | Pass | All3peers observe sustained swimming/serialized motion, floating slipper, actual pickup and dry exit through steps. Owner minimum root-.926m. |
| Hero Strike pool,150ms one-way delay, observer reconnect | Pass after correcting evaluator's mislabeled clearance threshold | Rejoined observer has91active-swim samples. All peers observe floating stock, pickup and dry exit. Owner minimum eye.0972m, water.04m; settled minimum eye.3786m. |
| Classic roof fall,150ms one-way delay, overlapping4s stun | Pass | Real rail jump/descent,10accepted mash presses,4.20-4.25s recovery; slipper returns9.97-10.04s later and is picked up. Independent stun remains3.93-3.99s. |

Full traces, results and actual-player captures:
Logs/net-swim-classic-v1, Logs/net-swim-hero-delay-rejoin-v1,
Logs/net-roof-new-rails-delay-v1. Owner footage uses the real FPP/recovery camera;
pool observer footage deliberately frames the replicated swimmer with a witness
camera. Do not label that framed camera as the observer's ordinary controls.

The first delayed-pool evaluator incorrectly called an absolute eye height below
.10m "submerged". Raw trace and the minimum-height frame showed the eye at.0972m,
above the.04m water and the camera's.05m near-plane clearance. The corrected
evaluation uses those actual surface/camera dimensions and additionally requires
settled eye clearance of.08m above water. No game code was changed to fit this
check. Original result.json remains unchanged; result-clearance-corrected.json
records the re-evaluation of the same saved trace. This was not a second runtime
run or a camera submersion fix.

The current ring/nameplate UI still needs the UI agent's replacement; in water,
its ground role disc currently sits below the body rather than on the surface.
That finding was delegated to the sole UI agent as part of in-game UI. Broader
graphics/scalability, throwing/equipment/skills and complete UI remain open.

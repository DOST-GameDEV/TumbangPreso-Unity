# Unqualified scenery cache snapshot

The checked source base is 3dac5c7ae0d5cc10aaa4ffc876b32367131c32cc on competition-laptop-gameplay-next1003. This snapshot preserves unfinished descriptor-cache changes and retained measurements. It is not a checked runtime publication or release candidate.

The two production-path changes cache stable bone/renderer descriptors and reuse scratch lists/material property blocks while retaining detached pose snapshots. The new test covers detached old frames, renderer birth, hierarchy changes, visibility and codec round-trip. The latest generic renderer-birth detection and this new test have not executed in Unity.

Native43 passed one controlled actual twelve-model population case before the cache: cold 23.9053 ms, warm 0.9284533333 ms, encoded 280532 bytes, encode 343.5938 ms and decode 201.5134 ms. Its allocation reading of zero was not calibrated and is unavailable evidence.

Native44 passed the same population case on an earlier cache revision: cold 28.5441 ms, warm 0.7633366667 ms, encoded 281091 bytes, encode 284.512 ms and decode 229.5709 ms. The 16 KiB calibration returned zero, so captureBytes is -1, unavailable. These separate measurements do not prove zero allocation or full-game performance. The retained qualified-source files identify exact native input hashes.

Both runs are terminal and restored all 21401 input files, thirteen existing preferences and four profile files. No profile contents or private preferences are included. Native45 was prepared for three cases but never launched. Its scripts retain laptop-specific paths and require inspection and adaptation before use on another host.

The preserved-unqualified directory retains old practice-hop and solo-seat leads and scenery inventory fixtures outside normal test discovery. Their old source references are historical; they are not demonstrated current defects or candidate production fixes. Original untracked files remain on the laptop.

Current packaged visual parity, all scenery/hero/map coverage, allocation and GPU/frame pacing, bounded byte residence, natural maximum population, long sessions, physical devices, operator acceptance and two-machine LAN results remain open. Optional scenery samples do not close full one-to-one visuals.

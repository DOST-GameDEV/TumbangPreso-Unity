# Avoid unused storage in empty replay trail samples

The 20 Hz replay sampler created a capacity-12 trail list even when every shoe
was held or on the ground. A calibrated current-thread Unity GC.Alloc recorder
measured 1,000 allocation events for 500 empty captures with four nonflying shoes.
The deliberate 4,096-byte calibration was detected before measuring capture.

Result storage is now created only when a valid trail is found. Empty samples
return a shared Array.Empty result. No pooling, registry, wire format, trail
sampling, art or gameplay change. This complements existing slipper-inventory reuse.

Same native empty case changes from failure to pass: 500 captures, no trails,
zero allocation events. Existing actual flying-trail data/lifecycle control also
passes; final 2/2. Two frozen inputs unchanged; no fixture repairs; guarded profile
and shared input preferences restored. No FPS or full-match performance claim.
Current internal Windows player from7b48f76d7 predates this optimization and the
later preview-audio guard. Final log remains Logs/replay-empty-capture1002/final.log
in the isolated native project. The older Mono allocated-byte counter is not
used as proof; this result uses the explicitly calibrated Unity recorder.

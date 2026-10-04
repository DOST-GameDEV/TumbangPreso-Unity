# Status recovery investigation: no reproduction yet

Two new native PlayMode cases pass with Sean and Nemu bots' own abilities enabled.
A real bot enters an actual throw windup, then Cheska's actual Absolute Zero
ability applies Frozen plus the subsequent Chilled duration. Without clearing
status or resetting AI, each bot resumes physical movement or a real throw in
the same round after natural Frozen expiry. Existing thaw Chilled is asserted.
Sean passes5.702s and Nemu5.860s; combined2/2 in11.673s.

Small physical floor, can and owned slipper; actual AI/Carrier/motor/hero-system
components, no authored models. Headless run, not visual or full-map/cinematic
acceptance. This does not reproduce or close the owner's report of AI stopping
for the remainder of a round, and no bot production behavior was changed.

Final run exit0/no guard, tree RSS3,355,258,880/container7,244,754,944 bytes.
Settings/profile restored and frozen input hashes match. Initial compilation
finished and reloaded assemblies, then the import stage hit the headroom guard;
its failed receipt is retained. Runtime reused compiled assemblies after idle
cleanup instead of repeating compilation.

Also retain two previously uncommitted narrow regression cases: raw Frozen
recovery with an actual throw passed1/1 at06:45UTC in5.083s; raw Tagged passed1/1
at07:01UTC in8.873s. Their older source overlays/receipts are preserved separately.
They were not rerun or represented as current whole-branch qualification.

Open paths include full cinematic routing, live peers, other status combinations
and tutorial/practice-specific control ownership. The owner has been asked which
mode and status produced the observed stall; no answer is assumed. Continue
other authorized feature/presentation work without marking the report fixed.

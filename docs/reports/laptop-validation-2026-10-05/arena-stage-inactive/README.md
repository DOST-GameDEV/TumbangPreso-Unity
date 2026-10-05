# Inactive Arena stage preserves active movement ownership

ArenaStage.Awake and OnEnable previously claimed Instance, AIController.EdgeSense
and MatchRpc.MoveFloorY even when their scene was not active. An additive stage
can therefore replace the active stage owner and movement floor; parking that
stage then resets the globals while the original active stage is still enabled.
The fix confines those claims to the active scene. Awake still deepens its own
geometry, applies its own round layout and draws it, preserving preview content.
No recovery numbers, authority messages, map art or kits change.

Two added native cases create a real loaded inactive scene, move an inactive
stage object there and enable it. With an active owner, the original replaces
that owner/floor. Without one, it wrongly claims ordinary-map globals. Original
Unity41492/parent71169: two causal failures, seven controls pass. Candidate
Unity11376/parent37136: all nine pass, no skips. Controls preserve current/retired
teardown and the separately qualified inactive recovery hook behavior.
Both parents terminal,21126 inputs and279 generated deltas restored, plus
QualitySettings and13 existing editor preferences. Source basecdbb90ceb;
laptop gamergmae Windows11/Unity6000.5.8f1, isolatedqa-a. Exact source/fixture
hashes and XML/receipts retained; metadata/GUID unchanged.

Real additive callers exist in MapPreviewSurface and SplashScreen. Preview/prewarm
park loaded roots after completion, so Awake/OnEnable can execute first. This
fixture proves component activation and global state, not an entire preview
operator flow, an observed movement artifact, network/QA causality or full map
acceptance. It does not qualify later activation after changing the active scene
without enabling the component again. Rendering behavior is retained in source;
current packaged preview and gameplay must still be exercised.

The filtered run retains the declared existing Home fixture and original HubHome
compile input from its baseline; published e447 Home source was independently
qualified and pulled on main while this physically isolated worker remained
frozen. No Home or complete-match acceptance is attributed to these nine cases.

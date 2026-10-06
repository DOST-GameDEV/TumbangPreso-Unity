# Keep the retired first-person rig from hiding a spectated body

The public watch handoff calls CameraRig.SetActive(false), which restores its hidden body immediately. A later EndEmoteView callback directly calls ApplyFppSelfHide; that method checked character/mode/arrival but not active ownership. It could put the former player's body back into ShadowsOnly after the spectator had taken over.

Original Unity24360 reproduces two failures: both normal On and authored shadowless Off renderers become ShadowsOnly after Follow/BeginEmoteView/public RebindLocalSeat watch/EndEmoteView. Active FPP emote return, immediate handoff and inactive Follow controls pass. Candidate20244 passes all five after the central helper restores any prior hide then refuses to apply a new hide for an inactive rig.

The body retains its actual authored shadow mode; active FPP behavior remains. No model, material, animation, skill, watch seating or Director changes. These are actual native public lifecycle calls with a simple visible mesh. They prove the release-callback defect, not the exact cause of every earlier missing-body frame. A natural no-witness-override render with actual installed model bounds/visibility/shadow flags remains required; the laptop owns that next selected-wide event observation.

Both runs restore21166 frozen inputs and216 importer/settings deltas each, input/editor preferences, quality settings and seed, plus fixture provider/cursor/launch state. Original failures and controls remain. Source63d43853a plus the scoped rig/test change; later laptop geometry receipts are integrated separately after terminal restoration.

# Hero view preparation across frames

First Hero opening previously constructed the whole view in one callback.
Development traces measure substantial method compilation, canvas and preview
costs there. The method-entry experiment was rejected after its actual player
frame worsened. This change instead yields between actual view-construction
stages without caching stale account data or changing authored presentation.

Home remains visible and focused while the new view is prepared under an
inactive parent. Its preview camera stays disabled. The completed view is then
shown, Home hidden and focus placed on the current Hero primary action. Back or
another route cancels preparation and destroys its owned preview. Repeated Hero
presses do not start duplicate views. Ordinary pages retain their existing path.

Native62780 passes three current graphical controls covering completion/focus,
Home visibility, settings preservation, Back/cancel cleanup, alternate routing,
repeated press and Shop configuration. All21,423 frozen inputs/preferences were
restored. The only subsequent source difference corrects an opt-in diagnostic
label from CPU duration to coroutine wall duration including yields.

Actual player frame benefit, first canvas/preview render costs, input responsiveness
and ordinary-release acceptance are still pending. Do not infer performance
approval from these behavior controls. No new build/Desktop folder was created
and the qualified Desktop release remains unchanged.

The first actual Development candidate completed14windows but still reached
118.399ms CPU in its worst Hero frame. The final PrepareScreen callback consumed
67.036ms and303 Mono.JIT samples totaled62.188ms; canvas pre-render17.033ms and
ColourGrade11.577ms followed. This is still a smoothness failure. All21,423
player/export/build inputs and preferences restored; exact receipts are retained.

Candidate2 separates description, model binding, kit preparation and each ability
tile into additional yielded stages before revealing the view. Native24100
passes the same three behavior controls on those exact bytes with all21,423
inputs/preferences restored. Its actual player comparison remains pending.

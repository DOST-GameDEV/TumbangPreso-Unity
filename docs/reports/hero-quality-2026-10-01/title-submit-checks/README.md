# Title Submit stays on its opening screen

The current112 Linux player exposed Return entering Home already matchmaking.
Its temporary lobby was cancelled/deleted. The focused offline baseline proves
Home loads while the opening Enter remains held; the prior Period-only route
check did not exercise this condition.

The title now accepts its opening action once, waits for the current UI Submit
to release, then crosses a frame boundary before opening Home. This prevents
Home's newly enabled UI map from treating that held input as another PLAY.
Keyboard/pad bindings remain owned by MenuNav/the existing module. Pointer/touch
clicks and non-Submit keys share the same callback without a held-key delay.
No title artwork, gameplay, protocol, saved binding or generic input rewrite.

Final three native graphics cases pass in6.5848261seconds: held keyboard Enter,
held pad South, each followed by a fresh real-module Submit that starts offline
queueing; and ordinary Period entry plus hamburger/Back picture preservation.
The first final fixture yielded past its injected input frame, so fresh Submit
was not processed. One bounded fixture repair processes the actual UI module
in that injected frame and asserts its performed action. Runtime unchanged for
that retry; retain failed XML rather than treating it as a product regression.
All frozen inputs unchanged after validation; no new OOM. Source base392d9e6b
with the four explicit title-input/test files, current protocol114 runtime.

These are synthetic native keyboard/pad checks, not physical hardware, touch
interaction, actual-peer or rebuilt-player certification. The existing Linux112
player does not contain this new fix. Earlier player screenshots are not final
fix screenshots. Final behavior requires a later coherent player refresh.

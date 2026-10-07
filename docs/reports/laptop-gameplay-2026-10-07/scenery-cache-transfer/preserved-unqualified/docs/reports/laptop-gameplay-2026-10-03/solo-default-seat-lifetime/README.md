# Solo shortcut return-seat lifetime

Status: fixture preflight pending. Runtime unchanged; no native result yet.

The solo F6 shortcut promises to return to the original human character. Its
current dispatcher discovers DefaultSlot again on each F6 press by finding a
body with no AI or a disabled AI. Claiming a bot with F1-F4 disables that bot's
AI, so it becomes another candidate for this discovery. The ordinary installer
creates seats in ascending order, and the default solo human is P2; claiming
P1 therefore makes the earlier bot eligible before the original human.

The six proposed cases send queued Keyboard state events through InputSystem
and invoke the shipping switcher's Update dispatcher. The causal case first
proves original P2 discovery and an accepted F1 handover, then requires F6 to
return to P2. Five controls cover a later-created bot's return, repeated F6
before a handover, ordinary F2 handover, a networked refusal, and the existing
fallback with no actors. Actor creation follows the installer's ascending slot
sequence. Neither discovery order nor the switcher's private identity is seeded,
and the tests do not call Assign directly.

The actors and provider are supplied for isolated dispatcher and reader/AI/input
custody acceptance. This is queued InputSystem keyboard state and manually invoked
shipping Update, not a physical keyboard, full scene installation, menu operation,
network delivery or map-play result. The initially idle practice-bot discovery
case remains outside this unit. Public static DefaultSlot currently retains its
dynamic meaning; if original proof justifies a patch, only the shortcut's return
identity should be retained at its existing lazy initial resolution.

Original logical source is ASTRA d34cffb237f9f39182b576137fc543106cb377fa.
DebugPlayerSwitcher working SHA256 70aa345d33e74d6117b7dac7d212cfaa574f5f6df064af22ad1a597c0fad5a35.
Fixture SHA256 425721b966d4c7a47ff9b446752437ac7a230f1d8a8f83fa3b8ad48c63936cd4;
meta f43d98ad0291ecf242ada70bfbadea2bb6dbce31f7e19a1a3af0522d0f266fd1.

The separate HopRestore API fixture is held unqualified and excluded from this
unit's source overlays and test filters; no hop runtime change or native result
is claimed here.

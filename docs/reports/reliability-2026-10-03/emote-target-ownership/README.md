# Follow the actual input owner for wheel choices

The wheel calls MatchInstaller.Driven to select the emote recipient. Its previous
AI-absence scan could choose a remote human or the original solo body after its
reader retired. Several bodies can have no active AI, so this did not identify
the body receiving local input.

Candidateb5fabf6a6 selects an unparked actor with an active PlayerInputReader,
restricts network selection to LocalSlot and checks every attached reader so an
older disabled predecessor cannot hide its enabled replacement. Without an
eligible input owner it returns null. The explicit tutorial student fallback
remains. Input backend, debug switching, RPC layout and hero behavior are unchanged.

Original with the frozen fixture isbfe2c2fab. Eight native EditMode cases cover
stable four-actor reader matrices in solo/network, stale remote readers,
no-reader and parked-owner cases, replacement reader, temporary active AI with
human hero input and tutorial fallback. No discovery order is imposed. An empty
isolated scene is required so unrelated actors cannot determine the result.

Original8 reproduced five causal failures and passed three controls. Candidate8
passed8/8. Root inspected raw XML, terminal/restored/free receipts and full3390
input maps published at99ae1469f: only MatchInstaller.cs changed. The fixture and
meta remained identical, with native line-ending conversions checked against
the immutable Git refs. There was no native repair or repeated run. Runtime and
test assemblies compile too. The fixture namespace was corrected during source
preflight before any native run.

[Raw evidence](../../laptop-validation-2026-10-03/emote-target-ownership/README.md).
This checks the exact selector used by the wheel callback, not whole device,
wheel UI, RPC or possession playback. Actual multiplayer emotes remain part of
the matching-player gate.

The checked selector is integrated with current ASTRAReworks and the independently
checked cast-preparation guard. The laptop performed native validation while
Claude owned PC Unity. No PC Unity run was started.

# Background gameplay focus lifetime

The current Windows game continues ticking in the background. Losing focus
previously cleared the reader once, then subsequent reader frames restored held
gamepad movement. Returning to the game also imported a jump button pressed while
away. The reader now keeps intent neutral while unfocused and captures held
buttons on return, requiring release before a fresh gameplay press. It leaves
pause ownership, accepted possession, network ticking and device bindings intact.

Only PlayerInputReader.cs changes production behavior. Original fixture source
06b618bd073387b267af1b0e9d30bf20dc91e612 and candidate source
295d53db561c8d8f0942787052569a03a6a6b6df use the identical new fixture and metadata.

## Native evidence

- Original: four cases, two causal failures and two controls passed. Held
  background movement became (0,1); focus return accepted the held jump.
- Candidate:28/28, zero failures or skips. The same four cases pass alongside
  existing focus3, touch movement5, device loss9 and reader seat ownership7.
- Both local Unity6000.5.8f1 runs used the existing isolated qa-a profile,
  physical independent Library, graphics,4096MiB budget plus1024MiB reserve,
  one worker and a300-second deadline. No parallel Unity job or tooling repair.
- All3438 frozen inputs stayed unchanged in each run. Quality settings were
  restored byte exactly; both guards are terminal with preservation complete
  and leases free. Session6433 original and18739 candidate are closed.
- The sixteen raw blobs include XML, maps, receipts, audits, quality snapshots,
  tested original/candidate reader and the unchanged fixture/metadata. manifest.json
  records their SHA256 values and .gitattributes preserves their raw bytes.

## Scope and remaining acceptance

The supplied Input System device runs with IgnoreFocus to model background-capable
input; actual reader Update and focus callbacks are exercised. This proves the
reader boundary under controlled native input, not a physical Alt-Tab session or
all hardware backends. The new background-button control samples the same frame
as focus loss and is not evidence of later background press handling on the
original. The two failures above independently establish the corrected lifetime.

Current packaged Alt-Tab, practice operator acceptance, normal same-artifact
two-machine admission/match/saved results and recovery/replay remain open. No
current144 artifact was built or peer pair launched by this unit. Existing private
artwork, finalized heroes, shipping importer work and PC native slot were preserved.

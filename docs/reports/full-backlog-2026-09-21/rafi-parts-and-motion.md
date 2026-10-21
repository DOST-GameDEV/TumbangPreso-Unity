# Rafi: hero-only part refinement and actual playback

The owner rejected the previous expression/hair and retexture-like identity, then
asked for careful part-by-part work on bracelets, accessories and hair. Rafi is
only in the Hero Strike roster; AllPeople is the asset union. The actual picker
uses Roster.GetPeople(selectedMode). Classic actors in earlier deck diagnostics
were never his style references.

The original voxel builder and six existing hero models remain unchanged. The
dedicated Rafi copy retains the native skull, seven-bone rig, family remap, normal
smoothing and packing. Current raw model:682140bytes,6804body/1726head vertices,
33base clips and.775m source height. No gills.

## Shape work and native inspection

- Connected chamfered volumes replace stacked hair pieces; swept roots/tips,
  nape/temples, compact tied tail and folded headwrap are inspected in four angles.
- His own flat graphic eyes and weighted brows replace the donor smile-eyes.
  The short closed mouth stays readable beside the existing hero faces.
- The bracelet is a continuous fitted cord at the wrist, with a small tied
  closure and sea-glass bead. Initial ellipse clipping and placement across the
  palm were corrected from actual side/quarter images. The right wrist stays clear.
- The hip coil is continuous faceted rope with visible clearance and a hanger,
  replacing the gear-like ring of cubes. The orange float clip has a fitted saddle.
- V collar, shoulder lining, sash and sailcloth use shaped, fitted overlaps.
- Palms have more depth/taper; sandals have visible supported toe/heel/side straps.
  These are the same source hand/sleeve/cord vertices used in first person.

RafiNativeModelReview.Parts renders actual source triangles by bone in the four
canonical views, with native ToonSkin/palette and fixed2.38scale. No AI image is
presented as implementation proof. Baseline, v2/v3 fit faults and final v4 sheets
are versioned in validation Logs/rafi-parts-*. The final full lineup contains only
Sean, Cheska, Dante, Zack, Nemu, Phaister and Rafi. Human taste approval is not claimed.

## Actual integration repairs

The earlier generic FPP defect had two causes: missing Rafi ID normalization and
failure to use his extracted arms. Both are fixed. Actual native checks assert
the displayed left/right mesh references, not only the selected name.

A further real gap was found before this motion pass: the C# Rafi body curves
were editor-only and lacked shipping assets AND action-chain registration.
Previous input/field/FPP-geometry passes did not prove those body casts played.
RafiMotionAuthor now bakes exactly his three named nonlegacy clips, with the
retained grounding solver anchored to the rest floor. RosterBookBuilder references
them, giving36runtime clips while preserving the33base GLB clips. Existing heroes'
clips/models are untouched. The shared introduction already uses supported legacy
render-copy sampling and retains its behavior.

v52 actual Windows evidence:

- 8owner and8observer role/choice cases pass, including actual named body playback,
  source-arm identity, and legal charge/release with empty hands afterward.
- Shared introduction, exact live Breakwater clip, return and retained replay pass.
- Three real peers with150ms one-way simulated delay all observe the same3effects
  AND play all3named body clips:448/441/439rows, one ultimate, exact charges,
 6repeated snapshots,0remaining fields; largest expiry offset.222s.

Exact receipts are in expansion-evidence. Staged inputs and sampled/recorded native
views are not human freeform balance, WAN, hardware or listening approval.

The mastery server SOURCE now includes Rafi; its ID set matches all7client heroes.
No live UGS deployment or service call has been made. Canonical hero turnarounds,
FPP inventory and the hand-action coverage list now include the seventh hero.

The overall goal remains open for full TODO reconciliation and coherent final
qualification. This report closes specific implementation gaps, not the assignment.

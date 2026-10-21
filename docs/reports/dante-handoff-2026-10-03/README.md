# Dante introduction follow-through

## Change under validation

The introduction already plants Dante's strike. The old live fissure clip then
raised both hands overhead and struck again. The replacement continues from the
introduction's exact final local rotations, settles into pressure at the same
0.4-second release and recovers to ready. The owner-view hands stay low rather
than lifting the slipper across the lens a second time.

Only the shipped `hero-dante-fissure` GLB action and `fissure-slam` viewmodel path
change. The editor-only procedural fallback is not the shipping roster asset and
is not used as qualification. The original 37 other animations, all geometry
JSON and original binary-buffer prefix remain identical. There are still 38
clips. All 61 authored samples are grounded against actual skinned vertices.
The authoring tool converts the introduction's raw Unity rotations to glTF;
a native sampled-pose assertion checks the imported result against Unity's own
Quaternion.Euler rather than trusting the conversion formula alone.

## Evidence status

- Original wall-clock observation: passed the shared route, but missed early
  motion. It is not sufficient cadence evidence.
- One bounded witness-clock correction: native baseline passes in 73.623s,
  84 actual owner/observer images plus introduction. Inspected frames show the
  duplicate live overhead raise and the original 0.4-second release.
- Candidate: the actual shared-route case passes in 72.843s. Imported first
  rotations match all six exact introduction bone rotations within 0.1 degrees.
  All five live bands remain; the first is observed after the 0.4-second warning.
  Peak tree RSS 5,279,076,352 bytes, no guard stop, exit 0 and profile restored.
  Frozen candidate hashes remain unchanged after the run; roster bindings need
  no rewrite. No tooling correction was needed for this candidate.
- Inspected introduction, 0.20s, 0.47s and 0.87s paired views: the live body stays
  planted rather than re-raising, then stands; the slipper stays below the old
  overhead obstruction. The can, court and first spreading fault remain visible.
  This is a narrow presentation result, not final approval of the whole cinematic.

[Evidence](evidence/) includes the exact native XML, frozen inputs, resource
receipt, band trace, byte-preservation check and before/after body/owner frames.
The silent 1280x720 comparison repeats normal speed and labels its half-speed
segment. Earlier wall-clock and fixed-clock baseline receipts are retained in
cloud evidence, not overwritten by this candidate.

The native review uses real accepted input and the shared introduction, an actual
owner CameraRig, and a separate observer. It does not change authority, windup,
phase timing, the five bands, scoring, network messages, sound or other heroes.
Fixed 60Hz simulation is for paired review frames, not measured player FPS.
No packaged-player, remote-peer, listening or human-taste acceptance is implied.

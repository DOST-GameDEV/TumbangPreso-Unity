# New-three court footage and capture provenance

Native authoring and final current-source visual/decoder gates pass. Full
tournament and shipping-package acceptance remain open.

Owner scope: Padre Faura, Manila (IlalimNgTulay), LagoonCove and BGC, Taguig
(Kanto). Eskinita/BayanPlaza/SaBubong are being remade and are not redesigned or
re-recorded. Arena also stays unchanged.

Observed original framing: Bridge foreground parking/near-side spawn average
placed its playable court too far away; Cove's near rock wall covered much of
its court. Actual settled native frame experiments justified Bridge selection
pivotXZ0/distance16/height6.4 and Cove yaw-35/distance32/height22. These are camera
changes only. Lobby shots, old maps, Arena, authored geometry/materials/light,
58-degree lens, original sway and finalized heroes are preserved.

Actual new3 footage uses1920x1080/30fps/780frames/26s, HDR/MSAA4 source,
CRF14/slower H.264 without audio. ffprobe facts retained. First posters were
visually inspected; footage is the actual native camera, not generated scenery.
Three video/poster pairs are replaced with their existing Unity GUIDs. Other
four movie/poster pairs have byte-identical SHA-256 values.

Current canonical source descriptions differ from actual new3 capture source
only in MatchInstaller's gameplay/replay code. Restoring its actual f753 hash
reconstructs ALL THREE original captured fingerprints exactly. MapPreviewSurface
holds PreviewOnly and the isolated recorder begins from a fresh empty PlayMode,
so that code does not run during preview capture. The authoring fingerprint now
excludes the suppressed installer and uses effective camera parameters instead
of unrelated SceneFlow display names/descriptions.

For the unchanged four recordings, current dependency delta intersects ONLY the
installer. Restoring d66 installer/SceneFlow hashes and the original camera
contract reconstructs ALL FOUR original receipts exactly. AimAt after removing
the two new-map-only branches equals the original apart from one blank line;
all five other producer methods are unchanged. This is a demonstrated producer
and dependency equivalence migration; receipts are not blindly stamped.

Canonical descriptions, exact reconstructed/captured hashes, camera equivalence
and preserved movie/poster SHA values are attached. Native authoring validation
checks all7 current source/media pairs without rewriting receipts. Decoder
checks must separately prove the three newly encoded movies actually prepare,
draw1080/30/780, seek/loop, release and do not instantiate a live arena.

This does not prove seamless ambient traffic loops, portable codec availability,
visible GPU performance, current shipping package, physical input or full
competition readiness. Existing Kanto ambient-loop discontinuity remains a
separate limit. All source/profile/preference restoration must be terminal
before claiming a job complete. Private profiles are never included in Git.

## Playback identity regression caught by visual inspection

The first native final gate returned3PASS for metadata/loops/noarena, but all3
readback screenshots were byte-identical Eskinita. Those weak green tests and
wrong snapshots are preserved. An independent encoded first frame proves the
Padre Faura asset itself is correct. Strengthened original42140 then fails the
actual clip-name assertion: requestedIlalimNgTulay-loop, gotEskinita-loop.

MapPreviewSurface.Start checked only live _showing/_wantedMap, while recorded
Show stores its selected map in MapPreviewVideo.Map. Start therefore overwrote
an explicit recorded choice with the global default. The guard now usesShowing,
which includes the recorded choice. This preserves the live guard and prevents
fresh map-vote/results/selection surfaces from changing their selected picture.
The candidate also compares actual post-seek decoded pixels with the matching
poster instead of treating clock/loop callbacks as visual identity proof.

Final64332: all3casesPASS, actualcorrectclip+pixels+1080/30/780+seek/loop/noarena/release. MeanRGB5.0783/5.8140/5.7968 outof255 againstmatchingposters; correctdecodedimagesinspected. All21345sourceinputs/13prefs4originalprofilefilesrestored. ExacttestedpacketrawSHAverifiedbeforetextnormalization; committedcode/mediaequalsraworLF-normalizedtestedbytes. All8oldmovie/posterGitblobSHAunchanged. No shippingGPU/physical/peer/portable/seamless-loop claim.

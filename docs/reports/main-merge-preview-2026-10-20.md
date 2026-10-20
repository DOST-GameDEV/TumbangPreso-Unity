# Main integration and recorded map preview repair

Owner request, October 20: merge every GitHub branch into main and correct the
old map shown by Tondo's preview. Work used an isolated integration checkout.
Existing checkouts and their uncommitted settings, artwork and intro edits stay intact.

QoLUpdates adcd0f957 already contains ASTRAReworks 1735f1d4b. The backup branch
1fcb64d12 deliberately has the exact tree of old main 85504a52c; its commit
message identifies its other parents as recoverable snapshots, not integrated
release content. Preserve that archive's history with an ours merge, retaining
QoLUpdates' newer playable tree. This includes every current remote branch head
without replacing current source with the archive's old main tree.

The incoming Eskinita video depicted the old flat street while its poster and
playable scene depicted the rebuilt stepped alley. The same integration reverted
five recorded loops without reverting their posters or source receipts. Restore
Eskinita, IlalimNgTulay, LagoonCove, Kanto and Arena loops from the recorded
October 18 publication. Existing media GUIDs, posters and playable maps are unchanged.

All six selectable video/poster pairs match their recorded SHA-256 receipts.
ffprobe confirms 1920x1080, 30 fps, 780 frames and 26 seconds for each clip.
Decoded first frames match their posters within the existing native fixture's
20-level mean RGB tolerance; Tondo measures 2.448. Its restored frame was
visually inspected and shows the stepped alley.

The first native all-map run passed each restored clip, then failed on Payo.
Its incoming MP4 actually contains HEVC; Windows Media Foundation rejected it
with 0xc00d5212 and Unity could not decode it. Convert that same recorded footage
to H.264 without changing dimensions, frame rate or frame count. Preserve the
poster and source fingerprint; update only the video's hash in its receipt.

The H.264 conversion preserves all 780 frames at 1920x1080 and 30 fps. Its
100,126,525-byte blob stays below GitHub's 100 MiB limit. Full-clip SSIM against
the incoming HEVC recording is 0.977105. No new map capture or framing change.

Core tests pass 796/796 with no skips. Native revalidation passes 1/1, covering
all six selectable maps in NewThreeRecordedPreviewTests:
AllSelectableCourtFootageDecodesAtOriginalQualityWithoutLoadingAnArena. This
checks actual decoded pixels against the matching poster, clip identity,
resolution, frame count, frame rate, end-to-start loops and decoder disposal.
No arena scene loads solely for a recorded preview. Native Tondo's mean RGB
difference is 6.563; Payo's is 6.648, both below the existing limit of 20.

Validation used Unity 6000.5.8f1 and D3D11 with an isolated company/product and
named profile. Test-only project settings were restored to their original bytes;
the runner verified preservation of the named profile and input preferences.
Original failed and final passing log/XML evidence is retained locally under
work/verification/merge-main-1020 after temporary-checkout cleanup. Existing
Windows timestamp/colour warnings remain; playback assertions pass.

No player build, target-device playback or broader gameplay qualification is
claimed. Source dependency fingerprints were not regenerated or restamped.

# Main integration and current map previews

All six selectable maps now have newly recorded videos, first-frame posters and
matching fallback thumbnails from the current source: Tondo, Ermita, Sitangkai,
BGC, The Philippine Arena and Payo. Existing map IDs, framing, map assets and
media GUIDs are preserved. Each receipt records its actual capture fingerprint
and the installed video/poster hashes.

## Integration

Owner October 20 requested every GitHub branch in main. QoLUpdates adcd0f957
already contains ASTRAReworks 1735f1d4b. Backup 1fcb64d12 deliberately has the
exact tree of old main 85504a52c and keeps recoverable snapshots as its parents.
An ours merge preserves that archive's history while retaining the newer
QoLUpdates playable tree. All current branch heads are included in main.
Initial integration was published as 2fa53e3ee.

## Preview repair

Incoming Tondo footage showed the old flat street while its poster and playable
scene showed the rebuilt stepped alley. Five loops had reverted during earlier
integration. Payo's incoming HEVC recording also failed native Windows decoding
with 0xc00d5212. Initial restoration and H.264 conversion passed playback, then
the owner's all-preview request prompted a stronger source-freshness audit.
That audit rejected all six receipts after later rendering/quality changes.

Recorded all six again with the existing MapPreviewSurface camera and settled
lighting. Every capture retained the same source fingerprint before and after
recording. All six installed videos are H.264 at 1920x1080, 30 fps, 780 frames and
26 seconds. Payo's delivery encode uses CRF 14/slower with 30M maxrate and 60M buffer;
its 100,125,475-byte file is below 100 MiB. Resolution, frame count and framing stay
intact. Each fallback card is byte-identical to its new matching poster.

## Validation

- Current MapPreviewFreshness.Validate succeeds for all six map/source/media receipts.
- Full ffmpeg decode of every 780-frame installed clip succeeds without errors.
- Native NewThreeRecordedPreviewTests.AllSelectableCourtFootageDecodesAtOriginalQualityWithoutLoadingAnArena passes 1/1, exercising all six maps: decoded-pixel/poster matching, clip identity, resolution/fps/frame count, end-to-start loops and decoder disposal. No live arena scene is loaded solely for a recording.
- All six newly captured posters were visually inspected.
- Core 796/796 passed with no skips; unchanged code reuses that integration evidence.

Unity 6000.5.8f1/D3D11 used an isolated company/product and named profile. The
runner verified profile/input-preference preservation. Test-only settings were
restored to their exact original bytes. Original failure logs and current
capture/test/validation evidence are retained locally in
work/verification/merge-main-1020. Windows timestamp/colour warnings remain;
native playback assertions pass. No player build, mobile/Linux device playback
or broader gameplay qualification is claimed.

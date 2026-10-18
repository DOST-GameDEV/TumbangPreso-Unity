# Prepare the poster while the map view is inactive

MapPreviewVideo.Show previously allocated a native VideoPlayer/RenderTexture and called Prepare even when its GameObject or view component was disabled. That spent decoder resources before the screen was shown and could emit the disabled-player preparation error. Native original48372 fails both allocation cases on the actual components.

Prepare now requires a visible, active and enabled view. Poster and cached clip lookup still work while hidden. The existing OnEnable/deferred Update path adopts the clip and prepares once the view becomes active. A late native preparation callback also checks that both player and view are enabled; SetVisible defers active work for a disabled view.

Candidate58484 passes the same two native cases: inactive GameObject and disabled component both retain a poster with no VideoPlayer, then produce an actual decoded frame after activation. Original and candidate restore all21311 frozen inputs,13 existing preferences and four original isolated-profile files after295 preserved import deltas. Exact original failures and source manifests remain alongside the passing result.

This is view lifecycle/resource evidence, not a full performance or packaged-player gate. Unchanged playback/hidden-timeout/reduced-motion/map-switching and all-map decoding evidence is reused. Full-resolution current packaged browsing, platform codecs, hardware and broader competition acceptance remain separate. No startup, networking, map geometry, hero design or artwork changed.

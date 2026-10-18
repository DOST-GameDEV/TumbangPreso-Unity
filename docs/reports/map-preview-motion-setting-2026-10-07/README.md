# Visible map movies honor reduced motion immediately

MapPreviewVideo previously read ReducedUiMotion only when Show/SetVisible was called. Changing accessibility settings while the movie remained visible could therefore leave the background moving. HubMapVote and the results picker own their recorded player independently of the hub's live-court visibility refresh. The earlier packaged lifecycle check explicitly refreshed visibility and did not establish this direct-setting path.

Original native58800 fails one actual case: after a decoded Arena movie is playing, setting ReducedUiMotion=true without changing visibility leaves isPlaying=true. The correction remembers the last motion value and calls the existing visibility handler only when that value changes. It pauses to the matching poster and resumes the prepared movie when motion returns, without duplicate decoding or changing map/player ownership.

Candidate57932 passes the same native case: actual play, direct setting change, paused native player plus correct poster, direct restoration of motion and resumed native movie/render texture. Original and candidate restore all21315 frozen inputs,13 existing preferences and four original named-profile files after295 preserved import deltas. Exact original failure/source receipts remain alongside the result.

This is accessibility state/lifecycle evidence, not physical hardware, visible GPU performance or a new packaged-source gate. Unchanged map switching/hidden timeout/inactive preparation/all-map decoding evidence remains qualified only within its recorded scope. No networking, startup, AI, authored hero or artwork/media changes occur.

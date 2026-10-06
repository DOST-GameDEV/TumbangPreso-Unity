# Corrected startup and first Home animation

Owner acceptance: Unity logo, BH Studios, login, supplied main/title composition
as a loading view for at least5 seconds and until required lobby preparation
finishes, then lobby/Home automatically. The old illustrated GETTING THE PLAYERS
READY surface must not appear before login. Desktop source4ba did not satisfy
this order; its build success was not startup acceptance.

Splash startup now plays the studio intro and loads MainMenu directly without
constructing the old loading surface or warming every arena before login.
The supplied title stays hidden during login. MainMenu preserves asynchronous
map-preview metadata/poster and Home clip/decoder preparation, reserves at least
5 seconds after admission and activates the lobby when readiness is complete.
Full gameplay/arena prewarming is no longer part of the pre-login path.

Native27512 reached the corrected ordering but reproduced the first-Home bug:
the warmed Phaister decoder reported playing while its frame remained7 after
1.2s. On adoption the same VideoPlayer/target is stopped and reprepared, preserving
the clip/poster and showing the poster until a fresh frame is decoded. Candidate
16216 passes the exact cold-flow case and advances frames126 to163. The subsequent
10748 class run passes all9 playback controls, including same-player/target reuse,
failure fallback, reduced motion, opaque-court suspension, hero selection and
switching screens. All21249 frozen inputs and shared preferences restore for
each run. XML/compact receipts and actual1080/720 frames are here.

The opt-in StartupOrderPlayerProbe verifies order, minimum title visibility and
first decoded-frame advancement in a built player. It is never installed in
ordinary play. Packaged qualification and replacement remain pending until the
probe is run against the exact new executable; this report does not approve the
old4ba Desktop package or claim physical/peer acceptance.

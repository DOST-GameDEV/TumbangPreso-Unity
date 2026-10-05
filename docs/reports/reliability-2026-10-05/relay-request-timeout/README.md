# Recover one transient online join request timeout

The owner reports a visible public online room whose join fails with
RelayServiceException: Request timeout, plus a host returning to Home after
someone tried joining. The owner is unsure whether that host exit was caused by
the join or coincided with a timeout. LAN success does not resolve this report.

The installed SDK maps a network-level UnityWebRequest timeout to NetworkError
and has a ten-second request timeout. The existing game made one join request and
ended the attempt immediately when that request timed out. The targeted change
retries once after350ms for RequestTimeOut or NetworkError whose message explicitly
says timeout/timed out. Other network, expired-code and authentication errors keep
their original failure. Both attempts share the existing ownership gate; cancelled
or replaced operations cannot repeat a service call or invalidate their successor.
Repeated timeouts remain failures after the one retry. No wire/protocol change.

A behavior-preserving extraction of the same SDK call enabled the native baseline.
Corrected original: two injected timeout cases fail; immediate-success and expired
code controls pass. Candidate: all seven pass, adding repeated-timeout, cancelled
attempt and replaced-attempt checks. Original10952 exited2; candidate14752 exited0.
This proves SDK timeout recovery logic, not a fix for the reported host exit or
success on the tester's network. That host-lifetime investigation remains open.

Fixture corrections and failures are retained and excluded from cause evidence:
missing Unity.Services.Core test reference, an unreset per-case counter and a
blocking NUnit ThrowsAsync assertion around asynchronous delay. The stalled owned
editor7356 was stopped; the corrected assertion awaits its task normally.

All parents are terminal.19,294 protected inputs compared, generated importer
changes retained then restored exactly, shared preferences/QualitySettings/profiles
restored. Only intended session/test/dependency changes remain. A narrow existing
Unity.Services.Core test reference was added; no package version changed.

Separately, actual Guest/title/Home/CustomONLINE/publicJOIN on accepted release97a313
created T7FV/JWCNDQ and admitted the client2/4; the host survived admission and client
exit. Both players exited0 and settings were restored. This was one PC through the
real online service, not two-machine or tester-environment acceptance. Raw evidence
remains in Logs/relay-online-owner1005/local-ui-pair.

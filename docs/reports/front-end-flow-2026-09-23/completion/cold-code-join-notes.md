# Immediate code entry and LAN discovery

Native937313108 code F4DR was entered immediately after the join panel opened.
Both first entry and same-profile reentry returned HTTP429 from Lobby query and
stayed self-hosts. runner exit0 only means the report processes exited cleanly.
See code-rejoin-before-fix.json; raw logs/reports stay in QUAL Logs/ux1-code-rejoin.

Source inspection: ResolveCodeAsync checked only the current beacon dictionary.
The first advertisement is asynchronous, once per second. A pasted code/invite on
browser-open can therefore miss a local room. Meanwhile opening the browser starts
a public Lobby query and code resolution independently starts another query.
Unity documents a player limit of one query per second:
[Multiplayer rate limits](https://docs.unity.com/en-us/mps-sdk/rate-limits).

The proposed fix starts LAN listening, returns an existing match immediately, and
allows one beacon interval plus margin for a cold room before online fallback.
Both actual online query sites now share one serialized spacing gate. This is a
bounded discovery window and service-query cadence, not repeated retries on429.

The focused case sends an actual loopback LAN datagram AFTER requesting the code,
then requires a local resolved address/port; the subsequent cached lookup must
complete immediately. No live UGS calls are needed for that case. A native code/
rejoin follow-up is still required to establish that real broadcast discovery works
on this machine. The source defect and fix do not by themselves prove that.

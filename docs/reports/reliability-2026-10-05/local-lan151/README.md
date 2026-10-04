# Normal two-player LAN flow on one Windows PC

Source: cf405096cbae20f4c7d8483e6c5ba8e385807a97, protocol 151.
Both ordinary standalone players used fresh named profiles and real UI controls.
This was local LAN transport on one PC, not a laptop or WAN test.

Guest/title/Home/Custom host and client LAN browser join succeeded. Both players
became ready. The host selected Hero Strike, four rounds and 30 seconds per round
through the normal Rules screen. The host manually locked a character; the client
used the selection timeout. The match naturally completed.

Both complete history objects matched: match 403ba3f43e1b4c49a0ff3256cf3bba5a,
four rounds, duration 119.93882751464844 seconds, scores 20/80/610/940.
The host result header showed four rounds but the client showed eight. The saved
result was correct. Completed-host departure used the corrected completed-match
wording, with the client's stale eight-round denominator still visible.

The client held W for two seconds through real scan-code input. The retained
16-second desktop film shows forward motion without an obvious backward snap in
the sampled movement window. This does not certify remote latency or WAN motion.
Setup logged a receive queue overflow and a transient 2873 ms RTT; gameplay later
reported 34-44 ms, with additional transient poor-link samples.

Other open observations: the host address hint selected Npcap's 169.254.38.78
instead of Ethernet 192.168.1.7, and the LAN browser showed two rows for one host.

## Shutdown evidence

Host exited 0. Client exited 0xC000041D after Alt-F4 during a cross-window focus
operation. Matched Unity 6000.5.8f1 symbols identify NewInput::Activate and
CleanupModule_Accessibility in the native quit stack, with UIAutomationCore.
An isolated ordinary startup/Alt-F4 control with no concurrent window activation
exited 0. The evidence narrows a shutdown/focus reentrancy condition; it does not
establish a game networking or rendering fault. No speculative engine workaround
was introduced.

## Evidence and cleanup

Raw logs, screenshots, complete record comparison, movement input receipt,
accepted film and compact crash dump remain in
Logs/integration-current1511005/local-ui-pair. Both players and the recorder are
terminal. The shared input guard and both isolated profiles were restored.
The Desktop release and unrelated processes were preserved.

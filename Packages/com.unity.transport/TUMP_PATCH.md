# Local receive-pool repair

Upstream: Unity Transport 6.5.0, copied from the installed package without an
engine/package version upgrade. Original license, metadata, editor code, docs and
samples are retained. This package remains under its included Unity Companion
License. Official license: https://unity.com/legal/licenses/unity-companion-license

Only Runtime/UDPNetworkInterface.cs differs from the original runtime source.
Original SHA256: 9ccdca3a402e3c56e39eb8a3431e700afc38544bb4b4cf16cb3b876b82358e01

ReceiveJob now returns an acquired buffer when completion fails, its received
byte count is invalid, or enqueue fails. Those paths formerly neither enqueued the
buffer for later release nor returned it directly, eventually exhausting receive
capacity. ScheduleAllReceives could then schedule nothing while the driver kept
appearing alive. No packet format, send rate or gameplay rule changes here.

Regression: TransportReceiveRecoveryTests sends bounded empty datagrams to a real
local UDP driver, then requires a legitimate connection. The unpatched package
fails this test in Logs/transport-empty-before.xml. Re-run the test and real direct
hard-reconnect cases after any package replacement; do not patch ignored Library.

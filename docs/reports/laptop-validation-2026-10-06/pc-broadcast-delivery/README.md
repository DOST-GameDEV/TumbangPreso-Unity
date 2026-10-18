# Separate actual UDP route observation

After current917 PC-host natural discovery/code failure and direct admission success, both actual game clients were terminal. No game discovery observer or extra8911 listener ran. A one-round prefix-filtered diagnostic receiver33504/parent50921 bound0.0.0.0:18991 on laptop192.168.1.144. Root PC192.168.1.7 sent four agreed datagrams, closing each sender socket. This tests delivery on the current network, not game protocol or playable acceptance.

| Route | Peer-reported sender | Bytes accepted by send | Locally received |
|---|---|---:|---|
| Unbound directed192.168.1.255 | 0.0.0.0:54947 |42|No|
| Unbound limited255.255.255.255 | 0.0.0.0:54948 |41|No|
| Bound directed192.168.1.255 | 192.168.1.7:54949 |40|No|
| Bound unicast192.168.1.144 | 192.168.1.7:54950 |39|Yes|

Actual local receipt was2026-10-05T19:56:48.328611Z, source192.168.1.7:54950, payload `tump-route-diagnostic1006:bound-unicast`,39bytes. No other matching packet arrived during the armed receiver period and15seconds after explicit sender-completed ACK; ignored packet count0. The receiver closed normally at19:58:51.264696Z, exit0; port18991 was verified gone. Actual raw local observations and the corrected sender ACK are retained; initial peer prose byte counts45/44/43/42 were corrected to actual42/41/40/39 and are not used.

The separate Root native Mono endpoint/send-call probe included physical192.168.1.255 and accepted sends. That did not prove delivery. This observation contradicts a simple packet-parser or endpoint-omission explanation for the directional failure, but does not identify whether the three broadcasts were stopped by OS filtering, routing, an access point or another network component. No Windows/firewall/security setting, source, game port, profile or package was changed.

A client solicitation with host unicast response is a proposed architecture fit for this observed direction; it still needs native lifecycle/visibility controls and actual matching packaged two-machine qualification. Root owns LanBeacon. These diagnostic datagrams are neither a discovery fix nor a game-pass claim.

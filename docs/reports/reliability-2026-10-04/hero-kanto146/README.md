# Actual default eight-round HeroStrike on Kanto

Source23168/protocol146, identical258-file Windows player on both machines.
Fresh Hub2/empty rules selected the canonical Hero default wire
1|0|8|90|0|3|0|1|0|0|0. No tournament override, AllBots, shortened custom wire
or forced match end. Normal two-peer lobby/readiness and actual host UI Kanto map
selection. Eight rounds ended naturally:50/490/3225/3130, seat2 wins.

Host14780 and client23528 final reports are HOST/CLIENT/networked146/HeroStrike/
Kanto/round8inactive. Both normal exit0/profile+input restoration and full258-file
hash checks pass. Root independently verified all10 client raw Git hashes and
exact full History/Queue/Witness equality. Canonical recordSHA
112983f88d7a74cc3c85b1f7e5c6b6428a93f67717a5d554cfe39890d719f1cb,
match94d0da49d99b4f9e94bb83dbe41763ff, witness87d324400ea61b25, markers clear.
Client HostLost appears after NetReport during own shutdown; no midmatch recovery
or physical disconnect is claimed. Host final seat1 is handed to a bot after
client's earlier scheduled exit; the natural-end record preserves both human origins.

[Client raw](../../laptop-validation-2026-10-04/hero8-146-kanto-client/README.md).
This qualifies this functional package. Newer presentation commits, real operator
input/devices, Relay, further maps and actual slow peer replay still need acceptance.

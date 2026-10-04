# Wallet cache backup recovery

WalletStore.Load previously called SafeStore.Read without a validity predicate and parsed the returned text afterward. A truncated primary was therefore accepted by SafeStore; the later parse failed and a valid backup was never tried. The offline wallet lost its known cached balance despite recoverable saved bytes. The reader now passes its existing Cache parser as a predicate, matching other SafeStore consumers. The existing account-owner gate remains after parsing. Transaction, response-owner and deferred-refresh behavior is unchanged.

Four guarded native EditMode cases validate the actual Load method against named-profile files: a malformed primary recovers the current account's backup and balance77; a valid primary wins over the backup and returns99; a foreign account's readable backup is refused; and a malformed primary without a backup remains unknown (-1). The fixture uses a dormant WalletStore, controlled identity override and public Known/Balance assertions. It asserts reading does not rewrite any wallet primary/backup/temp bytes, then restores the original files and identity. No SDK or transaction is started.

## Causal evidence

- Baseline joint7: Wallet3 controls passed/1 backup recovery case failed. Independently, the companion force-equip fixture2 controls passed/1 causal failure. Overall5 passed/2 failed; no fixture failures.
- Final joint7: Wallet4/4 and companion force-equip3/3 passed. Overall7 passed/0 failed, fresh nonempty XML and exit0. Both fixture sources are identical between baseline and final. No tooling or fixture repair was needed.

Baseline session 95238 and final 63788 used Unity 6000.5.8f1 EditMode through the serialized CPU pool, 1536MB budget/2048MB reserve, 450-second limit and named `wallet-force-equip1002` profile. Baseline preparation yielded session 93576, which was awaited to terminal exit0 before launching; final preparation also completed exit0 before launch. Exactly six frozen source/fixture/meta inputs were checked for each phase, including the companion agent's separately owned source pair and focused existing fixture. The existing SlipperHandoverTests meta was protected rather than copied. Only these seven selected cases ran.

Both guards finished restoration with no held lease. All 1285 protected source/private/meta hashes matched after final, and every installed native input matched its frozen hash. This includes all other current ACK/rebind/input/round source and the protected QualitySettings, RosterArms and two UI metas. No Ready/Buffer/Emote input was installed or changed. The baseline guard preserved zero existing profile files; the final guard preserved one file in the named task profile. Both restored one shared Editor input preference. No task-owned Unity/player process remained when the slot was released.

## Inputs and limits

Baseline WalletStore SHA256: `58B1E22EA82113E8C19BA51EB37686E2A7B11AE7940ACF82A41DC1E1E8D37970`.

Final WalletStore SHA256: `9FE39202F078FBE42D97CC627BDBA09F18E554B59D60470D25B552457AB8F45B`.

Unchanged new fixture SHA256: `855744368C4019900AEEBC15207A6AC59084BDAA25116ABBF7093AA1A21EE324`; meta `27267E754A818E63B823F857F59C50A97D63E2592EBD7B557F7302DCA72A4FA8`.

Raw XML/logs/guard receipts, separate per-fixture counts and source/preparation/protected hash manifests are preserved beside this report. These checks establish local cache recovery and account rejection; they do not validate live economy services, purchases, claims, a standalone build containing this change or full competition readiness. The companion force-equip unit has its own acceptance report and ownership.

# Account changes close the previous account's scorecard

PlayerHub's owner-change refresh cleared the old history rows, page and profile draft, but its separately built detail overlay remained active outside the rebuilt list. A new account could therefore appear behind the previous account's still-open scorecard. The existing owner-change branch now deactivates that detail overlay. Same-account refreshes keep the current scorecard open. Authored layout, history navigation/request fences and Account/Social behavior are unchanged.

Three focused native PlayMode cases use the actual current PlayerHub.Install, real OpenMatchDetail button onClick, and controlled local-only PlayerAccount.SignInAsGuest transition. That public transition raises the subscribed Changed event, reaching actual OnDataChanged/Show. The old-account scorecard must close, old shown/page state must clear and request generation must advance. Controls verify same-owner refresh retains the open record/page/request and owner change before any detail exists keeps the hub usable. Dormant controlled account state and temporarily null Career/Social references prevent SDK requests; services are restored afterward. Existing PlayModeWorld supplies scene cleanup.

## Causal results and one fixture repair

- First baseline 3 (session 95721): one no-detail control passed, two fixture lookup failures before the product assertion. OwnerUiLayout.Canvas creates a detached scene root bound with CanvasLifetime, so looking under the hub component did not find its actual button. Original XML/logs/input bytes were preserved.
- One bounded fixture repair changed only the lookup to the actual private_canvas subtree, retaining the same exact button action and behavior assertions. Repaired original 3 (session 79587): two controls passed and one causal failure reproduced the old scorecard remaining active after account Changed.
- Candidate 3 (session 1437): all three passed against the identical repaired fixture. No further repair or unrelated suite ran.

Unity 6000.5.8f1 PlayMode ran through the exclusive serialized GPU pool, 2048MB budget/2048MB reserve, 450-second limit, D3D11 graphics 960x540 and named `playerhub-owner-detail1002` profile. Every preparation completed exit0 before dependent launch, verifying exactly three frozen source/fixture/meta inputs. All 1293 protected source/private hashes and three installed native inputs matched after final. All three guard receipts report terminal restoration and no held lease. No task-owned Unity/player process remained at slot release to the separately owned capture run.

This is current UI/account-event lifecycle acceptance. It does not claim physical input, rendered layout/pixel approval, live authentication, full match behavior or a standalone build containing this later change. No input asset, Account/Social production source, private artwork or normal profile was edited.

Original PlayerHub.OwnerPainted SHA256: `A74208D517E3D08F2AD9D8539EA3B99CFB89A23CB11B05EE828EB69790AF5F83`.

Final source SHA256: `42B88C4D97F45AA1B4A9BC9A77AC32C8CC447BEB910E84662D2E72C3471200EB`.

Original fixture SHA256: `5C083234A8BC2527898B3C5E2BDD68A0A8F25FAE94765572967FC73CA2635935`; corrected fixture hash is in fixture-repair.json and final-inputs.json. Meta stayed `ABCAA775A138D2A5CCE41551A232FCC8B4AAE3626A8819BDB3970640D042515F`.

Raw XML, logs, guard receipts, separate counts and preparation/protected/frozen manifests are preserved alongside this report. These narrow cases do not establish full competition readiness.

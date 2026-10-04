# Closing the scorecard restores usable hub focus

The current history scorecard has its own nested ScreenFocus. Closing its selected CLOSE control merely hid the modal, leaving EventSystem's selection on an inactive child. The parent focus path excludes nested controls, so its control count did not change and it never rebuilt a usable selection.

The current CLOSE button and existing PlayerHub Escape detail branch now use one CloseDetail helper. It hides the detail and rebuilds the base canvas focus only when the current selected object belonged to that detail. Another live screen's selection is left alone, and closing an already hidden detail is a no-op. Authored layout, record/history state and the separately shipped account-change detail closure remain unchanged.

Three native PlayMode cases use current PlayerHub.Install/Show, the actual OpenMatchDetail and CloseMatchDetail button callbacks, and real EventSystem selection. The causal case selects CLOSE and checks next-frame selection is active in the base hub rather than in the hidden modal. Two controls preserve another live canvas's selection and verify idempotent closure leaves history/page/selection unchanged. Controlled null Account/Career/Social service references avoid SDK calls and are restored; existing PlayModeWorld provides settled scene cleanup.

Original baseline 3 (session 38512) passed two controls and reproduced one selected-modal focus failure. Candidate 3 (session 9064) passed all three against identical fixture/meta bytes. No fixture or tooling repair, repeated unrelated test or input-event framework was used.

Unity 6000.5.8f1 PlayMode ran in the exclusive serialized GPU pool 2048MB/2048MB reserve, 450-second limit, D3D11 graphics 960x540 and named `scorecard-focus1002` profile. Both preparations completed exit0 before launch and verified exactly four frozen source/fixture/meta inputs. All 1295 protected source/private hashes and four installed native inputs matched after final. Both guards completed restoration without a held lease, and no task-owned Unity/player process remained at release.

This validates current button closure and common focus-helper behavior. The Escape dispatch shares that helper in source; a physical Escape/controller press was not executed. There is no physical-input, rendered pixel/layout, live-service, whole-match or standalone-inclusion claim. No Account/Social production source, input asset, private artwork or schema was modified.

Final PlayerHub.cs SHA256: `6EB28EF0A3C0129EB6BD9C88033DA0E134D4ABA051711A82DF20A63B3E63488F`.

Final PlayerHub.OwnerHistory.cs SHA256: `AAF8D7C145837FA0AC10432F7997E7596EA6834C90F69EF5EF81C4A52EA6E366`.

Unchanged fixture SHA256: `EA08C1A60A5169A7DD373C6616D4C383BBED66762A62DE29F1309C62F38E0710`; meta `6E458830A9DCFCA3ED72FFD3E4552BB327477E188FF0A22820164250AF684C57`.

Original source hashes, raw XML/logs/guard receipts, separate counts and preparation/protected/frozen manifests accompany this report. These narrow results do not establish full competition readiness.

# Controller map matches the dark settings workspace

The owner reopened the controller page's styling while retaining the controller
in the middle and its lines. The original diagram, calibrated anchors,18callouts,
bindings and rebind/refusal/reset behavior are preserved. The surrounding UI now
uses dark grey, Darumadrop headings, Lydian body labels and contrasting control
glyphs. Focus identifies the callout, its actual leader segments and the matching
physical-control anchor. This adds useful feedback without replacing the diagram.

The map is now directly accessible from default PC Controls through Controller map
OPEN. Returning from it shows the controller bindings. Keyboard/touch and the
existing save/discard transaction remain available. No input-backend replacement.

The focused ControllerAndTouchViewsKeepTheirRealReturnAndCancelPaths case passed
twice, first for the theme and then for direct entry. Ten representative PC sizes,
all18glyphs/callouts, actual leader objects, focus/target feedback and real return/
cancel callbacks pass. Receipts338c17789684 and2103d859771d preserve scoped profiles.
The1920px views were inspected. A further2/2settings check passed after removing
the remaining italic face from sidebar headings/binding values and using a back
arrow (receipt292fcdaf99af).

Nativev11 passed direct entry,18callouts/leaders, three actual Windows sizes
(960x540,1366x768,1920x1080), return to controller bindings and the existing both-mode
picker/match/result routes. The built controller and settings views were inspected.
The automated review drives the actual EventSystem handlers; physical-device
comfort and owner visual approval remain separate. Shared input was unchanged.

Artifact: Builds/demo-2026-09-16-v11/TumbangPreso.exe and adjacent data folder.
Runtime SHA256:8F4CE2F929F7AA9BA910A9A8987F660895F2E58DEB6477F9D5918EB65E64B61F.
DLL2026-09-15T08:37:00.2104200Z; build1081MB/45s, guard539de26eb047. Protocol38.
The input backend and network rules are unchanged from the qualifiedv9 candidate.

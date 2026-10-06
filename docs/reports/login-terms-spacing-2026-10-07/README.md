# Conditional terms spacing

The owner requested the terms row slightly higher, lowering only when feedback
text appears. Its normal position is now12 logical pixels above the original
738-unit top offset. Visible field/status feedback returns it to the original
position and clearing that feedback raises it again. Ordinary typed text does
not lower the row. Checkbox, link, fonts, Create/Guest placement and consent gate
remain intact; OwnerUiMotion's new baseline setter preserves its entry offset.

Hidden D3D11 native29796 passes2/2 on base4655fbac9 plus the recorded source:
OwnerTermsFeedbackLayoutTests checks normal/typed/confirmation-fault/corrected
states and unchanged Create placement at1080/720. The existing consent test
checks the intentional12-unit feedback shift plus simultaneous pulses, clearing
and reduced motion. All21232 source inputs and shared preferences restore after
termination. No local invalid scenario dispatches an account request.

[Raised1080](TermsRaised1007-1920.png), [feedback1080](TermsFeedbackSpace1007-1920.png),
[raised720](TermsRaised1007-1280.png) and [feedback720](TermsFeedbackSpace1007-1280.png)
are actual native frames. They use the unchecked checkbox state; the bold check
asset is unchanged from its preceding qualification. XML and compact receipt
are here; full frozen hashes/restoration remain in local Logs. Desktop770 is
unchanged and these scoped results do not imply whole-UI owner approval.

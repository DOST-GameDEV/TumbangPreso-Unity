# Online availability follows real editor Play sessions

QA testers used Unity Editor MatchSetup, while most earlier real online evidence
used standalone players. The source EditorSettings has Play options enabled with
both domain and scene reload disabled. The investigation used the exact normal
nonbatch6000.5.8f1 editor, existing settings and a named pc-ed profile. Pipeline
opened MatchSetup and controlled ordinary enter/exit Play; sign-in/cache state
was only read, never forced. Hosting and peer joining used visible game controls.

## Concrete defect and correction

Original first Play reported initialized Unity Services and a real signed-in SDK.
After normal Stop and re-enter, Unity Services reported Uninitialized while
NetIdentity still reported SignedIn with its old completed attempt and count1.
Its permanent-success cache bypassed initialization for the new SDK lifetime.
The retained read-only second-play receipt proves this state mismatch.

NetIdentity now invalidates its availability answer at SubsystemRegistration using
the existing ForgetCurrentSession, before boot sign-in. It retains the explicit
launch profile, local identity and existing sign-in coalescing/retry rules. It does
not sign out/delete an account, alter EditorSettings or use ResetForTesting.
The small hook applies once at the actual runtime session boundary.

## Real editor validation and limits

Original second Play still created ONLINE roomJJRM via retained SDK service
objects. Normal standalone74 peer2532 joined by typed code; both showed2/4 and
the editor host survived admission and peer exit. That is not a reproduction of
the QA Relay Request timeout/host kick, so this report does not claim closure.
SDK Core and game availability could disagree while those old objects worked.

Candidate editor19572 was launched fresh with only the scoped source hook. First
and second natural Play both report Services Initialized, NetIdentity SignedIn,
SDK IsSignedIn true and unchanged profilepc-ed151c1005. No test/reset seam ran.
Normal HOST ROOM->ONLINE->CREATE after the corrected second Play creates7WXK and
read-only state confirms live SDK plus actual networked host. This is successful
current-editor boot/restart/host creation, not a candidate real-peer match or WAN
failure reproduction. No broad unchanged native suites were repeated.

Originaleditor4760 and candidate19572 both exit0 after normal leave/Stop/close.
Original peer2532 exits0 first; its parent47013 restores before editorparent73973.
Candidate parent34184 restores exact profile, shared/editor preferences, input,
Quality, EditorSettings options and previous last-scene cache.202 incidental
input/composition metadata per editor are retained and restored to frozen bytes.
The original restoration helper's final source hash saw the next intentional
NetIdentity patch because it began before that helper finished; a separate receipt
verifies only that intended source differed. No other original input was lost.
Candidate19310 frozen inputs remain exact after classified metadata restoration.
Auditor/private draft/Desktop and supplied artwork remain. No unused owned actors
or browser remains; the ongoing goal and QA host-loss issue stay active/open.

## Tooling correction

PATH unity.cmd merely forwarded to Unity.exe, so status launched an owned empty
Launch Unity dialog. It was normally closed without a project. The real installed
CLI is AppData/Local/Unity/bin/unity.exe; Pipeline reported exact editor PID/project
ready on localhost7800. The file dialog rejected a forward-slash path and native
capture/input geometry timed out; the supported open_scene command selected the
exact existing scene. No source scene or editor setting was rewritten to bypass
the repro. Tool failures are separate from product evidence.

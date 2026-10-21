# Keep menu-close movement out of camera look

The existing closing-frame guard suppressed buttons but ReadLookDelta still
forwarded late touch drag and held gamepad look. When the menu closed and the
pointer relocked in that frame, camera input could move before gameplay regained
its own frame. The observed Practice camera turn prompted this investigation;
the precise desktop legacy-mouse contribution is not measured by these tests.

ReadLookDelta now returns zero only in the existing menu-close frame and consumes
a late touch delta there. Ordinary look and a sustained stick resume next frame.
Mouse, stick, touch, possessed-companion and emote camera paths retain their
shared producer; no sensitivity/backend/binding or network-schema change.

Graphics-enabled Windows Unity6000.5.8f1 original23460: held-stick closing input
(0.22,0) and late touch(40,-15) fail the zero-look requirement; ordinary touch
control passes. Candidate9168: same3/3 pass, including next-frame fresh touch
and held-stick resumption. Both parents/settings/Quality/input are restored;
202 generated owned UI metadata changes per run restored to frozen bytes.

This is actual input producer evidence for two devices, not a current packaged
mouse-pointer/relock film or multiplayer movement acceptance. Those remain
separate. Native XML and exact case receipts are adjacent.

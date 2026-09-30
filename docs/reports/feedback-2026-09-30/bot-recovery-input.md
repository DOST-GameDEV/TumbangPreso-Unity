# Bot recovery between render and physics updates

ENG-0930-RECOVERY, source1f7012af3 plus the recovery pulse buffer and native fixture.

AIController toggled recovery Jump on each render Update. With an even number
of render updates before physics, its clear/toggle loop released the press before
CharacterMotor could consume it. A bot could therefore fail to recover at an
otherwise higher frame rate.

The recovery branch now carries an observed pending press through its ordinary
intent clear and buffers the current pulse. It uses InputIntent.BufferPress, the
existing human input bridge. The real motor retires that buffer at CommitFrame.
Core recovery durations, the10Hz rate limit, authority and hero mechanics remain.
No loading or presentation edits.

## Native evidence

Unity6000.5.8f1 Windows D3D11 PlayMode, Eskinita, isolated profile
feedback-0930-bot-recovery. Actual AIController.Update and CharacterMotor.FixedUpdate:

- Baseline3cases fail:2,4or6render updates cancel the pending Jump edge.
- Fixed3cases pass. Each update ratio retains one recovery press, real physics
  accepts it once and clears the edge, repeated physics does not consume it again,
  and another immediate pulse remains subject to the unchanged Core rate limit.
- 553 frozen overlay inputs, unrelated main dirt excluded, no non-metadata drift.

Raw XML and manifests are in checks/bot-recovery; full logs remain in the isolated
checkout's Logs/feedback-0930. Controlled component scheduling represents render/
physics ratios. Whole-match bot recovery rates and physical-device performance
are not measured by this fixture.

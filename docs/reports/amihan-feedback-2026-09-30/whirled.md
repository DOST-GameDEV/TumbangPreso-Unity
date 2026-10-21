# Whirled can-reset restriction

The current Status Effects table says Whirled prevents slipper retrieval and can
resetting for2.5seconds. Source prevented only retrieval. Native baseline3/3fails:
a Whirled defender completed a reset, a running reset kept progressing, and the
host eligibility gate accepted that defender.

Carrier.HasResetTarget now refuses Whirled, so the local reset input/HUD and
channel share the existing gate. HostMayChannelReset mirrors it, including the
host's ongoing-channel cancellation checks. General CanAct remains true: this is
not a stun, and walking/other legal actions are not broadly disabled. Status text
now names both restrictions. Protocol105 prevents mixed reset rules.

Final3/3native passes5.31seconds: no reset during status, interruption clears the
partial channel, held input can start a fresh reset after expiry, and the host gate
refuses the same condition. Eight Core status/Amihan contracts pass. No memory
stop or new OOM. No actual-peer or new-player result is implied.

Together with the existing Drift/Featherfall/Whirlwind behavior, the shipped
Second Wind/Drift reconciliation and Airburst correction, this closes the current
complete Amihan Wiki feedback. Human approval remains independent.

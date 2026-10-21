# HUD text removal, October 21, 2026

Owner requested removing the train announcements from code, then clarified that
all train/Overclock announcements must go, including the Hero Strike advance
warning. The owner also requested removing the distant-can reset instruction.

## Changes

- Deleted all three train toast strings, Announce and OnWindowOpened, their calls
  and the train controller's HUD import. No hidden or disabled toast implementation
  remains. Train movement, audio, shake and cooldown state are unchanged.
- Deleted the distant-can instruction assignment. The action prompt stays empty
  outside interaction reach; the reachable Reset Can binding, touch emphasis,
  reset channel and cancellation feedback remain.
- Updated CanResetTouchCueUsesTheActualReachAndRestoreSetting to expect an empty
  distant prompt, retaining its existing reachable/reset/cancel assertions.

## Validation and limits

Final runtime direct compilation passed with exit0, using the installed compiler
and Unity-generated runtime response file with outputs redirected internally.
Warnings remain; no compiler errors were reported. Local log:
Logs/train-toast-removal-1021/compile-final.log.

Source search confirms the removed strings and train announcement methods are
absent from runtime source. The focused diff has no whitespace errors. Native
PlayMode tests were not run, preserving the recorded Unity-slot reservation.
No player build or gameplay/device acceptance is claimed.

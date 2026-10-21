# Abandoned ready countdown candidate

Host loss revokes authority while the old scene may still exist. Its ready
countdown could finish and emit RoundShouldBegin before asynchronous scene exit.
Check the accepted abandonment latch at continuations and retire countdown state
instead of starting an abandoned round. Normal ready/quorum/input behavior stays.

Three actual IEnumerator/latch cases cover loss after first tick, loss after GO,
and ordinary completion. Static review and Runtime/fixture compilation pass.
Native coroutine timing, slow scene exit and live peers remain pending laptop
validation. Original source retained. No socket, SDK, input, loading or runner edit.

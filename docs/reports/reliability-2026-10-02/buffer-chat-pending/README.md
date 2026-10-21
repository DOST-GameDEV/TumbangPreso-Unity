# Buffer chat intent candidate remains pending

Source inspection found that `BufferSkipVote.Update` reads READY directly without
the gameplay reader's chat focus guard. A candidate rejects new manual intent while
chat is typing, after the existing pending-vote retransmission path. It remains
**unstaged and unqualified**; no shipping or runtime improvement is claimed.

The combined first baseline has one passing buffer retransmission control and
three pre-assertion fixture failures: the synthetic action did not report a press
in EditMode. The single joint context repair used a transient InputSettings clone;
the repaired buffer subset again passed one case and failed three during setup.
The installed InputSystem destroys a replaced HideAndDontSave settings instance,
so preserving its original reference did not provide a valid restoration target.
Neither run reproduced the manual-input product defect.

The native retry limit is exhausted for this fixture route. Raw combined XML and
job receipts remain here, with separate per-fixture outcomes. The new failed buffer
test sources were moved out of the default suite into task-owned snapshots under
`Logs/buffer-chat1002-inputs/retired`; the earlier source/test snapshots and manifests
are preserved. Existing tests and user settings assets were not removed or reset.

Qualification logs: `tump-feedback-0930/Logs/rebind-ready-buffer-chat1002`.
First joint baseline85263; repaired baseline84619. Both jobs terminated and
completed profile/input preservation. Sol's distinct rebind action-state issue
remains causally reproduced by its independent ten cases.

Future acceptance must exercise real gameplay input context through a different
useful scenario. Do not replay this unchanged EditMode fixture or lower its press
assertion. The pending production hunk is owned by root and remains preserved.

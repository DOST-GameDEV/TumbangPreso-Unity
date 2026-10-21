# All-seat score observations in the replay diagnostic

The existing trace only recorded score1. A catch awards defender0, so one score
cannot establish that recorded playback does not change any other seat score.
Append readonly score0/score2/score3 to the header and invariant trace row;
all original16 columns and gameplay/contact/default/timer behavior remain.
No networking/protocol/gameplay changes or extra implementation-mirroring tests.

The exact native-qualified catch helper bodies are unchanged, verified by source
comparison. That existing1/1 evidence is reused only for those helpers. The new
19-column trace must compile in the upcoming coherent build and its actual paired
rows must be inspected before a score-isolation/network claim. This small source
change is not represented as a rerun of the old entire Runtime-file native hash.

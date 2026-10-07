# Two-machine rematch gate preparation

The existing LAN runner now accepts `--rematch`, invoking the already installed
`-tp-autorematch` driver through the normal result-board RequestRematch action.
The host explicitly selects Eskinita before starting. No review-rule override,
gameplay, networking, protocol or UI change is introduced. Existing normal
completion and deliberate host-loss scenarios remain separate and strict.

The new gate requires two natural completed Hero1/30 matches, real result-board
votes and the second ready/live flow, connected terminal roles/own seats, court
rotation to BayanPlaza and unchanged room rules. Both saved records must have
distinct match IDs, the same two human identities, four complete seats and the
correct own human line. Two History/Queue/QueueWitness entries and a cleared
in-match marker are required. History and Queue are compared by match ID because
they legitimately store newest-first and oldest-first respectively. Terminal
standings must agree with the latest record. Full canonical hashes of each saved
record are exposed for independent comparison between the two machines.

Twenty-five runner checks pass, including rejecting incomplete rematches,
duplicate IDs, failed rotation, changed queue data, missing witnesses, identity
loss, wrong own seat, uncleared markers and disconnected/wrong final standings.
These are test-runner checks. Actual paired rematch acceptance is pending the
PC's coherent opening unit and an agreed free test slot. Use the same checked
artifact, fresh profiles and prepared/start barriers; preserve all package and
profile/input bytes after natural process termination.

This proposed gate does not establish spectator/physical-input/WAN behavior,
every map or hero, sustained eight-round tournament performance or later source
fix acceptance on an older package.

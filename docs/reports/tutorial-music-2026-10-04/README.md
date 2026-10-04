# Tutorial music replacement

Owner-supplied Tutorial.mp3 replaces only guided-tutorial music. Exactly the
first3seconds (132300 decoded frames per channel at44100Hz) are removed.
The81.506122s stereo PCM WAV equals the original decoded audio after that offset
byte-for-byte; no fades, normalization or extra lossy encoding were applied.
Unity generated its importer metadata. Existing menu/match assets are unchanged.

GameServices loads a distinct tutorial cue; MatchInstaller and both countdown
HUD routes select it while GuidedTutorial is active. Ordinary matches/practice
retain the match cue. Existing music volume, ducking and looping behavior remain.

Native Unity6000.5.8f1 PlayMode2/2 passes in3.6146724s at07:43:38–41UTC:
- actual tutorial entry chooses the new track and a countdown cannot replace it;
- clip length/channels/resource identity, actual playback, end-to-start loop and
  normal-match selection are checked.

All frozen inputs unchanged and settings restored, exit0/no new OOM. The initial
compile/import/full-scene process hit the8GiB limit and an OOM kill, with no test
result; its receipt remains. Fresh cached execution passed. This does not claim
a target-device build or a subjective listening/mix review.

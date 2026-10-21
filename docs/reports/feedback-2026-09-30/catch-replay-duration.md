# Tagged replay duration

## Change

The victim-only catch reconstruction is capped at3 seconds instead of1.1 seconds.
It still ends sooner when authoritative recovery is about to finish, the player
regains control, the round changes, an obscuring panel opens, cinematic motion is
disabled, reduced motion is enabled, or neither camera side is clear. It does not
change the five-second tag penalty, immediate teleport, scoring or taya control.
The separate Feedback report about truthful contact/gaps remains open.

## Native validation

Unity6000.5.8f1 Linux64 with graphics, guarded isolated cloud-catch-duration profile.
The baseline real accepted catch reported1.10000002 seconds remaining against the
requested2.9-to3.01-second acceptance interval. The corrected candidate passes5/5:

- Real punch/tag scoring, roughly3-second playback active after1.3 seconds,
  unchanged taya position/action and return to the victim before control recovers.
- Open-side camera selection and safe fallback when both sides are blocked.
- An event arriving before authoritative recovery waits, but an expired event does
  not attach itself to later recovery.
- Independent camera control keeps recovery in first person.
- Late events and reduced motion do not extend the penalty.

Frozen source hashes are unchanged. An initial cold full-scene test exceeded the
180-second fixture timeout before reaching acceptance. One bounded environment
retry allowed360 seconds and960x540 graphics; it reached the expected duration
failure in4.46 seconds. The corrected five-case run completed in117.81 seconds.
No assertions were weakened. The older repeated-catch fixture now waits for the
actual remaining replay clock instead of its retired hard-coded1.1-second value;
that ten-catch repetition was not rerun.

Two capture-helper images were inspected. They show the later first-person recovery
view because capture preparation advanced past the short overlay; they are not
visual proof of replay contact or composition. They are retained in local evidence,
not represented as successful replay screenshots. This report claims native timing,
authority and exit behavior only. Full player builds, actual network peers, physical
devices and truthful contact visualization remain unqualified.

## Evidence hashes

- baseline.xml: SHA-256 4dc4078d13917e499b3822167b183699a5a610afefcdd5a660daac04feb8d011
- baseline-retry.xml: SHA-256 903f6bb060ecdfd6c9d0a73a33fcab1f48e0ec878c165fca5d75c78e192a4742
- fixed.xml: SHA-256 8fa716a525fdd057a0d2dfcfa1c93553d8f633b32e5438897e32a69ab0f11af5
- baseline-inputs.json: SHA-256 1ceb59db4f7337ccf6a29dd26980cdd301d0e8e56e2f00567306269b2e49b068
- baseline-retry-inputs.json: SHA-256 c03c43e65b6b974a951536b71980b97d72fd4e79132df113b54a7c5f1a1271cf
- fixed-inputs.json: SHA-256 ab867b59e9a4aa7b74feaed31f5f71ba6b11ab4808f9fd082f3e4e9c46c0caa5
- catch-after-one-second.png: SHA-256 faba67fca16eda705960f41aba8f300f9d1a05831ef1234c4884cb9eeb1a74f4
- recovery-after-replay.png: SHA-256 0ad57097a19eeb17eb3a5133d891ced1db9521c30cda16b2069e0fcc132c201a

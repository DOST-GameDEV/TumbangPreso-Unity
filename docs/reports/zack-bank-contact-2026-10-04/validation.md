# Powered contact validation

The native component project imports the exact BankShotContact and VfxRenderTag
sources, not substitutes. It does not contain a match, netcode or production art.
Unity6000.5.8f1 Linux; graphical run uses actual OpenGL softpipe rendering.

- Initial six component cases: five passed, one failed. LineRenderer stores
  gradient colors at Color32 precision; .25 becomes .2509804. The fixture now
  compares the exact Color32 conversion rather than assuming float storage.
- Initial graphical seven-case run: six passed, one failed. An expanding stroke
  can cover more pixels while fading. The fixture incorrectly compared occupied
  area. It now measures total gold color energy and still demands zero visible
  gold pixels at the end. The production component was unchanged.
- Corrected graphical run: seven passed, zero skipped,0.1843458s at04:46:37UTC.
  Terminal exit0, no resource guard, peak tree1,071,603,712bytes and
  cgroup3,897,901,056bytes. Every frozen input and both restored settings matched.

Actual0ms,110ms and220ms images were inspected. Three compact angular gold
strokes expand slightly, dim and disappear. This close component composition
establishes shape/fade only. Whole-court readability, real-time owner walking,
packaged render/shader inclusion and actual remote-peer acceptance remain open.
The footage requirement is not satisfied by these static component images.

The first import generated packages-lock.json for the explicitly added physics
and imageconversion built-in modules. All frozen source/settings inputs remained
unchanged. Subsequent graphical input maps were fully unchanged.

Runtime integration uses exact Runtime/Core/Transport sources and focused
shipping bounce fixtures in a separate validation project. Production scenes,
art and unused authoring/render packages are omitted to reduce import memory.
This is a source-integration/behavior gate, not a full game import or build.
The initial runtime compile exited1 because the reduced fixture set omitted the
existing GameplayShots capture helper. Runtime code compiled; no tests ran.
One bounded setup repair copied exact GameplayShots and ProbeWait dependencies.
The six selected native PlayMode cases then passed, zero skipped,0.5189481s at
04:51:18–19UTC. Terminal exit0/no guard; peak tree2,504,871,936bytes and
cgroup6,185,988,096bytes. Both project settings were restored. Production code,
fixture and package input hashes remained unchanged; nine automatically created
validation-only parent-folder metadata files were normalized by Unity. These
were not shipping metadata and are explicitly excluded from a source identity
claim. No product runtime source is replaced by a test stub.

Cases cover unspun powered one/two-bank events and strength/position/actor,
powered ceiling silence and retained scoring credit, and ordinary ceiling
credit retirement. The first six-case batch did not independently exercise obstacle colliders;
the follow-through below closes that focused gate. The existing GameBuilder
already preserves Sprites/Default. Packaged shader availability is not rerun.

No wire layout, kind enum, scoring or restitution changes. BankShot strength1
selects powered contact; strength0 retains ordinary spin-bank presentation.
Existing BankShot highlights now also record each real powered wall contact,
including the second Overclock bank. This does not award objective points.


Qualification source was4689a5b0 with the reported overlay. Incoming3661b191
changes PlayerHub and documentation only; the checked bank/runtime inputs are
unchanged by normal integration. The integrated whole game was not rerun.

## Obstacle and lifetime follow-through

Three additional native PlayMode cases passed05:06:04–05UTC,0.6596854s,
zero skips, exit0 and no guard. Exact production BoxCollider spherecast contact
announces once at the resolved shoe position; ordinary spun banks retain only
one strength0 event; the contact automatically destroys itself and its owned
material after its lifetime. All frozen inputs and restored settings matched.
Peak tree2,287,194,112bytes, cgroup5,699,207,168bytes. This adds behavioral
coverage without repeating the unchanged six earlier cases.

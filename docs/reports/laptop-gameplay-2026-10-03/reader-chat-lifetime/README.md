# Reader action lifetime across chat context

Date: 2026-10-03. Original reader source `0368f56f1`. Main's focused five-case
original native gate reproduced three causal failures and passed both controls.
The narrow reader candidate passes all seven native cases on its first run.
Main verified matching worker/primary source and terminal preservation before
approving publication.

## Contract and source path

Opening chat withdraws gameplay input. A pending throw or lunge must cancel,
while a committed lunge keeps its existing contact window and cooldown. A
button held while typing must be released before entering gameplay. A released
button followed by a fresh gameplay press must work.

Original `PlayerInputReader.Update` clears and commits intent when
`LobbyChat.AnyTyping` is true, without calling its existing cancellation API.
`CharacterMotor.CanAct` does not include chat context. Carrier and CombatVerbs
therefore see a pending held verb turn false and can resolve it as an intentional
release. Chat exit also has no held-button discard gate. Existing
`GameplayChatInputTests` qualifies ready, buffer voting and emote consumers;
it does not cover these pending reader actions.

## Fixture and stopping condition

Run exactly `TumbangPreso.PlayTests.ReaderChatLifetimeTests`, five cases.
Expected original: three causal failures and two passing controls.

- Enter chat during actual TouchInput throw charge, then run Carrier.Update;
  the shoe must remain held with its pending charge/tell retired.
- Enter chat during actual TouchInput lunge charge, then run CombatVerbs.Update;
  no new lunge contact or cooldown may be spent.
- Hold throw while typing, close typing, then step the next frame;
  no charge may begin without release.
- Enter chat after public HostResolveLunge; existing contact/cooldown remain.
- Release the typing button, close chat, wait one frame, then press again;
  the fresh gameplay press begins a new windup.

The context flag is supplied through the same property seam used by existing
GameplayChatInputTests. Actual TouchInput, reader and gameplay consumers are
used, with motor/flight updates disabled to isolate the input transition.
The fixture begins a real round and restores provider, launch, chat, stats,
touch, can and world state. It does not operate a chat field, inject transport
messages or qualify physical hardware.

Fixture SHA256:
`151764a65624aed250ab1e3313612b6d21789913ce6da5319287f341b1b8c643`.
Metadata GUID: `bace5a7b37304e5898f3150a5b28a946`.
Original reader SHA256:
`19ad79886800c852dba12a715b34eddf3407ae2db460e4eb293dba6c7a0cab98`.

Main owns frozen worker inputs and all executions. Stop at fresh expected
five-case XML and a terminal guard receipt. No source correction before the
original causal gate. A candidate should use the existing guarded
CancelPendingInput method so charge, reset and generic hero presentation input
retire together, while committed contact remains preserved. Online pause input
parking is a separate suspected boundary and is not qualified by this fixture.

## Original evidence and candidate

Main's `qa-b/Logs/reader-chat-original5` gives the exact three causal failures:
the pending throw launches, the pending lunge spends cooldown/contact, and a
held chat button begins a charge after typing closes. The committed-contact
and released/fresh-press controls pass. [Fresh original XML](native-original/tests.xml)
and [receipt](native-original/job-receipt.json) retain exit2, terminal state,
preservation completed and no lease held. No fixture repair was required.

Candidate reader SHA256:
`23ded6eb3a6e88b1b3e0c5a7b08dc9e3f75f8f4421629abbef98b5d97f37c169`.
It samples typing beside loading, retires pending actions with the existing
guarded API before Clear/Commit in the withdrawal branch, and invokes the
existing held-button discard on typing exit. The callback retires generic hero
presentation input as well as Carrier/Combat windups, preserving its existing
local-owner guard. No Parked writer, chat UI, transport or input backend changes.

The candidate gate keeps the five unchanged cases and adds existing
ordinary throw and lunge release controls from InputProducerCancellationTests,
seven expected cases total. Main owns snapshots, jobs and preservation checks.
The shared loading branch receives the same withdrawal cancellation but no
loading operator qualification is claimed by these chat-context cases.

Main's `qa-b/Logs/reader-chat-candidate7` passes exactly the five unchanged
chat cases and both ordinary release controls on the first candidate, with no
fixture repair or retry. [Candidate XML](native-candidate/tests.xml) and
[receipt](native-candidate/job-receipt.json) record exit0, terminal state,
preservation completed and no lease held. This is native local touch/reader
and supplied-context acceptance, not physical hardware, chat-field navigation
or actual-peer transport qualification. Main's separate online PausePanel
operator work does not form part of this seven-case result.

import json
import copy
from pathlib import Path
import tempfile
import unittest
from unittest import mock

import run_lan_peer as peer


class ArtifactChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='tump-lan-artifact-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.artifact = self.root / 'player'
        self.artifact.mkdir()
        self.exe = self.artifact / 'TumbangPreso.exe'
        self.exe.write_bytes(b'checked-launcher')
        managed = self.artifact / 'TumbangPreso_Data/Managed'
        managed.mkdir(parents=True)
        self.runtime = managed / 'TumbangPreso.Runtime.dll'
        self.core = managed / 'TumbangPreso.Core.dll'
        self.runtime.write_bytes(b'checked-runtime144')
        self.core.write_bytes(b'checked-core')
        identity = self.artifact / 'TumbangPreso_Data/StreamingAssets/build-identity.json'
        identity.parent.mkdir()
        identity.write_text(json.dumps({'sha': 'a' * 40}))
        files = [{'path': p.relative_to(self.artifact).as_posix(), 'bytes': p.stat().st_size,
                  'sha256': peer.file_sha256(p)} for p in self.artifact.rglob('*') if p.is_file()]
        self.manifest = dict(sourceCommit='a' * 40, protocol=144, fileCount=len(files),
                             strictReceiptPassed=True,
                             totalBytes=sum(p['bytes'] for p in files), files=files,
                             runtimeSha256=peer.file_sha256(self.runtime),
                             coreSha256=peer.file_sha256(self.core), exeSha256=peer.file_sha256(self.exe))
        self.manifest_path = self.root / 'checked-manifest.json'
        self.save()

    def save(self):
        self.manifest_path.write_text(json.dumps(self.manifest))

    def validate(self):
        return peer.checked_artifact(self.exe, self.manifest_path, 144, self.manifest['runtimeSha256'])

    def test_current_protocol_and_relocated_artifact_are_accepted(self):
        self.assertEqual('a' * 40, self.validate()['sourceCommit'])

    def test_legacy_protocol_is_rejected(self):
        self.manifest['protocol'] = 134
        self.save()
        with self.assertRaisesRegex(RuntimeError, 'protocol'):
            self.validate()

    def test_wrong_shared_runtime_pin_is_rejected(self):
        with self.assertRaisesRegex(RuntimeError, 'pinned'):
            peer.checked_artifact(self.exe, self.manifest_path, 144, '0' * 64)

    def test_unaccepted_build_is_rejected_and_classified_artifact_can_pass(self):
        self.manifest['strictReceiptPassed'] = False
        self.save()
        with self.assertRaisesRegex(RuntimeError, 'accepted'):
            self.validate()
        self.manifest['classifiedArtifactAccepted'] = True
        self.save()
        self.validate()

    def test_modified_core_and_launcher_are_rejected(self):
        for path in [self.core, self.exe]:
            original = path.read_bytes()
            path.write_bytes(original + b'changed')
            with self.assertRaisesRegex(RuntimeError, 'Packaged file'):
                self.validate()
            path.write_bytes(original)

    def test_unlisted_packaged_file_is_rejected(self):
        (self.artifact / 'unexpected.dll').write_bytes(b'extra')
        with self.assertRaisesRegex(RuntimeError, 'inventory'):
            self.validate()

    def test_manifest_cannot_read_a_file_outside_the_artifact(self):
        outside = self.root / 'outside.txt'
        outside.write_bytes(b'not-a-player-file')
        self.manifest['files'][0]['path'] = '../outside.txt'
        self.save()
        with self.assertRaisesRegex(RuntimeError, 'unsafe'):
            self.validate()

    def test_packaged_build_identity_must_match_source(self):
        self.manifest['sourceCommit'] = 'b' * 40
        self.save()
        with self.assertRaisesRegex(RuntimeError, 'build identity'):
            self.validate()

    def test_normal_profile_has_explicit_character_choice_and_unchanged_rules(self):
        for role, pick in [('host', 0), ('client', 2)]:
            seed = json.loads(peer.profile_seed('fresh-token', role, pick))
            self.assertEqual(pick, seed['CharacterPick'])
            self.assertEqual(peer.WIRE, seed['CustomRulesWire'])
            self.assertEqual('LAN' + role, seed['PlayerName'])

    def test_current_rules_are_validated_against_the_full_canonical_wire(self):
        wire = peer.WIRE
        self.assertEqual(11, len(wire.split('|')))
        self.assertEqual('0', wire.split('|')[10])
        self.assertEqual('3', wire.split('|')[6], 'Two humans must request filler seats through room rules.')
        answer = json.dumps(dict(wire=wire, rounds=1, seconds=30, manual=True))
        with mock.patch.object(peer.arrival.shutil, 'which', return_value='pwsh'), mock.patch.object(
                peer.arrival.subprocess, 'run', return_value=mock.Mock(stdout=answer)) as run:
            self.assertEqual(wire, peer.arrival.validate_rules(self.core, wire=wire)['wire'])
            self.assertEqual(wire, run.call_args.kwargs['env']['TUMP_COMPLETED_WIRE'])
            self.assertTrue(run.call_args.kwargs['check'])

    def test_old_canonical_output_cannot_masquerade_as_current_rules(self):
        answer = json.dumps(dict(wire=peer.arrival.WIRE, rounds=1, seconds=30, manual=True))
        with mock.patch.object(peer.arrival.shutil, 'which', return_value='pwsh'), mock.patch.object(
                peer.arrival.subprocess, 'run', return_value=mock.Mock(stdout=answer)):
            with self.assertRaisesRegex(RuntimeError, 'parser rejected'):
                peer.arrival.validate_rules(self.core, wire=peer.WIRE)

    def test_prepared_receipt_precedes_wait_and_default_does_not_wait(self):
        ready = self.root / 'prepared.json'
        receipt = peer.wait_for_start(None, ready, {'artifactVerified': True, 'role': 'client'})
        self.assertTrue(json.loads(ready.read_text())['artifactVerified'])
        self.assertIsNotNone(receipt['preparedAtUtc'])
        with self.assertRaises(FileExistsError):
            peer.wait_for_start(None, ready, {'artifactVerified': True})

    def test_coordinated_signal_wait_starts_only_after_ready_is_written(self):
        ready = self.root / 'prepared.json'; signal = self.root / 'start.signal'
        def signal_after_readiness(seconds):
            self.assertTrue(json.loads(ready.read_text())['artifactVerified'])
            signal.write_text('start')
        with mock.patch.object(peer.time, 'sleep', side_effect=signal_after_readiness) as sleep:
            peer.wait_for_start(signal, ready, {'artifactVerified': True})
        sleep.assert_called_once_with(.25)


class HostLossChecks(unittest.TestCase):
    def setUp(self):
        self.report = dict(role='HOST', networked='False', slot='0', protocol='153',
                           map='MatchSetup', round='0', active='False')
        self.log = ('[NetAuto] READY submitted from a client peer.\n'
                    '[Slice] round 1 begins, taya is seat 0\n'
                    '[Abandon] HostLost: ABANDONED at round 1 of 1; '
                    'this peer may no longer resolve anything\n')

    def evaluate(self, cache=None, log=None, report=None, exit_code=0):
        return peer.evaluate_host_loss(self.report if report is None else report,
            self.log if log is None else log, {} if cache is None else cache, 153, exit_code)

    def test_active_loss_without_career_file_is_valid_client_evidence(self):
        evidence, errors = self.evaluate()
        self.assertEqual([], errors)
        self.assertTrue(evidence['admissionBeforeLoss'])

    def test_lobby_loss_or_loss_before_actual_admission_cannot_pass(self):
        for log in [self.log.replace('[Slice] round 1 begins', 'never began'),
                    self.log.replace('at round 1 of 1', 'at round 0 of 0'),
                    '\n'.join(reversed(self.log.splitlines()))]:
            self.assertTrue(self.evaluate(log=log)[1])

    def test_completed_event_or_any_saved_result_cannot_pass(self):
        self.assertTrue(self.evaluate(log=self.log+'[Slice] match over\n')[1])
        for field in ('History', 'Queue', 'QueueWitness'):
            self.assertTrue(self.evaluate(cache={field: [{}]})[1], field)

    def test_replacement_network_host_or_live_arena_cannot_pass(self):
        for change in [dict(networked='True'), dict(active='True'),
                       dict(map='Eskinita'), dict(round='1'), dict(role='CLIENT'),
                       dict(slot='1'), dict(protocol='145')]:
            self.assertTrue(self.evaluate(report=dict(self.report, **change))[1], change)

    def test_client_crash_cannot_pass(self):
        self.assertTrue(self.evaluate(exit_code=1)[1])


class RematchChecks(unittest.TestCase):
    def setUp(self):
        self.history = []
        for identity, court in [('second-match', 'BayanPlaza'), ('first-match', 'Eskinita')]:
            self.history.append(dict(MatchId=identity, MapId=court, Online=True, Rounds=1, Mode='HeroStrike',
                Players=[dict(Slot=s, Score=score, IsBot=s>=2, PlayerId=('own' if s==0 else 'other' if s==1 else 'bot'+str(s)))
                         for s, score in enumerate([100, 0, 150, 0])]))
        self.cache = dict(History=self.history, Queue=copy.deepcopy(list(reversed(self.history))),
                          QueueWitness=['first-witness', 'second-witness'], InMatchSinceUtc='')
        self.report = dict(role='HOST', slot='0', protocol='153', networked='True', mode='HeroStrike',
                           map='BayanPlaza', round='1', active='False',
                           seats=[dict(seat=s, score=score, bot=s>=2, origin='Human' if s<2 else 'Bot')
                                  for s, score in enumerate([100, 0, 150, 0])])
        self.log = ('[NetAuto] READY submitted\n[Slice] match over\n'
                    '[NetAuto] REMATCH vote submitted from the result board.\n'
                    '[NetAuto] READY submitted\n[NetAuto] REMATCH began after the peer vote.\n'
                    '[Slice] match over\n')

    def evaluate(self):
        return peer.evaluate_rematch(self.report, 'selected rules : '+peer.WIRE, self.log,
                                      self.cache, 'own', 'host', 153, 0)

    def test_two_completions_accept_opposite_history_and_queue_order(self):
        evidence, errors = self.evaluate()
        self.assertEqual([], errors)
        self.assertEqual(2, len(evidence['recordSha256ByMatchIdSha256']))

    def test_client_own_seat_is_checked_independently(self):
        self.report.update(role='CLIENT', slot='1')
        self.assertEqual([], peer.evaluate_rematch(self.report, 'selected rules : '+peer.WIRE,
            self.log, self.cache, 'other', 'client', 153, 0)[1])
        self.assertTrue(peer.evaluate_rematch(self.report, 'selected rules : '+peer.WIRE,
            self.log, self.cache, 'own', 'client', 153, 0)[1])

    def test_one_finished_match_or_no_real_rematch_cannot_pass(self):
        self.cache['History'] = self.history[:1]
        self.assertTrue(self.evaluate()[1])
        self.cache['History'] = self.history
        self.log = self.log.replace('[NetAuto] REMATCH began after the peer vote.', '')
        self.assertTrue(self.evaluate()[1])

    def test_reused_identity_or_unchanged_court_cannot_pass(self):
        for field, value in [('MatchId', 'first-match'), ('MapId', 'Eskinita')]:
            original = self.history[0][field]; self.history[0][field] = value
            self.assertTrue(self.evaluate()[1], field)
            self.history[0][field] = original

    def test_changed_queue_row_or_missing_witness_cannot_pass(self):
        self.cache['Queue'][0]['Players'][0]['Score'] += 1
        self.assertTrue(self.evaluate()[1])
        self.cache['Queue'] = copy.deepcopy(list(reversed(self.history)))
        self.cache['QueueWitness'] = ['one-only']
        self.assertTrue(self.evaluate()[1])

    def test_lost_human_identity_or_uncleared_marker_cannot_pass(self):
        self.history[0]['Players'][1]['PlayerId'] = 'different-person'
        self.assertTrue(self.evaluate()[1])
        self.history[0]['Players'][1]['PlayerId'] = 'other'
        self.cache['InMatchSinceUtc'] = 'still-in-progress'
        self.assertTrue(self.evaluate()[1])

    def test_disconnected_terminal_or_wrong_latest_standings_cannot_pass(self):
        self.report['networked'] = 'False'
        self.assertTrue(self.evaluate()[1])
        self.report['networked'] = 'True'; self.report['seats'][0]['score'] = 0
        self.assertTrue(self.evaluate()[1])


if __name__ == '__main__':
    unittest.main()

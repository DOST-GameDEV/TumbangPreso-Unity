import json
from pathlib import Path
import tempfile
import unittest

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


if __name__ == '__main__':
    unittest.main()

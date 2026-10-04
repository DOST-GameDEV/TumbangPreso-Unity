"""Matrix cleanup restores owned files and input values without touching a real registry."""
import json
from pathlib import Path
import tempfile
import types
import unittest
from unittest.mock import patch

import net_matrix
import playerprefs_guard


class MatrixPreservationTests(unittest.TestCase):
    def run_case(self, fail):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            profile = root / 'profiles/mtxhost'
            profile.mkdir(parents=True)
            cache = profile / 'cache.json'
            cache.write_bytes(b'original profile')
            out = root / 'evidence'
            out.mkdir()
            binding = 'tumbangpreso.bindings_h123'
            touch = 'tumbangpreso.touchlayout_h456'
            saved = playerprefs_guard.encode(b'original binding\0', 3)
            registry = {binding: saved, 'volume': playerprefs_guard.encode(73, 4)}
            calls = []
            def read():
                return {key: value for key, value in registry.items() if playerprefs_guard.allowed(key)}
            def restore(before):
                calls.append(before)
                playerprefs_guard.restore_values(before, read,
                    lambda key, value: registry.__setitem__(key, value), lambda key: registry.pop(key))
            modules = {
                'net_request_safety': types.SimpleNamespace(player_data_root=lambda: root / 'profiles',
                    named_profile=lambda data, name: data / name),
                'run_ui_player_review': types.SimpleNamespace(read_input_preferences=read),
                'run_completed_arrival': types.SimpleNamespace(restore_input=restore),
            }
            def exercise():
                with net_matrix.preserve_matrix_profiles(out):
                    cache.write_bytes(b'test changed profile')
                    registry[binding] = playerprefs_guard.encode(b'changed binding', 3)
                    registry[touch] = playerprefs_guard.encode(b'new layout', 3)
                    if fail:
                        raise RuntimeError('owned scenario failed')
            with patch.dict('sys.modules', modules), patch.object(net_matrix, 'os', types.SimpleNamespace(name='nt')):
                if fail:
                    with self.assertRaisesRegex(RuntimeError, 'owned scenario failed'):
                        exercise()
                else:
                    exercise()
            self.assertEqual(cache.read_bytes(), b'original profile')
            self.assertEqual(registry, {binding: saved, 'volume': playerprefs_guard.encode(73, 4)})
            self.assertEqual(len(calls), 1)
            report = json.loads((out / 'profile-preservation.json').read_text())
            self.assertTrue(report['sharedInputWasChanged'])
            self.assertTrue(report['sharedInputRestored'])

    def test_success_restores_changed_bindings_and_absent_layout(self):
        self.run_case(False)

    def test_failed_scenario_still_restores_owned_files_and_input(self):
        self.run_case(True)


if __name__ == '__main__':
    unittest.main()

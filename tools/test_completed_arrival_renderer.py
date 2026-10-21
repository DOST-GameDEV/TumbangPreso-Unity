"""Do not mistake a command-line intention for the backend the player used."""
import unittest
from run_completed_arrival import observed_renderer


class RendererEvidenceTests(unittest.TestCase):
    def test_actual_version_line_is_required(self):
        self.assertIsNone(observed_renderer('Forcing GfxDevice: Direct3D 11'))
        self.assertIsNone(observed_renderer(''))

    def test_direct3d11_version_is_observed(self):
        self.assertEqual('Direct3D11', observed_renderer('Direct3D:\n    Version:  Direct3D 11.0 [level 11.1]'))

    def test_direct3d12_is_not_mislabeled_as_11(self):
        self.assertEqual('Direct3D12', observed_renderer('Version: Direct3D 12.0 [level 12.2]'))


if __name__ == '__main__':
    unittest.main()

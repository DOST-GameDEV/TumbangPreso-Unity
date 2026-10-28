import tempfile
import unittest
from pathlib import Path
import build_windows as build


class PlayerArtifactsTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.output=Path(self.temp.name)
        for name in ('TumbangPreso.exe','UnityPlayer.dll','TumbangPreso_Data/Managed/TumbangPreso.Runtime.dll'):
            path=self.output/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(b'player')
        self.data=self.output/'TumbangPreso_Data'

    def test_accepts_unpacked_player_data(self):
        data=self.data/'globalgamemanagers';data.write_bytes(b'serialized')
        self.assertIn(data,build.player_artifacts(self.output))

    def test_accepts_unity_compressed_player_data(self):
        data=self.data/'data.unity3d';data.write_bytes(b'UnityFS\0archive')
        self.assertIn(data,build.player_artifacts(self.output))

    def test_rejects_invalid_compressed_archive(self):
        (self.data/'data.unity3d').write_bytes(b'broken')
        with self.assertRaisesRegex(RuntimeError,'Invalid compressed'):
            build.player_artifacts(self.output)

    def test_rejects_missing_game_runtime_even_with_valid_archive(self):
        (self.data/'data.unity3d').write_bytes(b'UnityFS\0archive')
        (self.data/'Managed/TumbangPreso.Runtime.dll').unlink()
        with self.assertRaisesRegex(RuntimeError,'TumbangPreso.Runtime.dll'):
            build.player_artifacts(self.output)

    def test_rejects_missing_serialized_data(self):
        with self.assertRaisesRegex(RuntimeError,'Incomplete player'):
            build.player_artifacts(self.output)


if __name__=='__main__':unittest.main()

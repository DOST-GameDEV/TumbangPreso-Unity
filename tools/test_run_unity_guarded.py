"""File isolation regression; no Unity or real player profile is opened."""
import hashlib
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import run_unity_guarded as guard


class ProfileIsolationTests(unittest.TestCase):
    def setUp(self):
        reader=patch.object(guard.playerprefs_guard,'read_editor',return_value={})
        writer=patch.object(guard.playerprefs_guard,'restore_editor')
        reader.start();writer.start();self.addCleanup(reader.stop);self.addCleanup(writer.stop)

    def test_named_profile_uses_runtime_hash_and_trimming(self):
        expected=hashlib.sha256(b"map-review").hexdigest()
        self.assertEqual(guard.profile_root(["-TP-PROFILE"," map-review "]),
                         guard.PROFILE/"profiles"/expected)
        self.assertEqual(guard.profile_root(["-profile","map-review"]),
                         guard.PROFILE/"profiles"/expected)
        self.assertEqual(guard.profile_root(["-batchmode"]),guard.PROFILE)

    def test_missing_common_profile_alias_uses_existing_local_programdata(self):
        with tempfile.TemporaryDirectory() as temp:
            source={"PROGRAMDATA":temp,"KEEP":"unchanged"}
            result=guard.unity_environment(source)
            self.assertEqual(result["ALLUSERSPROFILE"],temp)
            self.assertEqual(result["KEEP"],"unchanged")
            self.assertNotIn("ALLUSERSPROFILE",source)

    def test_common_profile_repair_preserves_explicit_values_and_refuses_missing_paths(self):
        with tempfile.TemporaryDirectory() as temp:
            source={"ProgramData":temp,"ALLUSERSPROFILE":"explicit-profile"}
            self.assertEqual(guard.unity_environment(source),source)
            self.assertEqual(guard.unity_environment({"PROGRAMDATA":str(Path(temp)/"absent")}),
                             {"PROGRAMDATA":str(Path(temp)/"absent")})
            self.assertEqual(guard.unity_environment({}),{})

    def test_malformed_flags_cannot_fall_back_to_real_profile(self):
        for args in (["-tp-profile"],["-profile"," "],["-profile","-batchmode"],
                     ["-profile","one","-tp-profile","two"]):
            with self.subTest(args=args),self.assertRaises(ValueError):
                guard.profile_root(args)

    def test_named_restore_preserves_concurrent_main_save_even_on_failure(self):
        for exit_code in (0,1):
            with self.subTest(exit_code=exit_code),tempfile.TemporaryDirectory() as temp:
                root=Path(temp)
                with patch.object(guard,"ROOT",root),patch.object(guard,"PROFILE",root/"player"):
                    main=guard.PROFILE/"settings.json"
                    named=guard.profile_root(["-tp-profile","authoring"])/"settings.json"
                    named.parent.mkdir(parents=True)
                    main.write_bytes(b"original player")
                    named.write_bytes(b"original editor")
                    def simulated_unity(*args,**kwargs):
                        main.write_bytes(b"owner saved during test")
                        named.write_bytes(b"test changed editor")
                        return type("Result",(),{"returncode":exit_code})()
                    with patch.object(guard.subprocess,"run",side_effect=simulated_unity):
                        self.assertEqual(guard.run(["-tp-profile","authoring"]),exit_code)
                    self.assertEqual(main.read_bytes(),b"owner saved during test")
                    self.assertEqual(named.read_bytes(),b"original editor")

    def test_default_guard_still_restores_existing_main_files_on_exception(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp)
            with patch.object(guard,"ROOT",root),patch.object(guard,"PROFILE",root/"player"):
                save=guard.PROFILE/"save.json"
                save.parent.mkdir()
                save.write_bytes(b"original")
                def failing_unity(*args,**kwargs):
                    save.write_bytes(b"changed")
                    raise OSError("launch failed")
                with patch.object(guard.subprocess,"run",side_effect=failing_unity):
                    with self.assertRaises(OSError):guard.run([])
                self.assertEqual(save.read_bytes(),b"original")



class MachineResolutionTests(unittest.TestCase):
    """The guard on each machine: the Windows PC, the Mac, and the Linux cloud container."""

    def test_explicit_editor_path_wins_on_every_platform(self):
        for platform in ("win32","darwin","linux"):
            with self.subTest(platform=platform):
                self.assertEqual(guard.unity_executable({"UNITY_EDITOR_PATH":"/x/Unity"},platform),Path("/x/Unity"))
        self.assertEqual(guard.unity_executable({},"linux"),Path("/opt/tump/unity/Editor/Unity"))
        self.assertTrue(str(guard.unity_executable({},"win32")).endswith("Unity.exe"))

    def test_linux_profile_follows_xdg_config_home(self):
        self.assertEqual(guard.player_profile({"XDG_CONFIG_HOME":"/c"},"linux"),
                         Path("/c/unity3d/BH Studios/Tumbang Preso"))
        self.assertEqual(guard.player_profile({"USERPROFILE":"/u"},"win32"),
                         Path("/u/AppData/LocalLow/BH Studios/Tumbang Preso"))

    def test_headless_linux_gets_a_virtual_display_and_a_linux_target(self):
        command=guard.launch_command("/u/Unity",["-batchmode","-runTests"],{},"linux",has_xvfb=True)
        self.assertEqual(command[:4],["xvfb-run","-a","-s","-screen 0 1920x1080x24"])
        self.assertEqual(command[-2:],["-buildTarget","Linux64"])

    def test_nographics_a_display_or_an_explicit_target_are_left_alone(self):
        self.assertEqual(guard.launch_command("/u/Unity",["-nographics"],{},"linux",has_xvfb=True)[0],"/u/Unity")
        self.assertEqual(guard.launch_command("/u/Unity",[],{"DISPLAY":":1"},"linux",has_xvfb=True)[0],"/u/Unity")
        command=guard.launch_command("/u/Unity",["-buildTarget","Win64"],{"DISPLAY":":1"},"linux",has_xvfb=True)
        self.assertEqual(command.count("-buildTarget"),1)
        self.assertEqual(guard.launch_command("/u/Unity.exe",["-batchmode"],{},"win32"),
                         ["/u/Unity.exe","-projectPath",str(guard.ROOT),"-batchmode"])


if __name__=="__main__":unittest.main()

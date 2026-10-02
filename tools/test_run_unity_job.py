"""Lease/admission/process safety tests. No Unity executable or real profile is launched."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

import run_unity_job as job

CPU = ["-batchmode", "-nographics", "-runTests", "-testPlatform", "EditMode"]


class JobTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="tump-job-test-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.project("source", "BH Studios", "Tumbang Preso")
        self.first = self.project("first", "TUMP Validation", "Worker First", worker=True)
        self.second = self.project("second", "TUMP Validation", "Worker Second", worker=True)
        self.pool = self.root / "pool"

    def project(self, name, company, product, worker=False):
        root = self.root / name
        (root / "ProjectSettings").mkdir(parents=True)
        (root / "tools").mkdir()
        (root / "ProjectSettings/ProjectSettings.asset").write_text(f"PlayerSettings:\n  companyName: {company}\n  productName: {product}\n", encoding="utf-8")
        (root / "tools/run_unity_guarded.py").write_text("PROJECT_IDENTITY_GUARD_VERSION = 1\n", encoding="utf-8")
        if worker:
            (root / ".tump-validation-worker.json").write_text(json.dumps({
                "schemaVersion": 1, "workerId": name, "validationOnly": True,
                "projectRoot": str(root), "sourceProject": str(self.source),
                "companyName": company, "productName": product}), encoding="utf-8")
        return root

    def claim(self, project=None, kind="cpu", profile="one", ports=(), args=None):
        return job.make_claim(project or self.first, kind, 512, 2048, profile, ports, CPU if args is None else args, allow_parallel=True)

    def options(self, output="output"):
        return argparse.Namespace(project=str(self.first), output=str(self.root / output), kind="cpu",
            memory_mb=512, reserve_mb=2048, profile="isolated-one", ports="", unity_args=CPU,
            wait_seconds=0, timeout_seconds=1)

    def runtime(self, child):
        return patch.multiple(job, POOL=self.pool, free_memory_mb=Mock(return_value=8192),
                              processes=Mock(return_value=[]), pid_alive=Mock(return_value=True)), \
               patch.object(job.subprocess, "Popen", return_value=child)

    def test_marker_must_match_actual_project_identity(self):
        claim = self.claim()
        self.assertTrue(claim["worker"])
        self.assertNotEqual(claim["prefHive"], self.claim(self.second, profile="two")["prefHive"])
        marker = self.first / ".tump-validation-worker.json"
        data = json.loads(marker.read_text()); data["productName"] = "forged"
        marker.write_text(json.dumps(data))
        with self.assertRaisesRegex(ValueError, "isolation"):
            self.claim()

    def test_old_or_nonliteral_worker_guard_capability_is_rejected(self):
        target = self.first / "tools/run_unity_guarded.py"
        for source in ("# old hardcoded guard\n", "PROJECT_IDENTITY_GUARD_VERSION = 0\n",
                       "PROJECT_IDENTITY_GUARD_VERSION = True\n",
                       '"""PROJECT_IDENTITY_GUARD_VERSION = 1"""\n',
                       "PROJECT_IDENTITY_GUARD_VERSION = 1\nPROJECT_IDENTITY_GUARD_VERSION = 0\n"):
            with self.subTest(source=source):
                target.write_text(source, encoding="utf-8")
                with self.assertRaisesRegex(ValueError, "PROJECT_IDENTITY_GUARD_VERSION"):
                    self.claim()

    def test_missing_actual_project_and_nonpositive_budgets_are_rejected(self):
        (self.first / "ProjectSettings/ProjectSettings.asset").unlink()
        with self.assertRaisesRegex(ValueError, "existing Unity project"):
            self.claim()
        for memory, reserve, profile in ((0, 2048, "x"), (512, 0, "x"), (512, 2048, " ")):
            with self.assertRaises(ValueError):
                job.make_claim(self.source, "cpu", memory, reserve, profile, [], CPU)

    def test_worker_cannot_reuse_source_product_or_physical_library(self):
        marker = self.first / ".tump-validation-worker.json"
        with patch.object(job.guard, "project_identity", return_value=("Same", "Same")):
            with self.assertRaisesRegex(ValueError, "isolation"):
                self.claim()
        original = job.canonical
        with patch.object(job, "canonical", side_effect=lambda path: "same-library" if Path(path).name == "Library" else original(path)):
            with self.assertRaisesRegex(ValueError, "isolation"):
                self.claim()

    def test_cpu_cannot_hide_playmode_execute_method_or_build(self):
        for args in (CPU[:-1] + ["PlayMode"], CPU + ["-executeMethod", "Render.Capture"],
                     CPU + ["-buildTarget", "Win64"], CPU + ["-tp-uireview"], ["-runTests", "-testPlatform", "EditMode"]):
            with self.subTest(args=args), self.assertRaisesRegex(ValueError, "exclusive"):
                self.claim(args=args)
        self.assertEqual("build", job.classify(CPU + ["-executeMethod", "GameBuilder.BuildWindows"]))
        self.assertEqual("gpu", job.classify(CPU[:-1] + ["PlayMode"]))

    def test_validation_only_worker_cannot_produce_shipping_build(self):
        with self.assertRaisesRegex(ValueError, "shipping builds"):
            self.claim(kind="build", args=["-batchmode", "-executeMethod", "GameBuilder.BuildWindows"])
        with self.assertRaisesRegex(ValueError, "shipping builds"):
            self.claim(kind="build")
        self.assertEqual("build", self.claim(self.source, kind="build", args=["-batchmode", "-executeMethod", "GameBuilder.BuildWindows"])["kind"])

    def test_runner_owns_project_and_profile_arguments(self):
        for flag in ("-tp-profile", "-PROFILE", "-projectPath"):
            with self.assertRaisesRegex(ValueError, "owns"):
                self.claim(args=CPU + [flag, "other"])

    def test_collision_checks_project_cache_profile_preferences_and_ports(self):
        first = self.claim(ports=(7777,))
        other = self.claim(self.second, profile="two", ports=(7778,))
        self.assertIsNone(job.refusal(other, [first], 8192, []))
        for key in ("project", "library", "profile", "prefHive"):
            changed = dict(other, **{key: first[key]})
            self.assertIn("already leased", job.refusal(changed, [first], 8192, []))
        self.assertIn("port", job.refusal(dict(other, ports=[7777]), [first], 8192, []))

    def test_cpu_overlap_requires_two_isolated_workers_and_caps_at_two(self):
        first = self.claim()
        second = self.claim(self.second, profile="two")
        self.assertIn("isolated", job.refusal(dict(second, worker=False), [first], 8192, []))
        self.assertIn("isolated", job.refusal(second, [dict(first, worker=False)], 8192, []))
        third = dict(second, id="third", project="third", library="third-lib", profile="three", prefHive="third-hive")
        self.assertIn("Two CPU", job.refusal(third, [first, second], 8192, []))
        self.assertIsNone(job.refusal(dict(first, worker=False), [], 8192, []))

    def test_default_is_serialized_even_with_distinct_worker_identities(self):
        first = self.claim(); second = self.claim(self.second, profile="two")
        self.assertIn("--allow-parallel", job.refusal(dict(second, allowParallel=False), [first], 8192, []))
        self.assertIn("--allow-parallel", job.refusal(second, [dict(first, allowParallel=False)], 8192, []))

    def test_gpu_build_and_pending_restoration_are_exclusive(self):
        first = self.claim(); second = self.claim(self.second, profile="two")
        for kind in ("gpu", "build"):
            self.assertIn("exclusive", job.refusal(dict(second, kind=kind), [first], 8192, []))
            self.assertIn("exclusive", job.refusal(second, [dict(first, kind=kind)], 8192, []))
        self.assertIn("restoration", job.refusal(second, [dict(first, awaitingRestoration=True)], 8192, []))

    def test_memory_reserve_and_foreign_unity_refuse_admission(self):
        first = self.claim(); second = self.claim(self.second, profile="two")
        self.assertIn("memory", job.refusal(first, [], 2559, []))
        self.assertIn("memory", job.refusal(second, [first], 3071, []))
        self.assertIn("outside", job.refusal(first, [], 8192, [333]))

    def test_stale_cleanup_requires_every_recorded_process_verified_dead(self):
        first = dict(self.claim(), ownerPid=10, guardPid=20, unityPids=[30])
        self.assertEqual([], job.live_leases([first], alive=lambda pid: False))
        for live in (10, 20, 30):
            self.assertEqual([first], job.live_leases([first], alive=lambda pid: pid == live))
        self.pool.mkdir()
        (self.pool / "leases.json").write_text(json.dumps({"version": 1, "jobs": [{"ownerPid": -1}]}))
        with self.assertRaisesRegex(ValueError, "cannot be verified"):
            job.read_pool(self.pool)

    def test_foreign_inventory_recognizes_guard_and_editor_descendants(self):
        lease = dict(self.claim(), ownerPid=10, guardPid=20)
        rows = [{"pid": 20, "parent": 10, "name": "python.exe"},
                {"pid": 30, "parent": 20, "name": "Unity.exe"},
                {"pid": 40, "parent": 30, "name": "Unity.exe"},
                {"pid": 50, "parent": 999, "name": "Unity.exe"}]
        self.assertEqual([50], job.foreign_unity(rows, [lease]))

    def test_foreign_standalone_game_player_blocks_the_pool(self):
        first = self.claim()
        for name in ("TumbangPreso.exe", "TumbangPreso"):
            foreign = job.foreign_unity([{"pid": 500, "parent": 999, "name": name}], [])
            self.assertEqual([500], foreign)
            self.assertIn("outside", job.refusal(first, [], 8192, foreign))

    def test_acquire_persists_two_claims_and_refuses_third_without_launch(self):
        first = self.claim(); second = self.claim(self.second, profile="two")
        with patch.object(job, "pid_alive", return_value=True), patch.object(job, "processes", return_value=[]), patch.object(job, "free_memory_mb", return_value=8192):
            self.assertEqual(0, job.acquire(self.pool, first, 0)["activeJobsBefore"])
            self.assertEqual(1, job.acquire(self.pool, second, 0)["activeJobsBefore"])
            third = dict(second, id="third", project="third", library="third-lib", profile="three", prefHive="third-hive")
            with self.assertRaisesRegex(TimeoutError, "Two CPU"):
                job.acquire(self.pool, third, 0)
        self.assertEqual(2, len(job.read_pool(self.pool)))

    def test_output_arguments_cannot_escape_dedicated_directory(self):
        output = self.root / "output"
        args = job.output_arguments(self.first, output, CPU)
        self.assertIn(str(output / "tests.xml"), args)
        with self.assertRaisesRegex(ValueError, "dedicated"):
            job.output_arguments(self.first, output, CPU + ["-logFile", "outside.log"])

    def test_success_delegates_once_then_releases_lease_with_receipt(self):
        child = Mock(pid=999001); child.wait.return_value = 0; child.poll.return_value = 0
        runtime, launch = self.runtime(child)
        with runtime, launch as popen:
            self.assertEqual(0, job.run(self.options()))
        command = popen.call_args.args[0]
        self.assertIn(str(self.first / "tools/run_unity_guarded.py"), command)
        self.assertEqual(["-tp-profile", "isolated-one"], command[-2:])
        receipt = json.loads((self.root / "output/job-receipt.json").read_text())
        self.assertEqual("completed", receipt["status"])
        self.assertTrue(receipt["guardTerminal"])
        self.assertEqual([], job.read_pool(self.pool))
        child.terminate.assert_not_called()

    def test_timeout_waits_for_guard_cleanup_and_does_not_terminate_guard(self):
        child = Mock(pid=999001); child.wait.side_effect = [subprocess.TimeoutExpired("guard", 1), 2]; child.poll.return_value = 2
        runtime, launch = self.runtime(child)
        with runtime, launch, patch.object(job, "stop_owned_editors", return_value=[999002]) as stop:
            self.assertEqual(124, job.run(self.options()))
        receipt = json.loads((self.root / "output/job-receipt.json").read_text())
        self.assertEqual("timeout_guard_completed", receipt["status"])
        self.assertEqual(2, receipt["guardExitCode"])
        self.assertEqual([999002], receipt["stoppedUnityPids"])
        self.assertEqual([], job.read_pool(self.pool))
        stop.assert_called_once_with(child); child.terminate.assert_not_called()

    def test_unresolved_timeout_keeps_exclusive_restoration_lease(self):
        child = Mock(pid=999001); child.wait.side_effect = subprocess.TimeoutExpired("guard", 1); child.poll.return_value = None
        runtime, launch = self.runtime(child)
        with runtime, launch, patch.object(job, "stop_owned_editors", return_value=[]):
            self.assertEqual(124, job.run(self.options()))
        receipt = json.loads((self.root / "output/job-receipt.json").read_text())
        self.assertFalse(receipt["guardTerminal"])
        self.assertTrue(job.read_pool(self.pool)[0]["awaitingRestoration"])
        child.terminate.assert_not_called()

    def test_launch_failure_releases_claim_and_existing_receipt_is_not_overwritten(self):
        with patch.object(job, "POOL", self.pool), patch.object(job, "processes", return_value=[]), patch.object(job, "free_memory_mb", return_value=8192), patch.object(job.subprocess, "Popen", side_effect=OSError("launch refused")):
            self.assertEqual(1, job.run(self.options()))
            self.assertEqual([], job.read_pool(self.pool))
            with self.assertRaises(FileExistsError):
                job.run(self.options())

    @unittest.skipUnless(os.name == "nt", "Windows process handles")
    def test_windows_process_inventory_and_verified_dead_child(self):
        self.assertTrue(job.pid_alive(os.getpid()))
        self.assertIn(os.getpid(), [row["pid"] for row in job.processes()])
        child = subprocess.Popen([sys.executable, "-c", "pass"])
        child.wait(timeout=10)
        self.assertFalse(job.pid_alive(child.pid))


if __name__ == "__main__":
    unittest.main()

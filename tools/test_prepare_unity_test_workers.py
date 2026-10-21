"""Small temporary-file tests. No real Git worktree, Editor, or project cache is opened."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import prepare_unity_test_workers as workers

COMMIT = "a" * 40
SETTINGS = (b"%YAML 1.1\r\nPlayerSettings:\r\n  companyName: BH Studios\r\n"
            b"  productName: Tumbang Preso\r\n  bundleVersion: 1.0.0\r\n")
VERSION = b"m_EditorVersion: 6000.5.8f1\nm_EditorVersionWithRevision: 6000.5.8f1 (revision)\n"
GUARD = b"PROJECT_IDENTITY_GUARD_VERSION = 1\n"


class PrepareWorkerTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.repo = self.root / "source"
        (self.repo / "ProjectSettings").mkdir(parents=True)
        (self.repo / workers.SETTINGS).write_bytes(SETTINGS)
        (self.repo / workers.VERSION).write_bytes(VERSION)
        self.calls = []
        for name, replacement in (("git", self.fake_git), ("unity_processes", lambda: []),
                                  ("progress", lambda *args, **kwargs: None)):
            mock = patch.object(workers, name, replacement)
            mock.start()
            self.addCleanup(mock.stop)
        disk = patch.object(workers.shutil, "disk_usage",
                            return_value=type("Disk", (), {"free": 100 * 1024 ** 3})())
        disk.start()
        self.addCleanup(disk.stop)

    def arguments(self, **changes):
        values = dict(repo=self.repo, ref=COMMIT, destination=self.root / "workers",
                      workers=["worker-a", "worker-b"], cache_from=None,
                      copy_library=False, progress_every=2)
        values.update(changes)
        return argparse.Namespace(**values)

    def fake_git(self, repo, *args):
        self.calls.append((Path(repo), args))
        if args[:2] == ("rev-parse", "--show-toplevel"):
            return str(Path(repo)).encode()
        if args[0] == "rev-parse":
            return COMMIT.encode()
        if args[0] == "show":
            if args[1].endswith(workers.GUARD):
                return GUARD
            return SETTINGS if args[1].endswith(workers.SETTINGS) else VERSION
        if args[0] == "ls-tree":
            return b"100644 blob " + COMMIT.encode() + b" 100\tfile.txt\0"
        if args[:2] == ("worktree", "add"):
            # Simulates checkout bytes only; it never invokes Git or registers a worktree.
            target = Path(args[3])
            (target / "ProjectSettings").mkdir(parents=True)
            (target / workers.SETTINGS).write_bytes(SETTINGS)
            (target / workers.VERSION).write_bytes(VERSION)
            (target / "tools").mkdir()
            (target / workers.GUARD).write_bytes(GUARD)
            return b""
        if args[0] == "diff":
            return (workers.SETTINGS + "\n").encode()
        if args[0] == "status":
            return b""
        raise AssertionError(args)

    def cache(self):
        root = self.root / "idle cache"
        (root / "ProjectSettings").mkdir(parents=True)
        (root / workers.VERSION).write_bytes(VERSION)
        library = root / "Library"
        library.mkdir()
        (library / "SourceAssetDB").write_bytes(b"source-db")
        (library / "ArtifactDB").write_bytes(b"artifact-db")
        (library / "ArtifactDB-lock").write_bytes(b"leave original lock alone")
        (library / "EditorInstance.json").write_text('{"version":"6000.5.8f1","process_id":123}')
        (library / "Temp").mkdir()
        (library / "Temp/old-file").write_bytes(b"do not copy")
        return root

    def test_identity_change_preserves_every_other_byte(self):
        changed = workers.replace_identity(SETTINGS, workers.COMPANY, "Worker-A")
        restored = workers.replace_identity(changed, "BH Studios", "Tumbang Preso")
        self.assertEqual(restored, SETTINGS)
        self.assertEqual(workers.identity(changed),
                         {"companyName": workers.COMPANY, "productName": "Worker-A"})

    def test_two_workers_are_frozen_unique_and_do_not_change_source(self):
        result = workers.prepare(self.arguments())
        self.assertTrue(result["ok"], result)
        self.assertEqual((self.repo / workers.SETTINGS).read_bytes(), SETTINGS)
        self.assertFalse(result["editorStarted"])
        self.assertEqual(len(result["workers"]), 2)
        products = set()
        for row in result["workers"]:
            path = Path(row["projectRoot"])
            marker = json.loads((path / workers.MARKER).read_text())
            self.assertEqual(marker["sourceCommit"], COMMIT)
            self.assertEqual(marker["guardVersion"], 1)
            self.assertEqual(marker["guardSha256"], workers.digest(path / workers.GUARD))
            self.assertEqual(marker["sourceProject"], str(self.repo))
            self.assertEqual(marker["state"], "ready")
            self.assertTrue(marker["validationOnly"])
            self.assertFalse(marker["shippingBuildsAllowed"])
            self.assertTrue(marker["warmupRequired"])
            self.assertEqual(marker["editorValidation"], "not_run")
            self.assertEqual(marker["libraryPath"], str(path / "Library"))
            self.assertTrue((path / "Library").is_dir())
            self.assertFalse((path / "Library").is_symlink())
            products.add(marker["productName"])
        self.assertEqual(len(products), 2)
        self.assertEqual(len([args for _, args in self.calls if args[:2] == ("worktree", "add")]), 2)

    def test_existing_worker_is_never_reused_even_if_only_a_private_file_exists(self):
        target = self.root / "workers/worker-a"
        target.mkdir(parents=True)
        note = target / "unfinished.txt"
        note.write_text("keep me")
        result = workers.prepare(self.arguments())
        self.assertFalse(result["ok"])
        self.assertIn("already exists", result["error"])
        self.assertEqual(note.read_text(), "keep me")
        self.assertFalse(any(args[:2] == ("worktree", "add") for _, args in self.calls))

    def test_old_or_unrecognized_guard_is_refused_before_worker_creation(self):
        for guard in (b"PROFILE = MAIN_PROFILE\n", b"PROJECT_IDENTITY_GUARD_VERSION = 10\n"):
            with self.subTest(guard=guard):
                self.calls.clear()
                def old_guard(repo, *args):
                    if args[0] == "show" and args[1].endswith(workers.GUARD):
                        return guard
                    return self.fake_git(repo, *args)
                with patch.object(workers, "git", side_effect=old_guard):
                    result = workers.prepare(self.arguments())
                self.assertFalse(result["ok"])
                self.assertIn("guard version 1", result["error"])
                self.assertFalse(any(args[:2] == ("worktree", "add") for _, args in self.calls))

    def test_moving_ref_unsafe_names_and_nested_destinations_fail_before_mutation(self):
        for changes in ({"ref": "ASTRAReworks"}, {"workers": ["../escape", "worker-b"]},
                        {"workers": ["worker", "WORKER"]}, {"workers": ["NUL", "worker-b"]},
                        {"destination": self.repo / "nested"}, {"copy_library": True}):
            with self.subTest(changes=changes):
                self.calls.clear()
                result = workers.prepare(self.arguments(**changes))
                self.assertFalse(result["ok"], result)
                self.assertFalse(any(args[:2] == ("worktree", "add") for _, args in self.calls))

    def test_insufficient_disk_fails_without_any_worker(self):
        with patch.object(workers.shutil, "disk_usage", return_value=type("Disk", (), {"free": 1})()):
            result = workers.prepare(self.arguments())
        self.assertFalse(result["ok"])
        self.assertIn("Insufficient free disk", result["error"])
        self.assertFalse((self.root / "workers").exists())

    def test_cache_requires_opt_in_and_never_copies_locks_or_temporary_files(self):
        cache = self.cache()
        with patch.object(workers, "copy_library", wraps=workers.copy_library) as copy:
            result = workers.prepare(self.arguments(cache_from=cache))
            self.assertTrue(result["ok"], result)
            copy.assert_not_called()
        result = workers.prepare(self.arguments(destination=self.root / "copied",
                                                cache_from=cache, copy_library=True))
        self.assertTrue(result["ok"], result)
        for row in result["workers"]:
            library = Path(row["libraryPath"])
            self.assertEqual((library / "SourceAssetDB").read_bytes(), b"source-db")
            self.assertEqual((library / "ArtifactDB").read_bytes(), b"artifact-db")
            self.assertFalse((library / "ArtifactDB-lock").exists())
            self.assertFalse((library / "EditorInstance.json").exists())
            self.assertFalse((library / "Temp").exists())
            self.assertFalse(os.path.samefile(library / "SourceAssetDB", cache / "Library/SourceAssetDB"))
        first = Path(result["workers"][0]["libraryPath"])
        (first / "SourceAssetDB").write_bytes(b"worker-only")
        self.assertEqual((cache / "Library/SourceAssetDB").read_bytes(), b"source-db")
        self.assertEqual((cache / "Library/ArtifactDB-lock").read_bytes(), b"leave original lock alone")

    def test_matching_active_project_and_unknown_unity_are_refused(self):
        cache = self.cache()
        command = subprocess.list2cmdline(["Unity.exe", "-projectPath", str(cache)])
        with self.assertRaisesRegex(RuntimeError, "open in Unity PID 123"):
            workers.assert_idle(cache, [(123, command)])
        command = subprocess.list2cmdline(["Unity.exe", "-projectPath", str(cache) + "-other"])
        workers.assert_idle(cache, [(456, command)])
        for command in (None, "Unity.exe -batchmode"):
            with self.subTest(command=command), self.assertRaises(RuntimeError):
                workers.assert_idle(cache, [(789, command)])

    def test_cache_version_mismatch_and_open_database_fail_before_checkout(self):
        cache = self.cache()
        (cache / workers.VERSION).write_bytes(b"m_EditorVersion: 2022.3.1f1\n")
        result = workers.prepare(self.arguments(cache_from=cache, copy_library=True))
        self.assertFalse(result["ok"])
        self.assertIn("Editor version", result["error"])
        (cache / workers.VERSION).write_bytes(VERSION)
        with patch.object(workers, "assert_closed_database", side_effect=RuntimeError("database open")):
            result = workers.prepare(self.arguments(cache_from=cache, copy_library=True))
        self.assertFalse(result["ok"])
        self.assertIn("database open", result["error"])
        self.assertFalse(any(args[:2] == ("worktree", "add") for _, args in self.calls))

    def test_changed_cache_is_rejected_and_partial_workers_are_preserved(self):
        cache = self.cache()
        inventory = workers.cache_inventory(cache / "Library")
        (cache / "Library/SourceAssetDB").write_bytes(b"changed-before-copy")
        with self.assertRaisesRegex(RuntimeError, "changed since preflight"):
            workers.copy_library(cache, self.root / "unused", inventory, 2)
        with patch.object(workers, "copy_library", side_effect=RuntimeError("copy interrupted")):
            result = workers.prepare(self.arguments(cache_from=cache, copy_library=True))
        self.assertFalse(result["ok"])
        self.assertTrue(result["partialWorktreesPreserved"])
        path = Path(result["workers"][0]["projectRoot"])
        self.assertTrue(path.exists())
        self.assertEqual(json.loads((path / workers.MARKER).read_text())["state"], "failed")
        self.assertEqual((self.repo / workers.SETTINGS).read_bytes(), SETTINGS)


if __name__ == "__main__":
    unittest.main()

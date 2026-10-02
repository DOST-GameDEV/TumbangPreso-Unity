"""Prepare two detached validation-only worktrees. This never starts Unity.

Library copying is opt-in, from an idle checkout with the same Editor version.
Copies are ordinary independent files, never links. Existing worker directories
are refused, including leftovers from a failed run; nothing is reset or deleted.
Progress is JSON on stderr; stdout is one JSON receipt, including partial failures.
"""
import argparse
import ctypes
import hashlib
import json
import os
from pathlib import Path
import re
import shlex
import shutil
import stat
import subprocess
import sys
import time
import uuid

ROOT = Path(__file__).resolve().parents[1]
MARKER = ".tump-validation-worker.json"
COMPANY = "BH Studios Validation"
SETTINGS = "ProjectSettings/ProjectSettings.asset"
VERSION = "ProjectSettings/ProjectVersion.txt"
GUARD = "tools/run_unity_guarded.py"
RESERVE_BYTES = 8 * 1024 ** 3


def git(repo, *args):
    environment = dict(os.environ, GIT_LFS_SKIP_SMUDGE="1", GIT_TERMINAL_PROMPT="0")
    command = ["git", "-C", str(repo), "-c", "core.hooksPath=" + os.devnull, *args]
    result = subprocess.run(command, capture_output=True, check=True, env=environment)
    return result.stdout


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def progress(event, **details):
    print(json.dumps({"event": event, **details}), file=sys.stderr, flush=True)


def physical_path(path):
    """Refuse redirected ancestors rather than silently resolving a junction."""
    path = Path(os.path.abspath(path))
    for part in (path, *path.parents):
        if not os.path.lexists(part):
            continue
        info = part.lstat()
        if stat.S_ISLNK(info.st_mode) or getattr(info, "st_file_attributes", 0) & 0x400:
            raise ValueError(f"A physical path is required; link/reparse point: {part}")
    return path


def worker_name(value):
    reserved = {"CON", "PRN", "AUX", "NUL", *(f"COM{i}" for i in range(1, 10)),
                *(f"LPT{i}" for i in range(1, 10))}
    if not re.fullmatch(r"[a-zA-Z0-9][a-zA-Z0-9_-]{0,47}", value) or value.upper() in reserved:
        raise ValueError(f"Invalid worker name: {value!r}")
    return value


def identity(settings):
    answer = {}
    for field in ("companyName", "productName"):
        rows = re.findall(rb"(?m)^  " + field.encode() + rb": ([^\r\n]*)", settings)
        if len(rows) != 1 or not rows[0].strip():
            raise ValueError(f"Expected exactly one nonempty {field}")
        answer[field] = rows[0].decode("utf-8").strip().strip('"')
    return answer


def replace_identity(settings, company, product):
    identity(settings)
    for field, value in (("companyName", company), ("productName", product)):
        settings, count = re.subn(rb"(?m)^(  " + field.encode() + rb": )[^\r\n]*",
                                  lambda match: match[1] + value.encode("utf-8"), settings)
        if count != 1:
            raise ValueError(f"Could not replace exactly one {field}")
    return settings


def editor_version(data):
    match = re.search(rb"(?m)^m_EditorVersion: ([^\r\n]+)", data)
    if not match:
        raise ValueError("ProjectVersion.txt has no Editor version")
    return match[1].decode("utf-8").strip()


def command_arguments(command):
    if os.name != "nt":
        return shlex.split(command)
    from ctypes import wintypes
    shell = ctypes.WinDLL("shell32", use_last_error=True)
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    shell.CommandLineToArgvW.argtypes = (wintypes.LPCWSTR, ctypes.POINTER(ctypes.c_int))
    shell.CommandLineToArgvW.restype = ctypes.POINTER(wintypes.LPWSTR)
    kernel.LocalFree.argtypes = (ctypes.c_void_p,)
    kernel.LocalFree.restype = ctypes.c_void_p
    count = ctypes.c_int()
    parsed = shell.CommandLineToArgvW(command, ctypes.byref(count))
    if not parsed:
        raise OSError(ctypes.get_last_error(), "Cannot parse a Unity process command line")
    try:
        return [parsed[index] for index in range(count.value)]
    finally:
        kernel.LocalFree(parsed)


def unity_processes():
    if os.name == "nt":
        script = ("$ErrorActionPreference='Stop'; Get-CimInstance Win32_Process "
                  "-Filter \"Name = 'Unity.exe'\" | "
                  "Select-Object ProcessId,CommandLine | ConvertTo-Json -Compress")
        result = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", script],
                                capture_output=True, text=True, check=True)
        rows = json.loads(result.stdout) if result.stdout.strip() else []
        if isinstance(rows, dict):
            rows = [rows]
        return [(row["ProcessId"], row.get("CommandLine")) for row in rows]
    result = subprocess.run(["ps", "-eo", "pid=,comm=,args="],
                            capture_output=True, text=True, check=True)
    rows = [line.strip().split(None, 2) for line in result.stdout.splitlines()]
    return [(int(row[0]), row[2] if len(row) > 2 else None) for row in rows
            if len(row) > 1 and Path(row[1]).name.lower() in ("unity", "unity.exe")]


def assert_idle(project, processes=None):
    project = os.path.normcase(str(physical_path(project)))
    for pid, command in unity_processes() if processes is None else processes:
        if not command:
            raise RuntimeError(f"Unity PID {pid} has no readable command line; cannot prove cache idle")
        args = command_arguments(command)
        target = None
        for index, arg in enumerate(args):
            if arg.lower() == "-projectpath" and index + 1 < len(args):
                target = args[index + 1]
            elif arg.lower().startswith("-projectpath="):
                target = arg.split("=", 1)[1]
        if not target:
            raise RuntimeError(f"Unity PID {pid} has no explicit project path; cannot prove cache idle")
        if os.path.normcase(str(Path(target).resolve())) == project:
            raise RuntimeError(f"Project is open in Unity PID {pid}: {project}")


def assert_closed_database(path):
    """Opening read-only with no sharing checks live Windows handles without editing."""
    if not path.exists():
        return
    if os.name == "nt":
        from ctypes import wintypes
        kernel = ctypes.WinDLL("kernel32", use_last_error=True)
        kernel.CreateFileW.argtypes = (wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD,
                                      ctypes.c_void_p, wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE)
        kernel.CreateFileW.restype = wintypes.HANDLE
        kernel.CloseHandle.argtypes = (wintypes.HANDLE,)
        handle = kernel.CreateFileW(str(path), 0x80000000, 0, None, 3, 0x80, None)
        if handle == ctypes.c_void_p(-1).value:
            raise RuntimeError(f"Cache database is open or unreadable: {path}")
        kernel.CloseHandle(handle)
    else:
        import fcntl
        with path.open("rb") as stream:
            try:
                fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
                fcntl.flock(stream, fcntl.LOCK_UN)
            except OSError as error:
                raise RuntimeError(f"Cache database is locked: {path}") from error


def skip_cache(name):
    lower = name.lower()
    return (lower in {"temp", "tempartifacts", "editorinstance.json", "lockfile", "unitylockfile"}
            or lower.endswith(("-lock", ".lock", ".pid")))


def cache_inventory(library):
    """Metadata fingerprint is an immutability check, not a content-hash claim."""
    physical_path(library)
    entries = {}
    for directory, subdirs, names in os.walk(library, followlinks=False):
        subdirs[:] = sorted(name for name in subdirs if not skip_cache(name))
        for name in subdirs:
            physical_path(Path(directory) / name)
        for name in sorted(names):
            if skip_cache(name):
                continue
            path = Path(directory) / name
            data = path.lstat()
            if not stat.S_ISREG(data.st_mode) or getattr(data, "st_file_attributes", 0) & 0x400:
                raise ValueError(f"Only regular cache files may be copied: {path}")
            entries[path.relative_to(library).as_posix()] = (data.st_size, data.st_mtime_ns)
    return entries


def cache_preflight(cache, expected_version):
    cache = physical_path(cache)
    if Path(git(cache, "rev-parse", "--show-toplevel").decode().strip()).resolve() != cache.resolve():
        raise ValueError("--cache-from must be the checkout root")
    assert_idle(cache)
    if editor_version((cache / VERSION).read_bytes()) != expected_version:
        raise ValueError("Cache Editor version does not match the supplied source commit")
    metadata = cache / "Library/EditorInstance.json"
    if metadata.exists():
        instance = json.loads(metadata.read_text(encoding="utf-8"))
        version = instance.get("version") or instance.get("editorVersion")
        if version and version != expected_version:
            raise ValueError("Cache EditorInstance metadata has a different Editor version")
    if git(cache, "status", "--porcelain", "--untracked-files=no", "--", "*.cs").strip():
        raise ValueError("Cache checkout has tracked C# changes; freeze it before copying")
    library = physical_path(cache / "Library")
    if not library.is_dir():
        raise ValueError("Cache checkout has no physical Library directory")
    for name in ("SourceAssetDB", "ArtifactDB"):
        assert_closed_database(library / name)
    return cache, library


def copy_library(cache, target, expected, progress_every):
    source = cache / "Library"
    assert_idle(cache)
    if cache_inventory(source) != expected:
        raise RuntimeError("Cache changed since preflight; refusing a mixed Library")
    for name in ("SourceAssetDB", "ArtifactDB"):
        assert_closed_database(source / name)
    copied = copied_bytes = 0
    last_idle_check = time.monotonic()

    def copy_one(src, dst):
        nonlocal copied, copied_bytes, last_idle_check
        src = Path(src)
        relative = src.relative_to(source).as_posix()
        before = src.lstat()
        if not stat.S_ISREG(before.st_mode) or getattr(before, "st_file_attributes", 0) & 0x400:
            raise RuntimeError(f"Cache file became a link or special file: {relative}")
        if (before.st_size, before.st_mtime_ns) != expected[relative]:
            raise RuntimeError(f"Cache file changed before copy: {relative}")
        shutil.copy2(src, dst)
        after = src.stat()
        if (after.st_size, after.st_mtime_ns) != expected[relative]:
            raise RuntimeError(f"Cache file changed during copy: {relative}")
        if os.path.samefile(src, dst) or Path(dst).stat().st_size != before.st_size:
            raise RuntimeError(f"Cache copy is not an independent complete file: {relative}")
        copied += 1
        copied_bytes += before.st_size
        if copied % progress_every == 0:
            if time.monotonic() - last_idle_check >= 15:
                assert_idle(cache)
                last_idle_check = time.monotonic()
            progress("library-copy", worker=str(target.parent), files=copied,
                     totalFiles=len(expected), bytes=copied_bytes)
        return str(dst)

    shutil.copytree(source, target, dirs_exist_ok=True, copy_function=copy_one,
                    ignore=lambda directory, names: [name for name in names if skip_cache(name)])
    assert_idle(cache)
    if cache_inventory(source) != expected:
        raise RuntimeError("Cache changed during copy; worker must not be used")
    progress("library-copy", worker=str(target.parent), files=copied,
             totalFiles=len(expected), bytes=copied_bytes)
    return {"files": copied, "bytes": copied_bytes,
            "verification": "independent copies; size checked; source size/mtime unchanged"}


def write_marker(path, value):
    pending = path.with_name(path.name + ".tmp")
    pending.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    os.replace(pending, path)


def prepare(args):
    receipt = {"schemaVersion": 1, "ok": False, "workers": []}
    try:
        if not re.fullmatch(r"(?:[0-9a-fA-F]{40}|[0-9a-fA-F]{64})", args.ref):
            raise ValueError("--ref must be the full, explicitly supplied commit SHA")
        if args.copy_library and not args.cache_from:
            raise ValueError("--copy-library requires --cache-from")
        if args.progress_every < 1:
            raise ValueError("--progress-every must be positive")
        repo, destination = physical_path(args.repo), physical_path(args.destination)
        source_commit = git(repo, "rev-parse", "--verify", args.ref + "^{commit}").decode().strip()
        if source_commit.lower() != args.ref.lower():
            raise ValueError("Resolved commit differs from --ref")
        names = [worker_name(name) for name in args.workers]
        if len(names) != 2 or len({name.casefold() for name in names}) != 2:
            raise ValueError("Exactly two distinct worker names are required")
        targets = [physical_path(destination / name) for name in names]
        if any(target.is_relative_to(repo) for target in targets):
            raise ValueError("Worker destinations must be outside the source checkout")
        if any(os.path.lexists(target) for target in targets):
            raise ValueError("A worker destination already exists; no directory will be reused")
        guard_source = git(repo, "show", source_commit + ":" + GUARD)
        if not re.search(rb"(?m)^PROJECT_IDENTITY_GUARD_VERSION[ \t]*=[ \t]*1[ \t]*(?:#[^\r\n]*)?\r?$", guard_source):
            raise ValueError("The supplied commit lacks project identity guard version 1; refusing unsafe workers")
        source_settings = git(repo, "show", source_commit + ":" + SETTINGS)
        original_identity = identity(source_settings)
        current_settings_hash = digest(repo / SETTINGS)
        current_identity = identity((repo / SETTINGS).read_bytes())
        if current_identity["companyName"] == COMPANY:
            raise ValueError("The source already uses the reserved validation company")
        version = editor_version(git(repo, "show", source_commit + ":" + VERSION))
        tree = git(repo, "ls-tree", "-r", "-l", "-z", source_commit)
        checkout_bytes = sum(int(entry.split(b"\t", 1)[0].split()[-1]) for entry in tree.split(b"\0")
                             if entry and entry.split(b"\t", 1)[0].split()[1] == b"blob")
        cache = library = None
        inventory = {}
        if args.cache_from:
            cache, library = cache_preflight(args.cache_from, version)
            if any(target.is_relative_to(cache) for target in targets):
                raise ValueError("Worker destinations must be outside the cache checkout")
        if args.copy_library:
            inventory = cache_inventory(library)
        copy_bytes = sum(value[0] for value in inventory.values())
        disk_parent = destination
        while not disk_parent.exists():
            disk_parent = disk_parent.parent
        free = shutil.disk_usage(disk_parent).free
        required = 2 * (checkout_bytes + copy_bytes) + RESERVE_BYTES
        if free < required:
            raise RuntimeError(f"Insufficient free disk: need {required} bytes, have {free}")
        receipt.update(sourceProject=str(repo), sourceCommit=source_commit, editorVersion=version,
                       guardVersion=1,
                       cacheFrom=str(cache) if cache else None, copyLibrary=args.copy_library,
                       freeBytesBefore=free, estimatedRequiredBytes=required)
        if cache:
            receipt["cacheSourceCommit"] = git(cache, "rev-parse", "HEAD").decode().strip()
            receipt["cacheProjectVersionSha256"] = digest(cache / VERSION)
        if args.copy_library:
            receipt["libraryMetadataSha256"] = hashlib.sha256(
                json.dumps(inventory, sort_keys=True).encode()).hexdigest()
        progress("preflight", **{key: receipt[key] for key in
                 ("sourceCommit", "copyLibrary", "freeBytesBefore", "estimatedRequiredBytes")})
        destination.mkdir(parents=True, exist_ok=True)
        run_id = uuid.uuid4().hex[:12]
        for name, target in zip(names, targets):
            # Recheck immediately before mutation; never reuse a racing/new directory.
            physical_path(target)
            if os.path.lexists(target):
                raise ValueError(f"Worker destination appeared after preflight: {target}")
            progress("worktree-create", worker=name, path=str(target))
            worker = {"workerId": name + "-" + run_id, "projectRoot": str(target), "state": "preparing"}
            receipt["workers"].append(worker)
            git(repo, "worktree", "add", "--detach", str(target), source_commit)
            assert_idle(target)
            if git(target, "rev-parse", "HEAD").decode().strip() != source_commit:
                raise RuntimeError("Worker HEAD does not match the frozen source commit")
            settings_path = target / SETTINGS
            before = settings_path.read_bytes()
            if before.replace(b"\r\n", b"\n") != source_settings.replace(b"\r\n", b"\n"):
                raise RuntimeError("Worker ProjectSettings differ from the supplied commit before editing")
            before_identity = identity(before)
            product = "TumpWorker-" + name + "-" + run_id
            after = replace_identity(before, COMPANY, product)
            if before_identity != original_identity or identity(after) == current_identity:
                raise RuntimeError("Worker identity did not derive from the supplied source")
            settings_path.write_bytes(after)
            marker = {"schemaVersion": 1, "workerId": worker["workerId"], "validationOnly": True,
                      "projectRoot": str(target), "sourceProject": str(repo), "sourceCommit": source_commit,
                      "companyName": COMPANY, "productName": product, "editorVersion": version,
                      "guardVersion": 1, "guardSha256": digest(target / GUARD),
                      "libraryPath": str(target / "Library"), "libraryPhysical": True,
                      "libraryCopyFrom": str(library) if args.copy_library else None,
                      "requiredProfile": "validation-" + worker["workerId"],
                      "shippingBuildsAllowed": False, "warmupRequired": True,
                      "editorValidation": "not_run", "state": "preparing"}
            write_marker(target / MARKER, marker)
            try:
                physical_path(target / "Library")
                if args.copy_library:
                    worker["libraryCopy"] = copy_library(cache, target / "Library", inventory, args.progress_every)
                else:
                    (target / "Library").mkdir(exist_ok=True)
                dirty = git(target, "diff", "--name-only").decode().splitlines()
                if dirty != [SETTINGS]:
                    raise RuntimeError(f"Unexpected tracked changes in new worker: {dirty}")
                marker["state"] = "ready"
                write_marker(target / MARKER, marker)
            except Exception:
                marker["state"] = "failed"
                write_marker(target / MARKER, marker)
                worker["state"] = "failed"
                raise
            worker.update(state="ready", companyName=COMPANY, productName=product,
                          requiredProfile=marker["requiredProfile"], libraryPath=marker["libraryPath"],
                          markerPath=str(target / MARKER), markerSha256=digest(target / MARKER),
                          projectSettingsBeforeSha256=hashlib.sha256(before).hexdigest(),
                          projectSettingsAfterSha256=digest(settings_path),
                          identityDelta={"before": before_identity, "after": identity(after)},
                          editorVersionSha256=digest(target / VERSION))
            progress("worker-ready", worker=name, path=str(target), warmupRequired=True)
        if digest(repo / SETTINGS) != current_settings_hash:
            raise RuntimeError("Source ProjectSettings changed during preparation")
        if cache and git(cache, "rev-parse", "HEAD").decode().strip() != receipt["cacheSourceCommit"]:
            raise RuntimeError("Cache source commit changed during preparation")
        receipt["ok"] = True
        receipt["shippingBuildsAllowed"] = False
        receipt["editorStarted"] = False
    except Exception as error:
        if receipt["workers"] and receipt["workers"][-1]["state"] == "preparing":
            receipt["workers"][-1]["state"] = "failed"
        receipt["error"] = str(error)
        receipt["partialWorktreesPreserved"] = bool(receipt["workers"])
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ref", required=True, help="Exact full commit SHA, never a moving branch")
    parser.add_argument("--destination", type=Path, required=True)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--workers", nargs=2, default=["worker-a", "worker-b"])
    parser.add_argument("--cache-from", type=Path)
    parser.add_argument("--copy-library", action="store_true", help="Explicitly copy an idle physical Library")
    parser.add_argument("--progress-every", type=int, default=500)
    result = prepare(parser.parse_args())
    print(json.dumps(result, indent=2))
    return 0 if result["ok"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

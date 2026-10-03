"""Coordinate bounded local Unity jobs; only isolated CPU test workers may overlap."""
import argparse
import ast
import contextlib
import ctypes
from ctypes import wintypes
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import uuid

import run_unity_guarded as guard
import playerprefs_guard
import prepare_unity_test_workers as workers

POOL = Path(tempfile.gettempdir()) / "tump-unity-job-pool"


def canonical(path):
    return os.path.normcase(str(Path(path).resolve()))


def now():
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())


def win_kernel():
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    signatures = {
        "OpenProcess": (wintypes.HANDLE, [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]),
        "GetExitCodeProcess": (wintypes.BOOL, [wintypes.HANDLE, ctypes.POINTER(wintypes.DWORD)]),
        "CloseHandle": (wintypes.BOOL, [wintypes.HANDLE]),
        "CreateToolhelp32Snapshot": (wintypes.HANDLE, [wintypes.DWORD, wintypes.DWORD]),
        "Process32FirstW": (wintypes.BOOL, [wintypes.HANDLE, ctypes.c_void_p]),
        "Process32NextW": (wintypes.BOOL, [wintypes.HANDLE, ctypes.c_void_p]),
        "QueryFullProcessImageNameW": (wintypes.BOOL, [wintypes.HANDLE, wintypes.DWORD, wintypes.LPWSTR, ctypes.POINTER(wintypes.DWORD)]),
        "TerminateProcess": (wintypes.BOOL, [wintypes.HANDLE, wintypes.UINT]),
    }
    for name, (result, arguments) in signatures.items():
        function = getattr(kernel, name)
        function.restype, function.argtypes = result, arguments
    return kernel


def pid_alive(pid):
    """Unknown/access-denied owners remain live. Never use os.kill on Windows."""
    if not isinstance(pid, int) or pid <= 0:
        return False
    if os.name != "nt":
        return Path(f"/proc/{pid}").exists()
    kernel = win_kernel()
    handle = kernel.OpenProcess(0x1000, False, pid)
    if not handle:
        return ctypes.get_last_error() != 87  # ERROR_INVALID_PARAMETER: PID absent
    try:
        code = wintypes.DWORD()
        return not kernel.GetExitCodeProcess(handle, ctypes.byref(code)) or code.value == 259
    finally:
        kernel.CloseHandle(handle)


def free_memory_mb():
    if os.name != "nt":
        values = dict(line.split(":", 1) for line in Path("/proc/meminfo").read_text().splitlines())
        return int(values["MemAvailable"].split()[0]) // 1024
    class Memory(ctypes.Structure):
        _fields_ = [("length", wintypes.DWORD), ("load", wintypes.DWORD)] + [
            (name, ctypes.c_ulonglong) for name in
            ("totalPhysical", "availablePhysical", "totalPage", "availablePage", "totalVirtual", "availableVirtual", "extended")]
    value = Memory()
    value.length = ctypes.sizeof(value)
    if not ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(value)):
        raise OSError("Cannot verify available physical memory")
    return value.availablePhysical // (1024 * 1024)


def processes():
    """Return PID, parent PID and executable basename without command lines/credentials."""
    if os.name != "nt":
        rows = []
        for path in Path("/proc").iterdir():
            if not path.name.isdecimal():
                continue
            try:
                stat = (path / "stat").read_text()
                tail = stat[stat.rfind(")") + 2:].split()
                rows.append({"pid": int(path.name), "parent": int(tail[1]), "name": (path / "comm").read_text().strip()})
            except (OSError, ValueError):
                pass
        return rows
    class Entry(ctypes.Structure):
        _fields_ = [("size", wintypes.DWORD), ("usage", wintypes.DWORD), ("pid", wintypes.DWORD),
                    ("heap", ctypes.c_size_t), ("module", wintypes.DWORD), ("threads", wintypes.DWORD),
                    ("parent", wintypes.DWORD), ("priority", wintypes.LONG), ("flags", wintypes.DWORD),
                    ("name", wintypes.WCHAR * 260)]
    kernel = win_kernel()
    handle = kernel.CreateToolhelp32Snapshot(2, 0)
    if not handle or handle == ctypes.c_void_p(-1).value:
        raise OSError("Cannot inventory Unity processes safely")
    try:
        entry = Entry()
        entry.size = ctypes.sizeof(entry)
        rows = []
        valid = kernel.Process32FirstW(handle, ctypes.byref(entry))
        while valid:
            rows.append({"pid": entry.pid, "parent": entry.parent, "name": entry.name})
            valid = kernel.Process32NextW(handle, ctypes.byref(entry))
        return rows
    finally:
        kernel.CloseHandle(handle)


def descendants(rows, roots):
    owned = set(roots)
    while True:
        added = {row["pid"] for row in rows if row["parent"] in owned} - owned
        if not added:
            return owned
        owned.update(added)


def foreign_unity(rows, leases):
    roots = {pid for lease in leases for pid in (lease["ownerPid"], lease.get("guardPid")) if pid}
    owned = descendants(rows, roots)
    return [row["pid"] for row in rows if row["name"].casefold() in
            ("unity.exe", "unity", "tumbangpreso.exe", "tumbangpreso") and row["pid"] not in owned]


def coexistence(claim, foreign, editors=None):
    """Inspect an explicitly reserved outside Editor; never adopt or stop it."""
    project = claim.get("coexistEditorProject")
    if not project or not foreign:
        return foreign, []
    if claim["kind"] != "cpu" or not claim["worker"] or not claim["allowParallel"]:
        raise ValueError("Editor coexistence requires an isolated, opted-in CPU test worker")
    if claim["ports"]:
        raise ValueError("Headless coexistence tests must not reserve network ports")
    project = workers.physical_path(project)
    if canonical(project) == claim["project"] or canonical(project / "Library") == claim["library"]:
        raise ValueError("Outside Editor must use a separate project and Library")
    company, product = guard.project_identity(project)
    if playerprefs_guard.editor_key(company, product).casefold() == claim["prefHive"]:
        raise ValueError("Outside Editor must use separate input preferences")
    records = dict(workers.unity_processes() if editors is None else editors)
    accepted = []
    import_workers = []
    for pid in foreign:
        command = records.get(pid)
        if not command:
            raise ValueError("Outside process is not a verified Unity Editor")
        args = workers.command_arguments(command)
        targets = [args[i + 1] for i, arg in enumerate(args[:-1]) if arg.casefold() == "-projectpath"]
        targets += [arg.split("=", 1)[1] for arg in args if arg.casefold().startswith("-projectpath=")]
        if len(targets) != 1 or canonical(targets[0]) != canonical(project):
            raise ValueError("Outside Editor does not match the reserved project")
        parents = [args[i + 1] for i, arg in enumerate(args[:-1]) if arg.casefold() == "-parentpid"]
        if parents:
            if len(parents) != 1 or not parents[0].isdecimal() or int(parents[0]) not in foreign:
                raise ValueError("Outside import worker has no verified parent Editor")
            import_workers.append((pid, int(parents[0])))
        else:
            accepted.append({"pid": pid, "project": canonical(project)})
    if len(accepted) != 1:
        raise ValueError("Editor coexistence supports exactly one outside Editor")
    if any(parent != accepted[0]["pid"] for _, parent in import_workers):
        raise ValueError("Outside import workers must belong to the reserved Editor")
    accepted[0]["importWorkerPids"] = [pid for pid, _ in import_workers]
    return [], accepted


def classify(args):
    lower = [arg.casefold() for arg in args]
    if any(arg.startswith("-build") for arg in lower):
        return "build"
    if "-executemethod" in lower:
        index = lower.index("-executemethod")
        return "build" if index + 1 < len(lower) and "build" in lower[index + 1] else "gpu"
    if any(arg in lower for arg in ("-tp-uireview", "-tp-graphicsreport")):
        return "gpu"
    platform = lower[lower.index("-testplatform") + 1] if "-testplatform" in lower and lower.index("-testplatform") + 1 < len(lower) else None
    if all(flag in lower for flag in ("-batchmode", "-nographics", "-runtests")) and platform == "editmode":
        return "cpu"
    return "gpu"


def identity_guard_version(project):
    # Inspect the capability declaration without importing arbitrary worker code.
    tree = ast.parse((project / "tools/run_unity_guarded.py").read_text(encoding="utf-8-sig"))
    declarations = [statement.value for statement in tree.body if isinstance(statement, ast.Assign)
                    and any(isinstance(target, ast.Name) and target.id == "PROJECT_IDENTITY_GUARD_VERSION"
                            for target in statement.targets)]
    return (len(declarations) == 1 and isinstance(declarations[0], ast.Constant)
            and type(declarations[0].value) is int and declarations[0].value == 1)


def make_claim(project, kind, memory_mb, reserve_mb, profile, ports, args, allow_parallel=False, max_parallel_jobs=2, allow_gpu_parallel=False):
    project = Path(project).resolve()
    if not (project / "ProjectSettings/ProjectSettings.asset").is_file() or not (project / "tools/run_unity_guarded.py").is_file():
        raise ValueError("Target must be an existing Unity project with its guarded runner")
    if not profile.strip() or profile.startswith("-") or memory_mb <= 0 or reserve_mb <= 0:
        raise ValueError("Use a named profile and positive memory/reserve budgets")
    actual = classify(args)
    if {"cpu": 0, "gpu": 1, "build": 2}[kind] < {"cpu": 0, "gpu": 1, "build": 2}[actual]:
        raise ValueError(f"Arguments require an exclusive {actual} job, not {kind}")
    if any(arg.casefold() in ("-profile", "-tp-profile", "-projectpath") for arg in args):
        raise ValueError("The job runner owns project/profile flags")
    company, product = guard.project_identity(project)
    worker = False
    marker = project / ".tump-validation-worker.json"
    if marker.exists():
        if not identity_guard_version(project):
            raise ValueError("Validation worker requires PROJECT_IDENTITY_GUARD_VERSION = 1 in its target guarded runner")
        data = json.loads(marker.read_text(encoding="utf-8-sig"))
        source = Path(data["sourceProject"]).resolve()
        if (data.get("schemaVersion") != 1 or data.get("validationOnly") is not True or not data.get("workerId")
                or canonical(data["projectRoot"]) != canonical(project)
                or (data.get("companyName"), data.get("productName")) != (company, product)
                or canonical(source) == canonical(project)
                or not (source / "ProjectSettings/ProjectSettings.asset").is_file()
                or guard.project_identity(source) == (company, product)
                or canonical(source / "Library") == canonical(project / "Library")):
            raise ValueError("Validation worker identity/cache isolation does not match its marker")
        worker = True
    if worker and (kind == "build" or actual == "build"):
        raise ValueError("Validation-only worker identities must not produce shipping builds")
    return {"id": uuid.uuid4().hex, "ownerPid": os.getpid(), "guardPid": None, "unityPids": [],
            "createdUtc": now(), "kind": kind, "memoryMb": memory_mb, "reserveMb": reserve_mb,
            "project": canonical(project), "library": canonical(project / "Library"),
            "profile": profile.strip().casefold(), "prefHive": playerprefs_guard.editor_key(company, product).casefold(),
            "ports": sorted(set(ports)), "worker": worker, "allowParallel": allow_parallel,
            "maxParallelJobs": max_parallel_jobs, "allowGpuParallel": allow_gpu_parallel,
            "companyName": company, "productName": product}


def live_leases(leases, alive=None):
    # A crashed wrapper does not release a still-running guard or its restoration work.
    alive = pid_alive if alive is None else alive
    return [lease for lease in leases if any(alive(pid) for pid in
            (lease["ownerPid"], lease.get("guardPid"), *lease.get("unityPids", [])) if pid)]


def refusal(claim, active, available_mb, foreign):
    if foreign:
        return "A Unity process outside this pool is active"
    for lease in active:
        if lease.get("awaitingRestoration"):
            return "A prior guarded job has not finished restoration"
        if any(claim[key] == lease[key] for key in ("project", "library", "profile", "prefHive")):
            return "Project, Library, profile or preference hive is already leased"
        if set(claim["ports"]) & set(lease["ports"]):
            return "A declared port is already leased"
    if active and (claim["kind"] == "build" or any(lease["kind"] == "build" for lease in active)):
        return "Build jobs require the exclusive pool"
    if active and (claim["kind"] != "cpu" or any(lease["kind"] != "cpu" for lease in active)):
        if not claim.get("allowGpuParallel") or any(not lease.get("allowGpuParallel") for lease in active):
            return "GPU jobs require the exclusive pool unless every worker opts in"
    if active and (not claim["worker"] or any(not lease["worker"] for lease in active)):
        return "CPU overlap requires isolated validation workers on both jobs"
    if active and (not claim.get("allowParallel") or any(not lease.get("allowParallel") for lease in active)):
        return "CPU overlap requires explicit --allow-parallel on both jobs"
    limit = min([claim.get("maxParallelJobs", 2)] + [lease.get("maxParallelJobs", 2) for lease in active])
    if len(active) >= limit:
        return "Two CPU jobs are already active" if limit == 2 else f"The declared {limit}-job parallel pool is full"
    reserved = sum(lease["memoryMb"] for lease in active)
    if available_mb < claim["reserveMb"] + claim["memoryMb"] + reserved:
        return "Available physical memory is below the reserve plus job budgets"
    return None


@contextlib.contextmanager
def pool_lock(pool):
    pool.mkdir(parents=True, exist_ok=True)
    with (pool / "pool.lock").open("a+b") as handle:
        if handle.tell() == 0:
            handle.write(b"0"); handle.flush()
        handle.seek(0)
        if os.name == "nt":
            import msvcrt
            msvcrt.locking(handle.fileno(), msvcrt.LK_LOCK, 1)
        else:
            import fcntl
            fcntl.flock(handle, fcntl.LOCK_EX)
        try:
            yield
        finally:
            handle.seek(0)
            if os.name == "nt":
                msvcrt.locking(handle.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                fcntl.flock(handle, fcntl.LOCK_UN)


def read_pool(pool):
    path = pool / "leases.json"
    if not path.exists():
        return []
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("version") != 1 or not isinstance(data.get("jobs"), list):
        raise ValueError("Invalid lease registry; refusing to overwrite it")
    for job in data["jobs"]:
        if not isinstance(job.get("ownerPid"), int) or job["ownerPid"] <= 0:
            raise ValueError("Lease owner cannot be verified; refusing stale cleanup")
    return data["jobs"]


def write_pool(pool, jobs):
    path = pool / ("leases-" + uuid.uuid4().hex + ".tmp")
    path.write_text(json.dumps({"version": 1, "jobs": jobs}, indent=2), encoding="utf-8")
    os.replace(path, pool / "leases.json")


def update_lease(pool, job_id, update=None):
    with pool_lock(pool):
        jobs = read_pool(pool)
        if update is None:
            jobs = [job for job in jobs if job["id"] != job_id]
        else:
            for job in jobs:
                if job["id"] == job_id:
                    job.update(update)
        write_pool(pool, jobs)


def acquire(pool, claim, wait_seconds):
    until = time.monotonic() + wait_seconds
    while True:
        with pool_lock(pool):
            jobs = live_leases(read_pool(pool))
            available = free_memory_mb()
            foreign = foreign_unity(processes(), jobs)
            foreign, external = coexistence(claim, foreign)
            if external and jobs:
                raise ValueError("Outside Editor already occupies the first of two slots")
            available_for_job = available - (claim.get("coexistReserveMb", 0) if external else 0)
            reason = refusal(claim, jobs, available_for_job, foreign)
            if reason is None:
                jobs.append(claim)
            write_pool(pool, jobs)
        if reason is None:
            return {"availableMb": available, "activeJobsBefore": len(jobs) - 1,
                    "outsideEditors": external, "outsideReserveMb": claim.get("coexistReserveMb", 0) if external else 0,
                    "acquiredUtc": now()}
        if time.monotonic() >= until:
            raise TimeoutError(reason)
        time.sleep(min(1, max(0, until - time.monotonic())))


def output_arguments(project, output, args):
    args = list(args)
    for flag, filename in (("-logFile", "unity.log"), ("-testResults", "tests.xml")):
        positions = [i for i, value in enumerate(args) if value.casefold() == flag.casefold()]
        if not positions:
            if flag == "-logFile" or "-runtests" in [arg.casefold() for arg in args]:
                args.extend([flag, str(output / filename)])
            continue
        if len(positions) != 1 or positions[0] + 1 == len(args):
            raise ValueError("Ambiguous output argument")
        path = Path(args[positions[0] + 1])
        path = path.resolve() if path.is_absolute() else (project / path).resolve()
        if not path.is_relative_to(output):
            raise ValueError("Unity logs/results must stay in the dedicated output directory")
        args[positions[0] + 1] = str(path)
    return args


def stop_owned_editors(child):
    """Stop only live Unity executables still descended from this exact live guard."""
    if os.name != "nt" or child.poll() is not None:
        return []
    kernel = win_kernel()
    rows = processes()
    owned = descendants(rows, {child.pid})
    stopped = []
    for row in rows:
        if row["pid"] not in owned or row["name"].casefold() != "unity.exe":
            continue
        handle = kernel.OpenProcess(0x1001, False, row["pid"])
        if not handle:
            continue
        try:
            path, length, code = ctypes.create_unicode_buffer(32768), wintypes.DWORD(32768), wintypes.DWORD()
            if (child.poll() is None and kernel.QueryFullProcessImageNameW(handle, 0, path, ctypes.byref(length))
                    and canonical(path.value) == canonical(guard.unity_executable())
                    and row["pid"] in descendants(processes(), {child.pid})
                    and kernel.GetExitCodeProcess(handle, ctypes.byref(code)) and code.value == 259
                    and kernel.TerminateProcess(handle, 124)):
                stopped.append(row["pid"])
        finally:
            kernel.CloseHandle(handle)
    return stopped


class JobInterrupted(RuntimeError):
    """Our workload must yield; the outside application remains untouched."""


def monitor_running_job(claim):
    with pool_lock(POOL):
        active = live_leases(read_pool(POOL))
    foreign = foreign_unity(processes(), active)
    try:
        foreign, external = coexistence(claim, foreign)
    except (OSError, ValueError) as error:
        raise JobInterrupted(f"Outside workload changed: {error}") from error
    if foreign:
        raise JobInterrupted("An outside Unity/player workload started; yielding our exclusive job")
    if external and any(lease["id"] != claim["id"] for lease in active):
        raise JobInterrupted("An outside Editor would create a third active slot")
    minimum = claim["reserveMb"] + (claim.get("coexistReserveMb", 0) if external else 0)
    available = free_memory_mb()
    if available < minimum:
        raise JobInterrupted(f"Physical memory below runtime reserve: {available} MB available, {minimum} MB required")


def wait_for_guard(child, claim, timeout_seconds):
    deadline = time.monotonic() + timeout_seconds
    while True:
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            raise subprocess.TimeoutExpired("Unity guard", timeout_seconds)
        try:
            return child.wait(timeout=min(2, remaining))
        except subprocess.TimeoutExpired:
            if remaining <= 2:
                raise
            monitor_running_job(claim)


def run(options):
    project, output = Path(options.project).resolve(), Path(options.output).resolve()
    ports = [int(value) for value in options.ports.split(",") if value]
    if any(port < 1 or port > 65535 for port in ports):
        raise ValueError("Ports must be in 1..65535")
    args = options.unity_args[1:] if options.unity_args[:1] == ["--"] else options.unity_args
    if "-runtests" in [arg.casefold() for arg in args] and "-quit" in [arg.casefold() for arg in args]:
        raise ValueError("Test jobs must finish their XML; do not pass -quit")
    claim = make_claim(project, options.kind, options.memory_mb, options.reserve_mb, options.profile, ports, args,
                       getattr(options, "allow_parallel", False),
                       getattr(options, "max_parallel_jobs", 2),
                       getattr(options, "allow_gpu_parallel", False))
    coexist_project = getattr(options, "coexist_editor_project", None)
    if coexist_project:
        if claim["kind"] != "cpu" or not claim["worker"] or not claim["allowParallel"] or ports:
            raise ValueError("Editor coexistence requires isolated opted-in headless CPU tests without ports")
        reserve = getattr(options, "coexist_reserve_mb", 1024)
        if reserve < 1024:
            raise ValueError("Outside Editor requires at least 1024 MB of additional growth reserve")
        claim.update(coexistEditorProject=str(workers.physical_path(coexist_project)), coexistReserveMb=reserve)
    args = output_arguments(project, output, args)
    output.mkdir(parents=True, exist_ok=True)
    receipt = {"job": claim, "requestedUtc": now(), "status": "waiting", "exitCode": None}
    receipt_path = output / "job-receipt.json"
    with receipt_path.open("x", encoding="utf-8") as handle:
        json.dump(receipt, handle, indent=2)
    acquired = False
    child = None
    terminal = True
    try:
        receipt["admission"] = acquire(POOL, claim, options.wait_seconds)
        acquired = True
        environment = dict(os.environ, PYTHONUTF8="1", PYTHONIOENCODING="utf-8")
        command = [sys.executable, str(project / "tools/run_unity_guarded.py"), *args, "-tp-profile", options.profile]
        with (output / "guard-stdout.log").open("w", encoding="utf-8") as stdout:
            child = subprocess.Popen(command, cwd=project, env=environment, stdout=stdout, stderr=subprocess.STDOUT)
            receipt.update(status="running", startedUtc=now(), guardPid=child.pid)
            update_lease(POOL, claim["id"], {"guardPid": child.pid})
            receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
            try:
                receipt["exitCode"] = wait_for_guard(child, claim, options.timeout_seconds)
                receipt["status"] = "completed" if receipt["exitCode"] == 0 else "failed"
            except (subprocess.TimeoutExpired, JobInterrupted) as interruption:
                interrupted = isinstance(interruption, JobInterrupted)
                prefix = "interrupted" if interrupted else "timeout"
                if interrupted:
                    receipt["interruptionReason"] = str(interruption)
                # Let the guard's finally block restore state after its owned Editor exits.
                rows = processes()
                owned = descendants(rows, {child.pid})
                unity = [row["pid"] for row in rows if row["pid"] in owned and row["name"].casefold() in ("unity.exe", "unity")]
                update_lease(POOL, claim["id"], {"unityPids": unity, "awaitingRestoration": True})
                stopped = stop_owned_editors(child)
                try:
                    receipt["guardExitCode"] = child.wait(timeout=30)
                    receipt["status"] = prefix + "_guard_completed"
                except subprocess.TimeoutExpired:
                    terminal = False
                    receipt["status"] = prefix + "_awaiting_guard"
                receipt.update(exitCode=125 if interrupted else 124, unityPids=unity, stoppedUnityPids=stopped,
                               reason="Only verified owned Editors may be stopped; the guard was not terminated. A pending guard retains its lease.")
        return receipt["exitCode"]
    except (OSError, ValueError, TimeoutError) as error:
        receipt.update(status="refused" if not acquired else "failed", exitCode=1, reason=str(error))
        if child is not None and child.poll() is None:
            terminal = False
            update_lease(POOL, claim["id"], {"guardPid": child.pid, "awaitingRestoration": True})
        return 1
    finally:
        if child is not None and child.poll() is None:
            terminal = False
            update_lease(POOL, claim["id"], {"guardPid": child.pid, "awaitingRestoration": True})
        remaining_editors = [pid for pid in receipt.get("unityPids", []) if pid_alive(pid)]
        if acquired and terminal and not remaining_editors:
            update_lease(POOL, claim["id"])
        elif acquired and remaining_editors:
            update_lease(POOL, claim["id"], {"unityPids": remaining_editors, "awaitingRestoration": True})
        stdout_path = output / "guard-stdout.log"
        preserved = stdout_path.exists() and any(line.startswith("Preserved ") for line in stdout_path.read_text(encoding="utf-8", errors="replace").splitlines())
        try:
            remaining_memory = free_memory_mb()
        except OSError:
            remaining_memory = None
        receipt.update(endedUtc=now(), guardTerminal=terminal, preservationCompleted=preserved,
                       availableMbAtEnd=remaining_memory, leaseHeld=acquired and (not terminal or bool(remaining_editors)))
        receipt_path.write_text(json.dumps(receipt, indent=2), encoding="utf-8")
        print(json.dumps({"receipt": str(receipt_path), "status": receipt["status"], "exitCode": receipt["exitCode"]}), flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True)
    parser.add_argument("--kind", choices=("cpu", "gpu", "build"), required=True)
    parser.add_argument("--memory-mb", type=int, required=True)
    parser.add_argument("--reserve-mb", type=int, default=2048)
    parser.add_argument("--wait-seconds", type=float, default=300)
    parser.add_argument("--timeout-seconds", type=float, default=450)
    parser.add_argument("--profile", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--ports", default="", help="Comma-separated exclusive TCP/UDP port claims")
    parser.add_argument("--allow-parallel", action="store_true", help="Opt in to isolated worker overlap; default is serialized")
    parser.add_argument("--max-parallel-jobs", type=int, default=2, help="Shared opt-in worker limit; admission still reserves every memory budget")
    parser.add_argument("--allow-gpu-parallel", action="store_true", help="Allow isolated graphics test workers to overlap; shipping builds remain exclusive")
    parser.add_argument("--coexist-editor-project", help="Permit one verified outside Editor beside an isolated headless EditMode worker")
    parser.add_argument("--coexist-reserve-mb", type=int, default=1024, help="Additional free memory retained for outside Editor growth")
    parser.add_argument("unity_args", nargs=argparse.REMAINDER)
    options = parser.parse_args()
    if options.wait_seconds < 0 or options.timeout_seconds <= 0:
        parser.error("Wait must be nonnegative and timeout positive")
    if not 1 <= options.max_parallel_jobs <= 8:
        parser.error("Parallel worker limit must be1..8")
    try:
        return run(options)
    except (OSError, ValueError, KeyError) as error:
        parser.error(str(error))


if __name__ == "__main__":
    raise SystemExit(main())

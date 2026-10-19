"""Build the Windows installer (installer/TumbangPreso.iss) from a GameBuilder output folder.

    python tools/build_installer.py --source Builds/<candidate>
    python tools/build_installer.py --source Builds/<candidate> --out Builds/installer
    python tools/build_installer.py --source Builds/<candidate> --dry-run

WHY (LAN-VPN-JOIN-1020): the installer is where the game's one inbound UDP firewall rule is
added, with the player's single admin consent, so a LAN room over Hamachi or Radmin (Public
networks to Windows) answers out of the box. installer/TumbangPreso.iss says what the rule is and
why there is only one; docs/NETWORKING.md has the player-facing steps.

What this does, in order:
  1. checks the folder is a whole Windows player (TumbangPreso.exe, UnityPlayer.dll, TumbangPreso_Data),
  2. finds Inno Setup's compiler (ISCC.exe, Inno Setup 6.3 or later; --iscc, INNO_SETUP_ISCC or the
     usual install folders, per machine or per user),
  3. checks free space on the output drive (D: is tight on the owner's PC; the installer is about the
     size of the compressed player),
  4. reads the version from ProjectSettings, makes the setup icon from the team badge when node is
     present (tools/make_ico.js), and runs ISCC with the defines the script expects.

It never runs the installer. Running it adds a firewall rule, which is the player's decision.
Exit codes: 0 built (or printed with --dry-run), 2 bad source, 3 no compiler, 4 no space, 5 ISCC failed.
"""
import argparse
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "installer" / "TumbangPreso.iss"
REQUIRED = ("TumbangPreso.exe", "UnityPlayer.dll", "TumbangPreso_Data")


def player_problem(source):
    """None when `source` holds a whole Windows player, else what is missing."""
    if not source.is_dir():
        return f"{source} is not a folder"
    missing = [name for name in REQUIRED if not (source / name).exists()]
    if missing:
        return f"{source} is not a GameBuilder Windows output: missing {', '.join(missing)}"
    return None


def iscc_candidates(environment=None):
    environment = os.environ if environment is None else environment
    if environment.get("INNO_SETUP_ISCC"):
        yield Path(environment["INNO_SETUP_ISCC"])
    found = shutil.which("ISCC") or shutil.which("iscc")
    if found:
        yield Path(found)
    for base in ("ProgramFiles(x86)", "ProgramFiles", "LOCALAPPDATA"):
        root = environment.get(base)
        if not root:
            continue
        folder = Path(root) / ("Programs" if base == "LOCALAPPDATA" else "") / "Inno Setup 6"
        yield folder / "ISCC.exe"


def find_iscc(explicit=None, environment=None):
    if explicit:
        path = Path(explicit)
        return path if path.is_file() else None
    for candidate in iscc_candidates(environment):
        if candidate.is_file():
            return candidate
    return None


def project_version(root=ROOT):
    settings = (root / "ProjectSettings" / "ProjectSettings.asset").read_text(encoding="utf-8-sig")
    match = re.search(r"^\s*bundleVersion:\s*(\S+)\s*$", settings, re.MULTILINE)
    return match.group(1) if match else "0.0.0"


def folder_bytes(folder):
    total = 0
    for path in folder.rglob("*"):
        if "_BurstDebugInformation_DoNotShip" in str(path):
            continue
        if path.is_file():
            total += path.stat().st_size
    return total


def iscc_command(iscc, source, out, version, icon=None, script=SCRIPT):
    """The ISCC command line. Defines are passed one per argument, so no path is ever re-quoted."""
    command = [str(iscc), "/Qp", f"/DSourceDir={source}", f"/DAppVersion={version}",
               f"/DOutputName=TumbangPreso-Setup-{version}", f"/O{out}"]
    if icon:
        command.append(f"/DSetupIcon={icon}")
    command.append(str(script))
    return command


def make_icon(out):
    """The team badge as an .ico for the setup window, or None when node is not here."""
    node = shutil.which("node")
    badge = ROOT / "Assets" / "TumbangPreso" / "Art" / "ui" / "brand" / "app_icon.png"
    if not node or not badge.is_file():
        return None
    icon = out / "TumbangPreso-setup.ico"
    result = subprocess.run([node, str(ROOT / "tools" / "make_ico.js"), str(badge), str(icon)],
                            capture_output=True, text=True)
    return icon if result.returncode == 0 and icon.is_file() else None


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--source", required=True, help="GameBuilder output folder holding TumbangPreso.exe")
    parser.add_argument("--out", default=str(ROOT / "Builds" / "installer"), help="where the setup .exe goes")
    parser.add_argument("--iscc", help="path to ISCC.exe (Inno Setup 6.3 or later)")
    parser.add_argument("--version", help="override the version read from ProjectSettings")
    parser.add_argument("--dry-run", action="store_true", help="check and print the ISCC command; build nothing")
    args = parser.parse_args(argv)

    source = Path(args.source).resolve()
    out = Path(args.out).resolve()
    problem = player_problem(source)
    if problem:
        print("source  : " + problem)
        return 2
    version = args.version or project_version()
    print(f"source  : {source}")
    print(f"version : {version}")
    print(f"script  : {SCRIPT}")

    iscc = find_iscc(args.iscc)
    print(f"iscc    : {iscc or 'NOT FOUND'}")
    if iscc is None and not args.dry_run:
        print("Install Inno Setup 6.3 or later (https://jrsoftware.org/isinfo.php; a per-user install needs no admin),")
        print("or pass --iscc <path to ISCC.exe>, or set INNO_SETUP_ISCC.")
        return 3

    need = folder_bytes(source)
    probe = out
    while not probe.exists():
        probe = probe.parent
    free = shutil.disk_usage(probe).free
    print(f"player  : {need / 2**20:,.0f} MB; free on {Path(out.anchor)}: {free / 2**20:,.0f} MB")
    # The setup is compressed, but ISCC stages and writes it whole; ask for the player's size plus a margin.
    if free < need + 512 * 2**20:
        print("not enough free space for the installer; pass --out on a roomier drive")
        return 4

    if args.dry_run:
        print("command : " + subprocess.list2cmdline(iscc_command(iscc or "ISCC.exe", source, out, version)))
        return 0

    out.mkdir(parents=True, exist_ok=True)
    icon = make_icon(out)
    command = iscc_command(iscc, source, out, version, icon)
    print("command : " + subprocess.list2cmdline(command))
    result = subprocess.run(command)
    setup = out / f"TumbangPreso-Setup-{version}.exe"
    if result.returncode != 0 or not setup.is_file():
        print(f"ISCC failed (exit {result.returncode})")
        return 5
    print(f"setup   : {setup}  ({setup.stat().st_size / 2**20:,.0f} MB)")
    print("Not run here: installing adds the firewall rule. See docs/NETWORKING.md for the check.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

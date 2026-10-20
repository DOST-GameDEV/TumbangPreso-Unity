"""Where the built players live, for every tool that has to find one.

`GameBuilder.BuildsRoot` is the authority and this is its mirror for the Python harnesses:
`Builds/` at the root of the MAIN checkout, one folder per platform.

    Builds/Windows/TumbangPreso.exe
    Builds/Android/TumbangPreso.apk
    Builds/macOS/TumbangPreso.app
    Builds/WebGL/

WARNING: THESE TOOLS USED TO LOOK ON THE DESKTOP, AS `~/Desktop`, AND THAT WAS WRONG TWICE.
The builder stopped writing there on 2026-10-20 (`GameBuilder.BuildsRoot` has the scan that
found five copies of one day's player in five folders). And on the owner's profile the Desktop
is `OneDrive\\Desktop`, so `~/Desktop` was a folder that does not exist: every harness that
defaulted to it reported "no player" at a machine that had one.

WARNING: THE MAIN CHECKOUT, NOT THIS ONE. Sessions run from `.claude/worktrees/*`, and
`git rev-parse --git-common-dir` names the shared `.git` from any of them, so its parent is the
same folder whichever checkout asks. Without git, this checkout's own `Builds/` is the answer.
"""
import pathlib
import subprocess

REPO = pathlib.Path(__file__).resolve().parent.parent


def builds_root():
    try:
        common = subprocess.run(
            ["git", "rev-parse", "--path-format=absolute", "--git-common-dir"],
            cwd=REPO, capture_output=True, text=True, timeout=10, check=True,
        ).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        common = ""

    if common:
        path = pathlib.Path(common).resolve()
        if path.name.lower() == ".git":
            return path.parent / "Builds"

    return REPO / "Builds"


def windows_exe():
    return builds_root() / "Windows" / "TumbangPreso.exe"


def android_apk():
    return builds_root() / "Android" / "TumbangPreso.apk"


def mac_app():
    return builds_root() / "macOS" / "TumbangPreso.app"


def is_internal_build(path):
    """
    Whether `path` is a player inside a `Builds/` folder: the main checkout's or this one's.
    The same two roots `GameBuilder.IsInsideBuildsFolder` accepts, so a player the builder was
    allowed to write is a player these harnesses are allowed to launch.
    """
    path = pathlib.Path(path).resolve()
    return any(path.is_relative_to(root.resolve())
               for root in (builds_root(), REPO / "Builds"))


if __name__ == "__main__":
    print(builds_root())

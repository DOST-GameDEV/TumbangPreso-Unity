#!/usr/bin/env bash
# UNITY IN A CLOUD CONTAINER: install the editor, activate a licence, check it launches.
#
# ⚠️ WHY (2026-09-26): a cloud session had no editor, so nothing could be compiled for real,
# tested, filmed or built there, and every change waited for the Windows PC. This script is
# what made the first cloud PlayMode film possible. It is idempotent: run it from the cloud
# environment's setup script, or by hand, as often as you like.
#
#   bash tools/cloud_unity_setup.sh
#
# What it needs from the environment (set as environment variables, never in the repo):
#   UNITY_EMAIL, UNITY_PASSWORD   a Unity ID with a Personal licence. Activation uses Unity's
#                                 own licensing client (`--activate-ulf --include-personal`),
#                                 the route CI tools use; the editor's `-serial` flag only
#                                 activates paid seats.
# Optional:
#   UNITY_EDITOR_PATH             where the editor binary is (default /opt/tump/unity/Editor/Unity)
#
# ⚠️ Measured on the first run: the editor tarball is 4.3 GB (8.6 GB unpacked), the project's
# first import took about 6 minutes on 4 cores, and a PlayMode film at 1280x720 renders on
# Mesa's software OpenGL (llvmpipe) at roughly 1.3 frames per second of wall time.
# Launch Unity through `python3 tools/run_unity_guarded.py`, which adds the virtual display
# and the Linux64 target on this machine.
set -euo pipefail

VERSION=6000.5.8f1
CHANGESET=5cb7df797b7d
EDITOR="${UNITY_EDITOR_PATH:-/opt/tump/unity/Editor/Unity}"
ROOT_DIR="$(dirname "$(dirname "$EDITOR")")"
LICENCE="${XDG_DATA_HOME:-$HOME/.local/share}/unity3d/Unity/Unity_lic.ulf"

say() { printf '[cloud-unity] %s\n' "$*"; }

# 1. The system pieces a headless editor needs: a virtual display, software GL, ffmpeg for films.
missing=()
command -v xvfb-run >/dev/null || missing+=(xvfb)
command -v ffmpeg >/dev/null || missing+=(ffmpeg)
command -v glxinfo >/dev/null || missing+=(mesa-utils)
if [ ${#missing[@]} -gt 0 ]; then
    say "installing ${missing[*]}"
    apt-get update -q >/dev/null && apt-get install -y -q "${missing[@]}" libgl1 libglu1-mesa >/dev/null
fi

# 2. The editor, streamed straight into place (no 4.3 GB file left behind).
if [ -x "$EDITOR" ]; then
    say "editor present: $EDITOR"
else
    say "downloading Unity $VERSION ($CHANGESET) into $ROOT_DIR"
    mkdir -p "$ROOT_DIR"
    curl -sSL --retry 4 "https://download.unity3d.com/download_unity/$CHANGESET/LinuxEditorInstaller/Unity.tar.xz" \
        | tar -xJ -C "$ROOT_DIR"
    [ -x "$EDITOR" ] || { say "the download finished but $EDITOR is missing"; exit 1; }
fi

# 3. The licence.
if [ -s "$LICENCE" ]; then
    say "licence present: $LICENCE"
elif [ -n "${UNITY_EMAIL:-}" ] && [ -n "${UNITY_PASSWORD:-}" ]; then
    say "activating a Personal licence for the account in UNITY_EMAIL"
    "$ROOT_DIR/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client" \
        --activate-ulf --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD" --include-personal
    [ -s "$LICENCE" ] || { say "activation reported success but wrote no licence file"; exit 1; }
else
    say "no licence and no UNITY_EMAIL / UNITY_PASSWORD in the environment: the editor is installed but cannot run"
    exit 2
fi

say "ready: python3 tools/run_unity_guarded.py -batchmode -runTests -testPlatform EditMode ..."

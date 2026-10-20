#!/usr/bin/env bash
#
# Two player processes, one lobby, nobody at the keyboard.
#
# WHY THIS EXISTS. Section 52 of docs/TODO.md closed three lobby faults reasoned entirely from
# the source, and 52.6 wrote down in advance that none of it was a two machine test. It was not,
# and all three came back. The gap was never skill, it was reach: `-tp-host` and `-tp-join` both
# went straight to the arena, so the lobby screen was the one screen no automated run could open.
# `-tp-lobby` opens it and `-tp-autoseat` / `-tp-autoready` press its buttons.
#
# WHAT IT REPRODUCES. The host takes seat 0 and the client is seated at 1 on arrival. The client
# then asks for seat 2 and the host asks for seat 3, which is the reported fault word for word:
# a player cannot switch from p1 to p4. The host is given the LONGER delay on purpose, so its
# press lands after the client is in the lobby: switching seats alone always worked, and the
# report is about what happens once somebody else is there.
#
#   tools/probe_lobby.sh [port]
#
# Reads two logs into Logs/lobby_host.log and Logs/lobby_client.log. Neither process runs with
# -nographics: the lobby draws MapPreviewSurface into an offscreen camera, and NullGfxDevice dies
# inside RenderShadowMaps the first frame that happens. See CLAUDE.md section 6.

set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# The executable inside the bundle is named after the PRODUCT ("Tumbang Preso", with a space),
# not after the .app, so it is found rather than spelled out: renaming the product in
# PlayerSettings would otherwise break this script silently.
APP="$(ls "$ROOT"/Builds/macOS/TumbangPreso.app/Contents/MacOS/* 2>/dev/null | head -1)"
PORT="${1:-8910}"

HOST_LOG="$ROOT/Logs/lobby_host.log"
CLIENT_LOG="$ROOT/Logs/lobby_client.log"

if [ ! -x "$APP" ]; then
  echo "no player at $APP; build it first:"
  echo "  Unity -batchmode -nographics -projectPath . -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildMac"
  exit 1
fi

mkdir -p "$ROOT/Logs"
rm -f "$HOST_LOG" "$CLIENT_LOG"

COMMON="-screen-fullscreen 0 -screen-width 900 -screen-height 640"

"$APP" -tp-host "$PORT" -tp-lobby -tp-autoseat 3 -tp-autoready -tp-autodelay 14 \
       -tp-profile HOSTA -logFile "$HOST_LOG" $COMMON &
HOST_PID=$!

sleep 4

"$APP" -tp-join 127.0.0.1 "$PORT" -tp-lobby -tp-autoseat 2 -tp-autoready -tp-autodelay 6 \
       -tp-profile CLIENTB -logFile "$CLIENT_LOG" $COMMON &
CLIENT_PID=$!

# 60s is the whole run: 14s of settle, a seat round trip, a ready round trip, then the arena
# load on both sides with a 20s ceiling on the autopilot's own wait for it.
sleep 60

kill "$HOST_PID" "$CLIENT_PID" 2>/dev/null
wait "$HOST_PID" 2>/dev/null
wait "$CLIENT_PID" 2>/dev/null

echo "=============== HOST ==============="
grep -E "\[Net\]|\[NetBoot\]|\[Flow\]" "$HOST_LOG" 2>/dev/null
echo
echo "=============== CLIENT ============="
grep -E "\[Net\]|\[NetBoot\]|\[Flow\]" "$CLIENT_LOG" 2>/dev/null

#!/usr/bin/env bash
#
# The two verification gates, as a command rather than as a habit.
#
# ⚠️⚠️ THIS EXISTS BECAUSE "WHICH GATE DOES THIS CHANGE PAY FOR" WAS A CONVENTION NOBODY COULD
# RUN. `docs/TODO.md` § 7 item 2 asked for a named fast gate and a named full gate, and item 1
# (one launch for all five editor checks) landed while this stayed prose in a document. A rule
# that lives only in a document is a rule that is followed until somebody is in a hurry.
#
#   ./tools/verify.sh fast   every change
#   ./tools/verify.sh full   anything touching gameplay, and before every build
#
# ⚠️ IT ASSERTS ON THE XML, NEVER ON THE EXIT CODE. `CLAUDE.md` § 7: a PlayMode crash and a
# genuine test failure both come back as 0, and a run that wrote no XML at all is the third
# outcome that looks identical from the shell.
#
# ⚠️ PLAYMODE HAS NO `-nographics`. Adding it makes Unity select `NullGfxDevice`, the first
# offscreen camera dies inside it, no XML is written, and the run still exits 0.
#
# ⚠️ AND IT EXCLUDES `WallClock`. `AiDiagnosticProbe` runs at 1x for about 80 real seconds and
# its result depends on how busy the machine is; `docs/TODO.md` § 6 has the three failures and
# the decision. Run those deliberately with `./tools/verify.sh wallclock`.
set -u

UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe}"
MODE="${1:-fast}"
FAILED=0

say() { printf '\n=== %s\n' "$1"; }

# ⚠️ THE XML IS THE VERDICT. A missing file is a failure, not a pass.
xml_verdict() {
    local label="$1" path="$2"
    if [ ! -f "$path" ]; then
        printf '  FAIL  %-10s no %s was written, so the run never reported.\n' "$label" "$path"
        FAILED=1
        return
    fi
    python - "$label" "$path" <<'PY'
import sys, xml.etree.ElementTree as ET
label, path = sys.argv[1], sys.argv[2]
try:
    r = ET.parse(path).getroot()
except Exception as e:
    print(f'  FAIL  {label:<10} {path} is unreadable: {e}')
    raise SystemExit(1)
total, passed, failed = r.get('total'), r.get('passed'), r.get('failed')
ok = (failed == '0')
print(f'  {"OK  " if ok else "FAIL"}  {label:<10} {passed}/{total} passed, {failed} failed')
if not ok:
    for tc in r.iter('test-case'):
        if tc.get('result') != 'Passed':
            print(f'          {tc.get("fullname")}')
raise SystemExit(0 if ok else 1)
PY
    [ $? -ne 0 ] && FAILED=1
    return 0
}

run_core() {
    say "core rules, no Unity"
    if dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --nologo | tail -2; then
        :
    else
        FAILED=1
    fi
}

run_checks() {
    say "all five editor checks, one launch"
    "$UNITY" -batchmode -projectPath . \
        -executeMethod TumbangPreso.EditorTools.Checks.RunAll -logFile Logs/checks.log
    if [ -f Logs/checks.txt ]; then
        cat Logs/checks.txt
        grep -q '^RESULT: OK' Logs/checks.txt || FAILED=1
    else
        echo "  FAIL  checks     no Logs/checks.txt, so the launch never got that far."
        FAILED=1
    fi
}

run_editmode() {
    say "EditMode"
    rm -f Logs/edit.xml
    "$UNITY" -batchmode -runTests -nographics -projectPath . -testPlatform EditMode \
        -testResults Logs/edit.xml -logFile Logs/edit.log
    xml_verdict "editmode" Logs/edit.xml
}

run_playmode() {
    say "PlayMode, excluding WallClock"
    rm -f Logs/play.xml
    "$UNITY" -batchmode -runTests -projectPath . -testPlatform PlayMode \
        -testCategory '!WallClock' -testResults Logs/play.xml -logFile Logs/play.log
    xml_verdict "playmode" Logs/play.xml
}

run_wallclock() {
    say "PlayMode, WallClock only. Read the report; do not gate on it."
    rm -f Logs/ai.xml
    "$UNITY" -batchmode -runTests -projectPath . -testPlatform PlayMode \
        -testCategory 'WallClock' -testResults Logs/ai.xml -logFile Logs/ai.log
    xml_verdict "wallclock" Logs/ai.xml
}

case "$MODE" in
    fast)      run_core; run_checks; run_editmode ;;
    full)      run_core; run_checks; run_editmode; run_playmode ;;
    wallclock) run_wallclock ;;
    *)
        echo "usage: tools/verify.sh [fast|full|wallclock]"
        exit 2
        ;;
esac

say "verdict"
if [ "$FAILED" -eq 0 ]; then
    echo "  $MODE gate PASSED."
    exit 0
fi

echo "  $MODE gate FAILED. The per-suite lines above name what."
exit 1

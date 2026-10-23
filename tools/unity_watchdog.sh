#!/bin/bash
# Kills this checkout's Unity when its log goes silent for 120 s (a hung batch run).
#   bash tools/unity_watchdog.sh Logs/<run>.log
# Run beside `python tools/run_unity_guarded.py ... -logFile Logs/<run>.log`. Exits when Unity exits.
# Windows (Git Bash) and macOS.
log="$1"; last=-1; still=0
running() {
  if command -v tasklist >/dev/null 2>&1; then tasklist | grep -qi "^Unity.exe"
  else pgrep -f "Unity.*TumbangPreso-Unity" >/dev/null; fi
}
sleep 20
while running; do
  size=$(wc -c < "$log" 2>/dev/null || echo 0)
  if [ "$size" = "$last" ]; then still=$((still+10)); else still=0; last=$size; fi
  if [ $still -ge 120 ]; then
    echo "WATCHDOG: $log silent 120s, killing"
    if command -v powershell >/dev/null 2>&1; then
      powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | Where-Object { \$_.CommandLine -match 'TumbangPreso-Unity(?!-)' } | ForEach-Object { Stop-Process -Id \$_.ProcessId -Force }"
    else
      pkill -f "Unity.*TumbangPreso-Unity"
    fi
    exit 2
  fi
  sleep 10
done

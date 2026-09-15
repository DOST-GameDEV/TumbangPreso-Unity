#!/bin/bash
# usage: fixture_seeds.sh <projectdir> <label> <seeds...>  -- whole BotBehaviourProbe fixture, like tools/bot_sweep.py
P=$1; label=$2; shift 2
OUT=/Users/paul/Documents/GitHub/TumbangPreso-Unity/Logs/claude-c
UNITY=/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity
PROFILE="$HOME/Library/Application Support/BH Studios/Tumbang Preso"
cd "$P"; mkdir -p Logs
for seed in "$@"; do
  rm -f Logs/bot-behaviour-Classic-*.txt Logs/bot-behaviour-HeroStrike-*.txt
  "$UNITY" -projectPath "$P" -batchmode -runTests -buildTarget OSXUniversal -testPlatform PlayMode \
    -testCategory '!WallClock;!ThumbFloor' -testFilter TumbangPreso.PlayTests.BotBehaviourProbe \
    -testResults $OUT/$label-fixture-$seed.xml -logFile $OUT/$label-fixture-$seed.log -tp-bot-seed $seed
  for f in Logs/bot-behaviour-*-*.txt; do cp "$f" "$OUT/$label-$seed-$(basename $f)"; done
  echo "seed $seed: $(grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' $OUT/$label-fixture-$seed.xml | head -1) ilalim $(grep -o 'idle penalties [0-9]*' Logs/bot-behaviour-HeroStrike-IlalimNgTulay.txt)"
done

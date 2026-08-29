#!/usr/bin/env bash
#
# Move this project to a different UGS project, and prove the move landed.
#
# ⚠️⚠️ THE LINK IS TWO LINES IN ProjectSettings.asset AND NOTHING ELSE.
# `cloudProjectId` and `organizationId` are the only places the project id is written; no
# C# file, no manifest and no document holds a copy. `PlayerAccount.CallCloudAsync` reads
# `Application.cloudProjectId` at runtime, so it follows this file automatically. That is
# why relinking is a text edit rather than a migration, and it is worth keeping true.
#
# ⚠️⚠️ EVERY EXISTING ANONYMOUS PLAYER ID DIES WITH THE OLD PROJECT. A UGS PlayerId is scoped
# to its project, so relinking is not a transfer, it is a reset. That costs nothing while no
# real accounts exist, which is exactly why this is being done now rather than after Phase 2
# gives people profiles and stats worth keeping.
#
# ⚠️⚠️ AND EVERY MACHINE MUST AGREE. Two builds pointed at different UGS projects cannot see
# each other's online lobbies at all: the join code resolves in a different namespace and the
# room is simply not there. It does NOT look like a configuration error, it looks like the
# lobby is empty, which is the expensive way to discover this at a venue. LAN discovery is
# unaffected because it never touches UGS. Rebuild every machine off the same branch after a
# relink, exactly as `NetSession.ProtocolVersion` already forces for the wire format.
#
# Usage:
#   tools/relink_ugs_project.sh                      # report the current link and stop
#   tools/relink_ugs_project.sh <projectId> <orgId>  # relink, then verify
#
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SETTINGS="$ROOT/ProjectSettings/ProjectSettings.asset"
UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe"

read_setting() { sed -n "s/^  $1: \(.*\)$/\1/p" "$SETTINGS" | head -1; }

CUR_ID="$(read_setting cloudProjectId)"
CUR_ORG="$(read_setting organizationId)"
CUR_NAME="$(read_setting projectName)"

echo "current link"
echo "  cloudProjectId : ${CUR_ID:-<empty>}"
echo "  organizationId : ${CUR_ORG:-<empty>}"
echo "  projectName    : ${CUR_NAME:-<empty>}"

if [ "$#" -eq 0 ]; then
  echo
  echo "no change requested. Pass a project id and an organization id to relink."
  exit 0
fi

if [ "$#" -ne 2 ]; then
  echo "error: expected exactly two arguments, <projectId> and <organizationId>." >&2
  exit 2
fi

NEW_ID="$1"
NEW_ORG="$2"

# A mistyped id is indistinguishable from an unreachable service at runtime: both answer
# "not signed in". Refuse the shape rather than let that become a debugging session.
if ! printf '%s' "$NEW_ID" | grep -Eq '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'; then
  echo "error: '$NEW_ID' is not a UGS project id. It is a UUID, like the one printed above." >&2
  exit 2
fi

if [ "$NEW_ID" = "$CUR_ID" ]; then
  echo
  echo "already linked to $NEW_ID. Nothing to change."
  exit 0
fi

echo
echo "relinking to $NEW_ID (org $NEW_ORG)"

python - "$SETTINGS" "$NEW_ID" "$NEW_ORG" <<'PY'
import io, re, sys
path, new_id, new_org = sys.argv[1], sys.argv[2], sys.argv[3]
s = io.open(path, encoding='utf-8').read()
s, n1 = re.subn(r'^(  cloudProjectId: ).*$', r'\g<1>' + new_id,  s, count=1, flags=re.M)
s, n2 = re.subn(r'^(  organizationId: ).*$', r'\g<1>' + new_org, s, count=1, flags=re.M)
if n1 != 1 or n2 != 1:
    sys.exit(f"error: expected one cloudProjectId and one organizationId, replaced {n1} and {n2}")
io.open(path, 'w', encoding='utf-8', newline='').write(s)
print("  ProjectSettings.asset rewritten")
PY
[ $? -ne 0 ] && exit 1

echo "  now: cloudProjectId $(read_setting cloudProjectId), organizationId $(read_setting organizationId)"
echo
echo "relink done. What it can and cannot prove from here:"
echo
# ⚠️⚠️ UGS CANNOT BE VERIFIED HEADLESSLY AND THIS SCRIPT MUST NOT PRETEND IT CAN.
# `UnityServices.InitializeAsync` refuses outside Play Mode with "Unity Services can only be
# initialized in Play Mode", so a batchmode `UgsCheck.Run` reports step 2 and then fails step 3
# for a reason that has nothing to do with the project being wrong. Batchmode also has no Hub
# session token, so it cannot see the signed-in account either. Running it here would produce a
# confident FAIL against a perfectly good project, which is worse than not running it.
mkdir -p "$ROOT/Logs"
echo "  Relay and Lobby are only reachable from Play Mode, so the real check is the menu item:"
echo "    open the project, then Tumbang Preso > Check UGS Wiring"
echo "  It prints the signed-in user, the linked project, and one line per service."
echo
echo "  The headless half that IS meaningful, sign-in settling at boot:"
echo "    \"$UNITY\" -batchmode -runTests -projectPath . -testPlatform PlayMode \\"
echo "      -testFilter TumbangPreso.PlayTests.OnlineSignInProbe \\"
echo "      -testResults Logs/play.xml -logFile Logs/play.log"
echo "  ⚠️ Read Logs/play.xml, never the exit code. CLAUDE.md section 7."

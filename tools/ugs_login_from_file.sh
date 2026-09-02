#!/usr/bin/env bash
#
# Log the UGS CLI in from a two-line file, then destroy the file.
#
# ⚠️⚠️ THE POINT OF THIS SCRIPT IS THAT THE SECRET IS NEVER TYPED INTO A CHAT, A COMMAND LINE
# OR A COMMIT. A service-account secret in a chat transcript is in that transcript permanently,
# and one on a command line is in the shell history and in every process listing on the machine
# while it runs. This reads the two values from a file nobody has to read aloud, pipes them
# straight into `ugs login`, and shreds the file afterwards whether the login worked or not.
#
# ⚠️ THE FILE PATH IS DELIBERATELY OUTSIDE THE REPO. Dropping credentials anywhere under a git
# working tree is how they end up committed, and `.gitignore` is one `git add -f` away from not
# helping. The scratch directory is not version controlled at all.
#
# Usage:
#   1. Create the file with EXACTLY two lines, key id first, secret second:
#        <key id>
#        <secret key>
#   2. tools/ugs_login_from_file.sh <path to that file>
#
set -uo pipefail

CREDS="${1:-}"

if [ -z "$CREDS" ]; then
  echo "usage: tools/ugs_login_from_file.sh <path to two-line credentials file>" >&2
  exit 2
fi

if [ ! -f "$CREDS" ]; then
  echo "error: no file at '$CREDS'." >&2
  exit 2
fi

# ⚠️ VALIDATED BY SHAPE, NEVER BY PRINTING. A wrong file here fails inside `ugs login` with a
# generic error, and the obvious way to debug that is to echo the file, which is the one thing
# this script exists to avoid. Count the lines instead and say what is wrong without showing it.
LINES="$(grep -c '' "$CREDS")"
if [ "$LINES" -ne 2 ]; then
  echo "error: expected exactly 2 lines (key id, then secret), found $LINES." >&2
  echo "       Not printing the file. Fix it and re-run." >&2
  exit 2
fi

shred_creds() {
  # `shred` is not always present on Git Bash, so overwrite then delete either way.
  if command -v shred >/dev/null 2>&1; then
    shred -u "$CREDS" 2>/dev/null && return
  fi
  : > "$CREDS"
  rm -f "$CREDS"
}
trap shred_creds EXIT

export PATH="$PATH:/c/Users/matth/AppData/Roaming/npm"

echo "logging the UGS CLI in, reading from the file"
ugs login < "$CREDS" 2>&1 | grep -v -i "deprecat" | grep -v "trace-deprecation"

echo
echo "verifying by listing Cloud Code scripts on the configured project"
ugs cloud-code scripts list 2>&1 | grep -v -i "deprecat" | grep -v "trace-deprecation" | head -20

echo
echo "credentials file shredded on exit."

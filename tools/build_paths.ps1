# Prints the Builds folder every player is written to: `Builds/` at the root of the MAIN
# checkout, the same folder `GameBuilder.BuildsRoot` and `tools/build_paths.py` answer.
#
# The probe and capture scripts used to default to `$env:USERPROFILE\Desktop\TumbangPreso-Unity`.
# The builder no longer writes there, and on a profile whose Desktop is redirected to OneDrive
# that path never existed. `tools/build_paths.py` has the whole reason.
#
# Usage, as a parameter default:
#
#     [string]$Exe = (Join-Path (& "$PSScriptRoot\build_paths.ps1") 'Windows\TumbangPreso.exe')

# A caller running with ErrorActionPreference Stop must not die on git's stderr when there is
# no repository; the fallback below is the answer to that.
$ErrorActionPreference = 'Continue'

$repo = Split-Path -Parent $PSScriptRoot
$common = & git -C $repo rev-parse --path-format=absolute --git-common-dir 2>$null

if ($LASTEXITCODE -eq 0 -and $common -and (Split-Path -Leaf $common) -eq '.git') {
    Join-Path (Split-Path -Parent $common) 'Builds'
} else {
    Join-Path $repo 'Builds'
}

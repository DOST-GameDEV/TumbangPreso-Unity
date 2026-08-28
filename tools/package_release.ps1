# Packages the built Windows player into a folder that can be handed to somebody.
#
# Usage, from the repo root, after GameBuilder.BuildWindows has run:
#
#     powershell -ExecutionPolicy Bypass -File tools/package_release.ps1
#     powershell -ExecutionPolicy Bypass -File tools/package_release.ps1 -Zip
#
# WHAT THIS IS FOR
#
# The build output is a Unity player directory: an .exe beside UnityPlayer.dll, a _Data folder,
# MonoBleedingEdge and a Burst debug folder whose own name says not to ship it. Handing that to a
# tester is handing them a build artifact and hoping they find the right file. This produces the
# thing you actually send: the game, its icon, one page telling them what it is, and a script that
# puts a shortcut on their Desktop.
#
# THE DECISIONS, AND WHY
#
# * THE .exe STAYS AT THE ROOT. Burying it in a `Game\` subfolder and putting a .lnk beside it
#   reads tidier and breaks the moment the folder is zipped and extracted somewhere else: a .lnk
#   stores an ABSOLUTE target, and `WScript.Shell` has no way to write a relative one. A player
#   directory with the .exe at the top is what every Unity game ships as, it survives being moved,
#   emailed, extracted to a different drive or run off a USB stick, and the .exe already carries
#   the game's own icon from PlayerSettings, so it looks right in Explorer with no tricks at all.
#
# * THE SHORTCUT IS CREATED ON THE RECIPIENT'S MACHINE, NOT SHIPPED. Same reason. The .bat runs
#   where the folder actually ended up, so the shortcut it writes is always correct.
#
# * THE BURST DEBUG FOLDER IS DELETED. Unity names it `*_BurstDebugInformation_DoNotShip`. It is
#   tens of megabytes of symbols for a machine that will never debug this build.
#
# * `desktop.ini` GIVES THE FOLDER THE GAME'S ICON. It only survives a direct copy (a zip drops
#   the attributes it needs), so it is a nice-to-have on a USB stick or a network share rather
#   than something to rely on. It costs two files and cannot break anything if it is ignored.

[CmdletBinding()]
param(
    # Also produce a .zip beside the folder, which is what actually gets sent.
    [switch] $Zip
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$desktop = [Environment]::GetFolderPath('DesktopDirectory')

$build = Join-Path $desktop 'TumbangPreso-Unity'
$exe = Join-Path $build 'TumbangPreso.exe'

# The version and the product name come out of ProjectSettings rather than being typed here, or
# the folder a sponsor receives says 1.00 for the rest of the project's life.
$settings = Get-Content (Join-Path $repo 'ProjectSettings\ProjectSettings.asset') -Raw
$version = if ($settings -match '(?m)^\s*bundleVersion:\s*(.+)$') { $Matches[1].Trim() } else { '1.00' }
$product = if ($settings -match '(?m)^\s*productName:\s*(.+)$') { $Matches[1].Trim() } else { 'Tumbang Preso' }

$release = Join-Path $desktop "$product v$version"

Write-Output "product : $product"
Write-Output "version : $version"
Write-Output "source  : $build"
Write-Output "target  : $release"

if (-not (Test-Path $exe)) {
    throw "no player at $exe. Run GameBuilder.BuildWindows first; see CLAUDE.md section 7."
}

# ---------------------------------------------------------------------------------------------
# The folder
# ---------------------------------------------------------------------------------------------

# The whole point of a release folder is that it is exactly the current build. A stale file left
# from a previous version is the fault this repo already records for the build output itself.
if (Test-Path $release) { Remove-Item $release -Recurse -Force }
New-Item -ItemType Directory -Path $release | Out-Null

Copy-Item (Join-Path $build '*') -Destination $release -Recurse -Force

Get-ChildItem $release -Directory -Filter '*BurstDebugInformation_DoNotShip' |
    ForEach-Object {
        Write-Output "removed  : $($_.Name)"
        Remove-Item $_.FullName -Recurse -Force
    }

# ---------------------------------------------------------------------------------------------
# The art
# ---------------------------------------------------------------------------------------------

$icon = Join-Path $release 'TumbangPreso.ico'
$sourceIcon = Join-Path $repo 'Assets\TumbangPreso\Art\ui\brand\app_icon.png'

# ⚠️ THE .ico IS BUILT FROM THE TEAM'S OWN BADGE RATHER THAN SHIPPED AS A BINARY. See
# tools/make_ico.js: the source art is 1254 px and an ICO cannot describe anything over 256.
& node (Join-Path $repo 'tools\make_ico.js') $sourceIcon $icon
if ($LASTEXITCODE -ne 0) { throw 'make_ico.js failed' }

# The cover is what a folder shows in Explorer's tile and content views, and it is also the image
# to paste when somebody asks what the game is.
Copy-Item (Join-Path $repo 'Assets\TumbangPreso\Art\ui\brand\app_icon.png') `
          (Join-Path $release "$product.png") -Force

# ---------------------------------------------------------------------------------------------
# The page
# ---------------------------------------------------------------------------------------------

$readme = @"
================================================================================

     T U M B A N G   P R E S O                                    v$version

     Four kids, one street, one can. Knock it down, get your slipper back.

================================================================================

  HOW TO START

     1.  Double-click  Create Desktop Shortcut.bat
         That puts a Tumbang Preso icon on your Desktop. You only do this once.

     2.  Or just open  TumbangPreso.exe  right here in this folder.

     Windows may say it does not recognise the app. That is SmartScreen asking
     about any program without a paid certificate, not a warning about this one.
     Click  More info  then  Run anyway.


  THE GAME

     Four players, one taya (the defender), 4 rounds of 90 seconds.

     The taya guards the can. Everyone else throws a slipper at it.

     The taya ROTATES every round, so everybody defends exactly once, and
     everybody attacks three times. Empty seats are filled by bots.

     The throw is the easy part. Getting your slipper BACK is the game: while
     the can is standing the taya can tag you, and a tagged attacker is out
     until the round resets.

       Knock the can down          +100 to whoever threw
       Tag an attacker             +100 to the taya
       Sabotage                     +50
       Can left standing            +10 a second to the taya


  CONTROLS

     Move                 W A S D
     Look                 Mouse
     Sprint               Shift
     Jump                 Space
     Throw                Left mouse, hold to charge
     Pick up / shove      E     tap to pick up, hold to shove
     Reset the can        E     hold, as the taya
     Emote wheel          G
     Chat                 Enter
     Pause                Esc

     Every key can be changed in SETTINGS > CONTROLS.


  PLAYING WITH OTHER PEOPLE

     Press PLAY. You land in the lobby and it opens a room on your network
     straight away.

       On the same wifi    Your friends press PLAY, then LOBBY & SERVERS,
                           then JOIN. Your game shows up in their list.

       Anywhere else       Press LOBBY & SERVERS, then START SERVER, and read
                           the four-character code out to them. They type it
                           into the same panel.

     Up to four people share one lobby. Any seat nobody takes is played by a
     bot, so you never have to wait for a full room to start.

     Everybody must be on the SAME VERSION. Different builds refuse each other
     on purpose, because a half-matching build looks like the game is broken.


  MODES

     CLASSIC        The street game. No powers. Four rounds.
     HERO STRIKE    Five heroes, two skills and an ultimate each. Eight rounds.

     Neither is the "real" one. Pick whichever you want to play.


  IF SOMETHING GOES WRONG

     Tell us what you were doing and what you expected instead. A screenshot is
     worth more than a description, and the version number in the bottom-right
     corner of every screen tells us which build you were on.

================================================================================

  BH Studios  ·  1st place, Gear Up NCR Esports Game Dev Challenge
  Representing NCR at the nationals in General Santos City

================================================================================
"@

# ⚠️ CRLF AND UTF-8, because this is opened in Notepad by somebody who is not a developer. A file
# with bare LF renders as one unbroken line in older Notepad, and the whole point of this page is
# that it is readable at a glance.
$readme = $readme -replace "`r?`n", "`r`n"
[System.IO.File]::WriteAllText((Join-Path $release 'READ ME FIRST.txt'), $readme,
                               (New-Object System.Text.UTF8Encoding $false))

# ---------------------------------------------------------------------------------------------
# The shortcut maker
# ---------------------------------------------------------------------------------------------

# ⚠️⚠️ IT RESOLVES ITS OWN LOCATION WITH %~dp0 AND THAT IS THE WHOLE TRICK. The recipient extracts
# this anywhere: Downloads, a USB stick, D:\Games. `%~dp0` is the directory the .bat is sitting in
# at the moment it runs, so the shortcut it writes always points at the right copy.
#
# ⚠️ THE TRAILING BACKSLASH ON %~dp0 IS WHY THE PATHS BELOW HAVE NO SEPARATOR BEFORE THE FILENAME.
# Writing "%~dp0\TumbangPreso.exe" yields a doubled backslash, which works by luck in most places
# and not in a shortcut's WorkingDirectory.
#
# ⚠️⚠️ THE `$d` AND `$s` INSIDE THE POWERSHELL LINE ARE BACKTICK-ESCAPED, AND THEY HAVE TO BE. This
# is a DOUBLE-quoted here-string, because `$product` and `$version` are meant to be substituted
# now; without the escapes `$d` and `$s` are substituted now as well, to nothing, and the .bat
# ships containing `=[Environment]::GetFolderPath(...)`, which will not parse. That is what the
# first test of this produced. The failure was at least loud: the .bat checks `errorlevel` and
# tells the player to open the .exe instead, rather than reporting success over a broken shortcut.
#
# ⚠️ `pause` RATHER THAN `timeout`. `timeout` refuses to run at all when stdin is redirected
# ("Input redirection is not supported"), which is how it behaves under any automation that drives
# this, and a launcher whose last line errors reads as a launcher that failed.
$bat = @"
@echo off
setlocal

rem Puts a Tumbang Preso shortcut on this computer's Desktop, pointing at wherever
rem this folder happens to be. Safe to run more than once: it overwrites.

set "GAME=%~dp0TumbangPreso.exe"
set "ICON=%~dp0TumbangPreso.ico"

if not exist "%GAME%" (
    echo.
    echo   Could not find TumbangPreso.exe next to this file.
    echo   Keep this .bat inside the game folder and run it again.
    echo.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "`$d=[Environment]::GetFolderPath('DesktopDirectory');" ^
  "`$s=(New-Object -ComObject WScript.Shell).CreateShortcut((Join-Path `$d '$product.lnk'));" ^
  "`$s.TargetPath='%GAME%';" ^
  "`$s.WorkingDirectory='%~dp0';" ^
  "`$s.IconLocation='%ICON%';" ^
  "`$s.Description='$product v$version';" ^
  "`$s.Save()"

if errorlevel 1 (
    echo.
    echo   Could not create the shortcut. You can still play by opening
    echo   TumbangPreso.exe in this folder.
    echo.
    pause
    exit /b 1
)

echo.
echo   Done. There is a $product icon on your Desktop now.
echo   You can close this window.
echo.
pause
"@

$bat = $bat -replace "`r?`n", "`r`n"
[System.IO.File]::WriteAllText((Join-Path $release 'Create Desktop Shortcut.bat'), $bat,
                               (New-Object System.Text.ASCIIEncoding))

# ---------------------------------------------------------------------------------------------
# The folder's own icon
# ---------------------------------------------------------------------------------------------

# ⚠️ `desktop.ini` ONLY WORKS ON A FOLDER MARKED READ-ONLY OR SYSTEM, and the .ini itself has to be
# hidden or it shows up as a stray file in the folder somebody just opened. Both attributes are set
# here because leaving either off makes this silently do nothing, which reads as the icon being
# wrong rather than as the mechanism not being armed.
$ini = @"
[.ShellClassInfo]
IconResource=TumbangPreso.ico,0
[ViewState]
Mode=
Vid=
FolderType=Generic
"@

$ini = $ini -replace "`r?`n", "`r`n"
$iniPath = Join-Path $release 'desktop.ini'
[System.IO.File]::WriteAllText($iniPath, $ini, (New-Object System.Text.UTF8Encoding $false))

(Get-Item $iniPath -Force).Attributes = 'Hidden, System, Archive'
(Get-Item $release).Attributes = (Get-Item $release).Attributes -bor [IO.FileAttributes]::ReadOnly

# ---------------------------------------------------------------------------------------------
# Report, and optionally zip
# ---------------------------------------------------------------------------------------------

$bytes = (Get-ChildItem $release -Recurse -File -Force | Measure-Object -Property Length -Sum).Sum
Write-Output ''
Write-Output ("folder  : {0}  ({1:N0} MB)" -f $release, ($bytes / 1MB))

Get-ChildItem $release -Force |
    Sort-Object { $_.PSIsContainer }, Name |
    ForEach-Object { Write-Output ("          {0}" -f $_.Name) }

if ($Zip) {
    $archive = "$release.zip"
    if (Test-Path $archive) { Remove-Item $archive -Force }

    Write-Output ''
    Write-Output "zipping : $archive"

    Compress-Archive -Path (Join-Path $release '*') -DestinationPath $archive -CompressionLevel Optimal

    $zipMb = (Get-Item $archive).Length / 1MB
    Write-Output ("zip     : {0}  ({1:N0} MB)" -f $archive, $zipMb)
}

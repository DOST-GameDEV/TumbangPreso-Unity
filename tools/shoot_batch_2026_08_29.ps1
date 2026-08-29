# Photographs everything 🧑 asked about on 2026-08-29, from the BUILT PLAYER.
#
# ⚠️ THE BUILT PLAYER, NOT A PROBE. The spectator/first-person shader comparison, the held
# tsinelas and the lata card are things the editor renders differently or not at all;
# `docs/TODO.md` § 43 records an IKE fix signed off on a clean editor render that was still
# broken in the build.
#
# ⚠️⚠️ FULLSCREEN, AND THE WHOLE SCREEN IS CAPTURED, NOT A CLIENT RECT. The first version of this
# script ran the player in a window and captured `GetClientRect`, and the rect it computed
# extended past the actual window: the bottom fifth of every shot was the DESKTOP BEHIND THE
# GAME, which read exactly like a HUD element that had stopped drawing. The lata card was
# "missing" from four screenshots that had simply photographed the wrong rectangle. Borderless
# fullscreen at the native resolution removes the question, and it is also what
# `tools/shoot_charselect.ps1` already assumes.
#
# ⚠️ VERSIONED FILENAMES, per `CLAUDE.md` § 6.1. Bump -Tag, never overwrite: chat clients cache
# by filename and a re-shoot that overwrites is reviewed against an image no longer on disk.
#
# Run:  powershell -File tools/shoot_batch_2026_08_29.ps1 -Tag v2

param(
  [string]$Exe = "$env:USERPROFILE\Desktop\TumbangPreso-Unity\TumbangPreso.exe",
  [string]$OutDir = "Logs\shots-2026-08-29",
  [string]$Tag = "v2",
  [int]$BootSeconds = 30
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Shot {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, int e);
}
"@

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$script:proc = $null

function Snap([string]$name) {
  $bmp = New-Object System.Drawing.Bitmap($screen.Width, $screen.Height)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($screen.X, $screen.Y, 0, 0, (New-Object System.Drawing.Size($screen.Width, $screen.Height)))
  $path = Join-Path $OutDir "$name`_$Tag.png"
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Output "  $path ($($screen.Width)x$($screen.Height))"
}

function Launch([string[]]$extra) {
  Get-Process TumbangPreso -ErrorAction SilentlyContinue | Stop-Process -Force
  Start-Sleep -Seconds 2
  $script:proc = Start-Process -FilePath $Exe -PassThru -ArgumentList (@("-screen-fullscreen","1") + $extra)
  Start-Sleep -Seconds $BootSeconds
  [void][Shot]::SetForegroundWindow($script:proc.MainWindowHandle)
  Start-Sleep -Seconds 3
}

function Kill() {
  Get-Process TumbangPreso -ErrorAction SilentlyContinue | Stop-Process -Force
  Start-Sleep -Seconds 2
}

# ---------------------------------------------------------------- A · FIRST PERSON
# ⚠️ IT WAITS FOR ROUND 2 ON PURPOSE. The taya is `(round - 1) % 4`, so in round 1 the local
# seat 0 IS the defender, and a defender never holds a tsinelas: the first cut of these shots
# photographed two empty hands and proved nothing about the held slipper. Round 2 puts seat 0 on
# offence with its own shoe in hand. A round is 90 s plus the intermission.
Write-Output "A - first person: held slipper and wind-up (waiting for round 2)"
Launch @("-tp-host","8950","-tp-profile","shotA","-tp-autostart","1","-logFile","$OutDir\a.log")
Start-Sleep -Seconds 108
Snap "01-fpp-held-slipper"

# ⚠️ THE WIND-UP IS A HELD PRESS, NOT A CLICK. `Carrier.StepAttacker` charges while
# `Verb.SpecialAbility` (<Mouse>/leftButton) is DOWN and throws on release, so a click
# photographs the throw and never the pose.
[void][Shot]::SetCursorPos([int]($screen.Width / 2), [int]($screen.Height / 2))
Start-Sleep -Milliseconds 400
[Shot]::mouse_event(0x0002, 0, 0, 0, 0)
Start-Sleep -Milliseconds 800
Snap "02-fpp-windup"
Start-Sleep -Milliseconds 700
Snap "03-fpp-windup-fuller"
[Shot]::mouse_event(0x0004, 0, 0, 0, 0)
Start-Sleep -Milliseconds 1200
Snap "04-fpp-after-throw"
Kill

# ---------------------------------------------------------------- B · SPECTATOR
# -tp-allbots drives HumanSeat < 0, which is what puts this process on the spectator rig. This
# is the frame that had no ink pass before this batch.
Write-Output "B - spectator: ink parity and the lata card"
Launch @("-tp-host","8951","-tp-profile","shotB","-tp-allbots","-tp-autostart","1","-logFile","$OutDir\b.log")
Start-Sleep -Seconds 20
Snap "05-spectator-ink"
Start-Sleep -Seconds 25
Snap "06-spectator-later"
Kill

Write-Output "done"

# Launches the built player, drives it with real mouse clicks, and photographs the window.
#
# WHY THIS EXISTS: a PlayMode capture runs inside the editor, where scenes load by path,
# shaders are all present and the game view is whatever size the batch runner picked. Half
# the faults this port has produced only exist in a player: a stripped shader, a scene
# missing from the build settings, a locked cursor, an input backend that is switched off,
# a hit rect that does not sit under its own artwork. This is the only check that sees what
# the person opening the .exe sees.
#
# Keyboard synthesis does NOT reach the game window from a background shell. Mouse clicks
# do, which is why every step here is a click.
#
# ⚠️ EVERY COORDINATE IS A FRACTION OF THE CLIENT RECT, NEVER THE WINDOW RECT. A windowed
# player has a title bar and a border, so the same fraction of the WINDOW lands several per
# cent lower than that fraction of the picture — which on the mode screen is the difference
# between SINGLE PLAYER and MULTIPLAYER, and reads as "the button did the wrong thing".

param(
  [string]$Exe = "$env:USERPROFILE\Desktop\TumbangPreso-Unity\TumbangPreso.exe",
  [string]$OutDir = "Logs\shots-player"
)

Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Shot {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, int e);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$proc = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds 12
[void][Shot]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Seconds 2

function Client() {
  $r = New-Object Shot+RECT
  [void][Shot]::GetClientRect($proc.MainWindowHandle, [ref]$r)
  $p = New-Object Shot+POINT
  $p.X = 0; $p.Y = 0
  [void][Shot]::ClientToScreen($proc.MainWindowHandle, [ref]$p)
  return @{ X = $p.X; Y = $p.Y; W = $r.R - $r.L; H = $r.B - $r.T }
}

function Snap([string]$name) {
  $c = Client
  if ($c.W -le 0) { "  no window for $name"; return }

  $bmp = New-Object System.Drawing.Bitmap $c.W, $c.H
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($c.X, $c.Y, 0, 0, $bmp.Size)
  $bmp.Save("$OutDir\$name.png", [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  "  $name"
}

function Aim([double]$fx, [double]$fy) {
  $c = Client
  [void][Shot]::SetForegroundWindow($proc.MainWindowHandle)
  [void][Shot]::SetCursorPos([int]($c.X + $c.W * $fx), [int]($c.Y + $c.H * $fy))
  Start-Sleep -Milliseconds 350
}

# ⚠️ AIM IN THE .tscn's OWN COORDINATES, NOT IN FRACTIONS OF THE WINDOW. The canvases match
# on HEIGHT against a 1920x1080 reference, so a reference point maps to the client area by
# one scale factor derived from the HEIGHT alone, whatever the window's width happens to be.
# Fractions of the window drift the moment the player remembers a different size from its
# last run, and a drift of three per cent is the difference between SINGLE PLAYER and
# MULTIPLAYER on the mode screen.
function AimRef([double]$rx, [double]$ry) {
  $c = Client
  $k = $c.H / 1080.0
  $x = [int]($c.X + $c.W * 0.5 + ($rx - 960.0) * $k)
  $y = [int]($c.Y + $ry * $k)
  [void][Shot]::SetForegroundWindow($proc.MainWindowHandle)
  [void][Shot]::SetCursorPos($x, $y)
  Start-Sleep -Milliseconds 350
}

function Tap() {
  [Shot]::mouse_event(0x0002, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 90
  [Shot]::mouse_event(0x0004, 0, 0, 0, 0)
  Start-Sleep -Seconds 2
}

# Fractions are the .tscn's own rects over 1920x1080.
Snap "01-title"

AimRef 236 471 ; Tap ; Snap "02-mode"           # PLAY          (-123..596, 374..568)
AimRef 402 450 ; Tap ; Snap "03-setup"          # SINGLE PLAYER (95..709, 400..501)
AimRef 390 672 ; Tap ; Snap "04-match-ready"    # START MATCH

Start-Sleep -Seconds 4
Snap "05-match"

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
"done"

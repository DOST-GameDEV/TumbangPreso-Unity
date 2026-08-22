# Photographs the CHARACTER screen in the BUILT PLAYER, at the monitor's own resolution.
#
# WHY THIS EXISTS AND WHY THE PLAYMODE CAPTURES DO NOT REPLACE IT: the in-editor screenshot
# helper photographs an overlay canvas by flipping it to ScreenSpaceCamera and rendering it
# through the gameplay camera. That is the only way to get an overlay into a RenderTexture,
# and it puts the UI through the colour grade and through a resample. Text always comes back
# softer than it is on screen, so those PNGs cannot answer "is the type crisp", which is the
# question the character screen has to pass. A player screenshot is the framebuffer itself.
#
# ⚠️ THE BUILD IS BORDERLESS FULLSCREEN AT THE NATIVE RESOLUTION NOW, so the client rect is
# the whole monitor and a fraction of the client rect is a fraction of the picture. That was
# not true of the old forced 1600x900 window, where the title bar shifted every click down.
#
# ⚠️ CLICKS ARE GIVEN IN THE AUTHORED 1920x1080 SPACE, like every other tool here, because
# every screen in this game is laid out in it. They are converted to the live client rect.

param(
  [string]$Exe = "$env:USERPROFILE\Desktop\TumbangPreso-Unity\TumbangPreso.exe",
  [string]$OutDir = "Logs\shots-charselect",
  [string[]]$Clicks = @("236,471", "402,450"),
  [int]$BootSeconds = 22
)

Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class CharShot {
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

# ⚠️ THE BOOT WAIT IS LONG ON PURPOSE. The BH Studios screen is no longer skippable: it
# holds until the shader warm, the roster book, the audio bank and the MainMenu async load
# have all finished. Snapping before that photographs the loading screen and reports it as
# a broken menu.
Start-Sleep -Seconds $BootSeconds

[void][CharShot]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Seconds 2

function Client() {
  $r = New-Object CharShot+RECT
  [void][CharShot]::GetClientRect($proc.MainWindowHandle, [ref]$r)
  $p = New-Object CharShot+POINT
  $p.X = 0; $p.Y = 0
  [void][CharShot]::ClientToScreen($proc.MainWindowHandle, [ref]$p)
  return @{ X = $p.X; Y = $p.Y; W = $r.R - $r.L; H = $r.B - $r.T }
}

function Snap([string]$name) {
  $c = Client
  $bmp = New-Object System.Drawing.Bitmap($c.W, $c.H)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($c.X, $c.Y, 0, 0, (New-Object System.Drawing.Size($c.W, $c.H)))
  $path = Join-Path $OutDir "$name.png"
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  "$path  ($($c.W)x$($c.H))"
}

function AimRef([double]$rx, [double]$ry) {
  $c = Client
  $x = [int]($c.X + $c.W * ($rx / 1920.0))
  $y = [int]($c.Y + $c.H * ($ry / 1080.0))
  [void][CharShot]::SetCursorPos($x, $y)
  Start-Sleep -Milliseconds 220
}

function Tap() {
  [CharShot]::mouse_event(0x0002, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 90
  [CharShot]::mouse_event(0x0004, 0, 0, 0, 0)
  Start-Sleep -Seconds 2
}

Snap "00-title"

$step = 1
foreach ($click in $Clicks) {
  $parts = $click.Split(",")
  AimRef ([double]$parts[0]) ([double]$parts[1])
  Tap
  Snap ("{0:D2}-after-{1}-{2}" -f $step, $parts[0], $parts[1])
  $step++
}

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
"done"

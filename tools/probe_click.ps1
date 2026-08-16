# Isolates "the pointer never reaches the control" from "the click never fires".
#
# The wood buttons change face on hover, so a screenshot taken with the cursor resting on a
# control answers the first question on its own: a lit face means the raycast lands and only
# the click is missing; an unlit face means the pointer is not getting there at all, and the
# fix is a rect or a raycast target rather than a listener.

param(
  [string]$Exe = "$env:USERPROFILE\Desktop\TumbangPreso-Unity\TumbangPreso.exe",
  [string]$OutDir = "Logs\shots-player"
)

Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W2 {
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
[void][W2]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Seconds 2

# ⚠️ THE CLIENT RECT, NOT THE WINDOW RECT. A windowed player has a title bar and a border,
# so a fraction of the WINDOW is several per cent lower than the same fraction of the
# picture the game is drawing. On a control 90 px tall that is most of its height.
function Client() {
  $r = New-Object W2+RECT
  [void][W2]::GetClientRect($proc.MainWindowHandle, [ref]$r)
  $p = New-Object W2+POINT
  $p.X = 0; $p.Y = 0
  [void][W2]::ClientToScreen($proc.MainWindowHandle, [ref]$p)
  return @{ X = $p.X; Y = $p.Y; W = $r.R - $r.L; H = $r.B - $r.T }
}

function Shoot([string]$name) {
  $c = Client
  $bmp = New-Object System.Drawing.Bitmap $c.W, $c.H
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($c.X, $c.Y, 0, 0, $bmp.Size)
  $bmp.Save("$OutDir\$name.png", [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  "  $name  (client $($c.W)x$($c.H))"
}

function Aim([double]$fx, [double]$fy) {
  $c = Client
  [void][W2]::SetCursorPos([int]($c.X + $c.W * $fx), [int]($c.Y + $c.H * $fy))
  Start-Sleep -Milliseconds 400
}

function Click() {
  [W2]::mouse_event(0x0002, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 90
  [W2]::mouse_event(0x0004, 0, 0, 0, 0)
  Start-Sleep -Seconds 2
}

Shoot "p0-title"

# PLAY, centred on the pennant's own rect (-123..596 x, 374..568 y of 1920x1080).
Aim 0.12 0.436 ; Shoot "p1-hover-play"
Click            ; Shoot "p2-after-play"

# SINGLE PLAYER (95..709 x, 400..501 y).
Aim 0.209 0.417 ; Shoot "p3-hover-solo"
Click             ; Shoot "p4-after-solo"

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
"done"

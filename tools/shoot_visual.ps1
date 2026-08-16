# Drives the built player through the screens that a LOOK is the only check for, and
# photographs each one.
#
# WHY A THIRD SCRIPT: `shoot_player.ps1` walks the menus and `probe_play.ps1` proves a round
# is playable. Neither of them ever opens the CHARACTER screen, which is where the model
# preview lives, and neither of them moves the mouse during a round, which is the only way to
# see whether the camera turns at all. Both of those were reported as broken and both are
# invisible to every check that is not a picture.
#
# ⚠️ SCAN CODES THROUGH SendInput, NOT VIRTUAL KEYS. Unity's Input System reads the keyboard
# by scan code and ignores a synthetic press that has none.
#
# ⚠️ AND EVERY COORDINATE IS IN THE .tscn's OWN 1920x1080 SPACE, mapped through the CLIENT
# rect's height. The canvases match on height, so one scale factor derived from the height
# alone is correct whatever width the window happens to be.

param(
  [string]$Exe = "$env:USERPROFILE\Desktop\TumbangPreso-Unity\TumbangPreso.exe",
  [string]$OutDir = "Logs\shots-visual"
)

Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Vis {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, int e);
  [DllImport("user32.dll", SetLastError=true)] public static extern uint SendInput(uint n, INPUT[] i, int size);

  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

  [StructLayout(LayoutKind.Sequential)]
  public struct KEYBDINPUT { public ushort vk, scan; public uint flags, time; public IntPtr extra; }

  [StructLayout(LayoutKind.Explicit, Size=40)]
  public struct INPUT { [FieldOffset(0)] public uint type; [FieldOffset(8)] public KEYBDINPUT ki; }

  public static void Key(ushort scan, bool down) {
    var i = new INPUT();
    i.type = 1;
    i.ki.vk = 0;
    i.ki.scan = scan;
    i.ki.flags = (uint)(down ? 0x0008 : 0x000A);
    SendInput(1, new INPUT[]{ i }, Marshal.SizeOf(typeof(INPUT)));
  }
}
"@

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$proc = Start-Process -FilePath $Exe -PassThru

# ⚠️⚠️ WAIT FOR THE WINDOW, DO NOT SLEEP A GUESS AT IT. `MainWindowHandle` is 0 until the
# player has actually opened one, and a cached Process object never notices it appearing
# without a Refresh(). A fixed 12-second sleep worked once and then did not: the run after a
# fresh build spends longer warming the shader cache, every capture came back "no window", and
# every click landed outside the game entirely while the script reported it had driven the
# menus. Poll instead, and give up loudly.
$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) {
  $proc.Refresh()
  if ($proc.MainWindowHandle -ne 0) { break }
  Start-Sleep -Milliseconds 500
}

if ($proc.MainWindowHandle -eq 0) { throw "the player never opened a window" }

# The handle exists before the first frame is drawn; the splash and its fade are 3 seconds.
Start-Sleep -Seconds 9
[void][Vis]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Seconds 2

function Client() {
  $proc.Refresh()
  $r = New-Object Vis+RECT
  [void][Vis]::GetClientRect($proc.MainWindowHandle, [ref]$r)
  $p = New-Object Vis+POINT
  $p.X = 0; $p.Y = 0
  [void][Vis]::ClientToScreen($proc.MainWindowHandle, [ref]$p)
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

function Screen([double]$rx, [double]$ry) {
  $c = Client
  $k = $c.H / 1080.0
  return @{ X = [int]($c.X + $c.W * 0.5 + ($rx - 960.0) * $k); Y = [int]($c.Y + $ry * $k) }
}

function AimRef([double]$rx, [double]$ry) {
  $s = Screen $rx $ry
  [void][Vis]::SetForegroundWindow($proc.MainWindowHandle)
  [void][Vis]::SetCursorPos($s.X, $s.Y)
  Start-Sleep -Milliseconds 350
}

function Tap() {
  [Vis]::mouse_event(0x0002, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 90
  [Vis]::mouse_event(0x0004, 0, 0, 0, 0)
  Start-Sleep -Seconds 2
}

function RightTap() {
  [Vis]::mouse_event(0x0008, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 90
  [Vis]::mouse_event(0x0010, 0, 0, 0, 0)
  Start-Sleep -Seconds 1
}

# A real press-move-release, in reference coordinates. UGUI only raises OnDrag when the
# pointer actually travels while held, so the move has to happen in steps rather than as one
# teleport: a single jump can be swallowed as a click.
function Drag([double]$x0, [double]$y0, [double]$x1, [double]$y1) {
  AimRef $x0 $y0
  [Vis]::mouse_event(0x0002, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 120

  for ($i = 1; $i -le 12; $i++) {
    $t = $i / 12.0
    $s = Screen ($x0 + ($x1 - $x0) * $t) ($y0 + ($y1 - $y0) * $t)
    [void][Vis]::SetCursorPos($s.X, $s.Y)
    Start-Sleep -Milliseconds 45
  }

  [Vis]::mouse_event(0x0004, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 600
}

# ⚠️⚠️ EVERY PRESS IS CONFIRMED AGAINST `Player.log`, NOT AGAINST A SLEEP. `ConvertedScreen`
# logs one line per deliberate press for exactly this reason, and a script that assumes the
# menu advanced is how a whole run ends up clicking the title screen: the second attempt at
# this probe spent its entire sequence on the main menu because the splash ran a few seconds
# longer than the fixed wait, and the step that should have pressed BACK pressed QUIT.
#
# ⚠️ AND THE LOG IS OPENED SHARED. The player holds it open for writing, and a plain read
# throws "file in use" rather than returning what is there.
$LogPath = "$env:USERPROFILE\AppData\LocalLow\BH Studios\Tumbang Preso\Player.log"

function LogText() {
  if (-not (Test-Path $LogPath)) { return "" }
  $stream = [System.IO.File]::Open($LogPath, 'Open', 'Read', 'ReadWrite')
  $reader = New-Object System.IO.StreamReader($stream)
  $text = $reader.ReadToEnd()
  $reader.Dispose(); $stream.Dispose()
  return $text
}

function TapUntil([double]$rx, [double]$ry, [string]$marker, [int]$tries = 12) {
  $before = ([regex]::Matches((LogText), [regex]::Escape($marker))).Count

  for ($i = 0; $i -lt $tries; $i++) {
    AimRef $rx $ry
    Tap
    $now = ([regex]::Matches((LogText), [regex]::Escape($marker))).Count
    if ($now -gt $before) { "  pressed $marker"; return $true }
  }

  "  FAILED to press $marker after $tries attempts"
  return $false
}

function Press([UInt16]$scan, [int]$ms) {
  [Vis]::Key($scan, $true)
  Start-Sleep -Milliseconds $ms
  [Vis]::Key($scan, $false)
  Start-Sleep -Milliseconds 200
}

# A raw mouse move with the pointer captured, which is what the camera reads. Relative,
# because a locked cursor has no position to set.
# ⚠️ THE DELTAS ARE UNSIGNED IN THE API AND MUST BE CAST, NOT CONVERTED. mouse_event takes
# a DWORD that carries a signed value, so -30 has to arrive as 0xFFFFFFE2. PowerShell's
# [uint32] cast throws on a negative instead of wrapping it, which is what killed the
# look-left step on the first run.
function Look([int]$dx, [int]$dy, [int]$steps) {
  $ux = [System.BitConverter]::ToUInt32([System.BitConverter]::GetBytes([int]$dx), 0)
  $uy = [System.BitConverter]::ToUInt32([System.BitConverter]::GetBytes([int]$dy), 0)
  for ($i = 0; $i -lt $steps; $i++) {
    [Vis]::mouse_event(0x0001, $ux, $uy, 0, 0)   # MOUSEEVENTF_MOVE
    Start-Sleep -Milliseconds 25
  }
  Start-Sleep -Milliseconds 400
}

Snap "01-title"

TapUntil 236 471 "[UI] pressed StartButton" | Out-Null ; Snap "02-mode"   # PLAY
TapUntil 402 450 "[UI] pressed SoloButton"  | Out-Null ; Snap "03-setup"  # SINGLE PLAYER

# The CHARACTER row's button, measured off the setup screen's own capture.
TapUntil 667 442 "[UI] pressed CharacterButton" | Out-Null ; Snap "06-character-person"

# Drag across the model, which is the control the panel's hint line promises.
Drag 1450 560 1180 620 ; Snap "07-character-dragged"

# Right-click restores the framed shot.
AimRef 1450 560 ; RightTap ; Snap "08-character-reset"

# The LATA tab, which is the flat subject the framing maths is really about.
AimRef 394 321 ; Tap ; Snap "09-character-lata"

TapUntil 235 939 "[UI] pressed BackButton" | Out-Null ; Snap "10-back-to-setup"
TapUntil 390 672 "[UI] pressed PrimaryButton" | Out-Null      # START MATCH
Start-Sleep -Seconds 5
Snap "11-ready"

[void][Vis]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Milliseconds 500

Press 0x13 120                     # R: ready up
Start-Sleep -Milliseconds 1400
Snap "12-countdown"

Start-Sleep -Seconds 3
Snap "13-round-live"

Look 30 0 20                       # turn right: proves the mouse steers the camera at all
Snap "14-after-look-right"

Press 0x11 1400                    # W: walk forward
Snap "15-after-walk"

Look -30 0 20
Snap "16-after-look-left"

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
"done"

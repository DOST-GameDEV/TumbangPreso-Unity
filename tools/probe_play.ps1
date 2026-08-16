# Drives an actual round in the built player: click through to the arena, press R to ready
# up, hold W to walk, and photograph each beat.
#
# ⚠️ SCAN CODES THROUGH SendInput, NOT VIRTUAL KEYS THROUGH keybd_event. An earlier note in
# the handoff says keyboard synthesis "does not reach the game window"; what does not reach
# it is a virtual-key event with no scan code, because Unity's Input System reads the
# keyboard by SCAN CODE and ignores anything without one. With KEYEVENTF_SCANCODE set, the
# same synthetic press arrives exactly like a real one.
#
# This is the only check that proves the game is PLAYABLE rather than merely drawable: it
# exercises the Input System backend, the ready gate, the countdown and the motor.

param(
  [string]$Exe = "$env:USERPROFILE\Desktop\TumbangPreso-Unity\TumbangPreso.exe",
  [string]$OutDir = "Logs\shots-player"
)

Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Play {
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
    i.type = 1;                                   // INPUT_KEYBOARD
    i.ki.vk = 0;
    i.ki.scan = scan;
    i.ki.flags = (uint)(down ? 0x0008 : 0x000A);  // SCANCODE | (KEYUP)
    SendInput(1, new INPUT[]{ i }, Marshal.SizeOf(typeof(INPUT)));
  }
}
"@

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$proc = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds 12
[void][Play]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Seconds 2

function Client() {
  $r = New-Object Play+RECT
  [void][Play]::GetClientRect($proc.MainWindowHandle, [ref]$r)
  $p = New-Object Play+POINT
  $p.X = 0; $p.Y = 0
  [void][Play]::ClientToScreen($proc.MainWindowHandle, [ref]$p)
  return @{ X = $p.X; Y = $p.Y; W = $r.R - $r.L; H = $r.B - $r.T }
}

function Snap([string]$name) {
  $c = Client
  $bmp = New-Object System.Drawing.Bitmap $c.W, $c.H
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($c.X, $c.Y, 0, 0, $bmp.Size)
  $bmp.Save("$OutDir\$name.png", [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  "  $name"
}

function Aim([double]$fx, [double]$fy) {
  $c = Client
  [void][Play]::SetForegroundWindow($proc.MainWindowHandle)
  [void][Play]::SetCursorPos([int]($c.X + $c.W * $fx), [int]($c.Y + $c.H * $fy))
  Start-Sleep -Milliseconds 350
}

function Tap() {
  [Play]::mouse_event(0x0002, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 90
  [Play]::mouse_event(0x0004, 0, 0, 0, 0)
  Start-Sleep -Seconds 2
}

# Scan codes: W = 0x11, R = 0x13.
function Press([UInt16]$scan, [int]$ms) {
  [Play]::Key($scan, $true)
  Start-Sleep -Milliseconds $ms
  [Play]::Key($scan, $false)
  Start-Sleep -Milliseconds 200
}

Aim 0.12 0.436 ; Tap             # PLAY
Aim 0.209 0.417; Tap             # SINGLE PLAYER
Aim 0.20 0.62  ; Tap             # START MATCH
Start-Sleep -Seconds 4

Snap "10-ready"

[void][Play]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Milliseconds 500

Press 0x13 120                   # R: ready up
Start-Sleep -Milliseconds 1200
Snap "11-countdown"

Start-Sleep -Seconds 3
Snap "12-round-live"

Press 0x11 1400                  # W: walk forward
Snap "13-after-walk"

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
"done"

# Drives the GODOT original the same way `shoot_player.ps1` drives the Unity build, so the
# two can be compared frame for frame instead of from memory or from a phone photo.
#
# ⚠️⚠️ THIS IS THE ONLY HONEST WAY TO SETTLE A "THEY DON'T LOOK THE SAME" REPORT. A capture
# carries the frame's animation state, the engine's tonemap and whatever the chat client did
# to the PNG on the way over. One false delta has already been chased that way: the boot
# sting was recorded in the ledger as a colour-primaries problem when the grey in the
# comparison shot was simply the splash's own fade-in.
#
# ⚠️ THE GODOT REPO IS READ ONLY. This only ever runs the project; the `.godot` import cache
# it produces is already in that repo's .gitignore.

param(
  [string]$Project = "C:\Users\matth\Documents\GitHub\DOST-GameDev",
  [string]$OutDir  = "Logs\shots-godot"
)

Add-Type -AssemblyName System.Drawing

Add-Type @"
using System; using System.Runtime.InteropServices;
public class G {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, int e);
  [DllImport("user32.dll", SetLastError=true)] public static extern uint SendInput(uint n, INPUT[] i, int size);

  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
  [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort vk, scan; public uint flags, time; public IntPtr extra; }
  [StructLayout(LayoutKind.Explicit, Size=40)] public struct INPUT { [FieldOffset(0)] public uint type; [FieldOffset(8)] public KEYBDINPUT ki; }

  public static void Key(ushort scan, bool down) {
    var i = new INPUT();
    i.type = 1; i.ki.vk = 0; i.ki.scan = scan;
    i.ki.flags = (uint)(down ? 0x0008 : 0x000A);
    SendInput(1, new INPUT[]{ i }, Marshal.SizeOf(typeof(INPUT)));
  }
}
"@

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$godot = Get-ChildItem -Path "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter "Godot_v*_win64.exe" -ErrorAction SilentlyContinue |
         Select-Object -First 1 -ExpandProperty FullName

if (-not $godot) { throw "no Godot binary found" }

$proc = Start-Process -FilePath $godot -ArgumentList '--path', $Project, '--resolution', '1920x1080', '--windowed' -PassThru

function Client() {
  $r = New-Object G+RECT
  [void][G]::GetClientRect($proc.MainWindowHandle, [ref]$r)
  $p = New-Object G+POINT
  $p.X = 0; $p.Y = 0
  [void][G]::ClientToScreen($proc.MainWindowHandle, [ref]$p)
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
  "  $name  ($($c.W)x$($c.H))"
}

# The .tscn's own 1920x1080 coordinates, mapped through the client HEIGHT.
function AimRef([double]$rx, [double]$ry) {
  $c = Client
  $k = $c.H / 1080.0
  [void][G]::SetForegroundWindow($proc.MainWindowHandle)
  [void][G]::SetCursorPos([int]($c.X + $c.W * 0.5 + ($rx - 960.0) * $k), [int]($c.Y + $ry * $k))
  Start-Sleep -Milliseconds 350
}

function Tap() {
  [G]::mouse_event(0x0002, 0, 0, 0, 0)
  Start-Sleep -Milliseconds 90
  [G]::mouse_event(0x0004, 0, 0, 0, 0)
  Start-Sleep -Seconds 2
}

function Press([UInt16]$scan, [int]$ms) {
  [G]::Key($scan, $true); Start-Sleep -Milliseconds $ms; [G]::Key($scan, $false)
  Start-Sleep -Milliseconds 250
}

[void][G]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Seconds 2
Snap "g00-splash"

Start-Sleep -Seconds 6
Snap "g01-title"

AimRef 236 471 ; Tap ; Snap "g02-mode"
AimRef 402 450 ; Tap ; Snap "g03-setup"
AimRef 390 672 ; Tap ; Start-Sleep -Seconds 5 ; Snap "g04-ready"

Press 0x13 120                    # R
Start-Sleep -Milliseconds 1200
Snap "g05-countdown"

Start-Sleep -Seconds 3
Snap "g06-round-live"

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
"done"

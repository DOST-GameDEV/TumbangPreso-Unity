"""Existing peer read-only capture, with physical client/DPI observation.

No activation, input, resize, clipboard, settings or process-control APIs.
"""
import sys, ctypes, json, hashlib
from pathlib import Path
from ctypes import wintypes
from PIL import ImageGrab, Image

hwnd=int(sys.argv[1]); expected=int(sys.argv[2]); out=Path(sys.argv[3]).resolve()
root=Path(__file__).resolve().parent
assert out.is_relative_to(root) and out.suffix.lower()=='.png' and not out.exists()
u=ctypes.WinDLL('user32',use_last_error=True)
u.SetThreadDpiAwarenessContext.argtypes=[ctypes.c_void_p]
u.SetThreadDpiAwarenessContext.restype=ctypes.c_void_p
u.GetForegroundWindow.restype=wintypes.HWND
u.GetWindowThreadProcessId.argtypes=[wintypes.HWND,ctypes.POINTER(wintypes.DWORD)]
u.GetDpiForWindow.argtypes=[wintypes.HWND]; u.GetDpiForWindow.restype=wintypes.UINT
u.GetWindowDpiAwarenessContext.argtypes=[wintypes.HWND];u.GetWindowDpiAwarenessContext.restype=ctypes.c_void_p
u.GetAwarenessFromDpiAwarenessContext.argtypes=[ctypes.c_void_p];u.GetAwarenessFromDpiAwarenessContext.restype=ctypes.c_int
prior=u.SetThreadDpiAwarenessContext(ctypes.c_void_p(-4))
try:
    pid=wintypes.DWORD();u.GetWindowThreadProcessId(hwnd,ctypes.byref(pid));assert pid.value==expected
    assert u.GetForegroundWindow()==hwnd, 'Target is not foreground; no desktop crop'
    assert u.IsWindowVisible(hwnd) and not u.IsIconic(hwnd), 'Target is hidden/minimized'
    c=wintypes.RECT();w=wintypes.RECT();pt=wintypes.POINT(0,0)
    assert u.GetClientRect(hwnd,ctypes.byref(c)) and u.ClientToScreen(hwnd,ctypes.byref(pt))
    assert u.GetWindowRect(hwnd,ctypes.byref(w))
    rect=(pt.x,pt.y,pt.x+c.right-c.left,pt.y+c.bottom-c.top)
    assert rect[2]>rect[0] and rect[3]>rect[1]
    image=ImageGrab.grab(bbox=rect,all_screens=True)
    assert image.size==(c.right-c.left,c.bottom-c.top)
    assert u.GetForegroundWindow()==hwnd, 'Foreground changed while reading pixels'
    out.parent.mkdir(parents=True,exist_ok=True);image.save(out,format='PNG')
    assert out.read_bytes().startswith(b'\x89PNG\r\n\x1a\n')
    with Image.open(out) as check:assert check.format=='PNG' and check.size==image.size
    receipt={'pid':pid.value,'window':hwnd,'foregroundBeforeAndAfter':True,
        'physicalClientRect':rect,'physicalClientWidth':image.width,'physicalClientHeight':image.height,
        'physicalWindowRect':[w.left,w.top,w.right,w.bottom],'windowDpi':u.GetDpiForWindow(hwnd),
        'dpiAwareness':u.GetAwarenessFromDpiAwarenessContext(u.GetWindowDpiAwarenessContext(hwnd)),
        'observerDpiContext':'PerMonitorV2 for read-only coordinates; restored afterward',
        'image':str(out),'format':'PNG','sha256':hashlib.sha256(out.read_bytes()).hexdigest(),
        'resampling':False,'jpegConversion':False,'scope':'Physical foreground desktop client pixels; not engine backbuffer/audio/input acceptance'}
    out.with_suffix('.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(receipt))
finally:
    u.SetThreadDpiAwarenessContext(prior)

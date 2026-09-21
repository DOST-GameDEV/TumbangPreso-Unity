"""Protect TUMP's shared Editor input preferences, never the player hive.

Unity documents Editor PlayerPrefs separately from standalone PlayerPrefs:
https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.html
"""
import base64
import re
try:
    import winreg
except ImportError:
    winreg=None

EDITOR_KEY=r'Software\Unity\UnityEditor\BH Studios\Tumbang Preso'
NAMES=('tumbangpreso.bindings','tumbangpreso.touchlayout','tumbangpreso.genericpad')

def allowed(name):
    return any(re.fullmatch(re.escape(key)+r'(?:_h\d+)?',name,re.IGNORECASE) for key in NAMES)

def encode(value,kind):
    return {'kind':kind,'bytes':base64.b64encode(value).decode()} if isinstance(value,bytes) else {'kind':kind,'value':value}

def read_editor():
    if winreg is None:return {}
    result={}
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,EDITOR_KEY) as handle:
            for i in range(winreg.QueryInfoKey(handle)[1]):
                name,value,kind=winreg.EnumValue(handle,i)
                if allowed(name):result[name]=encode(value,kind)
    except FileNotFoundError:pass
    return result

def restore_values(before,read,write,delete):
    if any(not allowed(name) for name in before):raise ValueError('Snapshot contains an unrelated registry value')
    expected={name.casefold():value for name,value in before.items()}
    current=read()
    for name in current:
        if allowed(name) and name.casefold() not in expected:delete(name)
    for name,value in before.items():write(name,value)
    actual={name.casefold():value for name,value in read().items() if allowed(name)}
    if actual!=expected:raise RuntimeError('Shared Editor input preferences were not restored exactly')

def restore_editor(before):
    if winreg is None:
        if before:raise RuntimeError('No registry backend for existing snapshot')
        return
    def write(name,entry):
        value=base64.b64decode(entry['bytes']) if 'bytes' in entry else entry['value']
        with winreg.CreateKeyEx(winreg.HKEY_CURRENT_USER,EDITOR_KEY,0,winreg.KEY_SET_VALUE) as handle:
            winreg.SetValueEx(handle,name,0,entry['kind'],value)
    def delete(name):
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,EDITOR_KEY,0,winreg.KEY_SET_VALUE) as handle:
            winreg.DeleteValue(handle,name)
    restore_values(before,read_editor,write,delete)

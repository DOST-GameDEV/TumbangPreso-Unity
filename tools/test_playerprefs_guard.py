"""No real registry is touched: verify exact restore and narrow ownership."""
import unittest
from unittest.mock import patch
import playerprefs_guard as guard

class EditorInputPreferenceTests(unittest.TestCase):
    def test_worker_restore_does_not_change_the_other_worker_or_player(self):
        main=guard.EDITOR_KEY;a=guard.editor_key('BH Studios Validation','Worker-a')
        b=guard.editor_key('BH Studios Validation','Worker-b');name='tumbangpreso.bindings_h123'
        stores={main:{name:(b'player',3)},a:{name:(b'original-a',3)},b:{name:(b'original-b',3)}}
        class Key:
            def __init__(self,path):self.path=path
            def __enter__(self):return self
            def __exit__(self,*args):pass
        class Registry:
            HKEY_CURRENT_USER=1;KEY_SET_VALUE=2
            @staticmethod
            def OpenKey(hive,path,*args):
                if path not in stores:raise FileNotFoundError(path)
                return Key(path)
            @staticmethod
            def CreateKeyEx(hive,path,*args):stores.setdefault(path,{});return Key(path)
            @staticmethod
            def QueryInfoKey(key):return (0,len(stores[key.path]),0)
            @staticmethod
            def EnumValue(key,index):
                item=list(stores[key.path].items())[index];return (item[0],*item[1])
            @staticmethod
            def SetValueEx(key,name,reserved,kind,value):stores[key.path][name]=(value,kind)
            @staticmethod
            def DeleteValue(key,name):del stores[key.path][name]
        with patch.object(guard,'winreg',Registry):
            snapshot=guard.read_editor(a)
            stores[a][name]=(b'test-a',3);stores[b][name]=(b'live-b',3)
            guard.restore_editor(snapshot,a)
        self.assertEqual(stores[a][name],(b'original-a',3))
        self.assertEqual(stores[b][name],(b'live-b',3))
        self.assertEqual(stores[main][name],(b'player',3))

    def test_registry_and_profile_components_cannot_escape_their_identity(self):
        for name in ('','..','../player','Worker\\other','Worker:other','Worker.',' Worker'):
            with self.subTest(name=name),self.assertRaises(ValueError):guard.editor_key('Validation',name)

    def test_failed_run_restores_binding_bytes_and_absent_layout_only(self):
        key='tumbangpreso.bindings_h123';layout='tumbangpreso.touchlayout_h456'
        original=guard.encode(b'{"saved":"override"}\0',3)
        store={key:guard.encode(b'changed',3),layout:guard.encode(b'new layout',3),'volume':guard.encode(73,4)}
        guard.restore_values({key:original},lambda:dict(store),lambda k,v:store.__setitem__(k,v),lambda k:store.pop(k))
        self.assertEqual(store,{key:original,'volume':guard.encode(73,4)})
    def test_rejects_unrelated_snapshot_before_any_write(self):
        store={'unrelated':guard.encode('new',1)}
        with self.assertRaises(ValueError):
            guard.restore_values({'unrelated':guard.encode('old',1)},lambda:dict(store),lambda k,v:store.__setitem__(k,v),lambda k:store.pop(k))
        self.assertEqual(store['unrelated']['value'],'new')
    def test_only_exact_names_and_unity_hash_suffixes_match(self):
        for key in ['tumbangpreso.bindings','tumbangpreso.touchlayout_h123','tumbangpreso.genericpad_h456']:self.assertTrue(guard.allowed(key))
        for key in ['tumbangpreso.bindings.backup','tumbangpreso.bindings_habc','volume','tumbangpreso.touchlayout2','tumbangpreso.genericpad_backup']:self.assertFalse(guard.allowed(key))

    def test_generic_mapping_setting_restores_integer_and_absence(self):
        key='tumbangpreso.genericpad_h123';original=guard.encode(0,4)
        store={key:guard.encode(1,4),'volume':guard.encode(73,4)}
        guard.restore_values({key:original},lambda:dict(store),lambda k,v:store.__setitem__(k,v),lambda k:store.pop(k))
        self.assertEqual(store[key],original)
        guard.restore_values({},lambda:dict(store),lambda k,v:store.__setitem__(k,v),lambda k:store.pop(k))
        self.assertEqual(store,{'volume':guard.encode(73,4)})

if __name__=='__main__':unittest.main()

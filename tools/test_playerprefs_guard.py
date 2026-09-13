"""No real registry is touched: verify exact restore and narrow ownership."""
import unittest
import playerprefs_guard as guard

class EditorInputPreferenceTests(unittest.TestCase):
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
        for key in ['tumbangpreso.bindings','tumbangpreso.touchlayout_h123']:self.assertTrue(guard.allowed(key))
        for key in ['tumbangpreso.bindings.backup','tumbangpreso.bindings_habc','volume','tumbangpreso.touchlayout2']:self.assertFalse(guard.allowed(key))

if __name__=='__main__':unittest.main()

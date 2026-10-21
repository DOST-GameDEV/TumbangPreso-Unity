"""Adversarial CSV checks for the retired-mash diagnostic contract; not peer evidence."""
import csv
import importlib
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

with patch.dict(os.environ, {'USERPROFILE': tempfile.gettempdir()}):
    familiar = importlib.import_module('net_familiar_matrix')
import net_roof_matrix as roof

class TimedRecoveryEvaluators(unittest.TestCase):
    def files(self, directory, make, change=None):
        for name, seat in [('host', 0), ('owner', 1), ('observer', 2)]:
            rows = make(seat)
            if change: rows = change(rows)
            with (directory / (name + '.csv')).open('w', newline='') as f:
                w = csv.DictWriter(f, fieldnames=rows[0]); w.writeheader(); w.writerows(rows)

    @staticmethod
    def stun(seat, duration=4):
        return [dict(time=i/10, local=seat, stunLeft=max(0, duration-(i/10-1)) if i>=10 else 0,
                     mashPresses=0) for i in range(101)]

    @staticmethod
    def fall(seat, tag=False):
        return [dict(time=i/10, local=seat, map=3, shoeActive=int(i<10 or i>=110),
                     trip=max(0, 3.8-i/10) if i>=10 else 0,
                     stun=max(0, 5-i/10) if tag and i>=10 else 0,
                     mash=0, holding=int(i>=120), y=-.5 if 10<=i<38 else .1)
                for i in range(201)]

    def judge_stun(self, make=None, change=None):
        with tempfile.TemporaryDirectory() as d:
            self.files(Path(d), make or self.stun, change)
            return familiar.evaluate(Path(d), 'mash')['ok']

    def judge_fall(self, make=None, change=None, tag=False):
        with tempfile.TemporaryDirectory() as d:
            self.files(Path(d), make or self.fall, change)
            return roof.evaluate(Path(d), False, tag)['ok']

    def test_full_stun_and_expiry_pass(self): self.assertTrue(self.judge_stun())
    def test_shortened_stun_fails(self): self.assertFalse(self.judge_stun(lambda s:self.stun(s,2)))
    def test_long_stun_fails(self): self.assertFalse(self.judge_stun(lambda s:self.stun(s,6)))
    def test_accepted_stun_press_fails(self): self.assertFalse(self.judge_stun(change=lambda rows:[dict(r,mashPresses=1) for r in rows]))
    def test_no_expiry_fails(self): self.assertFalse(self.judge_stun(change=lambda rows:[r for r in rows if r['time']<5]))
    def test_wrong_seat_fails(self): self.assertFalse(self.judge_stun(change=lambda rows:[dict(r,local=9) for r in rows]))
    def test_timed_roof_and_stock_return_pass(self): self.assertTrue(self.judge_fall())
    def test_independent_tag_remains_full_length(self): self.assertTrue(self.judge_fall(lambda s:self.fall(s,True),tag=True))
    def test_accepted_roof_press_fails(self): self.assertFalse(self.judge_fall(change=lambda rows:[dict(r,mash=1) for r in rows]))
    def test_short_roof_hold_fails(self): self.assertFalse(self.judge_fall(change=lambda rows:[dict(r,trip=0) if r['time']>=2 else r for r in rows]))
    def test_no_returned_shoe_fails(self): self.assertFalse(self.judge_fall(change=lambda rows:[dict(r,shoeActive=0) if r['time']>=1 else r for r in rows]))
    def test_no_pickup_fails(self): self.assertFalse(self.judge_fall(change=lambda rows:[dict(r,holding=0) for r in rows]))
    def test_wrong_map_fails(self): self.assertFalse(self.judge_fall(change=lambda rows:[dict(r,map=1) for r in rows]))

if __name__ == '__main__': unittest.main()

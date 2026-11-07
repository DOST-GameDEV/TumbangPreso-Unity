import unittest
from grounding_pair_review import companion_motion_agreement


class CompanionMotionReviewTests(unittest.TestCase):
    @staticmethod
    def frames(frozen=False,shift=0,slot=4):
        return [{'phase':'play','round':1,'serverTime':i*.2,'companions':[
            {'slot':slot,'body':{'x':shift+(0 if frozen else i*.4),'y':.08,'z':0}}]}
            for i in range(20)]

    def test_moving_peers_with_a_small_delivery_gap_pass(self):
        result=companion_motion_agreement(self.frames(),self.frames(shift=-.16))
        self.assertTrue(result['passed']);self.assertAlmostEqual(.16,result['medianGap'])

    def test_the_original_frozen_client_bug_fails(self):
        result=companion_motion_agreement(self.frames(),self.frames(frozen=True))
        self.assertFalse(result['passed']);self.assertEqual(0,result['clientHorizontalSpan'])

    def test_an_unrelated_doll_cannot_stand_in_for_the_caster(self):
        self.assertFalse(companion_motion_agreement(self.frames(),self.frames(slot=5))['passed'])

    def test_moving_but_divergent_peers_still_fail(self):
        self.assertFalse(companion_motion_agreement(self.frames(),self.frames(shift=10))['passed'])

    def test_missing_companion_evidence_fails(self):
        self.assertFalse(companion_motion_agreement(self.frames(),[])['passed'])


if __name__=='__main__':unittest.main()

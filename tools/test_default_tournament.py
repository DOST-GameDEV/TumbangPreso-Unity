import copy
import unittest
from run_default_tournament import validate


class CompletedTournamentEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.report = dict(role='HOST', networked='True', slot='0', protocol='132',
                           round='8', active='False', mode='Classic', seats=[
                               dict(seat=seat, origin='Human' if seat < 2 else 'Bot') for seat in range(4)])
        self.text = 'tournament ruleset : OK\ntournament modifiers : none\nhub lobby seen : True\n'
        self.log = '[Slice] match over. scores: 40 / 40 / 3500 / 3200'

    def test_complete_terminal_evidence_is_accepted(self):
        self.assertEqual([], validate('host', self.report, self.text, self.log, 132))

    def test_active_wrong_mode_or_wrong_network_identity_is_rejected(self):
        for key, value in (('active', 'True'), ('round', '7'), ('mode', 'HeroStrike'),
                           ('role', 'CLIENT'), ('slot', '1'), ('protocol', '131'), ('networked', 'False')):
            with self.subTest(key=key):
                report = copy.deepcopy(self.report); report[key] = value
                self.assertTrue(validate('host', report, self.text, self.log, 132))

    def test_missing_natural_end_or_public_lobby_is_rejected(self):
        self.assertTrue(validate('host', self.report, self.text, '', 132))
        self.assertTrue(validate('host', self.report, self.text.replace('True', 'False'), self.log, 132))

    def test_modifiers_or_missing_human_origins_are_rejected(self):
        self.assertTrue(validate('host', self.report, self.text.replace('none', 'AllBots'), self.log, 132))
        self.report['seats'][1]['origin'] = 'Bot'
        self.assertTrue(validate('host', self.report, self.text, self.log, 132))


if __name__ == '__main__':
    unittest.main()

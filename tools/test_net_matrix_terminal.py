"""Terminal acceptance cannot hide resumed simulation behind a new network role."""
import unittest
import net_matrix


class MatrixTerminalTests(unittest.TestCase):
    def setUp(self):
        self.scenario = next(row for row in net_matrix.DISCONNECT if row.kill == 'host')

    def test_autohost_with_active_old_round_is_refused(self):
        self.assertIsNotNone(net_matrix.judge_client(self.scenario, {'role': 'HOST', 'active': 'True'}))

    def test_missing_round_state_is_not_terminal_evidence(self):
        self.assertIsNotNone(net_matrix.judge_client(self.scenario, {'role': 'HOST'}))

    def test_explicit_inactive_round_is_accepted_in_either_role(self):
        for role in ('HOST', 'CLIENT'):
            with self.subTest(role=role):
                self.assertIsNone(net_matrix.judge_client(self.scenario, {'role': role, 'active': 'False'}))


if __name__ == '__main__':
    unittest.main()

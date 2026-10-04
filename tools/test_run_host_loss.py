"""Pure acceptance controls for the bounded real-transport scenario; no player/registry writes."""
import copy
import unittest

import run_host_loss as runner


class HostLossAcceptanceTests(unittest.TestCase):
    def setUp(self):
        self.sha = "d28770a25504c8f6a069b429d8384af0ae4ff557"
        self.report = dict(active="False", round="0", map="MatchSetup", protocol="144",
                           role="HOST", text="build identity : TUMBANG PRESO 1.0.0 | d28770a25504 | protocol 144")
        self.receipt = dict(role="client", pid=42, sawLive=True, originHumanSeats=True, scene="MatchSetup",
                            matchEndedEvents=0, recordReadyEvents=0, recordId=None, error=None)
        self.log = "[Abandon] HostLost: ABANDONED at round 1 of 1; this peer may no longer resolve anything"

    def faults(self, report=None, receipt=None, killed=8):
        return runner.evaluate(report or self.report, receipt or self.receipt, self.log, self.sha, 42, killed, 0, 144)

    def test_old_or_missing_protocol_is_refused(self):
        for value in ("132", None):
            self.assertIn('Client report did not use the agreed artifact protocol',
                          self.faults(report=dict(self.report, protocol=value)))

    def test_host_loss_uses_the_current_canonical_rules(self):
        self.assertEqual(runner.lan.WIRE, runner.WIRE)
        self.assertEqual(11, len(runner.WIRE.split('|')))

    def test_inactive_autohost_is_valid(self):
        self.assertEqual([], self.faults())

    def test_active_autohost_and_missing_activity_are_refused(self):
        for value in ("True", None):
            report = dict(self.report, active=value)
            self.assertTrue(self.faults(report=report))

    def test_unproven_loss_wrong_identity_and_fabricated_completion_are_refused(self):
        self.assertTrue(self.faults(killed=None))
        self.assertTrue(self.faults(report=dict(self.report, text="build identity : another commit")))
        for field in ("matchEndedEvents", "recordReadyEvents"):
            self.assertTrue(self.faults(receipt=dict(self.receipt, **{field: 1})))

    def test_live_precondition_requires_owned_current_live_pair(self):
        live = dict(role="client", pid=42, phase="live", sawLive=True, originHumanSeats=True,
                    rounds=1, roundSeconds=30, scene="Eskinita", matchEndedEvents=0, recordReadyEvents=0)
        self.assertEqual([], runner.live_faults(live, "client", 42))
        for field, value in (("pid", 99), ("phase", "network_ready"), ("originHumanSeats", False),
                             ("matchEndedEvents", 1), ("recordReadyEvents", 1)):
            changed = copy.deepcopy(live); changed[field] = value
            self.assertTrue(runner.live_faults(changed, "client", 42))


if __name__ == "__main__":
    unittest.main()

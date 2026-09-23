"""The precise clock delegate may not hide a missing guard or another receiver."""
from contextlib import redirect_stdout
from io import StringIO
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest
import audit_wire_finite as audit


class ClockDelegateTests(unittest.TestCase):
    def test_exact_delegate_requires_both_guards(self):
        source = audit.SOURCE.read_text(encoding="utf-8")
        clock = audit.CLOCK_SOURCE.read_text(encoding="utf-8")
        original_source, original_clock = audit.SOURCE, audit.CLOCK_SOURCE
        with TemporaryDirectory() as directory:
            audit.SOURCE = Path(directory) / "MatchRpc.cs"
            audit.CLOCK_SOURCE = Path(directory) / "LataClockPresentation.cs"
            try:
                for receiver, implementation, expected in (
                    (source, clock, 0),
                    (source, clock.replace("if(!float.IsFinite(restore)||!float.IsFinite(protection))return;", ""), 1),
                    (source.replace("Visual.LataClockPresentation.For(GameServices.Round?.Lata)?.ApplySnapshot(restore,protection)",
                                    "OtherReceiver.ApplySnapshot(restore,protection)"), clock, 1),
                ):
                    audit.SOURCE.write_text(receiver, encoding="utf-8")
                    audit.CLOCK_SOURCE.write_text(implementation, encoding="utf-8")
                    with redirect_stdout(StringIO()) as output:
                        result = audit.main()
                    self.assertEqual(expected, result, output.getvalue())
            finally:
                audit.SOURCE, audit.CLOCK_SOURCE = original_source, original_clock


if __name__ == "__main__":
    unittest.main()

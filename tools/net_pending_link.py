"""Use the existing UDP relay with one trace-triggered server-to-client delay spike."""
import argparse
import csv
from pathlib import Path
import threading
import time

from net_link import Link, Shaper


class SpikeShaper(Shaper):
    def __init__(self, delay_ms):
        super().__init__(0, 0, 0, 20260904)
        self.spike_seconds = delay_ms / 1000
        self.until = 0.0
        self.delayed = 0

    def verdict(self, now, outage):
        when = super().verdict(now, outage)
        if when is not None and now < self.until:
            self.delayed += 1
            return when + self.spike_seconds
        return when


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--listen', type=int, required=True)
    parser.add_argument('--to', required=True)
    parser.add_argument('--delay', type=float, required=True, help='Transient downstream delay, milliseconds.')
    parser.add_argument('--trace', type=Path, required=True, help='Host CSV from the pending-cast fixture.')
    parser.add_argument('--seconds', type=float, default=110)
    args = parser.parse_args()
    if not 0 < args.delay <= 1000:
        parser.error('Use a bounded positive spike of at most 1000ms.')
    args.to_host, port = args.to.rsplit(':', 1)
    args.to_port = int(port)
    args.bind = '127.0.0.1'
    args.jitter = args.loss = args.outage_at = args.outage_for = 0
    args.seed = 20260904
    spike = SpikeShaper(args.delay)
    args.delay = 0  # Clock synchronization and all upstream traffic stay normal.
    link = Link(args)
    link.to_client = spike

    def watch():
        while link.running:
            if args.trace.is_file():
                with args.trace.open(encoding='utf-8') as handle:
                    for row in csv.DictReader(handle):
                        value = row.get('windup')
                        if value and 0 < float(value) <= .20:
                            spike.until = time.monotonic() + .7
                            print(f'[pending-link] armed at host elapsed={row["elapsed"]}, '
                                  f'windup={value}; downstream={spike.spike_seconds:.3f}s for0.7s', flush=True)
                            return
            time.sleep(.01)

    threading.Thread(target=watch, daemon=True).start()
    link.run()
    print(f'[pending-link] delayed datagrams={spike.delayed}', flush=True)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())

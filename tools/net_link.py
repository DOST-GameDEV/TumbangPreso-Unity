#!/usr/bin/env python3
"""A UDP link between two Tumbang Preso players that can be made bad on purpose.

WHAT THIS IS FOR
================

`docs/TODO.md` section 135.7 says the bad-wifi table and the executed disconnect matrix both
need "two live peers on a simulated link", and that no such harness exists. Two thirds of one
did exist and the entry missed it: `NetBootstrap` already starts a host and a client from the
command line, `NetStateReport` already writes what each peer BELIEVES to a file so the two can
be compared, and section 68.18.10 already drove two real built players on one machine. The only
missing piece was a way to make the link between them bad.

WHY A PROXY RATHER THAN THE TRANSPORT'S OWN SIMULATOR
=====================================================

The obvious answer is `UnityTransport.SetDebugSimulatorParameters`, and the handoff that opened
this session assumed it. It does not work here, and reading the installed package is the only
way to find that out:

    Library/PackageCache/com.unity.netcode.gameobjects@d43d28498f17/Runtime/Transports/UTP/
    UnityTransport.cs:348

      [Obsolete("DebugSimulator is no longer supported and has no effect. Use Network Simulator
                 from the Multiplayer Tools package.", false)]

The simulator pipeline stage is only configured at line 542 of that file, inside
`#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED`, a define that comes from
`com.unity.multiplayer.tools`. That package is NOT in `Packages/manifest.json` and NOT in
`Library/PackageCache`. So on this project both the property and the setter are silent no-ops:
they compile, they run, they change nothing, and a table built on them would be four rows of
numbers measured over a perfect link. That is worse than no table.

Three reasons this proxy is the better answer even if that package were added:

  1. It needs NO game-side change, so it runs against the ALREADY BUILT player on the Desktop
     rather than against a build that has to be made first. A switch added to the game can only
     be tested by a build that carries it; a proxy tests the build that exists.
  2. The tools simulator shapes ONE driver's own pipeline. This shapes the actual socket
     traffic, which is closer to what the question is about, and it is the only one of the two
     that can produce a genuine OUTAGE: stop forwarding and both ends discover it the way they
     would discover a router going down, through their own timeouts rather than through a
     cooperating stage inside one of them.
  3. It works unchanged for a phone against a PC, which is `docs/TODO.md` section 130.8 and
     section 126.11, the crossplay run nobody has done. Point the handset at this machine's
     proxy port instead of at the host port and the same table applies.

HOW IT ROUTES
=============

One socket faces the clients. For each distinct client endpoint a dedicated upstream socket is
opened towards the host, so the host's replies arrive on a socket that identifies which client
they belong to. That is the standard UDP proxy shape and it is what makes more than one client
work at all: without it, replies from the host cannot be attributed and a second joiner
silently steals the first one's traffic.

Delay, jitter and loss are applied in BOTH directions, and the numbers below are per direction.
A `--delay 75` link is a 150 ms round trip, which is what the table's rows are named after.

WARNING: THE SEED IS PART OF THE MEASUREMENT. Loss is random, so an unseeded run cannot be
compared against another one. `--seed` defaults to a fixed value for that reason; change it to
sample a different draw, never to make a run pass.
"""

import argparse
import heapq
import itertools
import random
import socket
import sys
import threading
import time

# The scheduler wakes at least this often even with nothing queued, so a shutdown is noticed
# promptly rather than at the next packet.
IDLE_TICK = 0.005

# WARNING: BIG ENOUGH FOR THE WHOLE DATAGRAM, BECAUSE UDP TRUNCATES IN SILENCE. A short recv
# buffer does not split a packet across two reads the way a stream socket would, it DISCARDS
# the tail and returns the head with no error anywhere. NGO's `MaxPayloadSize` defaults well
# above a single MTU and it batches, so a 2048 byte buffer quietly corrupts exactly the
# largest and most important packets.
RECV = 65535


class Shaper:
    """Delay, jitter, loss and an outage window, applied to one direction of one link."""

    def __init__(self, delay_ms, jitter_ms, loss, seed):
        self.delay = delay_ms / 1000.0
        self.jitter = jitter_ms / 1000.0
        self.loss = loss
        self.rng = random.Random(seed)

    def verdict(self, now, outage):
        """Returns the send time for a packet, or None when it is to be dropped.

        WARNING: AN OUTAGE IS A DROP, NOT A PAUSE. Holding packets and releasing them when the
        link comes back would model a router that buffers for five seconds, which nothing does.
        A real outage loses what was in flight, and the reconnect path is the thing under test.
        """
        if outage:
            return None

        if self.loss > 0.0 and self.rng.random() < self.loss:
            return None

        wait = self.delay
        if self.jitter > 0.0:
            # Uniform around the mean rather than one-sided, so `--delay 75 --jitter 20` has a
            # mean of 75 and not 85. A one-sided jitter silently raises the latency the row is
            # named after, which would make every jitter row also a latency row.
            wait += self.rng.uniform(-self.jitter, self.jitter)
            if wait < 0.0:
                wait = 0.0

        return now + wait


class Link:
    def __init__(self, args):
        self.args = args
        self.host_addr = (args.to_host, args.to_port)

        self.face = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        # WARNING: LOOPBACK BY DEFAULT, AND BINDING `0.0.0.0` BROKE THE FIRST RUN IN A WAY THAT
        # LOOKED LIKE A PROXY BUG AND WAS NOT. `LanBeacon` broadcasts the game's presence over
        # UDP across every interface, so a wildcard bind receives those broadcasts too, and this
        # proxy dutifully attached them as CLIENTS: the log read
        #
        #     [link] client 192.168.128.1:60776 attached
        #     [link] client 192.168.1.144:60776 attached
        #
        # for a run in which the only real client was on 127.0.0.1. Every stray datagram then
        # opened another upstream socket to the host. Bind the loopback and none of that traffic
        # is visible. Pass `--bind 0.0.0.0` on purpose when the far peer is a phone.
        self.face.bind((args.bind, args.listen))
        self.face.settimeout(0.05)

        # WARNING: STRICTLY MONOTONIC, AND THE OBVIOUS TIE-BREAK IS NOT. This started as
        # `self.sent + self.dropped`, which is not unique: two packets queued before either is
        # sent carry the same number, `heapq` falls through to the next tuple element, and
        # comparing two sockets raises `TypeError: '<' not supported between instances of
        # 'socket'` inside the scheduler thread. The proxy then stops forwarding while still
        # accepting, and the client dies of `MaxConnectionAttempts` several seconds later,
        # which reads as the GAME failing to connect.
        self.sequence = itertools.count()

        # client endpoint -> upstream socket towards the host
        self.upstream = {}
        # upstream socket -> client endpoint
        self.owner = {}

        self.queue = []
        self.lock = threading.Lock()
        self.running = True
        self.started = time.monotonic()

        self.to_host = Shaper(args.delay, args.jitter, args.loss, args.seed)
        self.to_client = Shaper(args.delay, args.jitter, args.loss, args.seed + 1)

        self.sent = 0
        self.dropped = 0
        self.outage_drops = 0

    def in_outage(self, now):
        if self.args.outage_for <= 0.0:
            return False

        since = now - self.started
        return self.args.outage_at <= since < (self.args.outage_at + self.args.outage_for)

    def push(self, when, sock, payload, addr):
        with self.lock:
            # The sequence breaks heapq ties so it never has to compare a socket object, which
            # is not orderable. See its note in the constructor.
            heapq.heappush(self.queue,
                           (when, next(self.sequence), sock, payload, addr))

    def offer(self, shaper, sock, payload, addr):
        now = time.monotonic()
        outage = self.in_outage(now)
        when = shaper.verdict(now, outage)

        if when is None:
            self.dropped += 1
            if outage:
                self.outage_drops += 1
            return

        self.push(when, sock, payload, addr)

    def client_facing(self):
        """Reads packets from clients and schedules them towards the host."""
        while self.running:
            try:
                payload, addr = self.face.recvfrom(RECV)
            except socket.timeout:
                continue
            except OSError:
                return

            sock = self.upstream.get(addr)
            if sock is None:
                sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
                sock.settimeout(0.05)
                self.upstream[addr] = sock
                self.owner[sock] = addr
                threading.Thread(target=self.host_facing, args=(sock,), daemon=True).start()
                print(f"[link] client {addr[0]}:{addr[1]} attached", flush=True)

            self.offer(self.to_host, sock, payload, self.host_addr)

    def host_facing(self, sock):
        """Reads the host's replies on one client's upstream socket and schedules them back."""
        addr = self.owner[sock]

        while self.running:
            try:
                payload, _ = sock.recvfrom(RECV)
            except socket.timeout:
                continue
            except OSError:
                return

            self.offer(self.to_client, self.face, payload, addr)

    def pump(self):
        """Sends packets whose delay has elapsed."""
        while self.running:
            now = time.monotonic()
            due = []

            with self.lock:
                while self.queue and self.queue[0][0] <= now:
                    due.append(heapq.heappop(self.queue))

            for _, _, sock, payload, addr in due:
                try:
                    sock.sendto(payload, addr)
                    self.sent += 1
                except OSError:
                    self.dropped += 1

            if not due:
                time.sleep(IDLE_TICK)

    def run(self):
        threading.Thread(target=self.client_facing, daemon=True).start()
        threading.Thread(target=self.pump, daemon=True).start()

        a = self.args
        print(f"[link] :{a.listen} -> {a.to_host}:{a.to_port}  "
              f"delay={a.delay}ms jitter={a.jitter}ms loss={a.loss * 100:.1f}% "
              f"outage={a.outage_at}s for {a.outage_for}s seed={a.seed}", flush=True)

        try:
            while self.running:
                time.sleep(0.2)
                if a.seconds > 0.0 and (time.monotonic() - self.started) >= a.seconds:
                    break
        except KeyboardInterrupt:
            pass

        self.running = False
        time.sleep(0.1)

        total = self.sent + self.dropped
        rate = (self.dropped / total * 100.0) if total else 0.0
        print(f"[link] forwarded={self.sent} dropped={self.dropped} "
              f"({rate:.1f}%) of which outage={self.outage_drops}", flush=True)


def main():
    p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    p.add_argument("--listen", type=int, required=True,
                   help="port the client joins, on this machine")
    p.add_argument("--bind", default="127.0.0.1",
                   help="interface to listen on. Loopback by default so the LAN beacon's "
                        "broadcasts are not mistaken for clients; use 0.0.0.0 for a phone")
    p.add_argument("--to", dest="to", required=True,
                   help="host address as ip:port, where the real host is listening")
    p.add_argument("--delay", type=float, default=0.0,
                   help="one-way delay in ms; a 150 ms round trip is --delay 75")
    p.add_argument("--jitter", type=float, default=0.0,
                   help="uniform jitter in ms either side of the delay")
    p.add_argument("--loss", type=float, default=0.0,
                   help="per-packet loss as a fraction, so 0.02 is two per cent")
    p.add_argument("--outage-at", dest="outage_at", type=float, default=0.0,
                   help="seconds after start at which the link goes down")
    p.add_argument("--outage-for", dest="outage_for", type=float, default=0.0,
                   help="how long the outage lasts; 0 means no outage")
    p.add_argument("--seconds", type=float, default=0.0,
                   help="run for this long then report and exit; 0 runs until killed")
    p.add_argument("--seed", type=int, default=20260904,
                   help="loss and jitter seed. Change it to sample, never to pass a run")

    args = p.parse_args()

    if ":" not in args.to:
        p.error("--to wants ip:port")

    host, port = args.to.rsplit(":", 1)
    args.to_host = host
    args.to_port = int(port)

    Link(args).run()
    return 0


if __name__ == "__main__":
    sys.exit(main())

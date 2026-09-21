"""Task-owned, measured UDP impairment for the focused presentation peer runners."""
import json
from pathlib import Path
import socket
import subprocess
import sys


class PresentationLink:
    def __init__(self, root: Path, out: Path, args):
        self.root, self.out, self.args = root, out, args
        self.process = self.log = None

    def start(self, host_port):
        a = self.args
        if not (0 <= a.delay <= 1000 and 0 <= a.jitter <= 1000 and 0 <= a.loss < 1):
            raise ValueError("Invalid simulated link settings")
        if not (a.delay or a.jitter or a.loss):
            return host_port
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as listener:
            listener.bind(("127.0.0.1", 0))
            port = listener.getsockname()[1]
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
        self.log = (self.out / "link.log").open("w")
        command = [sys.executable, str(self.root / "tools/net_link.py"), "--listen", str(port),
                   "--to", f"127.0.0.1:{host_port}", "--delay", str(a.delay), "--jitter", str(a.jitter),
                   "--loss", str(a.loss), "--seed", str(a.seed), "--seconds", "115"]
        self.process = subprocess.Popen(command, cwd=self.root, startupinfo=startup,
                                        stdout=self.log, stderr=subprocess.STDOUT)
        (self.out / "link-job.json").write_text(json.dumps({"pid": self.process.pid, "command": command,
            "oneWayDelayMs": a.delay, "jitterMs": a.jitter, "loss": a.loss, "seed": a.seed}, indent=2))
        return port

    def close(self):
        if self.process is not None and self.process.poll() is None:
            self.process.terminate()
            self.process.wait(timeout=10)
        if self.log is not None:
            self.log.close()


def arguments(parser):
    parser.add_argument("--delay", type=float, default=0, help="One-way UDP delay in milliseconds.")
    parser.add_argument("--jitter", type=float, default=0, help="UDP jitter in milliseconds.")
    parser.add_argument("--loss", type=float, default=0, help="UDP packet loss fraction.")
    parser.add_argument("--seed", type=int, default=52009, help="Repeatable link impairment seed.")

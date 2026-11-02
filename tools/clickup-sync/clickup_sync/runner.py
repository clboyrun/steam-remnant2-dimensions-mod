"""Background loop: periodic pull, quiet-period push."""
import threading
import time
from collections import deque

from . import config as cfgmod
from .api import Client, RateLimiter
from .sync import Engine, QuietGate


class Runner:
    def __init__(self, cfg, log_lines=None):
        self.cfg = cfg
        self.lines = log_lines if log_lines is not None else deque(maxlen=500)
        self.stop = threading.Event()
        self.wake = threading.Event()
        self.lock = threading.Lock()
        self.status = "idle"
        self._requests = set()
        self._thread = None
        self.gate = None

    def log(self, msg):
        line = time.strftime("%H:%M:%S ") + msg
        self.lines.append(line)
        print(line, flush=True)

    def request(self, what):  # "pull" | "push" | "reset" | "reset-all"
        self._requests.add(what)
        self.wake.set()

    def start(self):
        self._thread = threading.Thread(target=self._loop, daemon=True)
        self._thread.start()

    def _engine(self):
        key = self.cfg.api_key()
        if not key:
            self.status = f"no key: set environment variable {self.cfg.env_var_name}"
            return None
        client = Client(key, self.cfg.api_base, RateLimiter(self.cfg.rate_limit_per_minute), log=self.log)
        return Engine(self.cfg, client, self.log)

    def _loop(self):
        last_pull = 0.0
        while not self.stop.is_set():
            try:
                reqs, self._requests = self._requests, set()
                if self.gate is None or str(self.gate.root) != str(self.cfg.cache_path()):
                    self.gate = QuietGate(self.cfg.cache_path(), self.cfg.quiet_seconds)
                self.gate.quiet = self.cfg.quiet_seconds
                due_pull = self.cfg.auto_pull and time.time() - last_pull > self.cfg.pull_interval_minutes * 60
                due_push = self.cfg.auto_push and self.gate.poll()
                eng = self._engine() if (reqs or due_pull or due_push) else None
                if eng:
                    with self.lock:
                        if "reset" in reqs or "reset-all" in reqs:
                            eng.reset(keep_notes="reset-all" not in reqs)
                            reqs.add("pull")
                        if "push" in reqs or due_push:
                            self.status = "pushing"
                            eng.push()
                        if "pull" in reqs or due_pull:
                            self.status = "pulling"
                            eng.pull()
                            last_pull = time.time()
                        self.gate.rebase()
                if eng or self.cfg.api_key():
                    left = self.gate.seconds_left() if self.gate else None
                    self.status = "idle" if left is None else f"edits pending; pushing in {int(left)}s"
            except Exception as e:  # never die; surface and retry
                self.status = f"error: {e}"
                self.log(f"error: {e}")
                time.sleep(5)
            self.wake.wait(5)
            self.wake.clear()

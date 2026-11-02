"""Minimal ClickUp v2 client (urllib only) with a sliding-window rate limiter."""
import json
import threading
import time
import urllib.error
import urllib.request
from collections import deque


class ApiError(Exception):
    def __init__(self, status, body):
        super().__init__(f"HTTP {status}: {body[:300]}")
        self.status = status
        self.body = body


class RateLimiter:
    """At most `limit` calls in any rolling `window` seconds; also honours server-side pauses."""

    def __init__(self, limit=100, window=60.0, clock=time.monotonic, sleep=time.sleep):
        self.limit, self.window = max(1, int(limit)), window
        self._clock, self._sleep = clock, sleep
        self._stamps = deque()
        self._pause_until = 0.0
        self._lock = threading.Lock()

    def pause_for(self, seconds):
        with self._lock:
            self._pause_until = max(self._pause_until, self._clock() + seconds)

    def acquire(self):
        while True:
            with self._lock:
                now = self._clock()
                while self._stamps and now - self._stamps[0] >= self.window:
                    self._stamps.popleft()
                wait = max(0.0, self._pause_until - now)
                if not wait and len(self._stamps) >= self.limit:
                    wait = self.window - (now - self._stamps[0])
                if not wait:
                    self._stamps.append(now)
                    return
            self._sleep(wait + 0.01)


class Client:
    def __init__(self, token, base="https://api.clickup.com/api/v2", limiter=None, sleep=time.sleep, log=None):
        self.token, self.base = token, base.rstrip("/")
        self.limiter = limiter or RateLimiter()
        self._sleep = sleep
        self.log = log or (lambda m: None)
        self.calls = 0

    def request(self, method, path, params=None, body=None, retries=6):
        url = self.base + path
        if params:
            url += "?" + "&".join(f"{k}={v}" for k, v in params.items())
        data = json.dumps(body).encode() if body is not None else None
        for attempt in range(retries):
            self.limiter.acquire()
            req = urllib.request.Request(url, data=data, method=method)
            req.add_header("Authorization", self.token)
            req.add_header("Content-Type", "application/json")
            self.calls += 1
            try:
                with urllib.request.urlopen(req, timeout=60) as resp:
                    self._note_headers(resp.headers)
                    raw = resp.read().decode("utf-8")
                    return json.loads(raw) if raw else {}
            except urllib.error.HTTPError as e:
                text = e.read().decode("utf-8", "replace")
                if e.code == 429 or e.code >= 500:
                    wait = self._retry_wait(e.headers, attempt)
                    self.log(f"HTTP {e.code} on {method} {path}; waiting {wait:.0f}s (attempt {attempt + 1})")
                    self.limiter.pause_for(wait)
                    continue
                raise ApiError(e.code, text)
            except (urllib.error.URLError, TimeoutError) as e:
                wait = min(60, 2 ** attempt)
                self.log(f"network error {e}; retry in {wait}s")
                self._sleep(wait)
        raise ApiError(0, f"gave up after {retries} attempts: {method} {path}")

    def _note_headers(self, h):
        try:
            if h.get("X-RateLimit-Remaining") is not None and int(h["X-RateLimit-Remaining"]) <= 0:
                reset = float(h.get("X-RateLimit-Reset", 0)) - time.time()
                self.limiter.pause_for(min(max(reset, 1), 65))
        except (ValueError, TypeError):
            pass

    @staticmethod
    def _retry_wait(h, attempt):
        try:
            if h.get("Retry-After"):
                return min(float(h["Retry-After"]) + 1, 120)
            if h.get("X-RateLimit-Reset"):
                return min(max(float(h["X-RateLimit-Reset"]) - time.time(), 1) + 1, 120)
        except (ValueError, TypeError):
            pass
        return min(60, 5 * (attempt + 1))

    get = lambda self, p, params=None: self.request("GET", p, params)
    delete = lambda self, p, params=None: self.request("DELETE", p, params)
    put = lambda self, p, body: self.request("PUT", p, body=body)
    post = lambda self, p, body: self.request("POST", p, body=body)

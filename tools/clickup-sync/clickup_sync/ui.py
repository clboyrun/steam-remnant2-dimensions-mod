"""Tray icon (pystray, optional) + settings window (tkinter)."""
import os
import subprocess
import sys
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

from . import config as cfgmod
from .runner import Runner

try:
    import pystray
    from PIL import Image, ImageDraw
except ImportError:  # still works: window only
    pystray = None


def _icon():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((4, 4, 60, 60), fill=(123, 104, 238, 255))
    d.polygon([(20, 34), (32, 20), (44, 34), (38, 34), (32, 27), (26, 34)], fill="white")
    d.polygon([(22, 42), (42, 42), (42, 38), (22, 38)], fill="white")
    return img


class App:
    def __init__(self, show=True):
        self.cfg = cfgmod.load()
        self.runner = Runner(self.cfg)
        self.root = tk.Tk()
        self.root.title("ClickUp Sync")
        self.root.protocol("WM_DELETE_WINDOW", self.hide if pystray else self.quit)
        self._build()
        self.runner.start()
        self.tray = None
        if pystray:
            menu = pystray.Menu(
                pystray.MenuItem("Settings", lambda: self.root.after(0, self.show), default=True),
                pystray.MenuItem("Pull now", lambda: self.runner.request("pull")),
                pystray.MenuItem("Push now", lambda: self.runner.request("push")),
                pystray.MenuItem("Open cache folder", lambda: self.open_cache()),
                pystray.MenuItem("Quit", lambda: self.root.after(0, self.quit)))
            self.tray = pystray.Icon("clickup-sync", _icon(), "ClickUp Sync", menu)
            self.tray.run_detached()
        if not show and pystray:
            self.root.withdraw()
        self._tick()

    # ---- layout ----
    def _build(self):
        f = ttk.Frame(self.root, padding=10)
        f.grid(sticky="nsew")
        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(0, weight=1)
        f.columnconfigure(1, weight=1)
        f.rowconfigure(12, weight=1)
        c = self.cfg
        self.v = {
            "env_var_name": tk.StringVar(value=c.env_var_name),
            "cache_dir": tk.StringVar(value=str(c.cache_path())),
            "space_ids": tk.StringVar(value=c.space_ids),
            "rate_limit_per_minute": tk.StringVar(value=str(c.rate_limit_per_minute)),
            "quiet_seconds": tk.StringVar(value=str(c.quiet_seconds)),
            "pull_interval_minutes": tk.StringVar(value=str(c.pull_interval_minutes)),
            "auto_pull": tk.BooleanVar(value=c.auto_pull),
            "auto_push": tk.BooleanVar(value=c.auto_push),
            "push_start_date": tk.BooleanVar(value=c.push_start_date),
        }
        rows = [("API key environment variable", "env_var_name"), ("Cache folder", "cache_dir"),
                ("Space ids (comma, empty = all)", "space_ids"), ("Rate limit (calls/minute)", "rate_limit_per_minute"),
                ("Quiet seconds before push", "quiet_seconds"), ("Pull every (minutes)", "pull_interval_minutes")]
        for i, (label, key) in enumerate(rows):
            ttk.Label(f, text=label).grid(row=i, column=0, sticky="w", pady=2)
            ttk.Entry(f, textvariable=self.v[key], width=50).grid(row=i, column=1, sticky="ew", pady=2)
        ttk.Button(f, text="Browse...", command=self.browse).grid(row=1, column=2, padx=4)
        self.keystate = ttk.Label(f)
        self.keystate.grid(row=0, column=2, padx=4)
        for j, (label, key) in enumerate([("Pull automatically", "auto_pull"), ("Push automatically after quiet period", "auto_push"),
                                          ("Push start dates (off = never assign start dates)", "push_start_date")]):
            ttk.Checkbutton(f, text=label, variable=self.v[key]).grid(row=6 + j, column=0, columnspan=3, sticky="w")
        bar = ttk.Frame(f)
        bar.grid(row=9, column=0, columnspan=3, sticky="w", pady=6)
        for text, cmd in [("Save", self.save), ("Pull now", lambda: self.runner.request("pull")),
                          ("Push now", lambda: self.runner.request("push")), ("Open cache folder", self.open_cache),
                          ("Reset cache...", self.reset)]:
            ttk.Button(bar, text=text, command=cmd).pack(side="left", padx=2)
        self.status = ttk.Label(f, text="")
        self.status.grid(row=10, column=0, columnspan=3, sticky="w")
        ttk.Label(f, text="Log").grid(row=11, column=0, sticky="w")
        self.log = tk.Text(f, height=14, width=90, state="disabled")
        self.log.grid(row=12, column=0, columnspan=3, sticky="nsew")

    # ---- actions ----
    def browse(self):
        d = filedialog.askdirectory(initialdir=self.v["cache_dir"].get())
        if d:
            self.v["cache_dir"].set(d)

    def save(self):
        c = self.cfg
        try:
            c.env_var_name = self.v["env_var_name"].get().strip() or "API_KEY_CLICKUP"
            c.cache_dir = self.v["cache_dir"].get().strip()
            c.space_ids = self.v["space_ids"].get().strip()
            c.rate_limit_per_minute = max(1, int(self.v["rate_limit_per_minute"].get()))
            c.quiet_seconds = max(5, int(self.v["quiet_seconds"].get()))
            c.pull_interval_minutes = max(1, int(self.v["pull_interval_minutes"].get()))
        except ValueError:
            messagebox.showerror("ClickUp Sync", "Rate limit, quiet seconds and pull interval must be numbers.")
            return
        for k in ("auto_pull", "auto_push", "push_start_date"):
            setattr(c, k, self.v[k].get())
        cfgmod.save(c)
        self.runner.request("pull")
        self.runner.log("settings saved")

    def reset(self):
        r = messagebox.askyesnocancel(
            "Reset cache",
            f"Delete every ticket file in\n{self.cfg.cache_path()}\nand re-download from ClickUp?\n\n"
            "Unpushed edits are lost. YES = keep agent notes (restored after the pull). NO = discard notes too.")
        if r is not None:
            self.save()
            self.runner.request("reset" if r else "reset-all")

    def open_cache(self):
        p = self.cfg.cache_path()
        p.mkdir(parents=True, exist_ok=True)
        if sys.platform == "win32":
            os.startfile(str(p))
        else:
            subprocess.Popen(["xdg-open", str(p)])

    def show(self):
        self.root.deiconify()
        self.root.lift()

    def hide(self):
        self.root.withdraw()

    def quit(self):
        self.runner.stop.set()
        if self.tray:
            self.tray.stop()
        self.root.destroy()

    def _tick(self):
        self.keystate.config(text="key found" if self.cfg.api_key() else "KEY NOT SET")
        self.status.config(text=f"Status: {self.runner.status}")
        text = "\n".join(self.runner.lines)
        self.log.config(state="normal")
        self.log.delete("1.0", "end")
        self.log.insert("end", text)
        self.log.see("end")
        self.log.config(state="disabled")
        self.root.after(2000, self._tick)

    def run(self):
        self.root.mainloop()

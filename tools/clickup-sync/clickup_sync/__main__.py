"""python -m clickup_sync [--headless | --pull | --push | --install-startup | --remove-startup]"""
import argparse
import os
import sys
import time
from pathlib import Path

from . import config as cfgmod
from .runner import Runner


def startup_file():
    return Path(os.environ["APPDATA"]) / "Microsoft/Windows/Start Menu/Programs/Startup/ClickUpSync.vbs"


def install_startup():
    root = Path(__file__).resolve().parent.parent
    py = Path(sys.executable)
    pyw = py.with_name("pythonw.exe") if py.with_name("pythonw.exe").exists() else py
    f = startup_file()
    f.parent.mkdir(parents=True, exist_ok=True)
    f.write_text(f'Set s = CreateObject("WScript.Shell")\ns.CurrentDirectory = "{root}"\n'
                 f's.Run """{pyw}"" -m clickup_sync --minimized", 0, False\n', encoding="utf-8")
    print("Installed:", f)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--headless", action="store_true", help="no window or tray; run the sync loop")
    ap.add_argument("--minimized", action="store_true", help="start hidden in the tray")
    ap.add_argument("--pull", action="store_true", help="one pull, then exit")
    ap.add_argument("--push", action="store_true", help="one push, then exit")
    ap.add_argument("--install-startup", action="store_true")
    ap.add_argument("--remove-startup", action="store_true")
    a = ap.parse_args()
    if a.install_startup:
        return install_startup()
    if a.remove_startup:
        return startup_file().unlink(missing_ok=True)
    cfg = cfgmod.load()
    if a.pull or a.push:
        r = Runner(cfg)
        eng = r._engine()
        if not eng:
            sys.exit(r.status)
        eng.push() if a.push else eng.pull()
        return
    if a.headless:
        r = Runner(cfg)
        r.start()
        try:
            while True:
                time.sleep(60)
        except KeyboardInterrupt:
            return
    from .ui import App
    App(show=not a.minimized).run()


if __name__ == "__main__":
    main()

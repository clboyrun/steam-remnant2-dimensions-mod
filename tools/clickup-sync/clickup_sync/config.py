"""Settings, stored as JSON in %APPDATA%\\ClickUpSync\\config.json (or ~/.config/clickup-sync)."""
import json
import os
from dataclasses import dataclass, asdict, fields
from pathlib import Path


def config_dir() -> Path:
    base = os.environ.get("APPDATA")
    if base:
        return Path(base) / "ClickUpSync"
    return Path.home() / ".config" / "clickup-sync"


def default_cache_dir() -> Path:
    return Path.home() / "ClickUpCache"


@dataclass
class Config:
    env_var_name: str = "API_KEY_CLICKUP"
    cache_dir: str = ""
    # Space ids to mirror (comma list in the UI). Empty = every space in the workspace.
    space_ids: str = "90149675055"
    team_id: str = ""  # empty = the first workspace the key can see
    rate_limit_per_minute: int = 100
    quiet_seconds: int = 180        # no file create/modify/delete for this long before pushing
    pull_interval_minutes: int = 15
    auto_pull: bool = True
    auto_push: bool = True
    include_closed: bool = True
    # Astra, 2026-11-02: the sync must NOT assign start dates. Left off unless explicitly enabled.
    push_start_date: bool = False
    # Closing a ticket (status in closed_statuses) with nobody assigned assigns this username (empty = off).
    close_assignee: str = "Astra"
    closed_statuses: str = "complete,closed,done"
    milestone_tag_prefix: str = "ms-"
    api_base: str = "https://api.clickup.com/api/v2"

    def cache_path(self) -> Path:
        return Path(self.cache_dir) if self.cache_dir else default_cache_dir()

    def api_key(self):
        return (os.environ.get(self.env_var_name) or "").strip() or None

    def space_list(self):
        return [s.strip() for s in self.space_ids.split(",") if s.strip()]


def load(path: Path = None) -> Config:
    path = path or config_dir() / "config.json"
    cfg = Config()
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        names = {f.name for f in fields(Config)}
        for k, v in data.items():
            if k in names:
                setattr(cfg, k, v)
    except (OSError, ValueError):
        pass
    return cfg


def save(cfg: Config, path: Path = None) -> None:
    path = path or config_dir() / "config.json"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(asdict(cfg), indent=2), encoding="utf-8")

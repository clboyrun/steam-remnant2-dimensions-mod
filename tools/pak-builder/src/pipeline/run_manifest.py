import json
import os
import tempfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


class RunManifest:
    def __init__(self, path: Path) -> None:
        self.path = path
        self.data: dict[str, Any] = {}

    def start(self, config: dict[str, Any]) -> None:
        self.data = {
            "started_at": datetime.now(timezone.utc).isoformat(),
            "finished_at": None,
            "status": "running",
            "phase": "starting",
            "error": None,
            "config_snapshot": config,
        }
        self._save()

    def set_phase(self, phase: str) -> None:
        self.data["phase"] = phase
        self._save()

    def set_config_snapshot(self, config: dict[str, Any]) -> None:
        self.data["config_snapshot"] = config
        self._save()

    def finish(self, status: str, error: str | None = None) -> None:
        self.data["status"] = status
        self.data["error"] = error
        self.data["finished_at"] = datetime.now(timezone.utc).isoformat()
        self._save()

    def _save(self) -> None:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temporary_path: Path | None = None
        try:
            with tempfile.NamedTemporaryFile(
                mode="w",
                encoding="utf-8",
                dir=self.path.parent,
                prefix=f".{self.path.name}.",
                suffix=".tmp",
                delete=False,
            ) as temporary_file:
                temporary_path = Path(temporary_file.name)
                json.dump(self.data, temporary_file, indent=2)
                temporary_file.write("\n")
            os.replace(temporary_path, self.path)
        except OSError:
            if temporary_path is not None:
                temporary_path.unlink(missing_ok=True)
            raise
import json
import os
import tempfile
from pathlib import Path
from typing import Any


class MappingManager:
    def __init__(self, config_path: Path) -> None:
        self.config_path = config_path.resolve()

    def load_config(self) -> dict[str, Any]:
        with self.config_path.open("r", encoding="utf-8") as config_file:
            config = json.load(config_file)
        if not isinstance(config, dict):
            raise ValueError(f"Pipeline config must contain a JSON object: {self.config_path}")
        return config

    def load_mappings(self) -> dict[str, dict[str, Any]]:
        config = self.load_config()
        configured = config.get("mappings")
        if configured is not None:
            if not isinstance(configured, dict):
                raise ValueError("'mappings' must be an object keyed by mapping ID.")
            if any(not isinstance(mapping, dict) for mapping in configured.values()):
                raise ValueError("Each mapping must be a JSON object.")
            return {mapping_id: dict(mapping) for mapping_id, mapping in configured.items()}
        return self._legacy_mappings(config)

    def save_mappings(self, mappings: dict[str, dict[str, Any]]) -> None:
        config = self.load_config()
        config["mappings"] = mappings
        self._write_json_atomically(self.config_path, config)

    @staticmethod
    def _legacy_mappings(config: dict[str, Any]) -> dict[str, dict[str, Any]]:
        mappings: dict[str, dict[str, Any]] = {}
        paths = config.get("paths", {})
        project_path = Path(config.get("engine", {}).get("uproject", "Project.uproject"))
        project_name = project_path.stem
        cooked_root = Path(paths.get("cooked", "workspace/cooked"))

        for index, asset in enumerate(config.get("assets", []), start=1):
            original = asset.get("original", "")
            orig = asset.get("orig", {})
            blueprint = asset.get("blueprint", {})
            name = asset.get("name", f"Asset_{index}")
            original_path = Path(paths.get("fmodel", "")) / original
            exported_path = Path(paths.get("orig", "workspace/orig")) / (
                f"{orig.get('name', name + '_ORIG')}.uasset"
            )
            custom_package = blueprint.get("package", f"/Game/Mods/{name}")
            package_relative = custom_package.removeprefix("/Game/").replace("/", "\\")
            cooked_path = cooked_root / project_name / "Content" / Path(package_relative)
            mappings[f"ID_{index:03d}"] = {
                "original_uasset": str(original_path),
                "original_uexp": str(original_path.with_suffix(".uexp")),
                "exported_uasset": str(exported_path),
                "exported_uexp": str(exported_path.with_suffix(".uexp")),
                "unreal_package": orig.get("package", ""),
                "stub_blueprint": f"/Game/Generated/{name}_Stub",
                "custom_blueprint": custom_package,
                "parent_class": blueprint.get("parent", orig.get("parent", "")),
                "cooked_destination": str(cooked_path.with_suffix(".uasset")),
                "replacement_source": str(exported_path),
                "replacement_source_uexp": str(exported_path.with_suffix(".uexp")),
                "overwrite_after_cook": True,
                "components": blueprint.get("components", []),
                "replace_extensions": asset.get("replace_extensions", [".uasset", ".uexp"]),
            }
        return mappings

    @staticmethod
    def _write_json_atomically(path: Path, data: dict[str, Any]) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        temporary_path: Path | None = None
        try:
            with tempfile.NamedTemporaryFile(
                mode="w",
                encoding="utf-8",
                dir=path.parent,
                prefix=f".{path.name}.",
                suffix=".tmp",
                delete=False,
            ) as temporary_file:
                temporary_path = Path(temporary_file.name)
                json.dump(data, temporary_file, indent=2)
                temporary_file.write("\n")
            os.replace(temporary_path, path)
        except OSError:
            if temporary_path is not None:
                temporary_path.unlink(missing_ok=True)
            raise
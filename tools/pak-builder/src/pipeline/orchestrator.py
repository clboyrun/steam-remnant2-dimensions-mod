import shutil
from pathlib import Path
from typing import Any

from .mapping_manager import MappingManager
from .pak_builder import PakBuilder
from .preflight import Preflight
from .run_manifest import RunManifest
from .unreal_builder import UnrealBuilder


class Pipeline:
    def __init__(self, config_path: str | Path) -> None:
        requested_path = Path(config_path)
        if not requested_path.is_absolute():
            requested_path = Path.cwd() / requested_path
        self.config_path = requested_path.resolve()
        self.root = self.config_path.parent.parent
        self.mapping_manager = MappingManager(self.config_path)
        self.config: dict[str, Any] = {}
        self.mappings: dict[str, dict[str, Any]] = {}
        self.build_script = self.root / "unreal" / "scripts" / "build_blueprints.py"
        self.manifest = RunManifest(self.root / "config" / "last_run.json")
        self.unreal_builder = UnrealBuilder(self)

    def resolve(self, value: str | Path) -> Path:
        path = Path(value)
        return path if path.is_absolute() else (self.root / path).resolve()

    def execute(self) -> Path:
        self.config = self.mapping_manager.load_config()
        self.manifest.start(self.config)
        try:
            self.mappings = self.mapping_manager.load_mappings()
            if self.config.get("mappings") != self.mappings:
                self.mapping_manager.save_mappings(self.mappings)
            self.config["mappings"] = self.mappings
            self.manifest.set_config_snapshot(self.config)

            self.manifest.set_phase("preflight")
            Preflight(self).validate()

            self.manifest.set_phase("staging")
            self.unreal_builder.stage_originals()

            self.manifest.set_phase("blueprint_build")
            self.unreal_builder.build_blueprints()

            self.manifest.set_phase("bake")
            self.unreal_builder.bake()

            self.manifest.set_phase("post_bake_replacement")
            self._replace_cooked_assets()

            self.manifest.set_phase("pak_build")
            output = PakBuilder(self).build()
        except Exception as error:
            self.manifest.finish("failed", str(error))
            raise
        self.manifest.finish("completed")
        return output

    def _replace_cooked_assets(self) -> None:
        for mapping_id, mapping in self.mappings.items():
            if not mapping.get("overwrite_after_cook", True):
                continue
            replacement = self.resolve(
                mapping.get("replacement_source") or mapping["exported_uasset"]
            )
            replacement_uexp = self.resolve(
                mapping.get("replacement_source_uexp")
                or replacement.with_suffix(".uexp")
            )
            cooked = self.resolve(mapping["cooked_destination"])
            extensions = mapping.get("replace_extensions", [".uasset", ".uexp"])
            for extension in extensions:
                if extension == ".uasset":
                    source = replacement
                elif extension == ".uexp":
                    source = replacement_uexp
                else:
                    source = replacement.with_suffix(extension)
                if not source.is_file():
                    raise FileNotFoundError(
                        f"Mapping {mapping_id} replacement file does not exist: {source}"
                    )
                destination = cooked.with_suffix(extension)
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, destination)
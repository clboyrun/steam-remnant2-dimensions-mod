import subprocess
from typing import Any

from .uassetgui_adapter import UAssetGuiAdapter


class UnrealBuilder:
    def __init__(self, pipeline: Any) -> None:
        self.pipeline = pipeline

    def stage_originals(self) -> None:
        for mapping in self.pipeline.mappings.values():
            source = self.pipeline.resolve(mapping["original_uasset"])
            destination = self.pipeline.resolve(mapping["exported_uasset"])
            source_uexp_value = mapping.get("original_uexp")
            source_uexp = self.pipeline.resolve(source_uexp_value) if source_uexp_value else None
            destination_uexp_value = mapping.get("exported_uexp")
            destination_uexp = (
                self.pipeline.resolve(destination_uexp_value)
                if destination_uexp_value
                else destination.with_suffix(".uexp")
            )
            UAssetGuiAdapter.copy_asset_pair(
                source,
                source_uexp,
                destination,
                destination_uexp,
            )

    def build_blueprints(self) -> None:
        engine = self.pipeline.config["engine"]
        command = [
            str(self.pipeline.resolve(engine["editor_cmd"])),
            str(self.pipeline.resolve(engine["uproject"])),
            f"-ExecutePythonScript={self.pipeline.build_script}",
            f"-pipeline-config={self.pipeline.config_path}",
            "-unattended",
            "-nop4",
            "-nosplash",
        ]
        subprocess.run(command, check=True, cwd=self.pipeline.root)

    def bake(self) -> None:
        engine = self.pipeline.config["engine"]
        target_platform = engine.get("target_platform", "Windows")
        command = [
            str(self.pipeline.resolve(engine["editor_cmd"])),
            str(self.pipeline.resolve(engine["uproject"])),
            "-run=Cook",
            f"-TargetPlatform={target_platform}",
            "-CookAll",
            "-unattended",
            "-nop4",
            "-NoLogTimes",
        ]
        subprocess.run(command, check=True, cwd=self.pipeline.root)
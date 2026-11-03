import subprocess
from pathlib import Path


class PakBuilder:
    def __init__(self, pipeline):
        self.pipeline = pipeline

    def build(self):
        config = self.pipeline.config
        engine = config["engine"]
        pak_exe = self.pipeline.resolve(engine["unreal_pak"])
        output = self.pipeline.resolve(config["paths"]["output"]) / config["pak"]["name"]
        response = self.pipeline.resolve(config["pak"]["response_file"])
        response.parent.mkdir(parents=True, exist_ok=True)
        output.parent.mkdir(parents=True, exist_ok=True)
        self._write_response_file(response)
        output.unlink(missing_ok=True)
        command = [
            str(pak_exe),
            str(output),
            f"-Create={response}"
        ]
        subprocess.run(command, check=True, cwd=self.pipeline.root)
        print(f"Created PAK: {output}")
        return output

    def _write_response_file(self, response: Path) -> None:
        project_name = self.pipeline.resolve(
            self.pipeline.config["engine"]["uproject"]
        ).stem
        mount_point = self.pipeline.config.get("pak", {}).get("mount_point", "../../../")
        lines: list[str] = []
        for mapping_id, mapping in self.pipeline.mappings.items():
            cooked = self.pipeline.resolve(mapping["cooked_destination"])
            package = mapping["custom_blueprint"].removeprefix("/Game/").replace("\\", "/")
            target_base = f"{mount_point}{project_name}/Content/{package}"
            extensions = mapping.get("replace_extensions", [".uasset", ".uexp"])
            for extension in extensions:
                source = cooked.with_suffix(extension)
                if not source.is_file():
                    if extension == ".uasset":
                        raise FileNotFoundError(
                            f"Mapping {mapping_id} cooked UAsset does not exist: {source}"
                        )
                    continue
                lines.append(f'"{source}" "{target_base}{extension}"')
        if not lines:
            raise ValueError("No cooked asset files are available to add to the PAK.")
        response.write_text("\n".join(lines) + "\n", encoding="utf-8")
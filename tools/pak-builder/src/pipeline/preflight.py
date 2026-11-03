from typing import Any


class Preflight:
    def __init__(self, pipeline: Any) -> None:
        self.pipeline = pipeline

    def validate(self) -> None:
        config = self.pipeline.config
        for section, key in (
            ("engine", "editor_cmd"),
            ("engine", "unreal_pak"),
            ("engine", "uproject"),
        ):
            value = config.get(section, {}).get(key)
            if not value:
                raise ValueError(f"Missing required config value: {section}.{key}")
            path = self.pipeline.resolve(value)
            if not path.is_file():
                raise FileNotFoundError(f"Configured {section}.{key} does not exist: {path}")

        if not self.pipeline.mappings:
            raise ValueError("No asset mappings are configured.")

        for mapping_id, mapping in self.pipeline.mappings.items():
            self._validate_mapping(mapping_id, mapping)

        if not self.pipeline.build_script.is_file():
            raise FileNotFoundError(
                f"Unreal Python build script does not exist: {self.pipeline.build_script}"
            )

    def _validate_mapping(self, mapping_id: str, mapping: dict[str, Any]) -> None:
        required_fields = (
            "original_uasset",
            "exported_uasset",
            "unreal_package",
            "stub_blueprint",
            "custom_blueprint",
            "parent_class",
            "cooked_destination",
        )
        for field in required_fields:
            if not mapping.get(field):
                raise ValueError(f"Mapping {mapping_id} is missing required field '{field}'.")

        source = self.pipeline.resolve(mapping["original_uasset"])
        if not source.is_file():
            raise FileNotFoundError(f"Mapping {mapping_id} source UAsset does not exist: {source}")

        source_uexp_value = mapping.get("original_uexp")
        if source_uexp_value and not self.pipeline.resolve(source_uexp_value).is_file():
            raise FileNotFoundError(
                f"Mapping {mapping_id} source UEXP does not exist: "
                f"{self.pipeline.resolve(source_uexp_value)}"
            )

        extensions = mapping.get("replace_extensions", [".uasset", ".uexp"])
        if ".uasset" not in extensions:
            raise ValueError(f"Mapping {mapping_id} replace_extensions must include '.uasset'.")
        if any(extension not in (".uasset", ".uexp", ".ubulk") for extension in extensions):
            raise ValueError(f"Mapping {mapping_id} has an unsupported replacement extension.")

        if mapping.get("overwrite_after_cook", True):
            replacement = mapping.get("replacement_source") or mapping["exported_uasset"]
            replacement_path = self.pipeline.resolve(replacement)
            is_staged_source = replacement_path == self.pipeline.resolve(mapping["exported_uasset"])
            if not is_staged_source and not replacement_path.is_file():
                raise FileNotFoundError(
                    f"Mapping {mapping_id} replacement UAsset does not exist: {replacement_path}"
                )
            replacement_uexp = mapping.get("replacement_source_uexp") or str(
                replacement_path.with_suffix(".uexp")
            )
            if ".uexp" in extensions:
                source_uexp_path = self.pipeline.resolve(replacement_uexp)
                staged_uexp_path = self.pipeline.resolve(
                    mapping.get("exported_uexp")
                    or self.pipeline.resolve(mapping["exported_uasset"]).with_suffix(".uexp")
                )
                if not source_uexp_path.is_file() and not (
                    is_staged_source
                    and source_uexp_path == staged_uexp_path
                    and source_uexp_value
                ):
                    raise FileNotFoundError(
                        f"Mapping {mapping_id} replacement UEXP does not exist: "
                        f"{source_uexp_path}"
                    )

        components = mapping.get("components", [])
        if not isinstance(components, list):
            raise ValueError(f"Mapping {mapping_id} components must be a list.")
        for component in components:
            if not isinstance(component, dict) or not component.get("name") or not component.get("type"):
                raise ValueError(
                    f"Mapping {mapping_id} components require 'name' and 'type' values."
                )
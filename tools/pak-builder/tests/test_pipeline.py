import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


SOURCE_ROOT = Path(__file__).resolve().parents[1] / "src"
sys.path.insert(0, str(SOURCE_ROOT))

from pipeline.orchestrator import Pipeline
from pipeline.pak_builder import PakBuilder
from pipeline.preflight import Preflight


class PipelineTests(unittest.TestCase):
    def setUp(self):
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        (self.root / "config").mkdir()
        (self.root / "unreal" / "scripts").mkdir(parents=True)
        (self.root / "unreal" / "scripts" / "build_blueprints.py").write_text(
            "pass\n",
            encoding="utf-8",
        )
        for executable in ("fake-editor.exe", "fake-pak.exe"):
            (self.root / executable).write_bytes(b"")
        project_file = self.root / "Project" / "Project.uproject"
        project_file.parent.mkdir()
        project_file.write_text("{}", encoding="utf-8")

        self.original_uasset = self.root / "input" / "Parent.uasset"
        self.original_uexp = self.root / "input" / "Parent.uexp"
        self.original_uasset.parent.mkdir()
        self.original_uasset.write_bytes(b"old-uasset")
        self.original_uexp.write_bytes(b"old-uexp")

        self.cooked_uasset = self.root / "cooked" / "Content" / "Mods" / "Example.uasset"
        self.mapping = {
            "original_uasset": str(self.original_uasset),
            "original_uexp": str(self.original_uexp),
            "exported_uasset": str(self.root / "staged" / "Parent_ORIG.uasset"),
            "exported_uexp": str(self.root / "staged" / "Parent_ORIG.uexp"),
            "unreal_package": "/Game/Originals/Parent_ORIG",
            "stub_blueprint": "/Game/Generated/Parent_Stub",
            "custom_blueprint": "/Game/Mods/Example",
            "parent_class": "/Game/Originals/Parent_ORIG.Parent_ORIG_C",
            "cooked_destination": str(self.cooked_uasset),
            "replacement_source": str(self.root / "staged" / "Parent_ORIG.uasset"),
            "replacement_source_uexp": str(self.root / "staged" / "Parent_ORIG.uexp"),
            "overwrite_after_cook": True,
            "components": [],
            "replace_extensions": [".uasset", ".uexp"],
        }
        self.config_path = self.root / "config" / "pipeline.json"
        config = {
            "engine": {
                "editor_cmd": str(self.root / "fake-editor.exe"),
                "unreal_pak": str(self.root / "fake-pak.exe"),
                "uproject": "Project/Project.uproject",
            },
            "paths": {"output": "output"},
            "pak": {
                "name": "Mod_P.pak",
                "mount_point": "../../../",
                "response_file": "workspace/response.txt",
            },
            "mappings": {"ID_001": self.mapping},
        }
        self.config_path.write_text(json.dumps(config), encoding="utf-8")
        self.pipeline = Pipeline(self.config_path)

    def tearDown(self):
        self.temporary_directory.cleanup()

    def test_run_manifest_is_persisted_before_preflight_and_pipeline_order_is_preserved(self):
        calls = []

        def preflight(_preflight):
            manifest = json.loads(
                (self.root / "config" / "last_run.json").read_text(encoding="utf-8")
            )
            self.assertEqual(manifest["status"], "running")
            self.assertIsNotNone(manifest["started_at"])
            calls.append("preflight")

        def build():
            calls.append("blueprint_build")

        def bake():
            calls.append("bake")

        expected_output = self.root / "output" / "Mod_P.pak"
        with patch.object(Preflight, "validate", preflight), patch.object(
            self.pipeline.unreal_builder, "build_blueprints", build
        ), patch.object(self.pipeline.unreal_builder, "bake", bake), patch.object(
            PakBuilder, "build", return_value=expected_output
        ):
            result = self.pipeline.execute()

        self.assertEqual(result, expected_output)
        self.assertEqual(
            calls,
            ["preflight", "blueprint_build", "bake"],
        )
        self.assertEqual(
            Path(self.mapping["exported_uasset"]).read_bytes(),
            b"old-uasset",
        )
        self.assertEqual(
            Path(self.mapping["exported_uexp"]).read_bytes(),
            b"old-uexp",
        )
        self.assertEqual(
            self.cooked_uasset.read_bytes(),
            b"old-uasset",
        )
        self.assertEqual(
            self.cooked_uasset.with_suffix(".uexp").read_bytes(),
            b"old-uexp",
        )
        manifest = json.loads(
            (self.root / "config" / "last_run.json").read_text(encoding="utf-8")
        )
        self.assertEqual(manifest["status"], "completed")
        self.assertEqual(manifest["phase"], "pak_build")

    def test_failure_is_written_to_run_manifest(self):
        with patch.object(Preflight, "validate", side_effect=FileNotFoundError("missing input")):
            with self.assertRaisesRegex(FileNotFoundError, "missing input"):
                self.pipeline.execute()

        manifest = json.loads(
            (self.root / "config" / "last_run.json").read_text(encoding="utf-8")
        )
        self.assertEqual(manifest["status"], "failed")
        self.assertEqual(manifest["error"], "missing input")
        self.assertIsNotNone(manifest["finished_at"])

    def test_pak_response_maps_cooked_files_to_the_custom_package(self):
        self.cooked_uasset.parent.mkdir(parents=True)
        self.cooked_uasset.write_bytes(b"cooked-uasset")
        self.cooked_uasset.with_suffix(".uexp").write_bytes(b"cooked-uexp")
        self.pipeline.config = json.loads(self.config_path.read_text(encoding="utf-8"))
        self.pipeline.mappings = {"ID_001": self.mapping}

        response = self.root / "response.txt"
        PakBuilder(self.pipeline)._write_response_file(response)
        lines = response.read_text(encoding="utf-8").splitlines()

        self.assertEqual(len(lines), 2)
        self.assertIn('"../../../Project/Content/Mods/Example.uasset"', lines[0])
        self.assertIn('"../../../Project/Content/Mods/Example.uexp"', lines[1])

    def test_preflight_rejects_missing_original_input(self):
        self.pipeline.config = json.loads(self.config_path.read_text(encoding="utf-8"))
        self.pipeline.mappings = {"ID_001": self.mapping}
        self.original_uasset.unlink()

        with self.assertRaisesRegex(FileNotFoundError, "source UAsset"):
            Preflight(self.pipeline).validate()

    def test_legacy_config_is_saved_as_mappings_before_unreal_subprocess(self):
        config = json.loads(self.config_path.read_text(encoding="utf-8"))
        config.pop("mappings")
        config["paths"] = {"fmodel": str(self.original_uasset.parent)}
        config["assets"] = [
            {
                "name": "Parent",
                "original": self.original_uasset.name,
                "orig": {
                    "name": "Parent_ORIG",
                    "package": "/Game/Originals/Parent_ORIG",
                    "parent": "/Game/Originals/Parent_ORIG.Parent_ORIG_C",
                },
                "blueprint": {
                    "package": "/Game/Mods/Example",
                    "parent": "/Game/Originals/Parent_ORIG.Parent_ORIG_C",
                    "components": [],
                },
            }
        ]
        self.config_path.write_text(json.dumps(config), encoding="utf-8")
        self.pipeline = Pipeline(self.config_path)

        def verify_config_is_available(_preflight):
            saved_config = json.loads(self.config_path.read_text(encoding="utf-8"))
            self.assertIn("ID_001", saved_config["mappings"])
            self.assertEqual(saved_config["mappings"]["ID_001"]["custom_blueprint"], "/Game/Mods/Example")

        expected_output = self.root / "output" / "Mod_P.pak"
        with patch.object(Preflight, "validate", verify_config_is_available), patch.object(
            self.pipeline.unreal_builder, "build_blueprints"
        ), patch.object(self.pipeline.unreal_builder, "bake"), patch.object(
            PakBuilder, "build", return_value=expected_output
        ):
            self.pipeline.execute()

    def test_invalid_mapping_config_is_recorded_as_a_failed_run(self):
        config = json.loads(self.config_path.read_text(encoding="utf-8"))
        config["mappings"] = []
        self.config_path.write_text(json.dumps(config), encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "'mappings' must be an object"):
            self.pipeline.execute()

        manifest = json.loads(
            (self.root / "config" / "last_run.json").read_text(encoding="utf-8")
        )
        self.assertEqual(manifest["status"], "failed")
        self.assertIn("'mappings' must be an object", manifest["error"])


if __name__ == "__main__":
    unittest.main()

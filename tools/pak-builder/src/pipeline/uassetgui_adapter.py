import shutil
from pathlib import Path


class UAssetGuiAdapter:
    @staticmethod
    def copy_asset_pair(
        source_uasset: Path,
        source_uexp: Path | None,
        destination_uasset: Path,
        destination_uexp: Path | None,
    ) -> None:
        if not source_uasset.is_file():
            raise FileNotFoundError(f"Source UAsset does not exist: {source_uasset}")
        destination_uasset.parent.mkdir(parents=True, exist_ok=True)
        if source_uasset.resolve() != destination_uasset.resolve():
            shutil.copy2(source_uasset, destination_uasset)
        if source_uexp is not None:
            if not source_uexp.is_file():
                raise FileNotFoundError(f"Source UEXP does not exist: {source_uexp}")
            if destination_uexp is None:
                destination_uexp = destination_uasset.with_suffix(".uexp")
            destination_uexp.parent.mkdir(parents=True, exist_ok=True)
            if source_uexp.resolve() != destination_uexp.resolve():
                shutil.copy2(source_uexp, destination_uexp)
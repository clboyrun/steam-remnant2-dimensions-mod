# Pak Builder

The tool generates a stub Blueprint and a custom child Blueprint for each configured mapping, cooks the Unreal project, optionally replaces cooked `.uasset`/`.uexp` files with the configured exported pair, and packages the cooked files into a PAK.

## Run

Install the UI dependency and run the Python launcher from the repository root:

```powershell
py -m pip install -r tools\pak-builder\requirements.txt
py tools\pak-builder\src\main.py
```

This opens the mapping UI. Use **Execute All Active Cook Implementations** to save the mappings and launch the complete pipeline. For a non-UI run:

```powershell
py tools\pak-builder\src\main.py --run
```

Mappings are persisted in `config/pipeline.json`. `config/last_run.json` is written at execution start with a timestamp and complete config snapshot, then updated with the current phase and final result. Each execution rebuilds the configured Blueprints and runs a cook; it does not skip work based on cached timestamps.

## Configuration

Configure the Unreal Editor command, UnrealPak, project file, cook output directory, and PAK response-file location in `config/pipeline.json`. Each mapping specifies:

- The source UAsset and optional UEXP pair to stage at the exported paths.
- Unreal package and parent class paths.
- Stub and custom Blueprint package paths. The custom Blueprint is generated as a child of the generated stub.
- Optional stub components as JSON objects with `name`, Unreal `type`, and optional `parent`.
- The cooked UAsset destination, replacement UAsset/UEXP source pair, and post-cook overwrite toggle.

The Unreal project must have the Python Editor Script Plugin enabled and must be able to resolve the configured parent class and component classes. The configured replacement source must already contain the complete files listed in `replace_extensions` (default: `.uasset` and `.uexp`). A successful build writes a response file and invokes UnrealPak; any failed command or missing input is reported and recorded in `last_run.json`.

The source-pair staging step copies bytes as-is; it does not rewrite package names or binary headers. Use already prepared/exported source assets for the replacement pair. The generated Blueprint stubs provide the configured inheritance and component hierarchy inside the project.

## Tests

```powershell
py -m unittest discover -s tools\pak-builder\tests
```
# pak-builder config JSON (blueprint mappings)

This documents the JSON that `tools/pak-builder` reads to create Blueprints and a pak. It is written from the code
on the `pak-builder` branch (`mapping_manager.py`, `preflight.py`, `orchestrator.py`, `unreal_builder.py`,
`pak_builder.py`, `unreal/scripts/build_blueprints.py`). pak-builder is not in `main` yet. Nothing here has been run
in Unreal.

Run it from the repo root: `py tools\pak-builder\src\main.py --run` (no UI) or without `--run` (mapping UI). Both read
`tools/pak-builder/config/pipeline.json`.

## What a run does

Phases, in order (recorded in `config/last_run.json`):

1. `preflight`: checks the tool paths and every mapping's required fields and files.
2. `staging`: copies each mapping's original `.uasset`/`.uexp` to its exported path.
3. `blueprint_build`: runs `build_blueprints.py` in `UnrealEditor-Cmd`. For each mapping it creates a **stub**
   Blueprint (child of `parent_class`, with the listed `components`), then a **custom** Blueprint that is a child of
   the stub. Existing assets at those paths are deleted and rebuilt.
4. `bake`: `UnrealEditor-Cmd -run=Cook -CookAll` for `engine.target_platform`.
5. `post_bake_replacement`: copies the replacement `.uasset`/`.uexp` over the cooked files (unless
   `overwrite_after_cook` is false).
6. `pak_build`: writes the response file and runs UnrealPak to create the pak.

## Top-level shape

```json
{
  "engine":   { "...": "tool locations" },
  "paths":    { "...": "folders" },
  "pak":      { "...": "pak output" },
  "mappings": { "ID_001": { "...": "one mapping per asset" } }
}
```

Relative paths resolve against the `tools/pak-builder` folder.

### `engine`

| Field | Required | Meaning |
| --- | --- | --- |
| `uproject` | yes | Path to the project, e.g. `Rem2Proj/Remnant2.uproject`. Its file name (no extension) is also the project name used in the pak. |
| `editor_cmd` | yes | `UnrealEditor-Cmd.exe` (UE 5.2). |
| `unreal_pak` | yes | `UnrealPak.exe`. |
| `target_platform` | no | Defaults to `Windows`. |
| `version` | no | Informational (`5.2`). |

The three required paths must exist on disk or preflight fails.

### `paths`

| Field | Used for |
| --- | --- |
| `output` | Folder the pak is written to. |
| `cooked` | Cooked output root (only used when converting the older `assets` format, below). |
| `fmodel`, `orig` | Source and staging folders (only used when converting the older `assets` format). |
| `staged`, `backup` | Present in the sample config; not read by the code. |

### `pak`

| Field | Meaning |
| --- | --- |
| `name` | Pak file name, e.g. `MyMod_P.pak`. |
| `response_file` | Where the UnrealPak response file is written. |
| `mount_point` | Defaults to `../../../`. |

## `mappings`

An object keyed by a mapping ID (any string, e.g. `ID_001`). Each value is one asset to replace/create:

| Field | Required | Meaning |
| --- | --- | --- |
| `original_uasset` | yes | The exported original asset (from FModel/UAssetGUI). Must exist. |
| `original_uexp` | no | Its `.uexp`. If set, must exist. |
| `exported_uasset` | yes | Where the original is staged, e.g. `workspace/orig/BP_Example_ORIG.uasset`. |
| `exported_uexp` | no | Staged `.uexp`; defaults to the `.uasset` path with `.uexp`. |
| `unreal_package` | yes | Package path of the original inside the project, e.g. `/Game/Characters/BP_Example_ORIG`. |
| `stub_blueprint` | yes | Package path for the generated stub, e.g. `/Game/Generated/BP_Example_Stub`. Must start with `/Game/`. |
| `custom_blueprint` | yes | Package path for the generated custom Blueprint (child of the stub), e.g. `/Game/Mods/Characters/BP_Example`. Also decides the path inside the pak. |
| `parent_class` | yes | Class the stub derives from. A full class path (`/Game/Characters/BP_Example_ORIG.BP_Example_ORIG_C`) or an asset path without the `.Name_C` suffix, which is added for you. |
| `cooked_destination` | yes | Where the cooked `.uasset` will be, e.g. `<cooked>/Remnant2/Content/Mods/Characters/BP_Example.uasset`. |
| `components` | no | List of components added to the stub (below). |
| `replacement_source` | no | `.uasset` copied over the cooked file after cooking. Defaults to `exported_uasset`. |
| `replacement_source_uexp` | no | Matching `.uexp`. Defaults to the replacement's path with `.uexp`. |
| `overwrite_after_cook` | no | Default `true`. Set `false` to keep the cooked file. |
| `replace_extensions` | no | Default `[".uasset", ".uexp"]`. Must include `.uasset`; allowed values are `.uasset`, `.uexp`, `.ubulk`. Also the list of files packed. |

### `components`

Each entry is added to the stub Blueprint:

```json
{ "name": "Mesh", "type": "StaticMeshComponent", "parent": "DefaultSceneRoot" }
```

- `name` (required): component name.
- `type` (required): a class path, or the name of a class exposed in Unreal's Python API (e.g. `SceneComponent`,
  `StaticMeshComponent`). Unresolvable types fail the build.
- `parent` (optional): name of the component to attach under.

### Example mapping

```json
"mappings": {
  "ID_001": {
    "original_uasset": "C:/FModel/Exports/Game/Content/Characters/BP_Example.uasset",
    "original_uexp": "C:/FModel/Exports/Game/Content/Characters/BP_Example.uexp",
    "exported_uasset": "workspace/orig/BP_Example_ORIG.uasset",
    "unreal_package": "/Game/Characters/BP_Example_ORIG",
    "stub_blueprint": "/Game/Generated/BP_Example_Stub",
    "custom_blueprint": "/Game/Mods/Characters/BP_Example",
    "parent_class": "/Game/Characters/BP_Example_ORIG.BP_Example_ORIG_C",
    "cooked_destination": "C:/ModProject/Saved/Cooked/Windows/Remnant2/Content/Mods/Characters/BP_Example.uasset",
    "components": [
      { "name": "DefaultSceneRoot", "type": "SceneComponent" },
      { "name": "Mesh", "type": "StaticMeshComponent", "parent": "DefaultSceneRoot" }
    ]
  }
}
```

## Reading the FModel dump for a mapping (2026-11-04)

Astra's dump (`workspace/extracted/Remnant2/Content/...`) is FModel's **property view**: each asset is a `.json`
file, not a `.uasset`/`.uexp` pair. pak-builder's `original_uasset` and `original_uexp` need the real cooked files,
so export those separately (FModel "Export Raw Data", or UAssetGUI) and keep the JSON for reading values.

How to turn a dump path into the fields above:

- `unreal_package` and `parent_class`: the file path under `Content/` with `/Game/` in front and no extension.
  `Content/_Core/ItemTypes/ItemType_Ring.json` is package `/Game/_Core/ItemTypes/ItemType_Ring`, and its class is
  `/Game/_Core/ItemTypes/ItemType_Ring.ItemType_Ring_C`. The class is the first entry of the JSON
  (`Type: BlueprintGeneratedClass`, `Name: ItemType_Ring_C`); its `SuperStruct` shows the parent
  (`/Script/GunfireRuntime` `ItemType`).
- A `parent_class` of a native class is `/Script/<Module>.<Class>`, for example `/Script/Remnant.ZoneDef` for a zone def.
- The second JSON entry (`Default__<Name>_C`) holds the property values the child inherits, for example `TileSet`,
  `EncounterGroups` and `SkyBoxes` on a zone def. Those are what a child Blueprint overrides.
- Data tables (`Type: DataTable`) list `Rows` by name and a `RowStruct`. Row property names have a numeric id and GUID
  suffix (`Damage_2_201A70...`); use the full name from the dump when writing rows, not the short one.

Worked example from the dump: a zone def mapping would have `parent_class` `/Script/Remnant.ZoneDef` (or
`/Game/Campaign_Main/ZoneDefs/ZoneDef_RootEarth_City.ZoneDef_RootEarth_City_C` to inherit its encounter groups).
Dump files differ in how they name the parent: classes whose parent is native use `SuperStruct`, and some Blueprint
classes use `Super` instead (for example `Ring_BlessedRing_C`, parent `Trinket_Base_C`), so check both keys when finding
`parent_class`.

Nothing here has been run through pak-builder.

## Older `assets` format

If the config has no `mappings` key, pak-builder converts a top-level `assets` list into mappings (IDs `ID_001`,
`ID_002`, ...) and saves them. Each asset: `name`, `original` (path under `paths.fmodel`), `orig` (`name`,
`package`, `parent`), `blueprint` (`package`, `parent`, `components`) and optional `replace_extensions`. The sample
`pipeline.json` uses this format.

**Gotcha:** the sample file also contains `"mappings": {}`. An empty `mappings` object counts as "present", so the
`assets` list is ignored and the run stops with "No asset mappings are configured". Delete the `"mappings"` line
(or fill it in) to use `assets`.

## Limits and open items

- There is no JSON schema file; validation is the preflight code above.
- JSON describes Blueprint inheritance and components only. It does not set stats, loot, quests or other game
  data; those are still done in the editor, or are open design work (see [architecture.md](architecture.md)).
- Game facts (template classes, row structs, stat field names) are in `data/catalog.json`.

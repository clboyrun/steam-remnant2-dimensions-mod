"""
Dimensions editor step: applies a build plan (plan.json from `dimensions plan`/`dimensions build`) to the Unreal
project. Runs inside UE 5.2's editor Python, normally headless:

    UnrealEditor-Cmd.exe Remnant2.uproject -run=pythonscript -script=dimensions_apply.py

with DIMENSIONS_PLAN pointing at plan.json (or the path as the first script argument). Writes a result file
(DIMENSIONS_RESULT, default next to the plan) that the CLI reads.

Order of work:
  1. Check every asset the plan needs is in the project (templates, lists it edits). Stop if any are missing.
  2. Import textures and meshes, make material instances and data tables.
  3. Create every blueprint and object, empty, so they can all reference each other.
  4. Set their properties and components, add to existing lists, compile and save.
Assets under the dimension's content root are rebuilt from scratch each run. Game assets the plan edits are only
appended to, and entries added on an earlier run are replaced rather than added again.
"""

import hashlib
import json
import os
import sys
import traceback

import unreal

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from dimensions_names import enum_member, package_of, snake_case, split_package  # noqa: E402

TAG = "[Dimensions] "
assets = unreal.EditorAssetLibrary
tools = unreal.AssetToolsHelpers.get_asset_tools()


def log(message):
    unreal.log(TAG + message)


def warn(message):
    unreal.log_warning(TAG + message)


class PlanError(Exception):
    pass


class Context:
    def __init__(self, plan):
        self.plan = plan
        self.root = plan["contentRoot"]
        self.culture = plan["dimension"].get("defaultCulture", "en")
        self.overrides = set(plan.get("overrides", []))
        self.created = {}  # package -> asset
        self.modified = {}  # package -> asset
        self.errors = []
        self.warnings = []

    def error(self, message):
        self.errors.append(message)
        unreal.log_error(TAG + message)

    def warning(self, message):
        self.warnings.append(message)
        warn(message)

    def owns(self, package):
        return package.startswith(self.root + "/") or package in self.overrides


# ---------------------------------------------------------------- loading


def load_asset(path):
    asset = assets.load_asset(package_of(path)) if not path.startswith("/Script/") else None
    if asset is None:
        asset = unreal.load_object(None, path)
    if asset is None:
        raise PlanError("asset not found: " + path)
    return asset


def load_class(path):
    """A class from a native path (/Script/Module.Class) or a blueprint (/Game/.../X.X_C or the package)."""
    if path.startswith("/Script/"):
        cls = unreal.load_class(None, path)
        if cls is None:
            raise PlanError("native class not found: " + path + " (is the module in the project?)")
        return cls
    package = package_of(path)
    asset = assets.load_asset(package)
    if isinstance(asset, unreal.Blueprint):
        return asset.generated_class()
    cls = unreal.load_object(None, path if path.endswith("_C") else path + "_C")
    if cls is None:
        raise PlanError("class not found: " + path)
    return cls


def load_struct_type(path):
    if path.startswith("/Script/"):
        struct = unreal.load_object(None, path)
    else:
        struct = assets.load_asset(package_of(path))
    if struct is None:
        raise PlanError("row struct not found: " + path)
    return struct


def struct_type(name):
    cls = getattr(unreal, name, None)
    if cls is None:
        raise PlanError("struct type not exposed to Python: " + name)
    return cls


# ---------------------------------------------------------------- properties


def get_prop(obj, name):
    for candidate in (name, snake_case(name)):
        try:
            return obj.get_editor_property(candidate), candidate
        except Exception:  # noqa: BLE001 - Unreal raises plain Exception for unknown properties
            continue
    raise PlanError("{} has no property '{}'".format(type(obj).__name__, name))


def set_prop(obj, name, value):
    current, python_name = get_prop(obj, name)
    try:
        obj.set_editor_property(python_name, convert(value, current, obj))
    except Deferred:
        DEFERRED.append((obj, name, value))


def text(value, ctx_culture):
    if isinstance(value, dict):
        # Only the default culture goes in the asset. Other cultures need the project's localization gather.
        return unreal.Text(value.get(ctx_culture) or next(iter(value.values()), ""))
    return unreal.Text(value)


def stable_name(prefix, payload):
    digest = hashlib.sha1(json.dumps(payload, sort_keys=True).encode("utf-8")).hexdigest()[:10]
    return "{}_{}".format(prefix, digest)


CULTURE = "en"

# Instanced objects made with "$new" and a "$key", so other values can point at them with {"$node": key}.
NODES = {}
# (object, property, value) whose {"$node"} target didn't exist yet; set again once everything is built.
DEFERRED = []


class Deferred(Exception):
    pass


def merge_by_key(current, key, items, outer):
    """Updates elements of `current` whose `key` field matches an item, and appends the rest."""
    result = list(current or [])
    python_key = snake_case(key)
    for item in items:
        fields = item.get("$struct", item)
        wanted = convert(fields[key], None, outer)
        match = next((i for i, e in enumerate(result) if str(e.get_editor_property(python_key)) == str(wanted)), None)
        if match is None:
            result.append(convert(item, result[0] if result else None, outer))
            continue
        element = result[match]
        for field, value in fields.items():
            field_current, python_name = get_prop(element, field)
            element.set_editor_property(python_name, convert(value, field_current, outer))
        result[match] = element
    return result


def convert(value, current, outer):
    """A JSON value from the plan to the Unreal value for a property whose current value is `current`."""
    if value is None:
        return None
    if isinstance(value, dict):
        if "$asset" in value:
            return load_asset(value["$asset"])
        if "$class" in value:
            return load_class(value["$class"])
        if "$soft" in value:
            try:
                return unreal.SoftObjectPath(value["$soft"])
            except Exception:  # noqa: BLE001
                return load_asset(value["$soft"])
        if "$softClass" in value:
            try:
                return unreal.SoftClassPath(value["$softClass"])
            except Exception:  # noqa: BLE001
                return load_class(value["$softClass"])
        if "$text" in value:
            return text(value["$text"], CULTURE)
        if "$name" in value:
            return unreal.Name(value["$name"])
        if "$enum" in value:
            enum_type = type(current)
            member = enum_member(value["$enum"])
            if not hasattr(enum_type, member):
                raise PlanError("{} has no value {}".format(enum_type.__name__, value["$enum"]))
            return getattr(enum_type, member)
        if "$node" in value:
            if value["$node"] not in NODES:
                raise Deferred(value["$node"])
            return NODES[value["$node"]]
        if "$mergeByKey" in value:
            return merge_by_key(current, value["$mergeByKey"], value["items"], outer)
        if "$rowHandle" in value:
            handle = unreal.DataTableRowHandle()
            handle.set_editor_property("data_table", load_asset(value["$rowHandle"]["table"]))
            handle.set_editor_property("row_name", unreal.Name(value["$rowHandle"]["row"]))
            return handle
        if "$new" in value:
            cls = load_class(value["$new"])
            name = stable_name("Dim_" + cls.get_name(), value)
            obj = unreal.new_object(cls, outer=outer, name=name)
            if "$key" in value:
                NODES[value["$key"]] = obj
            for key, prop in value.get("properties", {}).items():
                set_prop(obj, key, prop)
            return obj
        if "$struct" in value:
            base = type(current)() if isinstance(current, unreal.StructBase) else struct_type(value["$type"])()
            for key, prop in value["$struct"].items():
                field_current, python_name = get_prop(base, key)
                base.set_editor_property(python_name, convert(prop, field_current, outer))
            return base
        if isinstance(current, unreal.StructBase):
            copy = type(current)()
            for key, prop in value.items():
                field_current, python_name = get_prop(current, key)
                copy.set_editor_property(python_name, convert(prop, field_current, outer))
            return copy
        raise PlanError("can't set an object value on a property holding {}".format(type(current).__name__))
    if isinstance(value, list):
        hint = current[0] if current is not None and len(current) > 0 else None
        return [convert(v, hint, outer) for v in value]
    if isinstance(current, unreal.Text):
        return unreal.Text(str(value))
    if isinstance(current, unreal.Name):
        return unreal.Name(str(value))
    if isinstance(current, float) and isinstance(value, int):
        return float(value)
    return value


# ---------------------------------------------------------------- asset creation


def prepare_slot(ctx, package):
    """Deletes an asset from an earlier run so it can be made again. Never touches assets the plan doesn't own."""
    if not assets.does_asset_exist(package):
        return
    if not ctx.owns(package):
        raise PlanError("refusing to replace " + package + ": it's outside the dimension's folder")
    if not assets.delete_asset(package):
        raise PlanError("couldn't delete the old " + package + " (is it open in an editor?)")


def import_texture(ctx, op):
    folder, name = split_package(op["asset"])
    prepare_slot(ctx, op["asset"])
    task = unreal.AssetImportTask()
    task.set_editor_property("filename", op["source"])
    task.set_editor_property("destination_path", folder)
    task.set_editor_property("destination_name", name)
    task.set_editor_property("automated", True)
    task.set_editor_property("replace_existing", True)
    task.set_editor_property("save", False)
    tools.import_asset_tasks([task])
    texture = assets.load_asset(op["asset"])
    if texture is None:
        raise PlanError("texture import failed: " + op["source"])
    if op.get("usage") == "ui":
        texture.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_EDITOR_ICON)
        texture.set_editor_property("lod_group", unreal.TextureGroup.TEXTUREGROUP_UI)
        texture.set_editor_property("mip_gen_settings", unreal.TextureMipGenSettings.TMGS_NO_MIPMAPS)
        texture.set_editor_property("never_stream", True)
    elif "normal" in name.lower():
        texture.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_NORMALMAP)
        texture.set_editor_property("srgb", False)
    ctx.created[op["asset"]] = texture


def import_mesh(ctx, op):
    folder, name = split_package(op["asset"])
    prepare_slot(ctx, op["asset"])
    source = op["source"]
    skeletal = op.get("meshType", "skeletal") == "skeletal"
    task = unreal.AssetImportTask()
    task.set_editor_property("filename", source)
    task.set_editor_property("destination_path", folder)
    task.set_editor_property("destination_name", name)
    task.set_editor_property("automated", True)
    task.set_editor_property("replace_existing", True)
    task.set_editor_property("save", False)
    if source.lower().endswith(".fbx"):
        ui = unreal.FbxImportUI()
        ui.set_editor_property("import_mesh", True)
        ui.set_editor_property("import_as_skeletal", skeletal)
        ui.set_editor_property("import_materials", False)
        ui.set_editor_property("import_textures", False)
        ui.set_editor_property("import_animations", False)
        ui.set_editor_property(
            "mesh_type_to_import",
            unreal.FBXImportType.FBXIT_SKELETAL_MESH if skeletal else unreal.FBXImportType.FBXIT_STATIC_MESH,
        )
        data = ui.get_editor_property("skeletal_mesh_import_data" if skeletal else "static_mesh_import_data")
        data.set_editor_property("import_uniform_scale", float(op.get("scale", 1.0)))
        if skeletal and op.get("skeleton"):
            ui.set_editor_property("skeleton", load_asset(op["skeleton"]))
        for key, value in op.get("options", {}).items():
            target = data if hasattr(data, key) else ui
            target.set_editor_property(key, value)
        task.set_editor_property("options", ui)
    elif op.get("scale", 1.0) != 1.0 or op.get("skeleton"):
        ctx.warning("{}: scale and skeleton are only applied to FBX imports".format(source))
    tools.import_asset_tasks([task])
    mesh = assets.load_asset(op["asset"])
    if mesh is None:
        raise PlanError("mesh import failed: " + source)
    ctx.created[op["asset"]] = mesh


def create_material_instance(ctx, op):
    folder, name = split_package(op["asset"])
    prepare_slot(ctx, op["asset"])
    mi = tools.create_asset(name, folder, unreal.MaterialInstanceConstant, unreal.MaterialInstanceConstantFactoryNew())
    lib = unreal.MaterialEditingLibrary
    lib.set_material_instance_parent(mi, load_asset(op["parent"]))
    for param, texture in op.get("textures", {}).items():
        if not lib.set_material_instance_texture_parameter_value(mi, param, load_asset(texture)):
            ctx.warning("{}: parent material has no texture parameter '{}'".format(op["asset"], param))
    for param, value in op.get("scalars", {}).items():
        if not lib.set_material_instance_scalar_parameter_value(mi, param, float(value)):
            ctx.warning("{}: parent material has no scalar parameter '{}'".format(op["asset"], param))
    for param, value in op.get("vectors", {}).items():
        rgba = list(value) + [1.0] * (4 - len(value))
        if not lib.set_material_instance_vector_parameter_value(mi, param, unreal.LinearColor(*rgba)):
            ctx.warning("{}: parent material has no vector parameter '{}'".format(op["asset"], param))
    lib.update_material_instance(mi)
    ctx.created[op["asset"]] = mi


def create_data_table(ctx, op):
    folder, name = split_package(op["asset"])
    prepare_slot(ctx, op["asset"])
    factory = unreal.DataTableFactory()
    factory.set_editor_property("struct", load_struct_type(op["rowStruct"]))
    table = tools.create_asset(name, folder, unreal.DataTable, factory)
    rows = [dict(row, Name=row_name) for row_name, row in op["rows"].items()]
    if not unreal.DataTableFunctionLibrary.fill_data_table_from_json_string(table, json.dumps(rows)):
        raise PlanError("{}: the rows don't match the row struct {} (check field names)".format(op["asset"], op["rowStruct"]))
    ctx.created[op["asset"]] = table


def create_blueprint(ctx, op):
    folder, name = split_package(op["asset"])
    prepare_slot(ctx, op["asset"])
    parent = load_class(op["parent"])
    if issubclass(parent, unreal.AnimInstance):
        # A child animation blueprint: same skeleton as its parent, layers changed through properties.
        factory = unreal.AnimBlueprintFactory()
        factory.set_editor_property("parent_class", parent)
        parent_bp = assets.load_asset(package_of(op["parent"]))
        if isinstance(parent_bp, unreal.AnimBlueprint):
            factory.set_editor_property("target_skeleton", parent_bp.get_editor_property("target_skeleton"))
    else:
        factory = unreal.BlueprintFactory()
        factory.set_editor_property("parent_class", parent)
    blueprint = tools.create_asset(name, folder, None, factory)
    if blueprint is None:
        raise PlanError("couldn't create blueprint {} from {}".format(op["asset"], op["parent"]))
    unreal.BlueprintEditorLibrary.compile_blueprint(blueprint)
    ctx.created[op["asset"]] = blueprint


def import_animation(ctx, op):
    folder, name = split_package(op["asset"])
    prepare_slot(ctx, op["asset"])
    task = unreal.AssetImportTask()
    task.set_editor_property("filename", op["source"])
    task.set_editor_property("destination_path", folder)
    task.set_editor_property("destination_name", name)
    task.set_editor_property("automated", True)
    task.set_editor_property("replace_existing", True)
    task.set_editor_property("save", False)
    ui = unreal.FbxImportUI()
    ui.set_editor_property("import_mesh", False)
    ui.set_editor_property("import_animations", True)
    ui.set_editor_property("import_materials", False)
    ui.set_editor_property("import_textures", False)
    ui.set_editor_property("mesh_type_to_import", unreal.FBXImportType.FBXIT_ANIMATION)
    if op.get("skeleton"):
        ui.set_editor_property("skeleton", load_asset(op["skeleton"]))
    task.set_editor_property("options", ui)
    tools.import_asset_tasks([task])
    anim = assets.load_asset(op["asset"])
    if anim is None:
        # FBX imports name the sequence after the take; find what landed in the folder.
        imported = [p for p in task.get_editor_property("imported_object_paths") or []]
        anim = assets.load_asset(imported[0]) if imported else None
        if anim is not None and package_of(anim.get_path_name()) != op["asset"]:
            assets.rename_asset(package_of(anim.get_path_name()), op["asset"])
            anim = assets.load_asset(op["asset"])
    if anim is None:
        raise PlanError("animation import failed: " + op["source"] + " (does it match the skeleton?)")
    ctx.created[op["asset"]] = anim


def create_object(ctx, op):
    folder, name = split_package(op["asset"])
    prepare_slot(ctx, op["asset"])
    cls = load_class(op["class"])
    if hasattr(unreal, "DimensionsObjectFactory"):
        factory = unreal.DimensionsObjectFactory()
        factory.set_editor_property("object_class", cls)
    elif issubclass(cls, unreal.DataAsset):
        factory = unreal.DataAssetFactory()
        factory.set_editor_property("data_asset_class", cls)
    else:
        raise PlanError(
            "{} needs the DimensionsEditor plugin to create a {} (run `dimensions setup`)".format(op["asset"], cls.get_name())
        )
    asset = tools.create_asset(name, folder, cls, factory)
    if asset is None:
        raise PlanError("couldn't create {} as {}".format(op["asset"], cls.get_name()))
    ctx.created[op["asset"]] = asset


def copy_asset(ctx, op):
    prepare_slot(ctx, op["asset"])
    asset = assets.duplicate_asset(package_of(op["source"]), op["asset"])
    if asset is None:
        raise PlanError("couldn't copy {} to {}".format(op["source"], op["asset"]))
    ctx.created[op["asset"]] = asset


# ---------------------------------------------------------------- properties and components


def property_target(asset):
    """Where properties go: a blueprint's class defaults, or the asset itself."""
    if isinstance(asset, unreal.Blueprint):
        return unreal.get_default_object(asset.generated_class())
    return asset


def apply_properties(ctx, op):
    asset = ctx.created[op["asset"]]
    target = property_target(asset)
    for key, value in op.get("properties", {}).items():
        try:
            set_prop(target, key, value)
        except Exception as e:  # noqa: BLE001
            ctx.error("{}: setting {} failed: {}".format(op["asset"], key, e))
    if op.get("addComponents"):
        add_components(ctx, op["asset"], asset, op["addComponents"])
    if op.get("components"):
        apply_components(ctx, op["asset"], asset, op["components"])


def add_components(ctx, package, blueprint, components):
    """Adds new components (quest objectives, quest entities, sub-quests) to a blueprint, then sets them up."""
    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    handles = subsystem.k2_gather_subobject_data_for_blueprint(blueprint)
    if not handles:
        ctx.error("{}: couldn't read its components".format(package))
        return
    root = handles[0]
    existing = blueprint_components(blueprint)
    for spec in components:
        name = spec["name"]
        if name not in existing:
            params = unreal.AddNewSubobjectParams(
                parent_handle=root, new_class=load_class(spec["class"]), blueprint_context=blueprint)
            handle, failure = subsystem.add_new_subobject(params)
            if not unreal.SubobjectDataBlueprintFunctionLibrary.is_handle_valid(handle):
                ctx.error("{}: couldn't add component {}: {}".format(package, name, failure))
                continue
            subsystem.rename_subobject(handle, unreal.Text(name))
    if any(spec.get("properties") for spec in components):
        apply_components(ctx, package, blueprint, {spec["name"]: spec.get("properties", {}) for spec in components})


def blueprint_components(blueprint):
    """Component name -> the template object to edit for this blueprint (inherited ones get an override)."""
    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    lib = unreal.SubobjectDataBlueprintFunctionLibrary
    found = {}
    for handle in subsystem.k2_gather_subobject_data_for_blueprint(blueprint):
        data = lib.get_data(handle)
        name = str(lib.get_variable_name(data))
        if not name or name == "None":
            continue
        try:
            obj = lib.get_object_for_blueprint(data, blueprint)
        except Exception:  # noqa: BLE001 - older builds only have get_object
            obj = lib.get_object(data)
        if obj is not None:
            found[name] = obj
    return found


def apply_components(ctx, package, blueprint, components):
    if not isinstance(blueprint, unreal.Blueprint):
        ctx.error("{}: components can only be set on blueprints".format(package))
        return
    available = blueprint_components(blueprint)
    for name, props in components.items():
        component = available.get(name)
        if component is None:
            ctx.error("{}: no component named '{}' (it has: {})".format(package, name, ", ".join(sorted(available)) or "none"))
            continue
        for key, value in props.items():
            try:
                if key == "$mesh":
                    set_mesh(component, load_asset(value["$asset"]))
                elif key == "$materials":
                    set_materials(component, [load_asset(v["$asset"]) for v in value], props.get("$materialSlots"))
                elif key == "$materialSlots":
                    continue
                else:
                    set_prop(component, key, value)
            except Exception as e:  # noqa: BLE001
                ctx.error("{}: component {} {}: {}".format(package, name, key, e))


def set_mesh(component, mesh):
    if isinstance(component, unreal.SkeletalMeshComponent):
        if not isinstance(mesh, unreal.SkeletalMesh):
            raise PlanError("the component is a SkeletalMeshComponent; import the model with type 'skeletal'")
        try:
            component.set_editor_property("skeletal_mesh_asset", mesh)
        except Exception:  # noqa: BLE001 - property name before 5.1
            component.set_editor_property("skeletal_mesh", mesh)
    elif isinstance(component, unreal.StaticMeshComponent):
        if not isinstance(mesh, unreal.StaticMesh):
            raise PlanError("the component is a StaticMeshComponent; import the model with type 'static'")
        component.set_editor_property("static_mesh", mesh)
    else:
        raise PlanError("{} isn't a mesh component".format(type(component).__name__))


def set_materials(component, materials, slots):
    """Puts each material on its named slot, or in order when no slot names are given."""
    if not (slots and any(slots)):
        component.set_editor_property("override_materials", materials)
        return
    names = [str(n) for n in component.get_material_slot_names()]
    overrides = list(component.get_editor_property("override_materials"))
    overrides += [None] * (max(len(names), len(materials)) - len(overrides))
    for i, material in enumerate(materials):
        slot = slots[i] if i < len(slots) else None
        overrides[names.index(slot) if slot in names else i] = material
    component.set_editor_property("override_materials", overrides)


def append_to_list(ctx, op):
    package = op["asset"]
    asset = ctx.created.get(package) or assets.load_asset(package)
    if asset is None:
        raise PlanError("list asset not found: " + package)
    target = property_target(asset)
    current, python_name = get_prop(target, op["property"])
    items = list(current)
    hint = items[0] if items else None
    for raw in op["items"]:
        value = convert(raw, hint, target)
        if isinstance(value, unreal.Object) and value.get_name().startswith("Dim_"):
            # Added on an earlier run: replace it instead of adding a duplicate.
            items = [i for i in items if not (isinstance(i, unreal.Object) and i.get_name() == value.get_name())]
        elif value in items:
            continue
        items.append(value)
    target.set_editor_property(python_name, items)
    if package not in ctx.created:
        ctx.modified[package] = asset


def table_rows_json(table):
    """The rows of an existing data table as a list of dicts (with Name), via whichever exporter this build has."""
    lib = unreal.DataTableFunctionLibrary
    for exporter in ("export_data_table_to_json_string", "get_data_table_as_json_string"):
        fn = getattr(lib, exporter, None)
        if fn is not None:
            exported = fn(table)
            data = json.loads(exported) if exported else []
            return data if isinstance(data, list) else [dict(v, Name=k) for k, v in data.items()]
    raise PlanError("this editor build can't export data tables to JSON")


def add_table_rows(ctx, op):
    """Adds rows to (or replaces rows of) an existing data table, keeping the rest."""
    package = op["asset"]
    table = ctx.created.get(package) or assets.load_asset(package)
    if not isinstance(table, unreal.DataTable):
        raise PlanError("data table not found: " + package)
    rows = {str(r.get("Name")): r for r in table_rows_json(table)}
    for name, row in op["rows"].items():
        base = rows.get(op.get("baseRows", {}).get(name), {}) if name not in rows else rows[name]
        rows[name] = dict(base, **row, Name=name)
    if not unreal.DataTableFunctionLibrary.fill_data_table_from_json_string(table, json.dumps(list(rows.values()))):
        raise PlanError("{}: the new rows don't match the table's row struct".format(package))
    if package not in ctx.created:
        ctx.modified[package] = table


def create_chunk_label(ctx, op):
    folder, name = split_package(op["asset"])
    prepare_slot(ctx, op["asset"])
    factory = unreal.DataAssetFactory()
    factory.set_editor_property("data_asset_class", unreal.PrimaryAssetLabel)
    label = tools.create_asset(name, folder, unreal.PrimaryAssetLabel, factory)
    rules = label.get_editor_property("rules")
    rules.set_editor_property("chunk_id", int(op["chunkId"]))
    rules.set_editor_property("priority", 100)
    rules.set_editor_property("apply_recursively", False)
    rules.set_editor_property("cook_rule", unreal.PrimaryAssetCookRule.ALWAYS_COOK)
    label.set_editor_property("rules", rules)
    label.set_editor_property("label_assets_in_my_directory", True)
    try:
        label.set_editor_property("explicit_assets", [load_asset(a) for a in op.get("explicitAssets", [])])
    except Exception as e:  # noqa: BLE001
        ctx.warning("chunk label: couldn't list the overridden assets ({}); they're packed by path anyway".format(e))
    ctx.created[op["asset"]] = label


# ---------------------------------------------------------------- driver

CREATE = {
    "importTexture": import_texture,
    "importMesh": import_mesh,
    "createMaterialInstance": create_material_instance,
    "createDataTable": create_data_table,
    "importAnimation": import_animation,
    "createBlueprint": create_blueprint,
    "createObject": create_object,
    "copyAsset": copy_asset,
}


def check_required(ctx):
    missing = [a for a in ctx.plan.get("requiredAssets", []) if not assets.does_asset_exist(a)]
    for a in missing:
        ctx.error(
            "missing from the project: {} (the repo's Rem2Proj/ has some game assets; export the rest from the game with FModel and "
            "import them with JsonAsAsset at the same path)".format(a)
        )
    return not missing


def run(plan_path, result_path):
    global CULTURE
    with open(plan_path, encoding="utf-8") as f:
        plan = json.load(f)
    if plan.get("format") != 1:
        raise PlanError("plan format {} isn't supported by this script".format(plan.get("format")))
    ctx = Context(plan)
    CULTURE = ctx.culture
    log("Applying {} {} ({} operations)".format(plan["dimension"]["name"], plan["dimension"]["version"], len(plan["operations"])))

    if check_required(ctx):
        assets.make_directory(ctx.root)
        operations = plan["operations"]
        for op in operations:  # 1-3: imports, materials, tables, empty blueprints and objects
            if op["op"] in CREATE:
                try:
                    CREATE[op["op"]](ctx, op)
                except Exception as e:  # noqa: BLE001
                    ctx.error("{} {}: {}".format(op["op"], op.get("asset"), e))
        for op in operations:  # 4: properties now that everything exists
            if op["op"] in ("createBlueprint", "createObject", "copyAsset") and op["asset"] in ctx.created:
                apply_properties(ctx, op)
        for obj, name, value in list(DEFERRED):  # values that point at a node made later (dialogue jumps)
            try:
                current, python_name = get_prop(obj, name)
                obj.set_editor_property(python_name, convert(value, current, obj))
            except Deferred as missing:
                ctx.error("{}: nothing has the node key '{}'".format(obj.get_path_name(), missing))
        for op in operations:
            try:
                if op["op"] == "appendToList":
                    append_to_list(ctx, op)
                elif op["op"] == "addTableRows":
                    add_table_rows(ctx, op)
                elif op["op"] == "createChunkLabel":
                    create_chunk_label(ctx, op)
            except Exception as e:  # noqa: BLE001
                ctx.error("{} {}: {}".format(op["op"], op.get("asset"), e))

        for package, asset in list(ctx.created.items()) + list(ctx.modified.items()):
            if isinstance(asset, unreal.Blueprint):
                unreal.BlueprintEditorLibrary.compile_blueprint(asset)
            if not assets.save_loaded_asset(asset, only_if_is_dirty=False):
                ctx.error("couldn't save " + package)

    result = {
        "ok": not ctx.errors,
        "created": sorted(ctx.created),
        "modified": sorted(ctx.modified),
        "errors": ctx.errors,
        "warnings": ctx.warnings,
    }
    with open(result_path, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2)
    log("Done: {} created, {} modified, {} errors, {} warnings".format(
        len(ctx.created), len(ctx.modified), len(ctx.errors), len(ctx.warnings)))
    return result["ok"]


def main():
    plan_path = os.environ.get("DIMENSIONS_PLAN") or (sys.argv[1] if len(sys.argv) > 1 else None)
    if not plan_path:
        unreal.log_error(TAG + "set DIMENSIONS_PLAN to the plan.json path")
        return
    result_path = os.environ.get("DIMENSIONS_RESULT") or os.path.join(os.path.dirname(plan_path), "apply-result.json")
    try:
        run(plan_path, result_path)
    except Exception as e:  # noqa: BLE001
        unreal.log_error(TAG + "failed: {}\n{}".format(e, traceback.format_exc()))
        with open(result_path, "w", encoding="utf-8") as f:
            json.dump({"ok": False, "errors": [str(e)], "traceback": traceback.format_exc()}, f, indent=2)


main()

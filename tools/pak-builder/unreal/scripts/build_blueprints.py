import sys

import unreal


def _pipeline_config_path():
    for argument in sys.argv:
        if argument.startswith("-pipeline-config="):
            return argument.split("=", 1)[1].strip('"')
    raise RuntimeError("Missing -pipeline-config=<path> argument.")


def _package_parts(package_path):
    normalized = package_path.replace("\\", "/").rstrip("/")
    if not normalized.startswith("/Game/"):
        raise ValueError("Blueprint package paths must start with /Game/: " + package_path)
    directory, _, asset_name = normalized.rpartition("/")
    if not directory or not asset_name:
        raise ValueError("Invalid Blueprint package path: " + package_path)
    return directory, asset_name, normalized


def _load_class(class_path):
    normalized = class_path.replace("\\", "/")
    if "." not in normalized.rsplit("/", 1)[-1]:
        asset_name = normalized.rsplit("/", 1)[-1]
        normalized += "." + asset_name + "_C"
    loaded_class = unreal.load_class(None, normalized)
    if loaded_class is None:
        raise RuntimeError("Unable to load Blueprint parent class: " + normalized)
    return loaded_class


def _create_blueprint(package_path, parent_class):
    directory, asset_name, object_path = _package_parts(package_path)
    if unreal.EditorAssetLibrary.does_asset_exist(object_path):
        if not unreal.EditorAssetLibrary.delete_asset(object_path):
            raise RuntimeError("Unable to remove existing Blueprint before rebuild: " + object_path)

    factory = unreal.BlueprintFactory()
    factory.set_editor_property("parent_class", parent_class)
    asset = unreal.AssetToolsHelpers.get_asset_tools().create_asset(
        asset_name,
        directory,
        unreal.Blueprint,
        factory,
    )
    if asset is None:
        raise RuntimeError("Unable to create Blueprint asset: " + object_path)
    return asset


def _add_components(blueprint, component_specs):
    for spec in component_specs:
        component_path = spec.get("type", "")
        component_class = unreal.load_class(None, component_path)
        if component_class is None:
            component_class = getattr(unreal, component_path, None)
        if component_class is None:
            raise RuntimeError("Unable to resolve component class: " + component_path)

        node = unreal.BlueprintEditorLibrary.add_component(
            blueprint,
            component_class,
            spec["name"],
        )
        if node is None:
            raise RuntimeError("Unable to add Blueprint component: " + spec["name"])
        parent_name = spec.get("parent")
        if parent_name:
            node.set_editor_property(
                "parent_component_or_variable_name",
                unreal.Name(parent_name),
            )


def _save_blueprint(blueprint, package_path):
    unreal.BlueprintEditorLibrary.compile_blueprint(blueprint)
    if not unreal.EditorAssetLibrary.save_loaded_asset(blueprint):
        raise RuntimeError("Unable to save Blueprint asset: " + package_path)


def build():
    config_path = _pipeline_config_path()
    with open(config_path, "r", encoding="utf-8") as config_file:
        config = __import__("json").load(config_file)
    mappings = config.get("mappings")
    if not isinstance(mappings, dict) or not mappings:
        raise RuntimeError("Pipeline config contains no mappings to build.")

    for mapping_id, mapping in mappings.items():
        parent_class = _load_class(mapping["parent_class"])
        stub = _create_blueprint(mapping["stub_blueprint"], parent_class)
        _add_components(stub, mapping.get("components", []))
        _save_blueprint(stub, mapping["stub_blueprint"])

        _, stub_name, stub_object_path = _package_parts(mapping["stub_blueprint"])
        custom_parent = _load_class(stub_object_path + "." + stub_name + "_C")
        custom = _create_blueprint(mapping["custom_blueprint"], custom_parent)
        _save_blueprint(custom, mapping["custom_blueprint"])
        unreal.log("Built stub and custom Blueprint for " + mapping_id)


build()
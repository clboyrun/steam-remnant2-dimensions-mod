"""Name helpers shared by the editor script. No `unreal` import, so they can be tested with plain Python."""

import re

_BOUNDARY_LOWER_UPPER = re.compile(r"(?<=[a-z0-9])(?=[A-Z])")
_BOUNDARY_ACRONYM = re.compile(r"(?<=[A-Z])(?=[A-Z][a-z])")


def snake_case(name: str) -> str:
    """Unreal property name to its Python name: bToggleable -> toggleable, HUDIcon -> hud_icon, NameID -> name_id."""
    if len(name) > 1 and name[0] == "b" and name[1].isupper():
        name = name[1:]
    name = _BOUNDARY_ACRONYM.sub("_", name)
    name = _BOUNDARY_LOWER_UPPER.sub("_", name)
    return name.lower()


def enum_member(name: str) -> str:
    """Enum value as written in JSON to Unreal Python's member name: IndividualChance -> INDIVIDUAL_CHANCE."""
    return snake_case(name).upper()


def split_package(package: str):
    """'/Game/A/B/Name' -> ('/Game/A/B', 'Name'). Object paths ('/Game/A/B.B') are cut at the dot first."""
    package = package.split(".", 1)[0]
    folder, _, name = package.rpartition("/")
    return folder, name


def package_of(path: str) -> str:
    return path if path.startswith("/Script/") else path.split(".", 1)[0]

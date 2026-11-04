"""Tests for the parts of the editor script that don't need Unreal: python -m unittest discover unreal/Python"""

import unittest

from dimensions_names import enum_member, package_of, snake_case, split_package


class NameTests(unittest.TestCase):
    def test_snake_case_matches_unreal_python_names(self):
        cases = {
            "Label": "label",
            "bToggleable": "toggleable",
            "HUDIcon": "hud_icon",
            "NameID": "name_id",
            "ScopeFOV": "scope_fov",
            "UITags": "ui_tags",
            "Ingredient1Quantity": "ingredient1_quantity",
            "TableGroupRollMin": "table_group_roll_min",
            "b": "b",
        }
        for name, expected in cases.items():
            self.assertEqual(snake_case(name), expected, name)

    def test_enum_member(self):
        self.assertEqual(enum_member("IndividualChance"), "INDIVIDUAL_CHANCE")
        self.assertEqual(enum_member("Adventure"), "ADVENTURE")

    def test_paths(self):
        self.assertEqual(split_package("/Game/A/B/Name"), ("/Game/A/B", "Name"))
        self.assertEqual(split_package("/Game/A/B.B_C"), ("/Game/A", "B"))
        self.assertEqual(package_of("/Game/A/B.B_C"), "/Game/A/B")
        self.assertEqual(package_of("/Script/Remnant.ZoneDef"), "/Script/Remnant.ZoneDef")


if __name__ == "__main__":
    unittest.main()

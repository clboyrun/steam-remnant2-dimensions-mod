# Ashfall (example)

Shows every content kind: a long gun with a fixed mod (`content/weapons`), a mutator, ring and amulet
(`content/gear`), a trait and an archetype with its engram (`content/character`), and a world with two zones, a loot
table and an Adventure option (`content/world`).

The art in `art/` is flat-colour placeholders and `models/emberline.gltf` is a single triangle, so validation and
planning work out of the box. Before building in Unreal:

- Replace the model with a real FBX weapon mesh and set `"type": "skeletal"` (game weapons use skeletal meshes),
  and use a weapon master material as `parent` with its real texture parameter names.
- Build the maps the world and adventure point at under `/Game/Dimensions/Ashfall/Maps/` in the editor.

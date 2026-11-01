using System.Text.Json.Nodes;
using Dimensions.Definitions;
using K = Dimensions.Definitions.DefinitionKinds;

namespace Dimensions.Pipeline;

/// <summary>
/// Turns a loaded dimension into a <see cref="BuildPlan"/>: which assets to import and create, which properties to
/// set, and which game lists to add to. Pure: it reads definitions and the catalog, and touches no files.
/// </summary>
public sealed class Planner(Catalog catalog)
{
    public static BuildPlan Plan(DimensionProject project, Catalog? catalog = null) =>
        new Planner(catalog ?? Catalog.Load()).Build(project);

    public BuildPlan Build(DimensionProject project)
    {
        var context = new Context(project, catalog);
        return context.Run();
    }

    /// <summary>Where a definition ends up in the project.</summary>
    public sealed record Target(string Package, bool IsBlueprint)
    {
        public string ObjectPath => GamePath.ObjectPath(Package);
        public string ClassPath => GamePath.ClassPath(Package);
        /// <summary>What a reference to this definition points at: the class for blueprints, else the object.</summary>
        public string ReferencePath => IsBlueprint ? ClassPath : ObjectPath;
    }

    private sealed class Context(DimensionProject project, Catalog catalog)
    {
        private readonly BuildPlan _plan = new()
        {
            Manifest = project.Manifest,
            ContentRoot = $"{catalog.ContentRoot}/{project.Manifest.Id}",
        };
        private readonly Dictionary<string, Target> _targets = [];
        private readonly Dictionary<string, JsonObject> _creates = [];
        private readonly Dictionary<(string Asset, string Property), JsonObject> _appends = [];
        private readonly Dictionary<string, string> _importedFiles = []; // source file + usage -> package
        private string Culture => project.Manifest.DefaultCulture;
        private string Root => _plan.ContentRoot;

        public BuildPlan Run()
        {
            foreach (var d in project.Concrete) AssignTarget(d);
            // Loot tables first, so items that drop from them add their entries straight into the table.
            foreach (var d in project.Concrete.OrderBy(d => d.Kind is K.LootTable or K.ItemCategory ? 0 : d.Kind is K.Archetype or K.Adventure ? 2 : 1))
            {
                switch (d.Kind)
                {
                    case K.Weapon: PlanWeapon(d); break;
                    case K.WeaponMod: PlanWeaponMod(d); break;
                    case K.Mutator: PlanMutator(d); break;
                    case K.Ring or K.Amulet: PlanTrinket(d); break;
                    case K.Armor: PlanArmor(d); break;
                    case K.Trait: PlanTrait(d); break;
                    case K.Archetype: PlanArchetype(d); break;
                    case K.LootTable: PlanLootTable(d); break;
                    case K.World: PlanWorld(d); CampaignAlternative(d); break;
                    case K.Adventure: PlanAdventure(d); break;
                    case K.Blueprint: PlanGenericBlueprint(d); break;
                    case K.QuestItem: PlanTrinket(d); break;
                    case K.ItemCategory: PlanItemCategory(d); break;
                    case K.Difficulty: PlanDifficulty(d); break;
                    case K.AnimationSet: PlanAnimationSet(d); break;
                    case K.Dialogue: PlanDialogue(d); break;
                    case K.Npc or K.Enemy: PlanCharacter(d); break;
                    case K.Quest: PlanQuest(d); break;
                }
                Acquisition(d, d.Json["acquisition"] as JsonArray);
                Registration(d);
            }
            foreach (var append in _appends.Values) _plan.Operations.Add(append);

            _plan.Operations.Add(new JsonObject
            {
                ["op"] = "createChunkLabel",
                ["asset"] = $"{Root}/DimensionLabel_{project.Manifest.Id}",
                ["chunkId"] = project.Manifest.ChunkId,
                ["explicitAssets"] = new JsonArray([.. _plan.Overrides.Select(o => (JsonNode)GamePath.ObjectPath(o))]),
            });
            return _plan;
        }

        // ---------- naming ----------

        private string AssetName(Definition d)
        {
            if (d.GetString("assetName") is { } explicitName) return explicitName;
            var prefix = catalog.Prefix(d.Kind);
            if (d.Kind == K.Armor)
            {
                var slot = catalog.Map(K.Armor, "slotNames").GetValueOrDefault(d.GetString("slot") ?? "", "");
                return $"{prefix}{(slot.Length > 0 ? slot + "_" : "")}{d.Id}";
            }
            return prefix + d.Id;
        }

        private string Folder(Definition d) => d.Kind == K.Blueprint && d.GetString("folder") is { } f
            ? $"{Root}/{f.Trim('/')}"
            : $"{Root}/{catalog.Folder(d.Kind)}/{d.Id}";

        private void AssignTarget(Definition d)
        {
            if (d.Kind == K.World) return; // a world is its zones; it has no asset of its own
            var replaces = d.Json["registration"]?["mode"]?.GetValue<string>() == "replace"
                ? d.Json["registration"]?["replaces"]?.GetValue<string>()
                : null;
            var package = replaces is not null ? GamePath.PackageOf(replaces) : $"{Folder(d)}/{AssetName(d)}";
            var isBlueprint = d.Kind switch
            {
                K.LootTable or K.Dialogue or K.AnimationSet => false,
                K.Blueprint => (d.GetString("asset") ?? "blueprint") == "blueprint",
                _ => true,
            };
            _targets[d.Id] = new Target(package, isBlueprint);
            if (replaces is not null) _plan.Overrides.Add(package);
        }

        private string Dir(Definition d) => GamePath.PackageOf(_targets[d.Id].Package)[.._targets[d.Id].Package.LastIndexOf('/')];
        private string Name(Definition d) => _targets[d.Id].Package[(_targets[d.Id].Package.LastIndexOf('/') + 1)..];

        /// <summary>An id or game path to the class/object path a property should hold.</summary>
        private string? Resolve(string? reference)
        {
            if (reference is null) return null;
            if (GamePath.IsGamePath(reference)) return reference;
            return _targets.TryGetValue(reference, out var t) ? t.ReferencePath : null;
        }

        private static bool IsClassPath(string path) => path.EndsWith("_C", StringComparison.Ordinal) || GamePath.IsNative(path);

        private JsonObject ClassValue(string reference) => Values.Class(Resolve(reference) ?? reference);
        private JsonObject SoftClassValue(string reference) => Values.SoftClass(Resolve(reference) ?? reference);

        // ---------- operations ----------

        private JsonObject CreateBlueprint(Definition d, string parent, JsonObject properties, JsonObject? components = null)
        {
            Require(parent);
            var op = new JsonObject
            {
                ["op"] = "createBlueprint",
                ["id"] = d.Id,
                ["asset"] = _targets[d.Id].Package,
                ["parent"] = parent,
                ["properties"] = properties,
            };
            if (components is { Count: > 0 }) op["components"] = components;
            _plan.Operations.Add(op);
            _creates[d.Id] = op;
            return op;
        }

        private JsonObject CreateSubBlueprint(string package, string parent, JsonObject properties)
        {
            Require(parent);
            var op = new JsonObject { ["op"] = "createBlueprint", ["asset"] = package, ["parent"] = parent, ["properties"] = properties };
            _plan.Operations.Add(op);
            return op;
        }

        private void Require(string path)
        {
            if (!GamePath.IsNative(path) && !path.StartsWith(Root + "/", StringComparison.Ordinal))
                _plan.RequiredAssets.Add(GamePath.PackageOf(path));
        }

        private string Texture(string source, string package, string usage = "ui")
        {
            if (GamePath.IsGamePath(source)) { Require(source); return GamePath.ObjectPath(source); }
            var file = project.ResolveFile(source);
            if (_importedFiles.TryGetValue(file + "|" + usage, out var existing)) return GamePath.ObjectPath(existing);
            _importedFiles[file + "|" + usage] = package;
            _plan.Operations.Add(new JsonObject { ["op"] = "importTexture", ["asset"] = package, ["source"] = file, ["usage"] = usage });
            return GamePath.ObjectPath(package);
        }

        private JsonObject DataTable(string package, string rowStruct, JsonObject rows)
        {
            Require(rowStruct);
            var op = new JsonObject { ["op"] = "createDataTable", ["asset"] = package, ["rowStruct"] = rowStruct, ["rows"] = rows };
            _plan.Operations.Add(op);
            return op;
        }

        private void Append(string target, string property, JsonNode item)
        {
            var package = GamePath.PackageOf(target);
            var key = (package, property);
            if (!_appends.TryGetValue(key, out var op))
            {
                op = new JsonObject { ["op"] = "appendToList", ["asset"] = package, ["property"] = property, ["items"] = new JsonArray() };
                _appends[key] = op;
                if (!package.StartsWith(Root + "/", StringComparison.Ordinal))
                {
                    _plan.Overrides.Add(package);
                    _plan.RequiredAssets.Add(package);
                }
            }
            ((JsonArray)op["items"]!).Add(item);
        }

        // ---------- shared item pieces ----------

        private JsonObject ItemProperties(Definition d)
        {
            var p = new JsonObject();
            Text(p, "Label", d.Json["name"]);
            Text(p, "Description", d.Json["description"]);
            Text(p, "FlavorText", d.Json["flavorText"]);
            Icons(d, p, d.Json["icon"] as JsonObject);
            if (d.GetString("category") is { } category)
            {
                if (GamePath.IsGamePath(category)) Require(category);
                p["Category"] = ClassValue(category);
            }
            return p;
        }

        private void PlanDifficulty(Definition d)
        {
            var row = new JsonObject();
            foreach (var (field, unrealName) in catalog.Map(K.Difficulty, "statFields"))
                if (d.Json[field] is { } v) row[unrealName] = v.DeepClone();
            foreach (var (k, v) in d.Json["row"] as JsonObject ?? []) row[k] = v?.DeepClone();
            var bases = (catalog.Kind(K.Difficulty)["baseDifficulties"] as JsonArray ?? []).Select(n => n!.GetValue<string>()).ToList();
            var rank = d.Json["rank"]?.GetValue<int>() ?? bases.Count;
            var rowName = d.GetString("rowName") ?? (rank + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var basedOn = d.GetString("basedOn") ?? bases.LastOrDefault();
            var name = d.Json["name"] is JsonValue nv ? nv.GetValue<string>() : d.Json["name"]?[Culture]?.GetValue<string>() ?? d.Id;
            if (catalog.Get(K.Difficulty, "scalingTable") is { } table)
            {
                var package = GamePath.PackageOf(table);
                var op = new JsonObject { ["op"] = "addTableRows", ["asset"] = package, ["rows"] = new JsonObject { [rowName] = row } };
                if (basedOn is not null && bases.IndexOf(basedOn) is >= 0 and var i)
                    op["baseRows"] = new JsonObject { [rowName] = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) };
                _plan.Operations.Add(op);
                if (!_plan.Overrides.Contains(package)) _plan.Overrides.Add(package);
                Require(table);
            }
            else
                _plan.Notes.Add($"difficulty {d.Id}: the difficulty scaling table isn't in the catalog (difficulty.scalingTable). Row {rowName}: {row.ToJsonString()}.");
            _plan.Notes.Add($"difficulty {d.Id}: '{name}' is difficulty {rank} (row {rowName}). The difficulty menu and its names live in the game's UI (catalog difficulty.menuList); until that's mapped, the new option won't be listed. Also check how loot and enemy level follow the difficulty.");
            Unverified(d, "the difficulty row names and menu");
        }

        private void PlanItemCategory(Definition d)
        {
            var p = new JsonObject();
            Text(p, "Label", d.Json["name"]);
            if (d.Json["icon"]?["small"]?.GetValue<string>() is { } icon) p["Icon"] = Values.Asset(Texture(icon, $"{Dir(d)}/T_{Name(d)}_Icon"));
            var group = d.GetString("group") ?? "playerQuest";
            var parent = d.Template ?? catalog.Get(K.ItemCategory, $"templates/{group}")!;
            CreateBlueprint(d, parent, WithRaw(p, d.Json["properties"]), Components(d));
            _plan.Notes.Add($"itemCategory {d.Id}: check in game that the inventory lists it as its own section. If the inventory's tabs are fixed in its widget instead, that widget has to be overridden (catalog ui.inventoryWidget, export it from FModel).");
        }

        private void Text(JsonObject properties, string name, JsonNode? text)
        {
            if (LocalizedText.From(text, Culture) is { } t) properties[name] = Values.Text(t);
        }

        private void Icons(Definition d, JsonObject p, JsonObject? icon, string? name = null)
        {
            if (icon is null) return;
            name ??= Name(d);
            var dir = Dir(d);
            if (icon["small"]?.GetValue<string>() is { } small)
            {
                var path = Texture(small, $"{dir}/T_{name}_Icon");
                p["Icon"] = Values.Asset(path);
                if (icon["hud"] is null) p["HUDIcon"] = Values.Asset(path);
            }
            if (icon["large"]?.GetValue<string>() is { } large) p["LargeIcon"] = Values.Soft(Texture(large, $"{dir}/T_{name}_IconLarge"));
            if (icon["hud"]?.GetValue<string>() is { } hud) p["HUDIcon"] = Values.Asset(Texture(hud, $"{dir}/T_{name}_IconHUD"));
        }

        /// <summary>Copies the author's raw 'properties' over the generated ones, resolving {"$ref": id}.</summary>
        private JsonObject WithRaw(JsonObject generated, JsonNode? raw)
        {
            if (raw is JsonObject r)
                foreach (var (k, v) in r) generated[k] = ResolveRefs(v?.DeepClone());
            return generated;
        }

        private JsonNode? ResolveRefs(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject o when o.Count == 1 && o["$ref"] is JsonValue v && v.TryGetValue<string>(out var id):
                    var path = Resolve(id) ?? id;
                    return IsClassPath(path) ? Values.Class(path) : Values.Asset(path);
                case JsonObject o:
                    foreach (var key in o.Select(p => p.Key).ToList()) o[key] = ResolveRefs(o[key]?.DeepClone());
                    return o;
                case JsonArray a:
                    return new JsonArray([.. a.Select(x => ResolveRefs(x?.DeepClone()))]);
                default:
                    return node;
            }
        }

        private JsonObject Components(Definition d, JsonObject? fromModel = null)
        {
            var components = fromModel ?? [];
            if (d.Json["components"] is JsonObject raw)
            {
                foreach (var (name, props) in raw)
                {
                    var existing = components[name] as JsonObject ?? [];
                    if (props is JsonObject po) foreach (var (k, v) in po) existing[k] = ResolveRefs(v?.DeepClone());
                    components[name] = existing;
                }
            }
            return components;
        }

        /// <summary>Imports the model and its materials, and returns the component overrides that show it.</summary>
        private JsonObject Model(Definition d, JsonObject? model)
        {
            var components = new JsonObject();
            if (model is null) return components;
            var name = Name(d);
            var dir = Dir(d);
            var type = model["type"]?.GetValue<string>() ?? "skeletal";
            var meshPackage = $"{dir}/{(type == "static" ? "SM_" : "SK_")}{name}";
            var import = new JsonObject
            {
                ["op"] = "importMesh",
                ["asset"] = meshPackage,
                ["source"] = project.ResolveFile(model["mesh"]!.GetValue<string>()),
                ["meshType"] = type,
                ["scale"] = model["scale"]?.GetValue<double>() ?? 1.0,
            };
            if (model["skeleton"]?.GetValue<string>() is { } skeleton) { import["skeleton"] = skeleton; Require(skeleton); }
            if (model["importOptions"] is JsonObject options) import["options"] = options.DeepClone();
            _plan.Operations.Add(import);

            var materials = new JsonArray();
            var slots = new JsonArray();
            var i = 0;
            foreach (var m in (model["materials"] as JsonArray ?? []).OfType<JsonObject>())
            {
                var parent = m["parent"]!.GetValue<string>();
                Require(parent);
                var textures = new JsonObject();
                foreach (var (param, src) in m["textures"] as JsonObject ?? [])
                    textures[param] = Texture(src!.GetValue<string>(), $"{dir}/T_{name}_{param}", "world");
                var mi = $"{dir}/MI_{name}_{i}";
                _plan.Operations.Add(new JsonObject
                {
                    ["op"] = "createMaterialInstance",
                    ["asset"] = mi,
                    ["parent"] = parent,
                    ["textures"] = textures,
                    ["scalars"] = m["scalars"]?.DeepClone() ?? new JsonObject(),
                    ["vectors"] = m["vectors"]?.DeepClone() ?? new JsonObject(),
                });
                materials.Add(Values.Asset(mi));
                slots.Add(m["slot"]?.GetValue<string>());
                i++;
            }
            var component = new JsonObject { ["$mesh"] = Values.Asset(meshPackage) };
            if (materials.Count > 0) { component["$materials"] = materials; component["$materialSlots"] = slots; }
            components[model["component"]?.GetValue<string>() ?? "Mesh"] = component;
            return components;
        }

        private static JsonObject StatsRow(JsonObject? stats, IReadOnlyDictionary<string, string> fields)
        {
            var row = new JsonObject();
            if (stats is null) return row;
            foreach (var (key, value) in stats) row[fields.GetValueOrDefault(key, key)] = value?.DeepClone();
            return row;
        }

        private void Unverified(Definition d, string what)
        {
            if (!catalog.Verified(d.Kind))
                _plan.Notes.Add($"{d.Kind} {d.Id}: the catalog entries for {what} aren't confirmed against the game yet (data/catalog.json, verified=false).");
        }

        // ---------- kinds ----------

        private void PlanWeapon(Definition d)
        {
            var type = d.GetString("weaponType") ?? "longGun";
            var parent = d.Template ?? catalog.Get(K.Weapon, $"templates/{type}")!;
            var p = ItemProperties(d);
            var name = Name(d);

            if (d.Json["statsRow"] is JsonObject statsRow)
            {
                p["Stats"] = Values.RowHandle(statsRow["table"]!.GetValue<string>(), statsRow["row"]!.GetValue<string>());
            }
            else if (d.Json["stats"] is JsonObject stats)
            {
                var structPath = catalog.Get(K.Weapon, $"statsStruct/{(type == "melee" ? "melee" : "ranged")}")!;
                var table = $"{Dir(d)}/DT_{name}_Stats";
                DataTable(table, structPath, new JsonObject { [name] = StatsRow(stats, catalog.Map(K.Weapon, "statFields")) });
                p["Stats"] = Values.RowHandle(table, name);
            }

            var modRef = d.GetString("mod");
            var mutatorSlot = d.Json["mutatorSlot"]?.GetValue<bool>() ?? true;
            if (modRef is not null || !mutatorSlot)
            {
                // ModSlots replaces the template's slots, so it's written out in full.
                var slots = new JsonArray();
                slots.Add(Values.Struct("EquipmentModSlot", new JsonObject
                {
                    ["NameID"] = Values.Name("Mod"),
                    ["DefaultMod"] = modRef is null ? null : ClassValue(modRef),
                }));
                if (mutatorSlot) slots.Add(Values.Struct("EquipmentModSlot", new JsonObject { ["NameID"] = Values.Name("Gem") }));
                p["ModSlots"] = slots;
                _plan.Notes.Add($"weapon {d.Id}: mod slots are written as 'Mod' and 'Gem'. Check the slot NameIDs and item Types against a game weapon in FModel.");
            }
            CreateBlueprint(d, parent, WithRaw(p, d.Json["properties"]), Components(d, Model(d, d.Json["model"] as JsonObject)));
        }

        private void PlanWeaponMod(Definition d)
        {
            var p = ItemProperties(d);
            if (d.Json["powerRequired"] is { } power) p["PowerBasis"] = power.DeepClone();
            if (d.Json["charges"] is { } charges) p["MaxCharges"] = charges.DeepClone();
            if (d.Json["toggleable"] is { } toggle) p["bToggleable"] = toggle.DeepClone();
            ItemStats(d, p, d.Json["stats"] as JsonObject, K.Ring);
            CreateBlueprint(d, d.Template!, WithRaw(p, d.Json["properties"]), Components(d));
        }

        private void PlanMutator(Definition d)
        {
            var p = ItemProperties(d);
            if (d.Json["levels"] is JsonArray levels && levels.Count > 0)
            {
                var max = d.Json["maxLevel"]?.GetValue<int>() ?? 10;
                var rows = new JsonObject();
                for (var level = 1; level <= max; level++)
                    rows[$"{Name(d)}_{level}"] = (levels[Math.Min(level, levels.Count) - 1] as JsonObject)?.DeepClone() ?? new JsonObject();
                var table = $"{Dir(d)}/DT_{Name(d)}_Levels";
                DataTable(table, catalog.Get(K.Mutator, "levelStruct")!, rows);
                p["Stats"] = Values.RowHandle(table, $"{Name(d)}_1");
                Unverified(d, "the per-level stats table");
            }
            CreateBlueprint(d, d.Template!, WithRaw(p, d.Json["properties"]), Components(d));
        }

        private void ItemStats(Definition d, JsonObject p, JsonObject? stats, string catalogKind)
        {
            if (stats is null || stats.Count == 0) return;
            var table = $"{Dir(d)}/DT_{Name(d)}_Stats";
            DataTable(table, catalog.Get(catalogKind, "statsStruct")!, new JsonObject { [Name(d)] = stats.DeepClone() });
            p["Stats"] = Values.RowHandle(table, Name(d));
            Unverified(d, "the stats table");
        }

        private void PlanTrinket(Definition d)
        {
            var p = ItemProperties(d);
            ItemStats(d, p, d.Json["stats"] as JsonObject, d.Kind);
            CreateBlueprint(d, d.Template!, WithRaw(p, d.Json["properties"]), Components(d));
        }

        private void PlanArmor(Definition d)
        {
            var p = ItemProperties(d);
            var stats = new JsonObject();
            if (d.Json["armor"] is { } armor) stats["armor"] = armor.DeepClone();
            if (d.Json["weight"] is { } weight) stats["weight"] = weight.DeepClone();
            foreach (var (k, v) in d.Json["resistances"] as JsonObject ?? []) stats[k] = v?.DeepClone();
            foreach (var (k, v) in d.Json["stats"] as JsonObject ?? []) stats[k] = v?.DeepClone();
            if (stats.Count > 0)
            {
                var table = $"{Dir(d)}/DT_{Name(d)}_Stats";
                DataTable(table, catalog.Get(K.Armor, "statsStruct")!,
                    new JsonObject { [Name(d)] = StatsRow(stats, catalog.Map(K.Armor, "statFields")) });
                p["Stats"] = Values.RowHandle(table, Name(d));
                Unverified(d, "the armor stats row");
            }
            CreateBlueprint(d, d.Template!, WithRaw(p, d.Json["properties"]), Components(d, Model(d, d.Json["model"] as JsonObject)));
        }

        private JsonObject TraitProperties(Definition d, string name, JsonObject json, string kindForCatalog)
        {
            var p = new JsonObject { ["NameID"] = Values.Name(name) };
            Text(p, "Label", json["name"]);
            Text(p, "Description", json["description"]);
            Text(p, "FlavorText", json["flavorText"]);
            var max = json["maxLevel"]?.GetValue<int>();
            if (max is not null) p["MaxLevel"] = max;
            if (json["cost"] is { } cost) p["Cost"] = cost.DeepClone();
            if (json["levels"] is JsonObject levels && levels.Count > 0)
            {
                var count = max ?? levels.Max(l => (l.Value as JsonArray)?.Count ?? 0);
                var rows = new JsonObject();
                for (var level = 1; level <= count; level++)
                {
                    var row = new JsonObject();
                    foreach (var (stat, values) in levels)
                        if (values is JsonArray a && a.Count > 0) row[stat] = a[Math.Min(level, a.Count) - 1]?.DeepClone();
                    rows[level.ToString()] = row;
                }
                var table = $"{Dir(d)}/DT_{name}_Levels";
                DataTable(table, catalog.Get(kindForCatalog, "levelStruct") ?? catalog.Get(K.Trait, "levelStruct")!, rows);
                p["StatTable"] = Values.Asset(table);
                Unverified(d, "the trait level table");
            }
            return p;
        }

        private void PlanTrait(Definition d)
        {
            var p = TraitProperties(d, Name(d), d.Json, K.Trait);
            CreateBlueprint(d, d.Template ?? catalog.Get(K.Trait, "template")!, WithRaw(p, d.Json["properties"]));
        }

        private void PlanArchetype(Definition d)
        {
            var dir = Dir(d);
            var name = Name(d);
            var p = new JsonObject { ["NameID"] = Values.Name(d.Id) };
            Text(p, "Label", d.Json["name"]);
            Text(p, "Description", d.Json["description"]);
            Text(p, "BriefDescription", d.Json["briefDescription"]);
            if (d.Json["flavorTags"] is JsonArray tags)
                p["FlavorTags"] = Values.Array(tags.Select(t => (JsonNode?)Values.Text(LocalizedText.From(t, Culture)!)));
            if (d.Json["difficulty"] is { } difficulty) p["Difficulty"] = difficulty.DeepClone();
            if (d.GetString("image") is { } image) p["Image"] = Values.Asset(Texture(image, $"{dir}/T_{name}_Image"));
            if (d.Json["uiTags"] is JsonArray uiTags) p["UITags"] = Values.Array(uiTags.Select(t => (JsonNode?)Values.Name(t!.GetValue<string>())));

            // The archetype's own trait (UArchetype): levels, perks.
            var traitJson = d.Json["archetypeTrait"] as JsonObject ?? [];
            var traitName = $"{catalog.Prefix(K.Archetype)}Trait_{d.Id}";
            var traitPackage = $"{dir}/{traitName}";
            var traitProps = TraitProperties(d, traitName, new JsonObject
            {
                ["name"] = d.Json["name"]?.DeepClone(),
                ["description"] = d.Json["description"]?.DeepClone(),
                ["levels"] = traitJson["levels"]?.DeepClone(),
            }, K.Trait);
            CreateSubBlueprint(traitPackage, traitJson["template"]?.GetValue<string>() ?? catalog.Get(K.Archetype, "traitTemplate")!,
                WithRaw(traitProps, traitJson["properties"]));
            p["Archetype"] = Values.SoftClass(GamePath.ClassPath(traitPackage));

            // The engram item that unlocks it.
            var engram = d.Json["engram"] as JsonObject ?? [];
            var engramName = $"Engram_{d.Id}";
            var engramPackage = $"{dir}/{engramName}";
            var engramProps = new JsonObject();
            Text(engramProps, "Label", engram["name"] ?? d.Json["name"]);
            Text(engramProps, "Description", engram["description"] ?? d.Json["description"]);
            Icons(d, engramProps, engram["icon"] as JsonObject, engramName);
            CreateSubBlueprint(engramPackage, engram["template"]?.GetValue<string>() ?? catalog.Get(K.Archetype, "engramTemplate")!,
                WithRaw(engramProps, engram["properties"]));
            p["Engram"] = Values.SoftClass(GamePath.ClassPath(engramPackage));
            AcquireItem(d, GamePath.ClassPath(engramPackage), engram["acquisition"] as JsonArray);

            if (d.Json["startingGear"] is JsonArray gear)
                p["StartingGear"] = Values.Array(gear.Select(g => (JsonNode?)ClassValue(g!.GetValue<string>())));
            if (d.Json["startingTraits"] is JsonArray traits)
                p["StartingTraits"] = Values.Array(traits.OfType<JsonObject>().Select(t => (JsonNode?)Values.Struct("ArchetypeStartingTrait", new JsonObject
                {
                    ["Trait"] = SoftClassValue(t["trait"]!.GetValue<string>()),
                    ["StartingLevel"] = t["level"]?.GetValue<int>() ?? 1,
                })));

            CreateBlueprint(d, d.Template ?? catalog.Get(K.Archetype, "template")!, WithRaw(p, d.Json["properties"]));
            Unverified(d, "the archetype, its trait and engram templates");
        }

        private JsonObject LootEntry(JsonObject entry)
        {
            var p = new JsonObject();
            string cls;
            if (entry["character"]?.GetValue<string>() is { } character)
            {
                cls = catalog.Get(K.LootTable, "characterEntryClass")!;
                p["CharacterBP"] = SoftClassValue(character);
                p["Chance"] = entry["chance"]?.GetValue<int>() ?? 100;
                if (entry["quantity"] is { } count) { p["QuantityMin"] = count.DeepClone(); p["QuantityMax"] = count.DeepClone(); }
                if (entry["levelMin"] is { } cmin) p["LevelMin"] = cmin.DeepClone();
                if (entry["levelMax"] is { } cmax) p["LevelMax"] = cmax.DeepClone();
                if (entry["tags"] is JsonArray ctags) p["Tags"] = Values.Array(ctags.Select(t => (JsonNode?)Values.Name(t!.GetValue<string>())));
                return Values.New(cls, p);
            }
            if (entry["table"]?.GetValue<string>() is { } table)
            {
                cls = catalog.Get(K.LootTable, "linkEntryClass")!;
                var path = Resolve(table) ?? table;
                p["Table"] = Values.Asset(path);
                if (GamePath.IsGamePath(table)) Require(table);
            }
            else
            {
                cls = catalog.Get(K.LootTable, "itemEntryClass")!;
                p["ItemBP"] = SoftClassValue(entry["item"]!.GetValue<string>());
            }
            p["Chance"] = entry["chance"]?.GetValue<int>() ?? 100;
            var quantity = entry["quantity"]?.GetValue<int>() ?? 1;
            p["QuantityMin"] = quantity;
            p["QuantityMax"] = quantity;
            if (entry["levelMin"] is { } min) p["LevelMin"] = min.DeepClone();
            if (entry["levelMax"] is { } max) p["LevelMax"] = max.DeepClone();
            if (entry["tags"] is JsonArray tags) p["Tags"] = Values.Array(tags.Select(t => (JsonNode?)Values.Name(t!.GetValue<string>())));
            return Values.New(cls, p);
        }

        private void PlanLootTable(Definition d)
        {
            var p = new JsonObject();
            if (d.GetString("tableType") is { } type) p["TableType"] = Values.Enum(type);
            if (d.Json["rolls"] is JsonObject rolls)
            {
                if (rolls["min"] is { } min) p["TableGroupRollMin"] = min.DeepClone();
                if (rolls["max"] is { } max) p["TableGroupRollMax"] = max.DeepClone();
            }
            p["Entries"] = Values.Array((d.Json["entries"] as JsonArray ?? []).OfType<JsonObject>().Select(e => (JsonNode?)LootEntry(e)));
            var op = new JsonObject
            {
                ["op"] = "createObject",
                ["id"] = d.Id,
                ["asset"] = _targets[d.Id].Package,
                ["class"] = d.Template ?? catalog.Get(K.LootTable, "class")!,
                ["properties"] = WithRaw(p, d.Json["properties"]),
            };
            _plan.Operations.Add(op);
            _creates[d.Id] = op;

            foreach (var inject in (d.Json["injectInto"] as JsonArray ?? []).OfType<JsonObject>())
            {
                Append(inject["table"]!.GetValue<string>(), "Entries", LootEntry(new JsonObject
                {
                    ["table"] = d.Id,
                    ["chance"] = inject["chance"]?.DeepClone(),
                }));
            }
        }

        private void PlanWorld(Definition d)
        {
            var dir = $"{Root}/{catalog.Folder(K.World)}/{d.Id}";
            foreach (var zone in (d.Json["zones"] as JsonArray ?? []).OfType<JsonObject>())
            {
                var zoneId = zone["id"]!.GetValue<string>();
                var p = new JsonObject();
                if (zone["maps"] is JsonArray maps) p["MapTemplates"] = Values.Array(maps.Select(m => (JsonNode?)Values.Soft(m!.GetValue<string>())));
                if (zone["tileSet"]?.GetValue<string>() is { } tileSet) { p["TileSet"] = Values.Asset(tileSet); Require(tileSet); }
                var loot = zone["loot"]?.GetValue<string>() ?? d.GetString("loot");
                if (loot is not null)
                {
                    p["SpawnLists"] = Values.Array([Values.Struct("TileSpawnList", new JsonObject
                    {
                        ["Name"] = Values.Name("Loot"),
                        ["Enabled"] = true,
                        ["SpawnTable"] = Values.Asset(Resolve(loot) ?? loot),
                    })]);
                }
                if (zone["encounters"] is JsonArray { Count: > 0 } encounters)
                {
                    var e = 0;
                    p["EncounterGroups"] = Values.Array(encounters.Select(enc => (JsonNode?)Values.Struct("EncounterGroup", new JsonObject
                    {
                        ["NameID"] = Values.Name($"{zoneId}_Encounter{++e}"),
                        ["SpawnTable"] = Values.Asset(Resolve(enc!.GetValue<string>()) ?? enc.GetValue<string>()),
                    })));
                }
                foreach (var m in zone["maps"] as JsonArray ?? [])
                {
                    var map = m!.GetValue<string>();
                    if (!map.StartsWith(Root + "/", StringComparison.Ordinal)) Require(map);
                }
                var package = $"{dir}/{catalog.Get(K.World, "zonePrefix") ?? "Zone_"}{d.Id}_{zoneId}";
                var op = CreateSubBlueprint(package, zone["template"]?.GetValue<string>() ?? catalog.Get(K.World, "zoneTemplate")!,
                    WithRaw(p, zone["properties"]));
                op["id"] = $"{d.Id}.{zoneId}";
                if (d.Json["registration"] is JsonObject reg && reg["mode"]?.GetValue<string>() == "append")
                    Append(reg["list"]!.GetValue<string>(), reg["property"]!.GetValue<string>(), Values.Class(GamePath.ClassPath(package)));
            }
        }

        private void CampaignAlternative(Definition d)
        {
            if (d.Json["campaign"] is not JsonObject campaign) return;
            var slot = campaign["alternativeTo"]!.GetValue<string>();
            var quest = campaign["quest"]!.GetValue<string>();
            var slotList = catalog.Json["campaign"]?["slots"]?[slot] is JsonValue v && v.TryGetValue<string>(out var sl) ? sl : null;
            var property = catalog.Json["campaign"]?["slotProperty"] is JsonValue pv && pv.TryGetValue<string>(out var ps) ? ps : null;
            if (slotList is null || property is null)
            {
                _plan.Notes.Add($"world {d.Id}: adding it as an extra '{slot}' roll needs the campaign's slot list (catalog campaign.slots.{slot} and campaign.slotProperty). Export Quest_Campaign from FModel so they can be filled in.");
                return;
            }
            var entry = new JsonObject { ["QuestBP"] = SoftClassValue(quest) };
            if (campaign["chance"] is { } chance) entry["ChanceToSpawn"] = chance.DeepClone();
            Append(slotList, property, Values.New(catalog.Get(K.Quest, "subQuestClass")!, entry));
        }

        private void PlanAdventure(Definition d)
        {
            var storyCampaign = d.GetString("mode") == "storyCampaign";
            var p = new JsonObject
            {
                ["QuestNameID"] = Values.Name(Name(d)),
                ["QuestGameMode"] = Values.Enum(storyCampaign ? "Campaign" : "Adventure"),
            };
            CampaignOption(d, p, storyCampaign);
            Text(p, "Label", d.Json["name"]);
            Text(p, "Description", d.Json["description"]);
            if (d.Json["icon"]?["small"]?.GetValue<string>() is { } icon) p["Icon"] = Values.Soft(Texture(icon, $"{Dir(d)}/T_{Name(d)}_Icon"));
            if (d.GetString("container") is { } container)
            {
                p["Container"] = Values.Soft(container);
                if (!container.StartsWith(Root + "/", StringComparison.Ordinal)) Require(container);
            }
            if (d.Json["levelMin"] is { } min) p["LevelMin"] = min.DeepClone();
            if (d.Json["levelMax"] is { } max) p["LevelMax"] = max.DeepClone();
            if (d.GetString("rarity") is { } rarity) p["Rarity"] = Values.Enum(rarity);
            if (d.GetString("world") is { } worldId && project.Find(worldId) is { } world && world.GetString("biomeName") is { } biome)
                p["BiomeName"] = biome;
            if (d.Json["rewards"] is JsonArray { Count: > 0 })
                _plan.Notes.Add($"adventure {d.Id}: 'rewards' are given by the quest graph in the container map; they're listed but not wired automatically yet.");
            var subQuests = new JsonArray();
            foreach (var entry in d.Json["storyline"] as JsonArray ?? [])
            {
                var quest = entry is JsonObject o ? o["quest"]!.GetValue<string>() : entry!.GetValue<string>();
                var sp = new JsonObject { ["QuestBP"] = SoftClassValue(quest) };
                if (entry is JsonObject eo)
                {
                    if (eo["chance"] is { } chance) sp["ChanceToSpawn"] = chance.DeepClone();
                    if (eo["optional"] is { } optional) sp["Optional"] = optional.DeepClone();
                }
                subQuests.Add(new JsonObject
                {
                    ["name"] = $"SubQuest_{(GamePath.IsGamePath(quest) ? GamePath.PackageOf(quest)[(GamePath.PackageOf(quest).LastIndexOf('/') + 1)..] : quest)}",
                    ["class"] = catalog.Get(K.Quest, "subQuestClass")!,
                    ["properties"] = sp,
                });
            }
            var op = CreateBlueprint(d, d.Template!, WithRaw(p, d.Json["properties"]));
            if (subQuests.Count > 0) op["addComponents"] = subQuests;
            Unverified(d, "the adventure quest template");
        }

        /// <summary>
        /// Story-campaign registration, play-once and extra difficulty. These need facts about the campaign menu that
        /// aren't in the catalog yet; until they are, the plan says what's missing instead of guessing.
        /// </summary>
        private void CampaignOption(Definition d, JsonObject p, bool storyCampaign)
        {
            string? Campaign(string key) => catalog.Json["campaign"]?[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
            if (d.Json["once"]?.GetValue<bool>() == true)
            {
                if (Campaign("onceProperty") is { } once) p[once] = true;
                else _plan.Notes.Add($"adventure {d.Id}: 'once' needs the play-once property (catalog campaign.onceProperty).");
            }
            if (d.Json["difficulty"] is { } difficulty)
            {
                if (Campaign("difficultyProperty") is { } prop) p[prop] = difficulty.DeepClone();
                else _plan.Notes.Add($"adventure {d.Id}: 'difficulty' needs the property that raises difficulty (catalog campaign.difficultyProperty).");
            }
            if (!storyCampaign) return;
            if (Campaign("storyCampaignList") is { } list && Campaign("storyCampaignProperty") is { } property)
                Append(list, property, Values.Class(_targets[d.Id].ClassPath));
            else
                _plan.Notes.Add($"adventure {d.Id}: story campaigns are added to the campaign menu's list, which isn't in the catalog yet (catalog campaign.storyCampaignList). Export the DLC campaign setup from FModel.");
        }

        private void PlanGenericBlueprint(Definition d)
        {
            var p = ItemProperties(d);
            var mode = d.GetString("asset") ?? "blueprint";
            if (mode == "blueprint")
            {
                CreateBlueprint(d, d.Template!, WithRaw(p, d.Json["properties"]), Components(d));
                return;
            }
            Require(d.Template!);
            var op = new JsonObject
            {
                ["op"] = mode == "copy" ? "copyAsset" : "createObject",
                ["id"] = d.Id,
                ["asset"] = _targets[d.Id].Package,
                [mode == "copy" ? "source" : "class"] = d.Template,
                ["properties"] = WithRaw(p, d.Json["properties"]),
            };
            _plan.Operations.Add(op);
            _creates[d.Id] = op;
        }

        // ---------- characters, animation, dialogue, quests ----------

        private void PlanAnimationSet(Definition d)
        {
            var dir = Dir(d);
            var name = Name(d);
            var skeleton = d.GetString("skeleton");
            if (skeleton is not null) Require(skeleton);
            var ids = new JsonArray();
            foreach (var (animId, animNode) in d.Json["animations"] as JsonObject ?? [])
            {
                var anim = (JsonObject)animNode!;
                var sequences = new JsonArray();
                var i = 0;
                foreach (var file in (anim["files"] as JsonArray ?? []).Select(f => f!.GetValue<string>()))
                {
                    if (GamePath.IsGamePath(file)) { Require(file); sequences.Add(Values.Asset(file)); continue; }
                    var package = $"{dir}/A_{name}_{animId}{(i++ == 0 ? "" : "_" + i)}";
                    var import = new JsonObject { ["op"] = "importAnimation", ["asset"] = package, ["source"] = project.ResolveFile(file) };
                    if (skeleton is not null) import["skeleton"] = skeleton;
                    _plan.Operations.Add(import);
                    sequences.Add(Values.Asset(package));
                }
                var entry = new JsonObject { ["Animations"] = sequences };
                if (anim["tags"] is JsonArray tags) entry["Tags"] = Values.Array(tags.Select(t => (JsonNode?)Values.Name(t!.GetValue<string>())));
                var fields = new JsonObject
                {
                    ["NameID"] = Values.Name(animId),
                    ["Entries"] = Values.Array([Values.Struct("AnimationEntry", entry)]),
                };
                if (anim["loop"] is { } loop) fields["Loop"] = loop.DeepClone();
                if (anim["blendTime"] is { } blend) fields["BlendTime"] = blend.DeepClone();
                if (anim["blendOut"] is { } blendOut) fields["BlendOut"] = blendOut.DeepClone();
                if (anim["startTime"] is { } start) fields["StartTime"] = start.DeepClone();
                foreach (var (k, v) in anim["properties"] as JsonObject ?? []) fields[k] = ResolveRefs(v?.DeepClone());
                ids.Add(Values.Struct("AnimationID", fields));
            }

            var p = new JsonObject();
            if (skeleton is not null) p["Skeleton"] = Values.Asset(skeleton);
            JsonObject op;
            if (d.Template is { } template)
            {
                // Start from a game animation set and replace or add IDs, so everything else it plays still works.
                Require(template);
                p["AnimationsIDs"] = Values.MergeByKey("NameID", ids);
                op = new JsonObject { ["op"] = "copyAsset", ["id"] = d.Id, ["asset"] = _targets[d.Id].Package, ["source"] = template };
            }
            else
            {
                p["AnimationsIDs"] = ids;
                op = new JsonObject { ["op"] = "createObject", ["id"] = d.Id, ["asset"] = _targets[d.Id].Package, ["class"] = catalog.Get(K.AnimationSet, "class")! };
            }
            op["properties"] = WithRaw(p, d.Json["properties"]);
            _plan.Operations.Add(op);
            _creates[d.Id] = op;
        }

        private void PlanCharacter(Definition d)
        {
            var kind = d.Kind;
            var dir = Dir(d);
            var name = Name(d);
            var parent = d.Template ?? catalog.Get(kind, "template")
                ?? throw new InvalidOperationException($"{kind} {d.Id} has no template");
            var p = new JsonObject();
            var meshComponent = d.Json["model"]?["component"]?.GetValue<string>() ?? catalog.Get(kind, "meshComponent") ?? "Mesh";
            var model = d.Json["model"] is JsonObject m ? (JsonObject)m.DeepClone() : null;
            if (model is not null) model["component"] = meshComponent;
            var components = Model(d, model);

            if (d.Json["animBlueprint"] is JsonObject abp)
            {
                var abpPackage = $"{dir}/ABP_{name}";
                var layers = new JsonArray();
                foreach (var (layer, set) in abp["layers"] as JsonObject ?? [])
                {
                    layers.Add(Values.Struct("AnimationLayer", new JsonObject
                    {
                        ["NameID"] = Values.Name(layer),
                        ["AnimationSet"] = Values.Asset(Resolve(set!.GetValue<string>()) ?? set.GetValue<string>()),
                    }));
                }
                var abpProps = new JsonObject();
                if (layers.Count > 0) abpProps["AnimationLayers"] = Values.MergeByKey("NameID", layers);
                CreateSubBlueprint(abpPackage, abp["template"]!.GetValue<string>(), WithRaw(abpProps, abp["properties"]));
                var mesh = components[meshComponent] as JsonObject ?? [];
                mesh["AnimClass"] = Values.Class(GamePath.ClassPath(abpPackage));
                components[meshComponent] = mesh;
            }

            if (d.Json["stats"] is JsonObject stats && stats.Count > 0)
            {
                var table = $"{dir}/DT_{name}_Stats";
                DataTable(table, catalog.Get(kind, "statsStruct")!, new JsonObject { [name] = stats.DeepClone() });
                p["Data"] = Values.RowHandle(table, name);
            }
            if (d.GetString("faction") is { } faction) { p["Faction"] = Values.Class(faction); Require(faction); }

            if (d.GetString("dialogue") is { } dialogue)
            {
                var component = d.GetString("dialogueComponent") ?? catalog.Get(kind, "dialogueComponent") ?? "Dialog";
                var props = components[component] as JsonObject ?? [];
                props[catalog.Get(kind, "dialogueProperty") ?? "EventTree"] = Values.Asset(Resolve(dialogue) ?? dialogue);
                components[component] = props;
            }
            if (d.Json["vendor"] is JsonObject vendor)
            {
                var list = $"{dir}/RL_{name}";
                var recipes = (vendor["items"] as JsonArray ?? []).OfType<JsonObject>()
                    .Select(i => (JsonNode?)Recipe(Resolve(i["item"]!.GetValue<string>()) ?? i["item"]!.GetValue<string>(), i));
                _plan.Operations.Add(new JsonObject
                {
                    ["op"] = "createObject",
                    ["asset"] = list,
                    ["class"] = "/Script/GunfireRuntime.CraftingRecipeList",
                    ["properties"] = new JsonObject { [catalog.RecipeListProperty] = Values.Array(recipes) },
                });
                p[vendor["property"]?.GetValue<string>() ?? catalog.Get(kind, "vendorProperty") ?? "RecipeList"] = Values.Asset(list);
            }
            if (d.GetString("loot") is { } loot)
                p[d.GetString("lootProperty") ?? catalog.Get(kind, "lootProperty") ?? "LootTable"] = Values.Asset(Resolve(loot) ?? loot);
            if (d.Json["unlock"] is JsonObject unlock)
            {
                var after = unlock["after"]!.GetValue<string>();
                if (GamePath.IsGamePath(after)) Require(after);
                _plan.Notes.Add($"{kind} {d.Id}: appears after {after} is {unlock["state"]?.GetValue<string>() ?? "Complete"}"
                    + (unlock["objective"] is { } o ? $" (objective {o})" : "")
                    + ". The condition is a QuestStateCondition, but the hub's spawn hook that would check it isn't in the catalog yet (npc.hub.spawnHook); until then gate her with a hub quest in the editor.");
            }
            if (d.Json["location"] is JsonObject location)
            {
                var hub = location["hub"]?.GetValue<string>();
                var map = location["map"]?.GetValue<string>() ?? (hub is null ? null : catalog.Get(kind, $"hub/{hub}"));
                if (map is not null && !map.StartsWith(Root + "/", StringComparison.Ordinal)) Require(map);
                _plan.Notes.Add(map is null
                    ? $"{kind} {d.Id}: location hub '{hub}' isn't mapped in the catalog (npc.hub.{hub}); export the hub map's path from FModel."
                    : $"{kind} {d.Id}: placing an actor in {map} would mean overriding that map, which no pak mod can do safely. She's spawned by the hub hook once npc.hub.spawnHook is known; the position is kept for it.");
            }
            if (d.Json["name"] is not null && d.Json["properties"]?["Label"] is null)
                _plan.Notes.Add($"{kind} {d.Id}: the character's display name property isn't known yet; 'name' is kept for documentation. Set it through 'properties' once found in FModel.");

            CreateBlueprint(d, parent, WithRaw(p, d.Json["properties"]), Components(d, components));
            Unverified(d, "the component and property names for dialogue, vendor, loot and stats");
        }

        private void PlanDialogue(Definition d)
        {
            var nodes = DialogueNodes(d, d.Json["nodes"] as JsonArray);
            var op = new JsonObject
            {
                ["op"] = "createObject",
                ["id"] = d.Id,
                ["asset"] = _targets[d.Id].Package,
                ["class"] = catalog.Get(K.Dialogue, "class")!,
                ["properties"] = WithRaw(new JsonObject { ["Nodes"] = nodes }, d.Json["properties"]),
            };
            _plan.Operations.Add(op);
            _creates[d.Id] = op;
            Unverified(d, "the conversation's trigger names");
        }

        private JsonArray DialogueNodes(Definition d, JsonArray? nodes) =>
            new([.. (nodes ?? []).OfType<JsonObject>().Select(n => (JsonNode?)DialogueNode(d, n))]);

        private JsonObject DialogueNode(Definition d, JsonObject n)
        {
            string NodeClass(string key) => catalog.Get(K.Dialogue, $"nodes/{key}")!;
            var p = new JsonObject();
            string cls;
            if (n["say"] is { } say)
            {
                cls = NodeClass("say");
                p["Text"] = Values.Text(LocalizedText.From(say, Culture)!);
                if (n["facial"]?.GetValue<string>() is { } facial) p["FacialAnimation"] = Values.Soft(facial);
                if (n["audio"] is not null)
                    _plan.Notes.Add($"dialogue {d.Id}: voice audio isn't wired yet (the sound struct isn't mapped); set 'Dialog' through the node's 'properties'.");
            }
            else if (n["choices"] is JsonArray choices)
            {
                cls = NodeClass("choices");
                p["Branches"] = Values.Array(choices.OfType<JsonObject>().Select(c =>
                {
                    var cp = new JsonObject
                    {
                        ["Label"] = Values.Text(LocalizedText.From(c["label"], Culture)!),
                        ["Nodes"] = DialogueNodes(d, c["then"] as JsonArray),
                    };
                    if (c["hidden"] is { } hidden) cp["Hidden"] = hidden.DeepClone();
                    return (JsonNode?)Values.New(NodeClass("choice"), cp);
                }));
            }
            else if (n["trigger"]?.GetValue<string>() is { } trigger)
            {
                cls = NodeClass("trigger");
                p["TriggerName"] = Values.Name(trigger);
                p["Nodes"] = DialogueNodes(d, n["then"] as JsonArray);
            }
            else if (n["onItem"]?.GetValue<string>() is { } item)
            {
                cls = NodeClass("onItem");
                p["ItemBP"] = SoftClassValue(item);
                p["Nodes"] = DialogueNodes(d, n["then"] as JsonArray);
            }
            else if (n["activateObjective"]?.GetValue<string>() is { } activate)
            {
                cls = NodeClass("activateObjective");
                p["Objective"] = Values.Name(activate);
            }
            else if (n["completeObjective"]?.GetValue<string>() is { } complete)
            {
                cls = NodeClass("completeObjective");
                p["Objective"] = Values.Name(complete);
            }
            else if (n["completeQuest"]?.GetValue<string>() is { } result)
            {
                cls = NodeClass("completeQuest");
                p["Result"] = Values.Enum(result);
            }
            else if (n["event"]?.GetValue<string>() is { } ev)
            {
                cls = NodeClass("event");
                p["EventName"] = Values.Name(ev);
                if (n["context"]?.GetValue<string>() is { } context) p["Context"] = Values.Name(context);
            }
            else if (n["questSwitch"] is JsonArray cases)
            {
                cls = NodeClass("questSwitch");
                p["Branches"] = Values.Array(cases.OfType<JsonObject>().Select(c =>
                {
                    var bp = new JsonObject
                    {
                        ["ValidForState"] = Values.Enum(c["when"]!.GetValue<string>()),
                        ["Nodes"] = DialogueNodes(d, c["then"] as JsonArray),
                    };
                    if (c["objectives"] is JsonArray objectives)
                        bp["ValidObjectives"] = Values.Array(objectives.Select(o => (JsonNode?)Values.Name(o!.GetValue<string>())));
                    return (JsonNode?)Values.New(NodeClass("questSwitchBranch"), bp);
                }));
            }
            else if (n["jump"]?.GetValue<string>() is { } jump)
            {
                cls = NodeClass("jump");
                p["JumpNode"] = Values.NodeRef($"{d.Id}.{jump}");
            }
            else if (n["exit"] is not null)
            {
                cls = NodeClass("exit");
            }
            else
            {
                cls = n["node"]!.GetValue<string>();
            }
            if (n["once"]?.GetValue<bool>() == true) p["DoOnce"] = true;
            var node = Values.New(cls, WithRaw(p, n["properties"]));
            if (n["id"]?.GetValue<string>() is { } id) node["$key"] = $"{d.Id}.{id}";
            return node;
        }

        private void PlanQuest(Definition d)
        {
            var p = new JsonObject { ["QuestNameID"] = Values.Name(Name(d)) };
            Text(p, "Label", d.Json["name"]);
            Text(p, "Description", d.Json["description"]);
            if (d.Json["icon"]?["small"]?.GetValue<string>() is { } icon) p["Icon"] = Values.Soft(Texture(icon, $"{Dir(d)}/T_{Name(d)}_Icon"));
            if (d.GetString("questType") is { } type) p["Type"] = Values.Enum(type);
            if (d.Json["levelMin"] is { } min) p["LevelMin"] = min.DeepClone();
            if (d.Json["levelMax"] is { } max) p["LevelMax"] = max.DeepClone();
            if (d.GetString("container") is { } container)
            {
                p["Container"] = Values.Soft(container);
                if (!container.StartsWith(Root + "/", StringComparison.Ordinal)) Require(container);
            }

            var add = new JsonArray();
            foreach (var o in (d.Json["objectives"] as JsonArray ?? []).OfType<JsonObject>())
            {
                var op = new JsonObject();
                Text(op, "Label", o["name"]);
                Text(op, "Description", o["description"]);
                if (o["hidden"] is { } hidden) op["Hidden"] = hidden.DeepClone();
                add.Add(new JsonObject { ["name"] = o["id"]!.GetValue<string>(), ["class"] = catalog.Get(K.Quest, "objectiveClass")!, ["properties"] = op });
            }
            foreach (var e in (d.Json["entities"] as JsonArray ?? []).OfType<JsonObject>())
            {
                var ep = new JsonObject { ["EventTree"] = Values.Soft(Resolve(e["dialogue"]!.GetValue<string>()) ?? e["dialogue"]!.GetValue<string>()) };
                Text(ep, "Label", e["name"]);
                add.Add(new JsonObject { ["name"] = e["id"]!.GetValue<string>(), ["class"] = catalog.Get(K.Quest, "entityClass")!, ["properties"] = ep });
                if (e["npc"] is not null)
                    _plan.Notes.Add($"quest {d.Id}: entity {e["id"]} names an NPC; how the game ties a quest entity to a placed NPC isn't confirmed, so place the NPC in the quest's map and check it in game.");
            }
            if (d.Json["rewards"] is JsonArray { Count: > 0 })
                _plan.Notes.Add($"quest {d.Id}: 'rewards' are listed but not wired yet; give them from the dialogue (event node) or the quest blueprint.");
            var create = CreateBlueprint(d, d.Template!, WithRaw(p, d.Json["properties"]), Components(d));
            if (add.Count > 0) create["addComponents"] = add;
            Unverified(d, "the quest objective and entity components");
        }

        // ---------- acquisition and registration ----------

        private void Acquisition(Definition d, JsonArray? list)
        {
            if (d.Kind is K.Archetype or K.LootTable or K.World or K.Adventure or K.Dialogue or K.AnimationSet or K.Quest
                || !_targets.ContainsKey(d.Id)) return;
            AcquireItem(d, _targets[d.Id].ReferencePath, list);
        }

        private void AcquireItem(Definition d, string itemClass, JsonArray? list)
        {
            foreach (var a in (list ?? []).OfType<JsonObject>())
            {
                switch (a["type"]?.GetValue<string>())
                {
                    case "loot":
                        var table = a["table"]!.GetValue<string>();
                        var entry = LootEntry(new JsonObject
                        {
                            ["item"] = itemClass,
                            ["chance"] = a["chance"]?.DeepClone(),
                            ["quantity"] = a["quantity"]?.DeepClone(),
                            ["levelMin"] = a["levelMin"]?.DeepClone(),
                            ["levelMax"] = a["levelMax"]?.DeepClone(),
                        }.Where(p => p.Value is not null).Aggregate(new JsonObject(), (o, p) => { o[p.Key] = p.Value!.DeepClone(); return o; }));
                        if (GamePath.IsGamePath(table)) Append(table, "Entries", entry);
                        else if (_creates.TryGetValue(table, out var op)) ((JsonArray)op["properties"]!["Entries"]!).Add(entry);
                        else Append(_targets[table].Package, "Entries", entry); // table defined later in the file order
                        break;
                    case "craft" or "vendor":
                        Append(a["recipeList"]!.GetValue<string>(), catalog.RecipeListProperty, Recipe(itemClass, a));
                        break;
                    case "startingGear":
                        var archetype = a["archetype"]!.GetValue<string>();
                        if (GamePath.IsGamePath(archetype))
                        {
                            Append(archetype, "StartingGear", Values.Class(itemClass));
                        }
                        else if (_creates.TryGetValue(archetype, out var arch))
                        {
                            var props = (JsonObject)arch["properties"]!;
                            if (props["StartingGear"] is not JsonArray gear) props["StartingGear"] = gear = [];
                            gear.Add(Values.Class(itemClass));
                        }
                        else
                        {
                            Append(_targets[archetype].Package, "StartingGear", Values.Class(itemClass));
                        }
                        break;
                    case "questReward":
                        _plan.Notes.Add($"{d.Kind} {d.Id}: quest rewards are given by the quest's graph; add the item there (not automated yet).");
                        break;
                }
            }
        }

        /// <summary>A UCraftingRecipe for an item: materials as ingredients, scrap as the last one.</summary>
        private JsonObject Recipe(string itemClass, JsonObject a)
        {
            var recipe = new JsonObject
            {
                ["Item"] = Values.SoftClass(itemClass),
                ["ItemQuantity"] = 1,
                ["Type"] = Values.Enum("Craft"),
            };
            var n = 1;
            foreach (var m in (a["materials"] as JsonArray ?? []).OfType<JsonObject>())
            {
                recipe[$"Ingredient{n}"] = SoftClassValue(m["item"]!.GetValue<string>());
                recipe[$"Ingredient{n}Quantity"] = m["quantity"]!.DeepClone();
                n++;
            }
            if (a["scrap"] is { } scrap)
            {
                recipe[$"Ingredient{n}"] = Values.SoftClass(catalog.ScrapItem);
                recipe[$"Ingredient{n}Quantity"] = scrap.DeepClone();
                n++;
            }
            foreach (var r in (a["requires"] as JsonArray ?? []).OfType<JsonObject>())
            {
                // A gate: an ingredient the vendor hides (or shows as ???) until the player owns it.
                recipe[$"Ingredient{n}"] = SoftClassValue(r["item"]!.GetValue<string>());
                recipe[$"Ingredient{n}Quantity"] = r["quantity"]?.DeepClone() ?? 1;
                if (r["level"] is { } level) recipe[$"Ingredient{n}LevelRequirement"] = level.DeepClone();
                var show = r["show"]?.GetValue<string>() ?? "hidden";
                recipe[show == "secret" ? $"bIngredient{n}SecretIfMissing" : $"bIngredient{n}HideIfMissing"] = true;
                n++;
            }
            foreach (var (k, v) in a["properties"] as JsonObject ?? []) recipe[k] = ResolveRefs(v?.DeepClone());
            return Values.New(catalog.RecipeClass, recipe);
        }

        private void Registration(Definition d)
        {
            if (d.Kind == K.World || !_targets.TryGetValue(d.Id, out var target)) return;
            var reg = d.Json["registration"] as JsonObject;
            switch (reg?["mode"]?.GetValue<string>())
            {
                case "append":
                    Append(reg["list"]!.GetValue<string>(), reg["property"]!.GetValue<string>(),
                        target.IsBlueprint ? Values.Class(target.ClassPath) : Values.Asset(target.ObjectPath));
                    break;
                case "replace":
                    break; // the asset is generated at the replaced path (see AssignTarget)
                default:
                    if (d.Kind is K.Archetype or K.Adventure)
                        _plan.Notes.Add($"{d.Kind} {d.Id}: registered by discovery (the game's asset registry scan). If it doesn't show up in game, switch 'registration' to 'append' or 'replace'.");
                    break;
            }
        }
    }
}

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
            foreach (var d in project.Concrete.OrderBy(d => d.Kind == K.LootTable ? 0 : 1))
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
                    case K.World: PlanWorld(d); break;
                    case K.Adventure: PlanAdventure(d); break;
                    case K.Blueprint: PlanGenericBlueprint(d); break;
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
                K.LootTable => false,
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
            return p;
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
                _plan.Notes.Add($"{d.Kind} {d.Id}: {what} uses catalog entries that aren't confirmed against the game yet (data/catalog.json, verified=false).");
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
                if (zone["encounters"] is JsonArray { Count: > 0 })
                    _plan.Notes.Add($"world {d.Id} zone {zoneId}: 'encounters' isn't generated yet; set EncounterGroups through the zone's 'properties'.");
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

        private void PlanAdventure(Definition d)
        {
            var p = new JsonObject
            {
                ["QuestNameID"] = Values.Name(Name(d)),
                ["QuestGameMode"] = Values.Enum("Adventure"),
            };
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
            if (d.Json["rewards"] is JsonArray { Count: > 0 } || d.Json["storyline"] is JsonArray { Count: > 0 })
                _plan.Notes.Add($"adventure {d.Id}: 'rewards' and 'storyline' are placed by the quest graph in the container map; they're listed but not wired automatically yet.");
            CreateBlueprint(d, d.Template!, WithRaw(p, d.Json["properties"]));
            Unverified(d, "the adventure quest template");
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

        // ---------- acquisition and registration ----------

        private void Acquisition(Definition d, JsonArray? list)
        {
            if (d.Kind is K.Archetype or K.LootTable or K.World or K.Adventure || !_targets.ContainsKey(d.Id)) return;
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
                        }
                        foreach (var (k, v) in a["properties"] as JsonObject ?? []) recipe[k] = ResolveRefs(v?.DeepClone());
                        Append(a["recipeList"]!.GetValue<string>(), catalog.RecipeListProperty, Values.New(catalog.RecipeClass, recipe));
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

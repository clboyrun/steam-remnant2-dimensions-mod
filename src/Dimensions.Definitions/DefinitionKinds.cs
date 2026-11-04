namespace Dimensions.Definitions;

public static class DefinitionKinds
{
    public const string Weapon = "weapon";
    public const string WeaponMod = "weaponMod";
    public const string Mutator = "mutator";
    public const string Ring = "ring";
    public const string Amulet = "amulet";
    public const string Armor = "armor";
    public const string Trait = "trait";
    public const string Archetype = "archetype";
    public const string LootTable = "lootTable";
    public const string World = "world";
    public const string Adventure = "adventure";
    public const string Blueprint = "blueprint";
    public const string Npc = "npc";
    public const string Enemy = "enemy";
    public const string AnimationSet = "animationSet";
    public const string Dialogue = "dialogue";
    public const string Quest = "quest";
    public const string QuestItem = "questItem";
    public const string ItemCategory = "itemCategory";
    public const string Difficulty = "difficulty";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Weapon, WeaponMod, Mutator, Ring, Amulet, Armor, Trait, Archetype, LootTable, World, Adventure, Blueprint,
        Npc, Enemy, AnimationSet, Dialogue, Quest, QuestItem, ItemCategory, Difficulty,
    };

    /// <summary>Kinds that are items the player can hold, so they can go in loot, recipes and starting gear.</summary>
    public static readonly IReadOnlySet<string> Items = new HashSet<string>
    {
        Weapon, WeaponMod, Mutator, Ring, Amulet, Armor, QuestItem, Blueprint,
    };

    /// <summary>Kinds that are characters, so they can be spawned from a spawn table.</summary>
    public static readonly IReadOnlySet<string> Characters = new HashSet<string> { Npc, Enemy, Blueprint };
}

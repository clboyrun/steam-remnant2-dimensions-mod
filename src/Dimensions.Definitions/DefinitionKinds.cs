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

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Weapon, WeaponMod, Mutator, Ring, Amulet, Armor, Trait, Archetype, LootTable, World, Adventure, Blueprint,
    };

    /// <summary>Kinds that are items the player can hold, so they can go in loot, recipes and starting gear.</summary>
    public static readonly IReadOnlySet<string> Items = new HashSet<string>
    {
        Weapon, WeaponMod, Mutator, Ring, Amulet, Armor, Blueprint,
    };
}

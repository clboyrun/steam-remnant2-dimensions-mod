/* Equipment gallery data for the About page. Edit this file to assign a drop dungeon or boss; the page picks it up.
   world:  grimm | deep | arcade | hell | ward13 | unassigned
   source: boss | dungeon | vendor | crafting | unassigned
   dungeon / boss: leave "" while undecided; the card then shows "Unassigned". Use "n/a" for items that are not drops.
   Public page: item and NPC names only, no personal or private data. */
window.EQUIPMENT = [
  { name: "Radar gun", slot: "Weapon", type: "Ranged weapon", world: "unassigned", source: "unassigned", dungeon: "", boss: "",
    text: "A gun with a side screen that always shows the map, even while aiming." },
  { name: "Sonar Neckpiece", slot: "Amulet", type: "Amulet", world: "unassigned", source: "unassigned", dungeon: "", boss: "",
    text: "Pulses every 10 seconds and finds everything within 25 yards: allies in green, hostiles in red." },
  { name: "Corruption-chance ring", slot: "Ring", type: "Ring", world: "ward13", source: "vendor", where: "Corrupted Shop, sold by Masterwork", dungeon: "n/a", boss: "n/a",
    text: "Raises the chance that each hostile spawn is corrupted. Name still to be chosen." },
  { name: "Multi-crit ring", slot: "Ring", type: "Ring", world: "ward13", source: "vendor", where: "Corrupted Shop, sold by Masterwork", dungeon: "n/a", boss: "n/a",
    text: "A ring for the Corrupted Shop that adds multiple critical hits." },
  { name: "Enchanted ring slots 5 to 8", slot: "Ring", type: "Ring slot upgrade", world: "ward13", source: "crafting", where: "The Enchanter", dungeon: "n/a", boss: "n/a",
    text: "Four extra ring slots, one for each armor piece the Enchanter unlocks. Rings stay swappable." },
  { name: "Masterwork weapons", slot: "Weapon", type: "Weapon upgrade", world: "ward13", source: "crafting", where: "Masterwork", dungeon: "n/a", boss: "n/a",
    text: "Normal weapons go to +30 and boss weapons to +15, on the base game's damage curve." },
  { name: "Masterwork mutators", slot: "Mutator", type: "Mutator upgrade", world: "ward13", source: "crafting", where: "Masterwork", dungeon: "n/a", boss: "n/a",
    text: "Mutators climb from level 10 to 20 and turn gold past level 10." },
  { name: "Forged armor", slot: "Armor", type: "Armor upgrade", world: "ward13", source: "crafting", where: "The Forge", dungeon: "n/a", boss: "n/a",
    text: "Ten Forge tiers per armor piece. Each tier adds 10% armor and +1 to every resistance." },
  { name: "Prism fusions", slot: "Prism", type: "Prism fusion", world: "ward13", source: "crafting", where: "The Prism", dungeon: "n/a", boss: "n/a",
    text: "Every pair of fragments gets a fusion. The Prism holds up to five fusion fragments." },
  { name: "Grimm completion token", slot: "Quest item", type: "Quest item", world: "grimm", source: "boss", dungeon: "", boss: "",
    where: "Final boss reward of the Grimm campaign", text: "Hidden token that proves the Grimm campaign is complete and unlocks its Quest Book." },
  { name: "Lovecraft completion token", slot: "Quest item", type: "Quest item", world: "deep", source: "boss", dungeon: "", boss: "",
    where: "Final boss reward of the Lovecraft campaign", text: "Hidden token that proves the Lovecraft campaign is complete and unlocks its Quest Book." },
  { name: "Arcade completion token", slot: "Quest item", type: "Quest item", world: "arcade", source: "boss", dungeon: "", boss: "",
    where: "Final boss reward of the Abandoned Arcade campaign", text: "Hidden token that proves the Abandoned Arcade campaign is complete and unlocks its Quest Book." },
  { name: "Hell completion token", slot: "Quest item", type: "Quest item", world: "hell", source: "boss", dungeon: "", boss: "",
    where: "Final boss reward of the Hell campaign", text: "Hidden token that proves the Hell campaign is complete and unlocks its Quest Book." },
  { name: "Quest Books", slot: "Quest item", type: "Quest item", world: "ward13", source: "vendor", where: "Falada, once a campaign token is owned", dungeon: "n/a", boss: "n/a",
    text: "One book per completed campaign, each pointing to an unlockable. A book vanishes once that item is owned." }
];
window.EQUIPMENT_LABELS = {
  world: { grimm: "Grimm", deep: "Lovecraft", arcade: "Arcade", hell: "Hell", ward13: "Ward 13", unassigned: "Unassigned" },
  source: { boss: "Boss drop", dungeon: "Dungeon drop", vendor: "Vendor", crafting: "Crafted / upgraded", unassigned: "Unassigned" }
};

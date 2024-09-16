﻿using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using ModKit;
using ModKit.Utility;
using System;
using System.Linq;
using UnityEngine;
using static ModKit.UI;
using ToyBox.Multiclass;

namespace ToyBox {
    public class PhatLoot {
        public static Settings Settings => Main.Settings;
        public static string searchText = "";

        //
        private const string MassLootBox = "Open Mass Loot Window";
        private const string OpenPlayerChest = "Open Player Chest";
        private const string RevealGroundLoot = "Reveal Ground Loot";
        private const string RevealHiddenGroundLoot = "Reveal Hidden Ground Loot";
        private const string RevealInevitableLoot = "Reveal Inevitable Loot";
        public static void ResetGUI() { }

        public static void OnLoad() {
            KeyBindings.RegisterAction(MassLootBox, LootHelper.OpenMassLoot);
            KeyBindings.RegisterAction(OpenPlayerChest, LootHelper.OpenPlayerChest);
            KeyBindings.RegisterAction(RevealGroundLoot, () => LootHelper.ShowAllChestsOnMap());
            KeyBindings.RegisterAction(RevealHiddenGroundLoot, () => LootHelper.ShowAllChestsOnMap(true));
            KeyBindings.RegisterAction(RevealInevitableLoot, LootHelper.ShowAllInevitablePortalLoot);
        }

        public static void OnGUI() {
            if (Game.Instance?.Player?.Inventory == null) return;
#if false
            Div(0, 25);
            var inventory = Game.Instance.Player.Inventory;
            var items = inventory.ToList();
            HStack("Inventory", 1,
                () => {
                    ActionButton("Export", () => items.Export("inventory.json"), Width(150));
                    Space(25);
                    ActionButton("Import", () => inventory.Import("inventory.json"), Width(150));
                    Space(25);
                    ActionButton("Replace", () => inventory.Import("inventory.json", true), Width(150));
                },
                () => { }
            );
#endif
            Div(0, 25);
            HStack("Loot".localize(), 1,
                () => {
                    BindableActionButton(MassLootBox, true, Width(400));
                    Space(95 - 150);
                    Label(RichText.Green("Lets you open up the area's mass loot screen to grab goodies whenever you want. Normally shown only when you exit the area".localize()));
                },
                () => {
                    BindableActionButton(OpenPlayerChest, true, Width(400));
                    Space(95 - 150);
                    Label(RichText.Green("Lets you open up your player storage chest that you find near your bed at the Inn and other places".localize()));
                },
                () => {
                    BindableActionButton(RevealGroundLoot, true, Width(400));
                    Space(95 - 150);
                    Label(RichText.Green("Shows all chests/bags/etc on the map excluding hidden".localize()));
                },
                () => {
                    BindableActionButton(RevealHiddenGroundLoot, true, Width(400));
                    Space(95 - 150);
                    Label(RichText.Green("Shows all chests/bags/etc on the map including hidden".localize()));
                },
                () => {
                    BindableActionButton(RevealInevitableLoot, true, Width(400));
                    Space(95 - 150);
                    Label(RichText.Green("Shows unlocked Inevitable Excess DLC rewards on the map".localize()));
                },
#if DEBUG
                () => Toggle("Show reasons you can not equip an item in tooltips".localize(), ref Settings.toggleShowCantEquipReasons),
#endif
                () => { }
            );
            Div(0, 25);
            HStack("Mass Loot".localize(), 1,
                   () => {
                       Toggle("Show Everything When Leaving Map".localize(), ref Settings.toggleMassLootEverything, 400.width());
                       150.space();
                       Label(RichText.Green("Some items might be invisible until looted".localize()));
                   },
                   () => {
                       Toggle("Steal from living NPCs".localize(), ref Settings.toggleLootAliveUnits, 400.width());
                       150.space();
                       Label(RichText.Green("Allow Mass Loot to steal from living NPCs".localize()));
                   },
                   () => {
                       Toggle("Allow Looting Of Locked Items".localize(), ref Settings.toggleOverrideLockedItems, 400.width());
                       150.space();
                       Label((RichText.Green("This allows you to loot items that are locked such as items carried by certain NPCs and items locked on your characters"
)
                             + RichText.Bold(RichText.Yellow("\nWARNING: "))
                             + RichText.Yellow("This may affect story progression (e.g. your purple knife)")).localize());
                   },
                   () => { }
                  );
            Div(0, 25);
            HStack("Loot Rarity Coloring".localize(), 1,
                   () => {
                       using (VerticalScope(300.width())) {
                           Toggle("Show Rarity Tags".localize(), ref Settings.toggleShowRarityTags, 300.width());
                           Toggle("Color Item Names".localize(), ref Settings.toggleColorLootByRarity, 300.width());
                       }
                       using (VerticalScope()) {
                           Label((RichText.Green($"This makes loot function like Diablo or Borderlands. {RichText.Orange("Note: turning this off requires you to save and reload for it to take effect.")}"
)).localize());
                       }
                   },
                   () => {
                       using (VerticalScope(400.width())) {
                           Label(RichText.Cyan("Minimum Rarity For Loot Rarity Tags/Colors".localize()), AutoWidth());
                           RarityGrid(ref Settings.minRarityToColor, 4, AutoWidth());
                       }
                   });
            Div(0, 25);
            HStack("Loot Rarity Filtering".localize(), 1,
                    () => {
                        using (VerticalScope(300)) {
                            using (HorizontalScope(300)) {
                                using (VerticalScope()) {
                                    Label(RichText.Cyan("Maximum Rarity To Hide:".localize()), AutoWidth());
                                    RarityGrid(ref Settings.maxRarityToHide, 4, AutoWidth());
                                }
                            }
                        }
                        50.space();
                        using (VerticalScope()) {
                            Label("");
                            HelpLabel($"This hides map pins of loot containers containing at most the selected rarity. {RichText.Orange("Note: Changing settings requires reopening the map.")}".localize());
                        }
                    },
                    // The following options let you configure loot filtering and auto sell levels:".green());
                    () => { }
                    );
            Div(0, 25);
            HStack("Bulk Sell".localize(), 1,
                   () => {
                       Toggle("Enable custom bulk selling settings".localize(), ref Settings.toggleCustomBulkSell, 400.width());
                   },
                   () => {
                       if (!Settings.toggleCustomBulkSell) return;
                       using (VerticalScope()) {
                           BulkSell.OnGUI();
                       }
                   });
            Div(0, 25);
            if (Game.Instance.CurrentlyLoadedArea == null) return;
            var isEmpty = true;
            HStack("Loot Checklist".localize(), 1,
                () => {
                    var areaName = "";
                    if (Main.IsInGame) {
                        try {
                            areaName = Game.Instance.CurrentlyLoadedArea.AreaDisplayName;
                        } catch { }
                        var areaPrivateName = Game.Instance.CurrentlyLoadedArea.name;
                        if (areaPrivateName != areaName) areaName += RichText.Yellow($"\n({areaPrivateName})");
                    }
                    Label(RichText.Bold(RichText.Orange(areaName)), Width(300));
                    Label(RichText.Cyan("Rarity: ".localize()), AutoWidth());
                    RarityGrid(ref Settings.lootChecklistFilterRarity, 4, AutoWidth());
                },
                () => {
                    ActionTextField(
                    ref searchText,
                    "itemSearchText",
                    (text) => { },
                    () => { },
                    Width(300));
                    Space(25); Toggle("Show Friendly".localize(), ref Settings.toggleLootChecklistFilterFriendlies);
                    Space(25); Toggle("Blueprint".localize(), ref Settings.toggleLootChecklistFilterBlueprint, AutoWidth());
                    Space(25); Toggle("Description".localize(), ref Settings.toggleLootChecklistFilterDescription, AutoWidth());
                },
                () => {
                    if (!Main.IsInGame) { Label(RichText.Orange("Not available in the Main Menu".localize())); return; }
                    var presentGroups = LootHelper.GetMassLootFromCurrentArea().GroupBy(p => p.InteractionLoot != null ? "Containers" : "Units");
                    var indent = 3;
                    using (VerticalScope()) {
                        foreach (var group in presentGroups.Reverse()) {
                            var presents = group.AsEnumerable().OrderByDescending(p => {
                                var loot = p.GetLewtz(searchText);
                                if (loot.Count == 0) return 0;
                                else return (int)loot.Max(l => l.Rarity());
                            }).ToList();
                            var rarity = Settings.lootChecklistFilterRarity;
                            var count = presents
                                        .Where(p =>
                                                   p.Unit == null
                                                   || (Settings.toggleLootChecklistFilterFriendlies
                                                       && !p.Unit.IsPlayersEnemy
                                                       || p.Unit.IsPlayersEnemy
                                                       )
                                                   || (!Settings.toggleLootChecklistFilterFriendlies
                                                       && p.Unit.IsPlayersEnemy
                                                       )
                                                   ).Count(p => p.GetLewtz(searchText).Lootable(rarity).Count() > 0);
                            Label($"{RichText.Cyan(group.Key.localize())}: {count}");
                            Div(indent);
                            foreach (var present in presents) {
                                var phatLewtz = present.GetLewtz(searchText).Lootable(rarity).OrderByDescending(l => l.Rarity()).ToList();
                                var unit = present.Unit;
                                if (phatLewtz.Any()
                                    && (unit == null
                                        || (Settings.toggleLootChecklistFilterFriendlies
                                            && !unit.IsPlayersEnemy
                                            || unit.IsPlayersEnemy
                                            )
                                        || (!Settings.toggleLootChecklistFilterFriendlies
                                            && unit.IsPlayersEnemy
                                            )
                                        )
                                    ) {
                                    isEmpty = false;
                                    Div();
                                    using (HorizontalScope()) {
                                        Space(indent);
                                        Label(RichText.Bold(RichText.Orange($"{present.GetName()}")), Width(325));
                                        if (present.InteractionLoot != null) {
                                            if (present.InteractionLoot?.Owner?.PerceptionCheckDC > 0)
                                                Label(" Perception DC: ".localize() + RichText.Bold(RichText.Green($"{present.InteractionLoot?.Owner?.PerceptionCheckDC}")), Width(125));
                                            else
                                                Label(RichText.Bold(RichText.Orange(" Perception DC: NA".localize())), Width(125));
                                            int? trickDc = present.InteractionLoot?.Owner?.Get<DisableDeviceRestrictionPart>()?.DC;
                                            if (trickDc > 0)
                                                Label(" Trickery DC: ".localize() + RichText.Bold(RichText.Green($"{trickDc}")), Width(125));
                                            else
                                                Label(RichText.Bold(RichText.Orange(" Trickery DC: NA".localize())), Width(125));
                                        }
                                        Space(25);
                                        using (VerticalScope()) {
                                            foreach (var lewt in phatLewtz) {
                                                var description = lewt.Blueprint.Description;
                                                var showBP = Settings.toggleLootChecklistFilterBlueprint;
                                                var showDesc = Settings.toggleLootChecklistFilterDescription && description != null && description.Length > 0;
                                                using (HorizontalScope()) {
                                                    //Main.Log($"rarity: {lewt.Blueprint.Rarity()} - color: {lewt.Blueprint.Rarity().color()}");
                                                    Label(lewt.Name.StripHTML().Rarity(lewt.Blueprint.Rarity()), showDesc || showBP ? Width(350) : AutoWidth());
                                                    if (showBP) {
                                                        Space(100); Label(RichText.Grey(lewt.Blueprint.GetDisplayName()), showDesc ? Width(350) : AutoWidth());
                                                    }
                                                    if (!showDesc) continue;
                                                    Space(100); Label(RichText.Green(description.StripHTML()));
                                                }
                                            }
                                        }
                                    }
                                    Space(25);
                                }
                            }
                            Space(25);
                        }
                    }
                },
                () => {
                    if (!isEmpty) return;
                    using (HorizontalScope()) {
                        Label(RichText.Orange("No Loot Available".localize()), AutoWidth());
                    }
                }
            );
        }
    }
}
using System;
using System.Linq;
using System.Collections.Generic;
using VRage.Game;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SEInventoryScript { 
    class Program : SEScriptTemplate {

        public int ScreenWidth = 32;
        public bool ThisGridOnly = true;

        public static List<MyInventoryItem> AllItems { get; set; } = new List<MyInventoryItem>();

        public static Dictionary<string, Func<MyItemInfo, bool>> InvScreens = new Dictionary<string, Func<MyItemInfo, bool>> {
            { "LCD Ores", i => i.IsOre },
            { "LCD Raw Materials", i => i.IsIngot },
            { "LCD Components", i => i.IsComponent }
        };

        public static string NoteScreenName = "LCD Notes";



        /*
        MyObjectBuilder_BlueprintDefinition/Canvas
        MyObjectBuilder_BlueprintDefinition/DetectorComponent
        MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent
        */

        public static List<LCDNote> Notes = new List<LCDNote> {
            new MineralRequirement("Cobalt", 10000, 0.30f),
            new MineralRequirement("Gold", 1000, 0.01f),
            new MineralRequirement("Iron", 50000, 0.70f),
            new MineralRequirement("Magnesium", 500, 0.007f),
            new MineralRequirement("Nickel", 10000, 0.40f),
            new MineralRequirement("Platinum", 200, 0.005f),
            new MineralRequirement("Silicon", 10000, 0.70f),
            new MineralRequirement("Silver", 1000, 0.1f),
            new MineralRequirement("Uranium", 500, 0.01f),
            new ComponentRequirement("BulletproofGlass", 500, 2000, "MyObjectBuilder_BlueprintDefinition/BulletproofGlass"),
            new ComponentRequirement("Computer", 1000, 2000, "MyObjectBuilder_BlueprintDefinition/ComputerComponent"),
            new ComponentRequirement("Construction", 3000, 5000, "MyObjectBuilder_BlueprintDefinition/ConstructionComponent"),
            new ComponentRequirement("Display", 300, 500, "MyObjectBuilder_BlueprintDefinition/Display"),
            new ComponentRequirement("Girder", 1000, 2000, "MyObjectBuilder_BlueprintDefinition/GirderComponent"),
            new ComponentRequirement("InteriorPlate", 1000, 2000, "MyObjectBuilder_BlueprintDefinition/InteriorPlate"),
            new ComponentRequirement("LargeTube", 500, 1000, "MyObjectBuilder_BlueprintDefinition/LargeTube"),
            new ComponentRequirement("Medical", 15, 30, "MyObjectBuilder_BlueprintDefinition/MedicalComponent"),
            new ComponentRequirement("MetalGrid", 1000, 2000, "MyObjectBuilder_BlueprintDefinition/MetalGrid"),
            new ComponentRequirement("Motor", 1000, 2000, "MyObjectBuilder_BlueprintDefinition/MotorComponent"),
            new ComponentRequirement("PowerCell", 80, 320, "MyObjectBuilder_BlueprintDefinition/PowerCell"),
            new ComponentRequirement("RadioCommunication", 0, 40, "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent"),
            new ComponentRequirement("Reactor", 24, 2000, "MyObjectBuilder_BlueprintDefinition/ReactorComponent"),
            new ComponentRequirement("SmallTube", 1000, 2000, "MyObjectBuilder_BlueprintDefinition/SmallTube"),
            new ComponentRequirement("SolarCell", 128, 1024, "MyObjectBuilder_BlueprintDefinition/SolarCell"),
            new ComponentRequirement("SteelPlate", 10000, 20000, "MyObjectBuilder_BlueprintDefinition/SteelPlate"),
            new ComponentRequirement("Superconductor", 1000, 2000, "MyObjectBuilder_BlueprintDefinition/Superconductor"),
            new ComponentRequirement("Thrust", 960, 3840, "MyObjectBuilder_BlueprintDefinition/ThrustComponent")
        };

        public void Main2() {
            var lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD Assembler");
            var assembler = (IMyAssembler)GridTerminalSystem.GetBlockWithName("Assembler 2");
            var lines = new List<string>();
            var items = new List<MyProductionItem>();
            assembler.GetQueue(items);
            foreach (var item in items) { 
                lines.Add(item.BlueprintId.TypeId + "/" + item.BlueprintId.SubtypeId);
            }

            lcd.WriteText(string.Join("\n", lines));
        }

        public void Main(string argument, UpdateType updateSource) {

            if (argument == "make") {
                AssembleRecommendedComponents();
                return;
            }

            var lcd = default(IMyTextPanel);
            var lines = default(List<string>);
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);
            AllItems.Clear();

            foreach (var block in blocks) {
                for (var i = 0; i < block.InventoryCount; i++) { 
                    var inv = block.GetInventory(i);
                    var items = new List<MyInventoryItem>();
                    inv.GetItems(items);
                    AllItems.AddRange(items);
                }
            }

            // Inventory Screens
            foreach (var screen in InvScreens) {

                var lcdName = screen.Key;
                var listItem = screen.Value;
                lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(lcdName);

                if (lcd == null)
                    continue;

                lines = new List<string> {
                    lcd.CustomData,
                    new string('-', ScreenWidth)
                };

                foreach (var item in AllItems.GroupBy(i => i.Type).OrderBy(i => GetItemDescription(i.Key))) {
                    var itemType = item.Key;
                    var itemInfo = itemType.GetItemInfo();
                    if (listItem(itemInfo)) {
                        var description = GetItemDescription(itemType);
                        var amount = item.Sum(i => i.Amount.ToIntSafe()).ToString();
                        var usedWidth = description.Length + amount.Length + 1;
                        var spaces = Math.Max(ScreenWidth - usedWidth, 0);
                        if (usedWidth > ScreenWidth)
                            description = description.Substring(0, ScreenWidth - amount.Length - 1);
                        lines.Add(description + ":" + new string(' ', spaces) + amount);
                    }
                }
                lcd.WriteText(string.Join("\n", lines));
            }

            // Notes Screen
            lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(NoteScreenName);
            if (lcd != null) { 
                lines = new List<string> {
                    lcd.CustomData,
                    new string('-', ScreenWidth)
                };

                foreach (var note in Notes) {
                    if (note.IsActive()) {
                        lines.Add(note.GetNoteText());
                    }
                }

                lcd.WriteText(string.Join("\n", lines));
            }
        }

        public static int GetItemCount(MyItemType itemType) {

            var items = AllItems.Where(i => i.Type == itemType);
            if (items.Count() == 0)
                return 0;
            return items.Sum(i => i.Amount.ToIntSafe());
        }

        public static string GetItemDescription(MyItemType itemType) {
            var description = itemType.SubtypeId;
            if (description == "Stone" && itemType.GetItemInfo().IsIngot)
                description = "Gravel";
            return description;
        }

        public void AssembleRecommendedComponents() {
            var assemblers = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType(assemblers);
    
            var assembler = assemblers.FirstOrDefault(a => !a.CooperativeMode && a.Enabled);

            if (assembler == null)
                return;

            foreach (var requirement in Notes.OfType<ComponentRequirement>()) {
                if (requirement.IsActive()) {
                    assembler.AddQueueItem(MyDefinitionId.Parse(requirement.BlueprintID), (decimal)requirement.Shortage);
                }
            }
        }

        public class LCDNote {

            public Func<bool> IsActive = () => true;

            public Func<string> GetNoteText = () => "Generic Note";

            public LCDNote() { }

            public LCDNote(Func<bool> isActive, Func<string> noteText) {
                IsActive = isActive;
                GetNoteText = noteText;
            }
        }

        public class MineralRequirement : LCDNote {

            public MyItemType ItemType;
            public int Quantity;
            public float OreFactor;

            public MineralRequirement(string ingotName, int quantity, float oreFactor) {
                ItemType = MyItemType.MakeIngot(ingotName);
                Quantity = quantity;
                OreFactor = oreFactor;
                IsActive = IsMineralRequirementActive;
                GetNoteText = GetMineralRequirementNoteText;
            }

            private bool IsMineralRequirementActive() => GetItemCount(ItemType) < Quantity;

            private string GetMineralRequirementNoteText() {
                var shortage = Quantity - GetItemCount(ItemType);
                var ore = shortage / OreFactor;
                return "Collect " + shortage + " " + ItemType.SubtypeId + ", " + ore.ToString("0") + " Ore";
            }
        }

        public class ComponentRequirement : LCDNote {

            public MyItemType ItemType;
            public int MinQuantity;
            public int? MaxQuantity;
            public string BlueprintID;

            public int ItemCount => GetItemCount(ItemType);

            public int Shortage => MinQuantity - ItemCount;

            public ComponentRequirement(string componentName, int minQuantity, int? maxQuantity = null, string blueprintId = null) {
                ItemType = MyItemType.MakeComponent(componentName);
                MinQuantity = minQuantity;
                MaxQuantity = maxQuantity;
                BlueprintID = blueprintId;
                IsActive = IsComponentRequirementActive;
                GetNoteText = GetComponentRequirementNoteText;
            }

            private bool IsComponentRequirementActive() =>
                GetItemCount(ItemType) < MinQuantity || 
                (MaxQuantity.HasValue && GetItemCount(ItemType) > MaxQuantity);

            private string GetComponentRequirementNoteText() {
                var itemCount = ItemCount;
                var shortage = MinQuantity - itemCount;
                
                if (shortage > 0) {
                    return "Make " + shortage + " " + ItemType.SubtypeId + " (" + itemCount + ")";
                }
                else if(MaxQuantity.HasValue) {
                    var excess = itemCount - MaxQuantity.Value;
                    if (excess > 0) {
                        return "Unmake " + excess + " " + ItemType.SubtypeId;
                    }
                }

                return "";
            }
        }

        //=======================================================================================
        //////////////////////////END//////////////////////////////////////////
        //=======================================================================================
    }
}
using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SEMaterialReport { 

    public class Program : SEScriptTemplate {




        private const string OreType = "MyObjectBuilder_Ore";
        private const string IngotType = "MyObjectBuilder_Ingot";

        private List<IMyRefinery> Refineries;
        private List<IMyCargoContainer> Containers;
        private IMyTextPanel LCD;

        public Program() {
            Refineries = new List<IMyRefinery>();
            Containers = new List<IMyCargoContainer>();
            LCD = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD Materials");
        }

        public void Main(string argument, UpdateType updateSource) {

            if (!string.IsNullOrWhiteSpace(argument)) {
                var words = argument.Split(' ');
                var w1 = words.ElementAtOrDefault(0);
                if (w1 == "cycle") {
                    var w2 = string.Join(" ", words.Skip(1));
                    var refinery = Refineries.FirstOrDefault(r => r.CustomName == w2);
                    if (refinery == null) {
                        Echo("Refinery not found: " + w2);
                        return;
                    }

                    CycleRefinery(refinery);
                    Echo("Refinery cycled");
                }
                else {
                    Echo("Command not recognized: " + w1);
                    return;
                }
            }

            LCD.WriteText("", append: false);

            GridTerminalSystem.GetBlocksOfType(Refineries);
            GridTerminalSystem.GetBlocksOfType(Containers);

            MaterialData.ClearAmounts();

            var refineryTable = new TextTable();
            var allItems = new List<MyInventoryItem>();

            foreach (var refinery in Refineries) {
                var activeItem = "(Idle)";
                var refineryItems = new List<MyInventoryItem>();
                refinery.InputInventory.GetItems(refineryItems);
                if (refineryItems.Count > 0)
                    activeItem = ((int)refineryItems.First().Amount) + " " + refineryItems.First().Type.SubtypeId;
                var itemNames = new List<string>();
                foreach (var item in refineryItems) {
                    var material = MaterialData.AddAmount(item);
                    if (material.OreAmount > 0f)
                        itemNames.Add(material.OreName);
                }
                refineryItems.Clear();
                refinery.OutputInventory.GetItems(refineryItems);
                foreach (var item in refineryItems)
                    MaterialData.AddAmount(item);
                LCD.WriteText(refinery.CustomName + ": " + activeItem + "\n", append: true);
                if (itemNames.Count > 1)
                    LCD.WriteText(" + " + string.Join(", ", itemNames.Skip(1)) + "\n", append: true);
            }

            LCD.WriteText("\n", append: true);

            foreach (var container in Containers) {
                var containerItems = new List<MyInventoryItem>();
                var inventoryCount = container.InventoryCount;
                for (var i = 0; i < inventoryCount; i++) {
                    var inventory = container.GetInventory(i);
                    inventory.GetItems(containerItems);
                    allItems.AddRange(containerItems);
                }
            }


            foreach (var item in allItems)
                MaterialData.AddAmount(item);

            var table = new TextTable();
            table.AlignRightColumns.AddRange(new[] { 0, 2 });
            table.AddRow(" Ore", "Material", "Ingots");
            table.AddRow();
            foreach (var material in MaterialData.All)
                table.AddRow(
                    material.OreName != null ? material.OreAmount.ToString("0") : "",
                    material.DisplayName,
                    material.IngotName != null ? material.IngotAmount.ToString("0") : "");
            LCD.WriteText(table.ToString(), append: true);
        }

        private void CycleRefinery(IMyRefinery refinery) {
            var inv = refinery.InputInventory;
            inv.TransferItemTo(inv, 0, inv.ItemCount, true);
        }

        public class MaterialData {

            public readonly string DisplayName;
            public readonly string OreName;
            public readonly string IngotName;

            public float OreAmount;
            public float IngotAmount;

            public MaterialData(string displayName, string oreName, string ingotName) {
                DisplayName = displayName;
                OreName = oreName;
                IngotName = ingotName;
            }

            public static List<MaterialData> All = new List<MaterialData> {
                new MaterialData("Ice", "Ice", null),
                new MaterialData("Stone", "Stone", null),
                new MaterialData("Scrap", "Scrap", null),
                NewIngot("Iron"),
                NewIngot("Nickel"),
                NewIngot("Silicon"),
                NewIngot("Cobalt"),
                NewIngot("Magnesium"),
                NewIngot("Silver"),
                NewIngot("Gold"),
                NewIngot("Platinum"),
                NewIngot("Uranium"),
                new MaterialData("Gravel", null, "Stone"),
            };

            public static MaterialData NewIngot(string displayName, string oreName = null, string ingotName = null) => new MaterialData(displayName, oreName ?? displayName, ingotName ?? displayName);

            public static void ClearAmounts() => All.ForEach(m => {
                m.OreAmount = 0;
                m.IngotAmount = 0;
            });

            public static MaterialData AddAmount(MyInventoryItem item) {
                if (item.Type.TypeId == OreType) {
                    var material = All.FirstOrDefault(m => m.OreName == item.Type.SubtypeId);
                    if (material != null)
                        material.OreAmount += (float)item.Amount;
                    else if (item.Type.SubtypeId != "Ice") {
                        throw new ArgumentException("Invalid ore: " + item.Type.SubtypeId);
                    }
                    return material;
                }
                else if (item.Type.TypeId == IngotType) {
                    var material = All.FirstOrDefault(m => m.IngotName == item.Type.SubtypeId);
                    if (material != null)
                        material.IngotAmount += (float)item.Amount;
                    else {
                        throw new ArgumentException("Invalid ingot: " + item.Type.SubtypeId);
                    }
                    return material;
                }
                return null;
            }
        }

        public class TextTable {

            public int ColumnSpacing = 1;

            public List<Row> Rows = new List<Row>();

            public List<int> AlignRightColumns = new List<int>();

            public void AddRow(params string[] values) => Rows.Add(new Row { Values = values });

            public override string ToString() {

                var colWidths = new Dictionary<int, int>();
                foreach (var row in Rows) {
                    for (var col = 0; col < row.Values.Length; col++) {
                        var value = row.Values[col];
                        if (!colWidths.ContainsKey(col) || colWidths[col] < value.Length)
                            colWidths[col] = value.Length;
                    }
                }

                var rows = new List<string>();

                foreach (var row in Rows) {
                    var columns = new List<string>();
                    for (var col = 0; col < colWidths.Count; col++) {
                        var colWidth = colWidths[col];
                        var value = row.GetValue(col) ?? "";
                        if (AlignRightColumns.Contains(col))
                            columns.Add(new string(' ', colWidth - value.Length) + value);
                        else
                            columns.Add(value + new string(' ', colWidth - value.Length));
                    }
                    rows.Add(string.Join(new string(' ', ColumnSpacing), columns));
                }

                return string.Join(Environment.NewLine, rows);
            }

            public struct Row {

                public string[] Values;

                public string GetValue(int col) => Values?.ElementAtOrDefault(col);
            }
        }





    }
}
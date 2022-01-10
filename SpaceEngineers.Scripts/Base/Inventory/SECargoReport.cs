using System;
using System.Linq;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SECargoReport { 

    public class Program : SEScriptTemplate {

        private static Color EmptyColor = Color.White;
        private static Color HalfColor = new Color(255, 255, 127);
        private static Color FullColor = Color.Red;

        private List<IMyCargoContainer> Containers;
        private IMyTextPanel LCD;

        public Program() {
            Containers = new List<IMyCargoContainer>();
            LCD = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD Cargo");
        }

        public void Main(string argument, UpdateType updateSource) {

            GridTerminalSystem.GetBlocksOfType(Containers, c => c.CubeGrid.EntityId == Me.CubeGrid.EntityId);

            LCD.WriteText("\n-Cargo Capacity-\n");
    
            var maxVolume = 0f;
            var curVolume = 0f;

            foreach (var container in Containers) {
                var containerItems = new List<MyInventoryItem>();
                var inventoryCount = container.InventoryCount;
                for (var i = 0; i < inventoryCount; i++) {
                    var inventory = container.GetInventory(i);
                    maxVolume += (float)inventory.MaxVolume;
                    curVolume += (float)inventory.CurrentVolume;
                    var cargoName = container.CustomName;
                    if (cargoName.Length > 17)
                        cargoName = cargoName.Substring(0, 14) + "...";
                    var cubes = (int)Math.Round(17f * ((float)inventory.CurrentVolume / (float)inventory.MaxVolume));
                    var shades = 17 - cubes;
                    LCD.WriteText(cargoName + ":\n" + new string('█', cubes) + new string('░', shades) + "\n\n", append: true);
                }
            }

            var textTable = new TextTable();
            textTable.AddRow("Cur:", (curVolume * 1000).ToString("0"), "L");
            textTable.AddRow("Max:", (maxVolume * 1000).ToString("0"), "L");
            textTable.AlignRightColumns.Add(1);
            LCD.WriteText(textTable.ToString(), append: true);

            var fullness = curVolume / maxVolume;

            if (fullness < 0.5f)
                LCD.FontColor = new Color(Vector3.Lerp(EmptyColor, HalfColor, fullness * 2));
            else
                LCD.FontColor = new Color(Vector3.Lerp(HalfColor, FullColor, (fullness - 0.5f) * 2));
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
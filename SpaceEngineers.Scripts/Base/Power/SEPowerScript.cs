using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SEPowerScript { 

    class Program : SEScriptTemplate {

        public string PowerScreen = "LCD Power";

        void Main() {

            var terminalBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(terminalBlocks);

            PowerUserType.ClearUsage();
            foreach (var terminalBlock in terminalBlocks)
                PowerUserType.TrackPowerUser(terminalBlock);

            var lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(PowerScreen);
            var textTable = new TextTable();
            textTable.AlignRightColumns.AddRange(new[] { 0, 2 });
            foreach (var powerUserType in PowerUserType.All.OrderByDescending(t => t.GetTotalUsage()))
                textTable.AddRow(powerUserType.Users.Count.ToString() + "x", powerUserType.TypeName, (powerUserType.GetTotalUsage() / 1000f).ToString("0") + " kW");
            lcd.WriteText(textTable.ToString());
        }

        public class PowerUserType {

            public static List<PowerUserType> All = new List<PowerUserType>();

            public static PowerUserType GetOrAddType(IMyTerminalBlock terminalBlock) {
                var typeName = terminalBlock.BlockDefinition.SubtypeName;
                var powerUserType = All.FirstOrDefault(t => t.TypeName == typeName);
                if (powerUserType == null) {
                    powerUserType = new PowerUserType {
                        TypeName = typeName,
                        Users = new List<PowerUser>(),
                    };
                    All.Add(powerUserType);
                }
                return powerUserType;
            }

            public static void TrackPowerUser(IMyTerminalBlock terminalBlock) {

                if (terminalBlock is IMyBatteryBlock) {
                    var battery = (IMyBatteryBlock)terminalBlock;
                    if (battery.ChargeMode != ChargeMode.Recharge || battery.CurrentStoredPower >= battery.MaxStoredPower)
                        return;
                }

                var powerUser = new PowerUser {
                    Name = terminalBlock.CustomName,
                    IsWorking = terminalBlock.IsWorking
                };

                var infoLines = terminalBlock.DetailedInfo.Split('\n');
                foreach (var line in infoLines) {
                    var isCurrentInput = line.StartsWith("Current Input");
                    var isMaxRequiredInput = line.StartsWith("Max Required Input");
                    if (isCurrentInput || isMaxRequiredInput) {
                        var inputParts = line.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries)[1].Split(' ');
                        var usage = float.Parse(inputParts[0]);
                        var unit = inputParts[1];
                        if (unit == "kW")
                            usage *= 1000f;
                        else if (unit == "MW")
                            usage *= 1000000f;
                        if (isCurrentInput)
                            powerUser.CurrentInput = usage;
                        else if (isMaxRequiredInput)
                            powerUser.MaxRequiredInput = usage;
                    }
                }

                if (powerUser.CurrentInput.HasValue || powerUser.MaxRequiredInput.HasValue) {
                    var powerUserType = GetOrAddType(terminalBlock);
                    powerUserType.Users.Add(powerUser);
                }
            }

            public static void ClearUsage() {
                foreach (var powerUserType in All)
                    powerUserType.Users.Clear();
            }

            public string TypeName;
            public List<PowerUser> Users;

            public float GetTotalUsage() => Users.Count == 0 ? 0f : Users.Sum(u => u.GetUsage());

            public float GetAverageUsage() => Users.Count == 0 ? 0f : Users.Average(u => u.GetUsage());
        }

        public class PowerUser {
            public string Name;
            public float? CurrentInput;
            public float? MaxRequiredInput;
            public bool IsWorking;

            public float GetUsage() => (CurrentInput ?? 0f) + (MaxRequiredInput.HasValue && IsWorking ? MaxRequiredInput.Value : 0f);
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
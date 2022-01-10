using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SEBaseScript { 

    class Program : SEScriptTemplate {

        const int ScreenWidth = 25;

        const float LowPowerThreshold = 1.0f;

        public Dictionary<string, Func<MyItemInfo, bool>> InvScreens = new Dictionary<string, Func<MyItemInfo, bool>> {
            { "LCD Ores", i => i.IsOre },
            { "LCD Raw Materials", i => i.IsIngot },
            { "LCD Components", i => i.IsComponent },
        };

        public string[] AssemblerScreens = { "LCD Assembler" };

        public string[] PowerScreens = { "LCD Power" };

        void Main() {

            var sensor = (IMySensorBlock)GridTerminalSystem.GetBlockWithName("Base Scripts Sensor");
            if (sensor != null && !sensor.IsActive)
                return;

            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);

            var allItems = new List<MyInventoryItem>();
            var allQueued = new List<MyProductionItem>();
            var allPower = new List<IMyBatteryBlock>();
            var allPowerProducers = new List<IMyPowerProducer>();
            var allReactors = new List<IMyReactor>();

            var totalPowerStored = 0f;
            var totalPowerInput = 0f;
            var totalPowerOutput = 0f;

            foreach (var block in blocks) {
                for (var i = 0; i < block.InventoryCount; i++) { 
                    var inv = block.GetInventory(i);
                    var items = new List<MyInventoryItem>();
                    inv.GetItems(items);
                    allItems.AddRange(items);
                }
                if (block is IMyAssembler) {
                    var assembler = (IMyAssembler)block;
                    var queued = new List<MyProductionItem>();
                    assembler.GetQueue(queued);
                    allQueued.AddRange(queued);
                }
                if (block is IMyBatteryBlock && block.CubeGrid == Me.CubeGrid) {
                    var battery = (IMyBatteryBlock)block;
                    totalPowerStored += battery.CurrentStoredPower;
                    totalPowerInput += battery.CurrentInput;
                    totalPowerOutput += battery.CurrentOutput;
                    allPower.Add(battery);
                }
                if (block is IMyPowerProducer && block.CubeGrid == Me.CubeGrid && !(block is IMyBatteryBlock)) {
                    var producer = (IMyPowerProducer)block;
                    allPowerProducers.Add(producer);
                    if (block is IMyReactor)
                        allReactors.Add(block as IMyReactor);
                }
            }

            foreach (var screen in InvScreens) {
                var lcdName = screen.Key;
                var listItem = screen.Value;
                var lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(lcdName);

                if (lcd == null)
                    continue;

                var lines = new List<string> {
                    lcd.GetPublicTitle(),
                    new string('-', ScreenWidth)
                };
                foreach (var item in allItems.GroupBy(i => i.Type).OrderBy(i => GetItemDescription(i.Key))) {
                    var itemType = item.Key;
                    var itemInfo = itemType.GetItemInfo();
                    if (listItem(itemInfo)) {
                        var description = GetItemDescription(itemType);
                        var amount = item.Sum(i => i.Amount.ToIntSafe()).ToString();
                        var usedWidth = description.Length + amount.Length + 1;
                        var spaces = Math.Max(ScreenWidth - usedWidth, 0);
                        if (usedWidth > 25)
                            description = description.Substring(0, ScreenWidth - amount.Length - 1);
                        lines.Add(description + ":" + new string(' ', spaces) + amount);
                    }
                }
                lcd.WriteText(string.Join("\n", lines));
            }

            foreach (var screen in AssemblerScreens) {
                var lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(screen);

                if (lcd == null)
                    continue;

                var lines = new List<string> {
                    lcd.GetPublicTitle(),
                    new string('-', ScreenWidth)
                };
                foreach (var item in allQueued.GroupBy(i => i.BlueprintId)) {
                    var itemType = item.Key;
                    var description = itemType.SubtypeName;
                    var amount = item.Sum(i => i.Amount.ToIntSafe()).ToString();
                    var usedWidth = description.Length + amount.Length + 1;
                    var spaces = Math.Max(ScreenWidth - usedWidth, 0);
                    if (usedWidth > 25)
                        description = description.Substring(0, ScreenWidth - amount.Length - 1);
                    lines.Add(description + ":" + new string(' ', spaces) + amount);
                }
                lcd.WriteText(string.Join("\n", lines));
            }

            foreach (var screen in PowerScreens) {
                var lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(screen);

                if (lcd == null)
                    continue;

                var lines = new List<string> {
                    lcd.GetPublicTitle(),
                    new string('-', ScreenWidth)
                };

                foreach (var battery in allPower.OrderBy(b => b.CustomName)) {
                    lines.Add(battery.CustomName + ": " + battery.CurrentStoredPower);
                }

                lines.Add("Total Power: " + totalPowerStored);

                lines.Add("");

                foreach (var producerGroup in allPowerProducers.GroupBy(p => p.BlockDefinition)) {
                    lines.Add(producerGroup.Key.SubtypeName.Replace("LargeBlock", "") + ": " + producerGroup.Sum(p => p.CurrentOutput));
                }


                lines.Add("");

                lines.Add("Total Input: " + totalPowerInput);
                lines.Add("Total Output: " + totalPowerOutput);
                lines.Add("Net Change: " + (totalPowerInput - totalPowerOutput));

                lcd.WriteText(string.Join("\n", lines));
            }

            var lowPower = totalPowerStored <= LowPowerThreshold;

            foreach (var reactor in allReactors)
                if (reactor.Enabled != lowPower)
                    reactor.Enabled = lowPower;

            var refineries = new List<IMyRefinery>();
            GridTerminalSystem.GetBlocksOfType(refineries);

            foreach (var refinery in refineries) {
                if (refinery.Enabled == lowPower)
                    refinery.Enabled = !lowPower;
            }

            var gasGenerators = new List<IMyGasGenerator>();
            GridTerminalSystem.GetBlocksOfType(gasGenerators);

            foreach (var gasGenerator in gasGenerators) {
                if (gasGenerator.Enabled == lowPower)
                    gasGenerator.Enabled = !lowPower;
            }
        }

        public string GetItemDescription(MyItemType itemType) {
            var description = itemType.SubtypeId;
            if (description == "Stone" && itemType.GetItemInfo().IsIngot)
                description = "Gravel";
            return description;
        }

        


    }
}
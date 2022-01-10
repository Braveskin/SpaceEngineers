using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SECargoEmptier { 

    public class Program : SEScriptTemplate {

        private string[] InventoryBlocksToEmpty = new[] {
            "MiningModuleDrill 1",
            "MiningModuleDrill 2",
            "MiningModuleDrill 3",
            "MiningModuleCockpit",
            "MiningModuleCargo",
            "MiningModuleConnector",
        };

        private string[] ExceptionItems = new[] {
            "OxygenBottle",
            "HydrogenBottle"
        };

        public void Main(string argument, UpdateType updateSource) {

            var blocksToEmpty = new List<IMyTerminalBlock>();
            var inventoriesToEmpty = new List<IMyInventory>();
            foreach (var blockName in InventoryBlocksToEmpty) {
                var block = GridTerminalSystem.GetBlockWithName(blockName);
                if (block == null)
                    continue;
                for (var i = 0; i < block.InventoryCount; i++) {
                    var inventory = block.GetInventory(i);
                    if (inventory.CurrentVolume > 0)
                        inventoriesToEmpty.Add(inventory);
                }
            }

            if (inventoriesToEmpty.Count == 0)
                return;

            var containers = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType(containers);

            if (containers.Count == 0)
                return;

            foreach (var invToEmpty in inventoriesToEmpty) {
                var itemsToEmpty = new List<MyInventoryItem>();
                invToEmpty.GetItems(itemsToEmpty);
                foreach (var item in itemsToEmpty) {
                    if (ExceptionItems.Contains(item.Type.SubtypeId))
                        continue;
                    Echo("Transferring: " + item.Type.SubtypeId);
                    var transferred = false;
                    foreach (var container in containers) {
                        if (InventoryBlocksToEmpty.Contains(container.CustomName))
                            continue;
                        Echo(" ? " + container.CustomName);
                        for (var i = 0; i < container.InventoryCount; i++) {
                            var destinationInv = container.GetInventory(i);
                            transferred = invToEmpty.TransferItemTo(destinationInv, item);
                            if (transferred)
                                break;
                        }
                        if (transferred)
                            break;
                    }
                    if (!transferred)
                        Echo("Item " + item.Type.SubtypeId + " could not be transferred");
                }
            }

            Echo("Transfer process complete");
        }




    }
}
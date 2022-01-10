using System;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SELargeAirlockScript { 

    public class Program : SEScriptTemplate {



        public enum AirlockStatus {
            /// <summary>
            /// Inside door closed, outside door open, depressurize on
            /// </summary>
            Outside,
            /// <summary>
            /// Inside door closed, outside door closed, depressurize on
            /// </summary>
            OutsideClosing,
            /// <summary>
            /// Inside door closed, outside door closed, depressurize off
            /// </summary>
            Pressurize,
            /// <summary>
            /// Inside door open, outside door closed, depressurize off
            /// </summary>
            Inside,
            /// <summary>
            /// Inside door closed, outside door closed, depressurize off
            /// </summary>
            InsideClosing,
            /// <summary>
            /// Inside door closed, outside door closed, depressurize on
            /// </summary>
            Depressurize
        };

        public static AirlockStatus CurrentAirlockStatus;
        public static string InsideDoorGroupName = "Lower Hangar Doors";
        public static string OutsideDoorGroupName = "Upper Hangar Doors";
        public static string VentName = "Airlock Vent";
        public static string LCDName = "Airlock LCD";
        public static string LightName = "Interior Light (Airlock)";
        public static Color NormalColor = new Color(120, 177, 255);
        public static Color DangerColor = new Color(255, 0, 0);

        public Program() {

            var insideDoors = GridTerminalSystem.GetBlockGroupWithName(InsideDoorGroupName);
            var outsideDoors = GridTerminalSystem.GetBlockGroupWithName(OutsideDoorGroupName);
            var vent = (IMyAirVent)GridTerminalSystem.GetBlockWithName(VentName);

            CloseDoors(insideDoors);
            CloseDoors(outsideDoors);
            vent.Depressurize = true;
            SetLightsColor(DangerColor);

            CurrentAirlockStatus = AirlockStatus.Depressurize;
        }

        public void Main(string argument, UpdateType updateSource) {

            var insideDoors = GridTerminalSystem.GetBlockGroupWithName(InsideDoorGroupName);
            var outsideDoors = GridTerminalSystem.GetBlockGroupWithName(OutsideDoorGroupName);

            if (argument == "cycle") {
                if (CurrentAirlockStatus == AirlockStatus.Inside) {
                    CloseDoors(insideDoors);
                    CurrentAirlockStatus = AirlockStatus.InsideClosing;
                    SetLightsColor(DangerColor);
                }
                else if (CurrentAirlockStatus == AirlockStatus.Outside) {
                    CloseDoors(outsideDoors);
                    CurrentAirlockStatus = AirlockStatus.OutsideClosing;
                    SetLightsColor(DangerColor);
                }
                return;
            }

    

            var vent = (IMyAirVent)GridTerminalSystem.GetBlockWithName(VentName);
            var lcds = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(lcds, l => l.CustomName == LCDName);

            if (CurrentAirlockStatus == AirlockStatus.OutsideClosing && AreDoorsClosed(outsideDoors)) {
                vent.Depressurize = false;
                CurrentAirlockStatus = AirlockStatus.Pressurize;
                SetLightsColor(DangerColor);
            }
            else if (CurrentAirlockStatus == AirlockStatus.Pressurize && vent.GetOxygenLevel() >= 0.99f) {
                OpenDoors(insideDoors);
                CurrentAirlockStatus = AirlockStatus.Inside;
                SetLightsColor(NormalColor);
            }
            else if (CurrentAirlockStatus == AirlockStatus.InsideClosing && AreDoorsClosed(insideDoors)) {
                vent.Depressurize = true;
                CurrentAirlockStatus = AirlockStatus.Depressurize;
                SetLightsColor(DangerColor);
            }
            else if (CurrentAirlockStatus == AirlockStatus.Depressurize && vent.GetOxygenLevel() <= 0.01f) {
                OpenDoors(outsideDoors);
                CurrentAirlockStatus = AirlockStatus.Outside;
                SetLightsColor(DangerColor);
            }

            var text = 
                "Airlock Status: "+ CurrentAirlockStatus + "\n" +
                "Airlock O2 Level: " + Math.Round((vent.GetOxygenLevel() * 100)) + "%";

            foreach (var lcd in lcds) {
                lcd.WriteText(text);
            }
        }

        public void SetLightsColor(Color color) { 
            var lights = new List<IMyInteriorLight>();
            GridTerminalSystem.GetBlocksOfType(lights, l => l.CustomName == LightName);
            foreach (var light in lights) {
                light.Color = color;
            }
        }

        public void OpenDoors(IMyBlockGroup doorGroup) {
            var doors = new List<IMyAirtightHangarDoor>();
            doorGroup.GetBlocksOfType(doors);
            foreach (var door in doors) {
                door.OpenDoor();
            }
        }

        public void CloseDoors(IMyBlockGroup doorGroup) {
            var doors = new List<IMyAirtightHangarDoor>();
            doorGroup.GetBlocksOfType(doors);
            foreach (var door in doors) {
                door.CloseDoor();
            }
        }

        public bool AreDoorsClosed(IMyBlockGroup doorGroup) {
            var doors = new List<IMyAirtightHangarDoor>();
            doorGroup.GetBlocksOfType(doors);
            foreach (var door in doors) {
                if (door.Status != DoorStatus.Closed)
                    return false;
            }
            return true;
        }





    }
}
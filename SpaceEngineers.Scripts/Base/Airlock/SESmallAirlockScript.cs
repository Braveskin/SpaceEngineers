using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SESmallAirlockScript {

    public class Program : SEScriptTemplate {

        public enum AirlockStatus {
            /// <summary>
            /// Inside door locked, outside door open
            /// </summary>
            Outside,
            /// <summary>
            /// Inside door open, outside door locked
            /// </summary>
            Inside
        };

        public static AirlockStatus CurrentAirlockStatus;
        public static string InsideDoorName = "Sliding Door (Inside)";
        public static string OutsideDoorName = "Sliding Door (Outside)";

        public Program() {

            var insideDoor = (IMyAirtightSlideDoor)GridTerminalSystem.GetBlockWithName(InsideDoorName);
            var outsideDoor = (IMyAirtightSlideDoor)GridTerminalSystem.GetBlockWithName(OutsideDoorName);

            if (insideDoor.Status == DoorStatus.Open && outsideDoor.Status != DoorStatus.Open) {
                CurrentAirlockStatus = AirlockStatus.Inside;
            }
            else if (outsideDoor.Status == DoorStatus.Open && insideDoor.Status != DoorStatus.Open) {
                CurrentAirlockStatus = AirlockStatus.Outside;
            }
            else {
                insideDoor.OpenDoor();
                outsideDoor.CloseDoor();
                CurrentAirlockStatus = AirlockStatus.Inside;
            }
        }

        public void Main(string argument, UpdateType updateSource) {

            var insideDoor = (IMyAirtightSlideDoor)GridTerminalSystem.GetBlockWithName(InsideDoorName);
            var outsideDoor = (IMyAirtightSlideDoor)GridTerminalSystem.GetBlockWithName(OutsideDoorName);

            if (argument == "cycle") {
                if (CurrentAirlockStatus == AirlockStatus.Inside) {
                    insideDoor.CloseDoor();
                }
                else if (CurrentAirlockStatus == AirlockStatus.Outside) {
                    outsideDoor.CloseDoor();
                }
                return;
            }
            else if (argument == "inside") {
                outsideDoor.CloseDoor();
                return;
            }
            else if (argument == "outside") {
                insideDoor.CloseDoor();
                return;
            }

            if (CurrentAirlockStatus == AirlockStatus.Inside && insideDoor.Status == DoorStatus.Closed) {
                insideDoor.Enabled = false;
                outsideDoor.Enabled = true;
                outsideDoor.OpenDoor();
                CurrentAirlockStatus = AirlockStatus.Outside;
            }
            else if (CurrentAirlockStatus == AirlockStatus.Outside && outsideDoor.Status == DoorStatus.Closed) {
                outsideDoor.Enabled = false;
                insideDoor.Enabled = true;
                insideDoor.OpenDoor();
                CurrentAirlockStatus = AirlockStatus.Inside;
            }
        }





    }
}
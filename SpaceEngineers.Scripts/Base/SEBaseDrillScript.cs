using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SEBaseDrillScript {

    public class Program : SEScriptTemplate {

        // Setup Variables

        string cockpitName = "Miner Controls";
        int cockpitLcdIndex = 0;

        string rotorName = "Miner Rotor";
        string hingeName = "Miner Hinge";
        string[] pistonNames = { "Miner Piston" };

        // Runtime Variables

        IMyCockpit cockpit;
        IMyTextSurface surface;
        IMyMotorStator rotor;
        IMyMotorStator hinge;
        IEnumerable<IMyPistonBase> pistons;
        float hingeTargetAngle;

        public Program() {
            cockpit = (IMyCockpit) GridTerminalSystem.GetBlockWithName(cockpitName);
            surface = cockpit.GetSurface(cockpitLcdIndex);
            rotor = (IMyMotorStator) GridTerminalSystem.GetBlockWithName(rotorName);
            hinge = (IMyMotorStator) GridTerminalSystem.GetBlockWithName(hingeName);
            pistons = pistonNames.Select(pistonName => (IMyPistonBase) GridTerminalSystem.GetBlockWithName(pistonName));
            hingeTargetAngle = hinge.Angle * 180 / (float)Math.PI;
        }

        public void Main(string argument, UpdateType updateSource) {

            if (argument.StartsWith("hinge")) {
                hingeTargetAngle += float.Parse(argument.Split(' ')[1]);
                return;
            }
            else if (argument.StartsWith("piston")) {
                foreach (var piston in pistons) {
                    piston.Velocity = float.Parse(argument.Split(' ')[1]);
                }
                return;
            }

            var hingeCurAngle = hinge.Angle * 180 / Math.PI;
            if (hingeCurAngle > hingeTargetAngle + 1) {
                hinge.RotorLock = false;
                hinge.TargetVelocityRPM = -Math.Abs(hinge.TargetVelocityRPM);
            }
            else if (hingeCurAngle < hingeTargetAngle - 1) {
                hinge.RotorLock = false;
                hinge.TargetVelocityRPM = Math.Abs(hinge.TargetVelocityRPM);
            }
            else {
                hinge.RotorLock = true;
            }

            var text = 
                "Rotor Angle: " + (rotor.Angle * 180 / Math.PI).ToString("0.0") + "°\n" +
                "Hinge Angle: " + hingeCurAngle.ToString("0") + "° (tgt " + hingeTargetAngle.ToString("0") + ")\n" + 
                "Piston Length: " + pistons.Sum(p => p.CurrentPosition).ToString("0.0") + "m\n";

            surface.WriteText(text, false);


        }


    }
}
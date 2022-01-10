using System;
using System.Linq;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using VRage.Library;
using System.Text;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Common;
using Sandbox.Game;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineers.Scripts;

namespace SEPistonScript { 

    public class Program : SEScriptTemplate {

        const float RAISE_SPEED = 0.5f;
        const float LOWER_SPEED = 0.01f;

        void Main(string argument) {

            var pistons = new List<IMyPistonBase>();
            GridTerminalSystem.GetBlocksOfType(pistons);

            foreach (var piston in pistons) {
                var upwards = piston.CustomData == "up";
                if (argument == "raise")
                    piston.Velocity = upwards ? RAISE_SPEED : -RAISE_SPEED;
                else if (argument == "lower")
                    piston.Velocity = upwards ? -LOWER_SPEED : LOWER_SPEED;
            }
        }

        


    }
}
using System;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.Scripts {

    public abstract class SEScriptTemplate {

        protected IMyGridTerminalSystem GridTerminalSystem = null;
        protected IMyGridProgramRuntimeInfo Runtime = null;
        protected Action<string> Echo = null;
        protected IMyTerminalBlock Me = null;

    }
}
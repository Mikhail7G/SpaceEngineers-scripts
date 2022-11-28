using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces.Terminal;



namespace SpaceEngineers.TurretTester
{
    public sealed class Program : MyGridProgram
    {

        IMyTextPanel panel;

        public Program()
        {
            

        }

        public void Main(string args)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            panel = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
            panel.WriteText($"", false);

            GridTerminalSystem.GetBlocks(blocks);

            foreach(var block in blocks)
            {
                Echo(block.BlockDefinition.ToString());

                List<ITerminalAction> actions = new List<ITerminalAction>();
                block.GetActions(actions);

                var dict = Me.GetProperty("WcPbAPI")?.As<IReadOnlyDictionary<string, Delegate>>().GetValue(Me);
                if (dict == null) throw new Exception($"WcPbAPI failed to activate");
                else
                {
                    panel.WriteText($"\n{block.Name}", true);
                }

                foreach (var act in actions)
                {
                   // panel.WriteText($"\n{act.GetType()}", true);
                }
            }

        }

        public void Save()
        {

        }


      

       

    }
}
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
using static SpaceEngineers.MoverTester.Program.ShipMover.DebugAPI;
using Sandbox.Game.Entities.Blocks;

namespace SpaceEngineers.Turret
{
    public sealed class Program : MyGridProgram
    {
        IMyTextPanel textPanel;

        string font;
        float fontSize;

        public Program()
        {
            Echo("");

            textPanel = GridTerminalSystem.GetBlockWithName("LCD Panel") as IMyTextPanel;

            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        public void Main(string argument, UpdateType updateType)
        {

            font = textPanel.Font;
            fontSize = textPanel.FontSize;


            textPanel.WriteText("", false);


        }



    }
}


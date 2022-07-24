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

namespace SpaceEngineers.ShipManagers.DisplayTester
{
    public sealed class Program : MyGridProgram
    {

        /////////////////////////////////////////////////////////////
        IMyTextPanel debugPanel;


        public Program()
        {
           

        }

        public void Main(string args)
        {
            debugPanel = GridTerminalSystem.GetBlockWithName("LCDText") as IMyTextPanel;
            TextWrite();
            //BuilderWrite();
        }

        public void TextWrite()
        {
            debugPanel?.WriteText("", false);

            for(int i=0;i<500;i++)
            {
                debugPanel?.WriteText("\n"+i.ToString(), true);
            }
            debugPanel?.WriteText("\nINSTR:" + Runtime.CurrentInstructionCount.ToString(), true);
        }

        public void BuilderWrite()
        {
            StringBuilder build = new StringBuilder();
            debugPanel?.WriteText("", false);

            for (int i = 0; i < 500; i++)
            {
                // debugPanel?.WriteText("\n" + i.ToString(), true);
                build.AppendLine(i.ToString());
            }
            debugPanel?.WriteText(build, true);
            debugPanel?.WriteText("\nINSTR:" + Runtime.CurrentInstructionCount.ToString(), true);
        }





        ///////////////////////////////////////////////////////////
    }
}

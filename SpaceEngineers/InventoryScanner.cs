using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

namespace SpaceEngineers.Stuff
{
    public sealed class Program : MyGridProgram
    {


        List<IMyAssembler> assemblers;


        public Program()
        {

            assemblers = new List<IMyAssembler>();

            GridTerminalSystem.GetBlocksOfType(assemblers);

            Runtime.UpdateFrequency = UpdateFrequency.Once;

        }

        public void Main(string args)
        {

           foreach(var ass in assemblers)
            {
                ass.ClearQueue();
            }

        

        }

        public void Save()
        {

        }

        /// <summary>
        /// Перекладка слитков из печек 
        /// </summary>

      
        ///////////////


    }
}
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
using System.Threading;

namespace SpaceEngineers.Stuff
{
    public sealed class Program : MyGridProgram
    {

        TimeSpan timer;


        public Program()
        {
            timer += Runtime.TimeSinceLastRun;
        }

        public void Main(string args)
        {

            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            timer += Runtime.TimeSinceLastRun;

            Echo(timer.TotalMilliseconds.ToString());
            Echo(Runtime.TimeSinceLastRun.ToString());

        }

        public void Save()
        {

        }

    

      
        ///////////////


    }
}
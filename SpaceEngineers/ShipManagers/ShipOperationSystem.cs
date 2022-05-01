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

namespace SpaceEngineers.ShipManagers
{
    public sealed class Program : MyGridProgram
    {

        /// ///////START SCRIPT//////////////////

        IMyCockpit cockpit;
        List<IMyThrust> thrusters;
        List<IMyCargoContainer> containers;

        float shipMass = 0;

        int updateTick = 0;

        public Program()
        {
            thrusters = new List<IMyThrust>();
            containers = new List<IMyCargoContainer>();

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            FindComponetns();

        }

        public void Main(string args)
        {
            string argument = args.ToUpper();
            switch(argument)
            {
                case "INIT":
                    FindComponetns();
                    break;
            }

            SlowUpdateFunctions();

        }

        void FindComponetns()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, b => b.CubeGrid == Me.CubeGrid);
            cockpit = blocks.Where(b => b is IMyCockpit).First() as IMyCockpit;
            thrusters = blocks.Where(b => b is IMyThrust).Select(t => t as IMyThrust).ToList();
            containers = blocks.Where(b => b is IMyCargoContainer).Select(t => t as IMyCargoContainer).ToList();

        }

        void SlowUpdateFunctions()
        {
            updateTick++;
            if (updateTick > 100)
            {
                updateTick = 0;

                FindComponetns();
                EchoInfo();
                ShipInfo();
            }

        }

        void EchoInfo()
        {
            if (cockpit == null) 
            {
                Echo($"No cocpit");
            }

            if (thrusters?.Count == 0)
            {
                Echo($"No trusters");
            }
            else
            {
                Echo($"Total trusters {thrusters.Count}");
            }

            if (containers?.Count == 0)
            {
                Echo($"No containers");
            }
            else
            {
                Echo($"Total containers {containers.Count}");
            }

            Echo($"Ship mass: {shipMass}");

        }

        void ShipInfo()
        {
            shipMass = cockpit.CalculateShipMass().TotalMass;

          
        }

        /////////////END OF SCRIPT//////////////////////////
    }
       
}
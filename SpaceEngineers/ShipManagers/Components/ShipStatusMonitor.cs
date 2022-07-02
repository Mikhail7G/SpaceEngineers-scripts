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
using VRage.Game.ModAPI.Ingame.Utilities;

namespace SpaceEngineers.ShipManagers.ShipMonitor
{
    public sealed class Program : MyGridProgram
    {

        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        public ShipMonitor ShipComplexMonitor;

        public Program()
        {
            ShipComplexMonitor = new ShipMonitor(this);
            ShipComplexMonitor.Init();

        }


        public void Main(string args, UpdateType updateType)
        {
            Update();


        }

        public void Update()
        {
            ShipComplexMonitor.Update();
        }

        public class ShipMonitor
        {
            public string cargoLCDName = "CargoLCD";

            private Program program;

            private IMyTextPanel cargoPanel;

            private List<IMyCargoContainer> containers;
            private List<IMyBatteryBlock> batteries;
            private  List<IMyPowerProducer> generators;
          
            public ShipMonitor(Program mainProg)
            {
                program = mainProg;

                containers = new List<IMyCargoContainer>();
                batteries = new List<IMyBatteryBlock>();
                generators = new List<IMyPowerProducer>();

            }

            public void Init()
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                program.GridTerminalSystem.GetBlocksOfType(blocks);

                containers = blocks.Where(b => b is IMyCargoContainer)
                                    .Where(c => c.IsFunctional)
                                    .Where(c => c.CubeGrid == program.Me.CubeGrid)
                                    .Select(t => t as IMyCargoContainer).ToList();

                batteries = blocks.Where(b => b is IMyBatteryBlock)
                                    .Where(c => c.IsFunctional)
                                    .Where(c => c.CubeGrid == program.Me.CubeGrid)
                                    .Select(t => t as IMyBatteryBlock).ToList();

                generators = blocks.Where(b => b is IMyPowerProducer)
                                    .Where(c => c.IsFunctional)
                                    .Where(c => c.CubeGrid == program.Me.CubeGrid)
                                    .Select(t => t as IMyPowerProducer).ToList();

                cargoPanel = program.GridTerminalSystem.GetBlockWithName(cargoLCDName) as IMyTextPanel;
 

            }

            public void Update()
            {
                program.Echo($"Total" +
                           $"\nConts: {containers.Count}" +
                           $"\nBatt: {batteries.Count}" +
                           $"\nGens: {generators.Count}");
            }


        }
      

        ///END OF SCRIPT///////////////
    }

}

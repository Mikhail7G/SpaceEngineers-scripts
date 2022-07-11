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

namespace SpaceEngineers.ShipManagers.Components
{
    public sealed class Program : MyGridProgram
    {

        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        public ShipMonitor ShipComplexMonitor;

        public Program()
        {
            ShipComplexMonitor = new ShipMonitor(this);
            ShipComplexMonitor.Init();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

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
            public string cargoLCDName = "CargoShipLCD";



            string largeNanoName = "SELtdLargeNanobotDrillSystem";
            string smallNanoName = "SELtdSmallNanobotDrillSystem";

            Program program;

            IMyTextPanel cargoPanel;

            List<IMyCargoContainer> containers;
            List<IMyBatteryBlock> batteries;
            List<IMyPowerProducer> generators;
            List<IMyTerminalBlock> nanoDrill;
            Dictionary<string, int> oreList;

            public ShipMonitor(Program mainProg)
            {
                program = mainProg;

                containers = new List<IMyCargoContainer>();
                batteries = new List<IMyBatteryBlock>();
                generators = new List<IMyPowerProducer>();
                nanoDrill = new List<IMyTerminalBlock>();
                oreList = new Dictionary<string, int>();

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

                if (program.Me.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == largeNanoName).ToList();
                }
                else if (program.Me.CubeGrid.GridSizeEnum == MyCubeSize.Small)
                {
                    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == smallNanoName).ToList();
                }


                cargoPanel = program.GridTerminalSystem.GetBlockWithName(cargoLCDName) as IMyTextPanel;


            }

            public void Update()
            {
                program.Echo($"Total" +
                           $"\nConts: {containers.Count}" +
                           $"\nBatt: {batteries.Count}" +
                           $"\nGens: {generators.Count}" +
                           $"\nNanoDrills:{nanoDrill.Count}");

                FindOres();
            }

            public void FindOres()
            {
                var containerInventory = containers.Select(c => c.GetInventory(0));
                oreList.Clear();

                foreach (var inventory in containerInventory)
                {
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    inventory.GetItems(items);

                    foreach (var item in items)
                    {
                        if (item.Type.TypeId == "MyObjectBuilder_Ore")
                        {
                            if (oreList.ContainsKey(item.Type.SubtypeId))
                            {
                                oreList[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                            }
                            else
                            {
                                oreList.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                            }
                        }
                    }
                }
            }

            public void PrintOres()
            {
                if (cargoPanel == null)
                    return;
            }


        }


        ///END OF SCRIPT///////////////
    }

}

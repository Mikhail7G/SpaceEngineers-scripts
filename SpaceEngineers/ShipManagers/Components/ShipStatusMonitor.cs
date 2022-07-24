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

namespace SpaceEngineers.ShipManagers.Components.ShipMonitor
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

            string cargoLCDName = "CargoShipLCD";
            string drillLCDName = "DrillShipLCD";
            string largeNanoName = "SELtdLargeNanobotDrillSystem";
            string smallNanoName = "SELtdSmallNanobotDrillSystem";

            double freeCargoStorageVolume;
            double totalCargoStorageVolume;
            double cargoPrecentageVolume;

            Program program;

            IMyTextPanel cargoPanel;
            IMyTextPanel drillPanel;

            List<MyInventoryItem> oreItems;
            List<IMyCargoContainer> containers;
            List<IMyBatteryBlock> batteries;
            List<IMyPowerProducer> generators;
            List<IMyTerminalBlock> nanoDrill;
            Dictionary<string, int> ores;

            List<List<object>> miningFields;
            //Dictionary<string, double> miningTargets; TimeSpan
            Dictionary<string, OreMiningSpeedData> miningTargets;

            TimeSpan miningTime;

            public ShipMonitor(Program mainProg)
            {
                program = mainProg;

                containers = new List<IMyCargoContainer>();
                batteries = new List<IMyBatteryBlock>();
                generators = new List<IMyPowerProducer>();
                nanoDrill = new List<IMyTerminalBlock>();
                oreItems = new List<MyInventoryItem>();
                ores = new Dictionary<string, int>();
                miningFields = new List<List<object>>();

               // miningTargets = new Dictionary<string, double>();
                miningTargets = new Dictionary<string, OreMiningSpeedData>();

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
                drillPanel = program.GridTerminalSystem.GetBlockWithName(drillLCDName) as IMyTextPanel;

            }

            public void Update()
            {
                program.Echo($"Total" +
                           $"\nConts: {containers.Count}" +
                           $"\nBatt: {batteries.Count}" +
                           $"\nGens: {generators.Count}" +
                           $"\nNanoDrills:{nanoDrill.Count}");

                FindOres();
                PrintOres();

                GetDrillData();
                PrintMiningOres();
            }

            public void FindOres()
            {
                var containerInventory = containers.Select(c => c.GetInventory(0));
                freeCargoStorageVolume = containerInventory.Sum(i => i.CurrentVolume.ToIntSafe());
                totalCargoStorageVolume = containerInventory.Sum(i => i.MaxVolume.ToIntSafe());

                cargoPrecentageVolume = Math.Round(((double)freeCargoStorageVolume / (double)totalCargoStorageVolume) * 100, 1);

                ores.Clear();

                foreach (var inventory in containerInventory)
                {
                    inventory.GetItems(oreItems);

                    foreach (var item in oreItems)
                    {
                        if (item.Type.TypeId == "MyObjectBuilder_Ore")
                        {
                            if (ores.ContainsKey(item.Type.SubtypeId))
                            {
                                ores[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                            }
                            else
                            {
                                ores.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                            }
                        }
                    }
                    oreItems.Clear();
                }
            }

            public void PrintOres()
            {
                if (cargoPanel == null)
                    return;

                cargoPanel?.WriteText("", false);
                cargoPanel?.WriteText("<--------------Cargo--------------->" +
                                      $"\nPayload: {cargoPrecentageVolume} %" +
                                      $"\n<--------------Ores--------------->", true);

                foreach(var key in ores)
                {
                    cargoPanel?.WriteText($"\n{key.Key} x {key.Value}", true);
                }
            }

            public void GetDrillData()
            {
                miningTargets.Clear();

                foreach (var drill in nanoDrill)
                {
                    if ((drill == null) && (drill.Closed))
                        continue;

                    if (drill.IsWorking == false)
                        continue;

                    miningFields.Clear();
                    miningFields = drill.GetValue<List<List<object>>>("Drill.PossibleDrillTargets");

                    foreach(var mining in miningFields)
                    {
                        string oreName = mining[3].ToString().Remove(0, 40);
                        string cuant = mining[4].ToString();

                        double cuantToDouble = 0;

                        if(double.TryParse(cuant,out cuantToDouble))
                        {

                        }

                        if (miningTargets.ContainsKey(oreName))
                        {
                           // miningTargets[oreName].Previous = miningTargets[oreName].Current;
                            miningTargets[oreName].Current += cuantToDouble;
                        }
                        else
                        {
                            miningTargets.Add(oreName, new OreMiningSpeedData { Current = cuantToDouble, Previous = 0 });
                        }
                    }
                }
            }

            public void PrintMiningOres()
            {
                if (drillPanel == null)
                    return;

                drillPanel?.WriteText("", false);
                drillPanel?.WriteText("<------Mining Targets-------->", true);

                foreach(var mining in miningTargets)
                {
                    drillPanel?.WriteText($"\n{mining.Key} x {mining.Value.Current} m3",true);
                }
            }

            public class OreMiningSpeedData
            {
                public double Current;
                public double Previous;
            }

        }/////////////////


        ///END OF SCRIPT///////////////
    }

}

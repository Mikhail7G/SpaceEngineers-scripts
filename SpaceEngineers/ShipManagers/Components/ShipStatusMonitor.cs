using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        //Lcd's
        string cargoLCDName = "CargoShipLCD";
        string drillLCDName = "DrillShipLCD";

        string oreContainersName = "Ore";

        string largeNanoName = "SELtdLargeNanobotDrillSystem";//NOT EDIT
        string smallNanoName = "SELtdSmallNanobotDrillSystem";//NOT EDIT

        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        public ShipMonitor ShipComplexMonitor;

        MyIni dataSystem;

        public Program()
        {
            dataSystem = new MyIni();

            ShipComplexMonitor = new ShipMonitor(this)
            {
                CargoLCDName = cargoLCDName,
                DrillLCDName = drillLCDName,
                LargeNanoName = largeNanoName,
                SmallNanoName = smallNanoName,
                OreContainersName = oreContainersName
            };

            ShipComplexMonitor.Init();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

        }


        public void Main(string args, UpdateType updateType)
        {
            Update();

            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                ShipComplexMonitor.Command(args);
        }

        public void Update()
        {
            ShipComplexMonitor.Update();
        }

        public class ShipMonitor
        {

            public string CargoLCDName = "CargoShipLCD";
            public string DrillLCDName = "DrillShipLCD";

            public string OreContainersName = "Ore";

            public string LargeNanoName = "SELtdLargeNanobotDrillSystem";
            public string SmallNanoName = "SELtdSmallNanobotDrillSystem";

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
            List<IMyShipConnector> connectors;
            Dictionary<string, int> ores;

            List<List<object>> miningFields;
            //Dictionary<string, double> miningTargets; TimeSpan
            Dictionary<string, OreMiningFieldData> miningTargets;

            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"Id=(\d+)");

            //TimeSpan miningTime;

            public ShipMonitor(Program mainProg)
            {
                program = mainProg;

                containers = new List<IMyCargoContainer>();
                batteries = new List<IMyBatteryBlock>();
                generators = new List<IMyPowerProducer>();
                nanoDrill = new List<IMyTerminalBlock>();
                oreItems = new List<MyInventoryItem>();
                connectors = new List<IMyShipConnector>();
                ores = new Dictionary<string, int>();
                miningFields = new List<List<object>>();

               // miningTargets = new Dictionary<string, double>();
                miningTargets = new Dictionary<string, OreMiningFieldData>();

            }

            /// <summary>
            /// Выполнение комманд от пользователя
            /// </summary>
            public void Command(string command)
            {
                string com = command.ToUpper();

                switch(com)
                {
                    case "UNLOAD":
                        UnloadOre();
                        break;
                }
            }

            public void Init()
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                program.GridTerminalSystem.GetBlocksOfType(blocks, (IMyTerminalBlock b) => b.CubeGrid == program.Me.CubeGrid);

                containers = blocks.Where(b => b is IMyCargoContainer)
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyCargoContainer).ToList();

                batteries = blocks.Where(b => b is IMyBatteryBlock)
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyBatteryBlock).ToList();

                generators = blocks.Where(b => b is IMyPowerProducer)
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyPowerProducer).ToList();

                connectors = blocks.Where(b => b is IMyShipConnector)
                                  .Where(c => c.IsFunctional)
                                  .Select(t => t as IMyShipConnector).ToList();

                if (program.Me.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == LargeNanoName).ToList();
                }
                else if (program.Me.CubeGrid.GridSizeEnum == MyCubeSize.Small)
                {
                    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == SmallNanoName).ToList();
                }

                cargoPanel = blocks.Where(b => b is IMyTextPanel && b.CustomName.Contains(CargoLCDName))
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyTextPanel).FirstOrDefault();

                drillPanel = blocks.Where(b => b is IMyTextPanel && b.CustomName.Contains(DrillLCDName))
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyTextPanel).FirstOrDefault();


                if (cargoPanel != null)
                {
                    cargoPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    cargoPanel.FontSize = 1;
                }

                if (drillPanel != null)
                {
                    drillPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    drillPanel.FontSize = 1;
                }

            }

            public void Update()
            {
                program.Echo($"Total" +
                           $"\nConts: {containers.Count}" +
                           $"\nBatt: {batteries.Count}" +
                           $"\nGens: {generators.Count}" +
                           $"\nNanoDrills:{nanoDrill.Count}");

                ScanCargo();
                PrintOres();

                GetDrillData();
                PrintMiningOres();
            }

            public void ScanCargo()
            {
                if (cargoPanel == null)
                    return;

                var containerInventory = containers.Select(c => c.GetInventory(0));
                freeCargoStorageVolume = containerInventory.Sum(i => i.CurrentVolume.ToIntSafe());
                totalCargoStorageVolume = containerInventory.Sum(i => i.MaxVolume.ToIntSafe());

                cargoPrecentageVolume = Math.Round((double)freeCargoStorageVolume / (double)totalCargoStorageVolume * 100, 1);

                ores.Clear();

                foreach (var inventory in containerInventory)
                {
                    inventory.GetItems(oreItems);

                    foreach (var item in oreItems)
                    {
                        if (item.Type.TypeId == "MyObjectBuilder_Ore")//Руды
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
                cargoPanel?.WriteText("<------------------Cargo------------------->" +
                                      $"\nPayload: {cargoPrecentageVolume} %" +
                                      $"\n<------------------Ores------------------>", true);

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

                    foreach (var mining in miningFields)
                    {

                        string fullName = mining[0].ToString();

                       
                        System.Text.RegularExpressions.Match match = regex.Match(fullName);

                        string oreName = mining[3].ToString().Remove(0, 40); 
                        string cuant = mining[4].ToString();

                        if (match.Success)
                        {
                            double cuantToDouble = 0;

                        
                            if (double.TryParse(cuant, out cuantToDouble))
                            {

                            }

                            if (miningTargets.ContainsKey(oreName))
                            {
                                // miningTargets[oreName].Previous = miningTargets[oreName].Current;
                                //if (miningTargets[oreName].FieldId != match.Value)
                                miningTargets[oreName].Current += cuantToDouble;
                            }
                            else
                            {
                                miningTargets.Add(oreName, new OreMiningFieldData { FieldId = match.Value, Current = cuantToDouble, Previous = 0 });
                            }
                        }
                    }
                }
            }

            public void PrintMiningOres()
            {
                if (drillPanel == null)
                    return;

                drillPanel?.WriteText("", false);
                drillPanel?.WriteText("<------------AVG Mining Targets-------------->", true);

                foreach(var mining in miningTargets)
                {
                    drillPanel?.WriteText($"\n{mining.Key} x {mining.Value.Current} m3",true);
                }
            }

            /// <summary>
            /// Выгрузка руды в контейнеры базы
            /// </summary>
            public void UnloadOre()
            {
                var connected = connectors.Where(c => c.Status == MyShipConnectorStatus.Connected);
                if (!connected.Any())
                    return;

                List<IMyCargoContainer> blocks = new List<IMyCargoContainer>();
                program.GridTerminalSystem.GetBlocksOfType(blocks, (IMyCargoContainer b) => b.CubeGrid != program.Me.CubeGrid && b.CustomName.Contains(OreContainersName));
                var availInv = blocks.Where(b => b.IsFunctional)
                                     .Select(b => b.GetInventory(0))
                                     .Where(i => !i.IsFull);

                if (!availInv.Any())
                    return;

                var myInventroy = containers.Select(c => c.GetInventory(0));

                foreach(var inv in myInventroy)
                {
                    var count = inv.ItemCount;

                    for (int i = 0; i <= count; i++)
                    {
                        var item = inv.GetItemAt(i);

                        if (item == null)
                            continue;

                        if (item.Value.Type.TypeId != "MyObjectBuilder_Ore")
                            continue;

                        foreach(var targetInv in availInv)
                        {
                            if (inv.TransferItemTo(targetInv, i, null, true))
                            {
                                if (item == null)
                                    break;
                            }  
                        }
                    }
                }

            }


            public class OreMiningFieldData
            {
                public string FieldId = "";
                public double Current;
                public double Previous;
            }

        }/////////////////


        ///END OF SCRIPT///////////////
    }

}

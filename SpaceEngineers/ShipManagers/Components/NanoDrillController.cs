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
using Sandbox.Game.Entities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.Arm;

namespace SpaceEngineers.ShipManagers.Components.NanodrillController
{
    public sealed class Program : MyGridProgram
    {

        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        public ShipMonitor ShipComplexMonitor;
        public Nanodrill NanodrillController;

        MyIni dataSystem;

        public Program()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, (IMyTerminalBlock b) => b.CubeGrid == Me.CubeGrid);

            dataSystem = new MyIni();

            ShipComplexMonitor = new ShipMonitor(this)
            {

            };
            ShipComplexMonitor.Init(blocks);

            NanodrillController = new Nanodrill(this);
            NanodrillController.Init(blocks);
            NanodrillController.GetDrillParams();


            Runtime.UpdateFrequency = UpdateFrequency.Update100;

        }


        public void Main(string args, UpdateType updateType)
        {
            Update();

            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                ShipComplexMonitor.Command(args);
                NanodrillController.Command(args);
            }
        }

        public void Update()
        {
            ShipComplexMonitor.Update();

        }

        public class ShipMonitor
        {
            public string StatusLCDName = "StatusShipLCD";
            public string CargoLCDName = "CargoShipLCD";
            //public string DrillLCDName = "DrillShipLCD";

            public string OreContainersName = "Ore";

            //public string LargeNanoName = "SELtdLargeNanobotDrillSystem";
            //public string SmallNanoName = "SELtdSmallNanobotDrillSystem";

            double freeCargoStorageVolume;
            double totalCargoStorageVolume;
            double cargoPrecentageVolume;

            double MTOW;//макс взлетный вес корабля
            double availTakeoffThrust;
            double gravity;
            float mass;

            Program program;

            IMyTextPanel statusPanel;
            IMyTextPanel cargoPanel;

            IMyShipController shipController;

            List<MyInventoryItem> oreItems;
            List<IMyCargoContainer> containers;
            List<IMyBatteryBlock> batteries;
            List<IMyPowerProducer> generators;
           // List<IMyTerminalBlock> nanoDrill;
            List<IMyShipConnector> connectors;
            Dictionary<string, int> ores;

            //Двигатели
            List<IMyThrust> thrusters;
            List<IMyThrust> upThrusters = new List<IMyThrust>();
            List<IMyThrust> downThrusters = new List<IMyThrust>();
            List<IMyThrust> leftThrusters = new List<IMyThrust>();
            List<IMyThrust> rightThrusters = new List<IMyThrust>();
            List<IMyThrust> forwardThrusters = new List<IMyThrust>();
            List<IMyThrust> backwardThrusters = new List<IMyThrust>();

            Dictionary<Base6Directions.Direction, double> engThrust = new Dictionary<Base6Directions.Direction, double>() { { Base6Directions.Direction.Backward, 0 }, { Base6Directions.Direction.Down, 0 }, { Base6Directions.Direction.Forward, 0 }, { Base6Directions.Direction.Left, 0 }, { Base6Directions.Direction.Right, 0 }, { Base6Directions.Direction.Up, 0 }, };


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
                oreItems = new List<MyInventoryItem>();
                connectors = new List<IMyShipConnector>();
                ores = new Dictionary<string, int>();
                miningFields = new List<List<object>>();

                miningTargets = new Dictionary<string, OreMiningFieldData>();

            }

            /// <summary>
            /// Выполнение комманд от пользователя
            /// </summary>
            public void Command(string command)
            {
                string com = command.ToUpper();

                switch (com)
                {
                    case "UNLOAD":

                        break;
                }
            }

            public void Init(List<IMyTerminalBlock> shipBlocks)
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                blocks = shipBlocks;

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

                thrusters = blocks.Where(b => b is IMyThrust)
                                .Where(c => c.IsFunctional)
                                .Select(t => t as IMyThrust).ToList();

                shipController = blocks.Where(b => b is IMyShipController)
                                     .Where(c => c.IsFunctional)
                                     .Select(t => t as IMyShipController).FirstOrDefault();

                if (shipController == null)
                    return;


                Matrix engRot = new Matrix();
                Matrix cocpitRot = new Matrix();

                shipController.Orientation.GetMatrix(out cocpitRot);

                for (int i = 0; i < thrusters.Count; i++)
                {
                    IMyThrust Thrust = thrusters[i];
                    Thrust.Orientation.GetMatrix(out engRot);
                    //Y
                    if (engRot.Backward == cocpitRot.Up)
                    {
                        upThrusters.Add(Thrust);
                    }
                    else if (engRot.Backward == cocpitRot.Down)
                    {
                        downThrusters.Add(Thrust);
                    }
                    //X
                    else if (engRot.Backward == cocpitRot.Left)
                    {
                        leftThrusters.Add(Thrust);
                    }
                    else if (engRot.Backward == cocpitRot.Right)
                    {
                        rightThrusters.Add(Thrust);
                    }
                    //Z
                    else if (engRot.Backward == cocpitRot.Forward)
                    {
                        forwardThrusters.Add(Thrust);
                    }
                    else if (engRot.Backward == cocpitRot.Backward)
                    {
                        backwardThrusters.Add(Thrust);
                    }
                }

                //if (program.Me.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                //{
                //    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == LargeNanoName).ToList();
                //}
                //else if (program.Me.CubeGrid.GridSizeEnum == MyCubeSize.Small)
                //{
                //    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == SmallNanoName).ToList();
                //}

                cargoPanel = blocks.Where(b => b is IMyTextPanel && b.CustomName.Contains(CargoLCDName))
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyTextPanel).FirstOrDefault();

                //drillPanel = blocks.Where(b => b is IMyTextPanel && b.CustomName.Contains(DrillLCDName))
                //                   .Where(c => c.IsFunctional)
                //                   .Select(t => t as IMyTextPanel).FirstOrDefault();

                statusPanel = blocks.Where(b => b is IMyTextPanel && b.CustomName.Contains(StatusLCDName))
                                    .Where(c => c.IsFunctional)
                                    .Select(t => t as IMyTextPanel).FirstOrDefault();


                if (cargoPanel != null)
                {
                    cargoPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    cargoPanel.FontSize = 1;
                }

                //if (drillPanel != null)
                //{
                //    drillPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                //    drillPanel.FontSize = 1;
                //}

                if (statusPanel != null)
                {
                    statusPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    statusPanel.FontSize = 1;
                }


            }

            public void Update()
            {
                program.Echo($"Total" +
                           $"\nConts: {containers.Count}" +
                           $"\nBatt: {batteries.Count}" +
                           $"\nGens: {generators.Count}");

                ScanCargo();
                PrintOres();

                GetShipData();
                PrintShipData();
            }

            public void GetShipData()
            {
                RefreshEngines();

                gravity = shipController.GetNaturalGravity().Length();
                mass = shipController.CalculateShipMass().PhysicalMass;

                if (gravity > 0.5f)
                {
                    MTOW = engThrust[Base6Directions.Direction.Up] / gravity;
                }
                else
                {
                    MTOW = engThrust[Base6Directions.Direction.Up];
                }

                availTakeoffThrust = 100 - ((mass * 100) / MTOW);


            }

            public void PrintShipData()
            {
                if (statusPanel == null)
                    return;

                statusPanel?.WriteText("", false);
                statusPanel?.WriteText("<-------------Ship Status------------->" +
                                      $"\nMTOW/Mass: {Math.Round(MTOW, 1)} / {mass} kg" +
                                      $"\nAvail take off thrust: {Math.Round(availTakeoffThrust, 1)} %", true);
            }

            void RefreshEngines()
            {
                engThrust[Base6Directions.Direction.Forward] = forwardThrusters.Sum(t => t.MaxEffectiveThrust);
                engThrust[Base6Directions.Direction.Backward] = backwardThrusters.Sum(t => t.MaxEffectiveThrust);

                engThrust[Base6Directions.Direction.Up] = upThrusters.Sum(t => t.MaxEffectiveThrust);
                engThrust[Base6Directions.Direction.Down] = downThrusters.Sum(t => t.MaxEffectiveThrust);

                engThrust[Base6Directions.Direction.Left] = leftThrusters.Sum(t => t.MaxEffectiveThrust);
                engThrust[Base6Directions.Direction.Right] = rightThrusters.Sum(t => t.MaxEffectiveThrust);
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
                cargoPanel?.WriteText($"Payload: {cargoPrecentageVolume} %" +
                                      $"\n<------------------Ores------------------>", true);

                foreach (var key in ores)
                {
                    cargoPanel?.WriteText($"\n{key.Key} x {key.Value}", true);
                }
            }

            //public void GetDrillData()
            //{
            //    miningTargets.Clear();

            //    foreach (var drill in nanoDrill)
            //    {
            //        if ((drill == null) && drill.Closed)
            //            continue;

            //        if (drill.IsWorking == false)
            //            continue;

            //        miningFields.Clear();
            //        miningFields = drill.GetValue<List<List<object>>>("Drill.PossibleDrillTargets");

            //        foreach (var mining in miningFields)
            //        {

            //            string fullName = mining[0].ToString();


            //            System.Text.RegularExpressions.Match match = regex.Match(fullName);

            //            string oreName = mining[3].ToString().Remove(0, 40);
            //            string cuant = mining[4].ToString();

            //            if (match.Success)
            //            {
            //                double cuantToDouble = 0;


            //                if (double.TryParse(cuant, out cuantToDouble))
            //                {

            //                }

            //                if (miningTargets.ContainsKey(oreName))
            //                {
            //                    // miningTargets[oreName].Previous = miningTargets[oreName].Current;
            //                    //if (miningTargets[oreName].FieldId != match.Value)
            //                    miningTargets[oreName].Current += cuantToDouble;
            //                }
            //                else
            //                {
            //                    miningTargets.Add(oreName, new OreMiningFieldData { FieldId = match.Value, Current = cuantToDouble, Previous = 0 });
            //                }
            //            }
            //        }
            //    }
            //}

            //public void PrintMiningOres()
            //{
            //    if (drillPanel == null)
            //        return;

            //    drillPanel?.WriteText("", false);
            //    drillPanel?.WriteText("<------------AVG Mining Targets-------------->", true);

            //    foreach (var mining in miningTargets)
            //    {
            //        drillPanel?.WriteText($"\n{mining.Key} x {mining.Value.Current / nanoDrill.Count} m3", true);
            //    }
            //}


            public class OreMiningFieldData
            {
                public string FieldId = "";
                public double Current;
                public double Previous;
            }

        }/////////////////


        public class Nanodrill
        {
            public string DrillLCDName = "DrillShipLCD";

            string LargeNanoName = "SELtdLargeNanobotDrillSystem";
            string SmallNanoName = "SELtdSmallNanobotDrillSystem";

            int maxDrillOffset = 25;
            float smallGridMod = 0.5f;
            float largeGridMod = 2.5f;
            float distMod = 1;

            Program program;

            IMyTextPanel drillPanel;

            IMyShipController shipController;
            List<IMyTerminalBlock> nanoDrill;


            public Nanodrill(Program mainProg)
            {
                program = mainProg;
                nanoDrill = new List<IMyTerminalBlock>();
            }

            public void Init(List<IMyTerminalBlock> shipBlocks)
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                blocks = shipBlocks;

                shipController = blocks.Where(b => b is IMyShipController)
                                       .Where(c => c.IsFunctional)
                                       .Select(t => t as IMyShipController).FirstOrDefault();

                if (program.Me.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    distMod = largeGridMod;
                    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == LargeNanoName).ToList();
                }
                else if (program.Me.CubeGrid.GridSizeEnum == MyCubeSize.Small)
                {
                    distMod = smallGridMod;
                    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == SmallNanoName).ToList();
                }

                drillPanel = blocks.Where(b => b is IMyTextPanel && b.CustomName.Contains(DrillLCDName))
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyTextPanel).FirstOrDefault();

                if (drillPanel != null)
                {
                    drillPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    drillPanel.FontSize = 1;
                }

            }

            /// <summary>
            /// Выполнение комманд от пользователя
            /// </summary>
            public void Command(string command)
            {
                string com = command.ToUpper();

                switch (com)
                {
                    case "RESET":
                        ResetDrills();
                        break;

                    case "INIT":
                        InitDrills();
                        break;


                    case "FWD":
                        MoveFWD();
                        break;

                    case "BCK":
                        MoveBCK();
                        break;

                    case "UP":
                        MoveUp();
                        break;

                    case "DN":
                        MoveDown();
                        break;

                    case "LT":
                        MoveLeft();
                        break;

                    case "RT":
                        MoveRirght();
                        break;
                }
            }

            public void ResetDrills()
            {
                foreach (var drill in nanoDrill)
                {
                    drill.SetValue<Single>("Drill.AreaOffsetLeftRight", 0);
                    drill.SetValue<Single>("Drill.AreaOffsetUpDown", 0);
                    drill.SetValue<Single>("Drill.AreaOffsetFrontBack", 0);
                }
            }

            public void InitDrills()
            {
                foreach (var drill in nanoDrill)
                {
                    drill.SetValue<Single>("Drill.AreaOffsetLeftRight", ((shipController.Position.X - drill.Position.X) * distMod));
                    drill.SetValue<Single>("Drill.AreaOffsetUpDown", ((shipController.Position.Y - drill.Position.Y) * distMod));
                    drill.SetValue<Single>("Drill.AreaOffsetFrontBack", ((drill.Position.Z - shipController.Position.Z) * distMod));

                    drill.SetValue<Single>("Drill.AreaOffsetFrontBack", drill.GetValue<Single>("Drill.AreaOffsetFrontBack") + (21));
                }
            }

            public void GetDrillParams()
            {
                drillPanel?.WriteText("", false);

                foreach (var drill in nanoDrill)
                {
                    drillPanel?.WriteText($"\nPos: {drill.Position} X {drill.Orientation.Forward} X{shipController.Position} X {shipController.Orientation.Forward}", true);

                    drillPanel?.WriteText($"\n {shipController.CubeGrid.Max} X {shipController.CubeGrid.Min}", true);

                    drill.SetValue<Single>("Drill.AreaOffsetLeftRight", ((shipController.Position.X - drill.Position.X) * distMod));
                    drill.SetValue<Single>("Drill.AreaOffsetUpDown", ((shipController.Position.Y - drill.Position.Y) * distMod));
                    drill.SetValue<Single>("Drill.AreaOffsetFrontBack", ((drill.Position.Z - shipController.Position.Z) * distMod));

                    drill.SetValue<Single>("Drill.AreaOffsetFrontBack", drill.GetValue<Single>("Drill.AreaOffsetFrontBack") + (21));

                }
            }

            public void MoveFWD()
            {
                foreach (var drill in nanoDrill)
                {
                    var val = drill.GetValue<Single>("Drill.AreaOffsetFrontBack") + maxDrillOffset;

                    if (val > 84)
                        return;
                }

                foreach (var drill in nanoDrill)
                {
                    drill.SetValue<Single>("Drill.AreaOffsetFrontBack", drill.GetValue<Single>("Drill.AreaOffsetFrontBack") + maxDrillOffset);
                }
            }

            public void MoveBCK()
            {
                foreach (var drill in nanoDrill)
                {
                    var val = drill.GetValue<Single>("Drill.AreaOffsetFrontBack") - maxDrillOffset;

                    if (val < -84)
                        return;
                }

                foreach (var drill in nanoDrill)
                {
                    drill.SetValue<Single>("Drill.AreaOffsetFrontBack", drill.GetValue<Single>("Drill.AreaOffsetFrontBack") - maxDrillOffset);
                }
            }

            public void MoveUp()
            {
                foreach (var drill in nanoDrill)
                {
                    var val = drill.GetValue<Single>("Drill.AreaOffsetUpDown") + maxDrillOffset;

                    if (val > 84)
                        return;
                }

                foreach (var drill in nanoDrill)
                {
                    drill.SetValue<Single>("Drill.AreaOffsetUpDown", drill.GetValue<Single>("Drill.AreaOffsetUpDown") + maxDrillOffset);
                }
            }

            public void MoveDown()
            {
                foreach (var drill in nanoDrill)
                {
                    var val = drill.GetValue<Single>("Drill.AreaOffsetUpDown") - maxDrillOffset;

                    if (val < -84)
                        return;
                }

                foreach (var drill in nanoDrill)
                {
                    drill.SetValue<Single>("Drill.AreaOffsetUpDown", drill.GetValue<Single>("Drill.AreaOffsetUpDown") - maxDrillOffset);
                }
            }

            public void MoveRirght()
            {
                foreach (var drill in nanoDrill)
                {
                    var val = drill.GetValue<Single>("Drill.AreaOffsetLeftRight") + maxDrillOffset;

                    if (val > 84)
                        return;
                }

                foreach (var drill in nanoDrill)
                {
                    drill.SetValue<Single>("Drill.AreaOffsetLeftRight", drill.GetValue<Single>("Drill.AreaOffsetLeftRight") + maxDrillOffset);
                }
            }

            public void MoveLeft()
            {
                foreach (var drill in nanoDrill)
                {
                    var val = drill.GetValue<Single>("Drill.AreaOffsetLeftRight") - maxDrillOffset;

                    if (val < -84)
                        return;
                }

                foreach (var drill in nanoDrill)
                {
                    drill.SetValue<Single>("Drill.AreaOffsetLeftRight", drill.GetValue<Single>("Drill.AreaOffsetLeftRight") - maxDrillOffset);
                }
            }


            public class NanoDrill
            {
                Vector3I localPosition;

                IMyTerminalBlock drill;

                public NanoDrill(IMyTerminalBlock block)
                {
                    drill = block;
                    localPosition = drill.Position;
                }

                public void Reset()
                {
                    drill.SetValue<Single>("Drill.AreaOffsetLeftRight", 0);
                    drill.SetValue<Single>("Drill.AreaOffsetUpDown", 0);
                    drill.SetValue<Single>("Drill.AreaOffsetFrontBack", 0);
                }
            }
        }


        ///END OF SCRIPT///////////////
    }

}

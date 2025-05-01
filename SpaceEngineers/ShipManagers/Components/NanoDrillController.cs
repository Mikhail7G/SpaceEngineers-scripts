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
using static SpaceEngineers.ShipManagers.Components.ShipMonitor.Program.ShipMonitor;
using VRage.Game.VisualScripting.Utils;

namespace SpaceEngineers.ShipManagers.Components.NanodrillController
{
    public sealed class Program : MyGridProgram
    {

        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        public DrillControl NanodrillController;

        MyIni dataSystem;

        public Program()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, (IMyTerminalBlock b) => b.CubeGrid == Me.CubeGrid);

            dataSystem = new MyIni();

            NanodrillController = new DrillControl(this, blocks);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;

        }

        public void Main(string args, UpdateType updateType)
        {
            Update();

            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                NanodrillController.Command(args);
            }
        }

        public void Update()
        {
            NanodrillController.Update();
        }

        public class DrillControl
        {
            public string DrillLCDName = "DrillShipLCD";

            public string NanoName = "DrillSystem";

            int currentPos = 0;
            int maxDrillOffset = 25;
            float smallGridMod = 0.5f;
            float largeGridMod = 2.5f;
            float distMod = 1;

            List<Vector3S> positions = new List<Vector3S>();

            Program program;

            IMyTextPanel drillPanel;

            IMyShipController shipController;
            List<IMyTerminalBlock> nanoDrillblocks;

            List<Nanodrill> nanoDrills;

            List<Vector3I> miningCoords;

            public DrillControl(Program mainProg, List<IMyTerminalBlock> shipBlocks)
            {
                program = mainProg;
                nanoDrillblocks = new List<IMyTerminalBlock>();
                nanoDrills = new List<Nanodrill>();
                miningCoords = new List<Vector3I>();

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                blocks = shipBlocks;

                shipController = blocks.Where(b => b is IMyShipController)
                                       .Where(c => c.IsFunctional)
                                       .Select(t => t as IMyShipController).FirstOrDefault();


                nanoDrillblocks = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString().Contains(NanoName)).ToList();

                drillPanel = blocks.Where(b => b is IMyTextPanel && b.CustomName.Contains(DrillLCDName))
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyTextPanel).FirstOrDefault();

                if (drillPanel != null)
                {
                    drillPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;

                    drillPanel.WriteText("", false);
                    drillPanel.WriteText($"{nanoDrillblocks.Count}", true);

                    var test = nanoDrillblocks.FirstOrDefault();

                    List<ITerminalAction> actions = new List<ITerminalAction>();
                    List<ITerminalProperty> props = new List<ITerminalProperty>();
                    test.GetActions(actions);

                    test.GetProperties(props);

                    //foreach(var ac in actions)
                    //{
                    //    drillPanel.WriteText($"\n{ac.Name}", true);
                    //}

                    foreach (var ac in props)
                    {
                        drillPanel.WriteText($"\n{ac.Id}", true);
                    }

                }

                foreach (var drill in nanoDrillblocks)
                {
                    nanoDrills.Add(new Nanodrill(drill, shipController, maxDrillOffset));
                }

                miningCoords.Add(new Vector3I(0, 0, 25));
                miningCoords.Add(new Vector3I(25, 0, 25));
                miningCoords.Add(new Vector3I(50, 0, 25));
                miningCoords.Add(new Vector3I(75, 0, 25));
                miningCoords.Add(new Vector3I(-25, 0, 25));
                miningCoords.Add(new Vector3I(-50, 0, 25));
                miningCoords.Add(new Vector3I(-75, 0, 25));

                miningCoords.Add(new Vector3I(0, 25, 25));
                miningCoords.Add(new Vector3I(25, 25, 25));
                miningCoords.Add(new Vector3I(50, 25, 25));
                miningCoords.Add(new Vector3I(75, 25, 25));
                miningCoords.Add(new Vector3I(-25, 25, 25));
                miningCoords.Add(new Vector3I(-50, 25, 25));
                miningCoords.Add(new Vector3I(-75, 25, 25));

                miningCoords.Add(new Vector3I(0, -25, 25));
                miningCoords.Add(new Vector3I(25, -25, 25));
                miningCoords.Add(new Vector3I(50, -25, 25));
                miningCoords.Add(new Vector3I(75, -25, 25));
                miningCoords.Add(new Vector3I(-25, -25, 25));
                miningCoords.Add(new Vector3I(-50, -25, 25));
                miningCoords.Add(new Vector3I(-75, -25, 25));






                //miningCoords.Add(new Vector3I(0, 0, 50));
                //miningCoords.Add(new Vector3I(0, 0, 75));


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
                        ResetAll();
                        break;

                    case "MOVE":
                        NextPos();
                        break;
                }
            }

            public void Update()
            {
                foreach (var drill in nanoDrills)
                {
                    drill.UpdateDrill();
                }
            }

            public void ResetAll()
            {
                currentPos = 0;
                foreach (var drill in nanoDrills)
                {
                    drill.ResetDrills();
                }
            }

            public void NextPos()
            {
                if (currentPos != miningCoords.Count)
                {
                    foreach (var drill in nanoDrills)
                    {
                        drill.MoveTo(miningCoords[currentPos]);
                    }
                    currentPos++;
                }

            }
        }

        public class Nanodrill
        {
            bool moveFinished;

            Single destX, destY, destZ;

            int drillOffset;

            IMyTerminalBlock drill;
            IMyShipController shipController;

            public Nanodrill(IMyTerminalBlock block, IMyShipController shipController, int maxDrillOffset)
            {
                drill = block;
                drillOffset = maxDrillOffset;
                moveFinished = true;
            }


            public void UpdateDrill()
            {
                if(!moveFinished)
                {
                    MoveDrillToPosition();
                }
            }

            public void MoveTo(Vector3I pos)
            {
                moveFinished = false;

                destX = pos.X;
                destY = pos.Y;
                destZ = pos.Z;
            }

            public void ResetDrills()
            {
                drill.SetValue<Single>("Drill.AreaOffsetLeftRight", 0);
                drill.SetValue<Single>("Drill.AreaOffsetUpDown", 0);
                drill.SetValue<Single>("Drill.AreaOffsetFrontBack", 0);
            }

            private void MoveDrillToPosition()
            {
                Single tX, tY, tZ;
                tX = drill.GetValue<Single>("Drill.AreaOffsetLeftRight");
                tY = drill.GetValue<Single>("Drill.AreaOffsetUpDown");
                tZ = drill.GetValue<Single>("Drill.AreaOffsetFrontBack");

                if (destX == tX && destY == tY && destZ == tZ)
                {
                    moveFinished = true;
                    return;
                }

                drill.SetValue<Single>("Drill.AreaOffsetLeftRight", destX);
                drill.SetValue<Single>("Drill.AreaOffsetUpDown", destY);
                drill.SetValue<Single>("Drill.AreaOffsetFrontBack", destZ);

            }
        }


        ///END OF SCRIPT///////////////
    }

}

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
using VRage.Game.VisualScripting.Utils;

namespace SpaceEngineers.ShipManagers.Components.GravDrive
{
    public sealed class Program : MyGridProgram
    {

        //////////////DO NOT EDIT BELOW THE LINE//////////////////
        GravDrive gravDrive;


        public Program()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, (IMyTerminalBlock b) => b.CubeGrid == Me.CubeGrid);


            gravDrive = new GravDrive(blocks);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main()
        {
            gravDrive.Update();

        }


        public class GravDrive
        {
            private float dampK = 0.1f;
            private IMyShipController controller;

            private List<IMyArtificialMassBlock> massBlocks = new List<IMyArtificialMassBlock>();
            private List<IMyGravityGeneratorBase> gravGens = new List<IMyGravityGeneratorBase>();
            List<IMyGravityGeneratorSphere> spheres = new List<IMyGravityGeneratorSphere>();


            public GravDrive(List<IMyTerminalBlock> blocks)
            {

                controller = blocks.Where(b => b is IMyShipController)
                                       .Where(c => c.IsFunctional)
                                       .Select(t => t as IMyShipController)
                                       .Where(ctr=>ctr.IsMainCockpit).FirstOrDefault();

                massBlocks = blocks.Where(b => b is IMyArtificialMassBlock)
                                       .Where(c => c.IsFunctional)
                                       .Select(t => t as IMyArtificialMassBlock).ToList();

                gravGens = blocks.Where(b => b is IMyGravityGeneratorBase)
                                    .Where(c => c.IsFunctional)
                                    .Select(t => t as IMyGravityGeneratorBase).ToList();

                spheres = blocks.Where(b => b is IMyGravityGeneratorSphere)
                                   .Where(c => c.IsFunctional)
                                   .Select(t => t as IMyGravityGeneratorSphere).ToList();

            }

            public bool CheckIntegrity()
            {
                if (controller.Closed)
                {
                    return false;
                }

                return true;
             }

            private void DisableAll()
            {
                gravGens.ForEach(g => g.Enabled = false);
            }

            public void Operate()
            {
                Vector3D myVelocity = controller.GetShipVelocities().LinearVelocity;
                Vector3D pilotInput = controller.MoveIndicator;

                if (controller.DampenersOverride)
                {
                    if (Math.Abs(pilotInput.X) < 0.00001)
                    {
                        pilotInput.X = -myVelocity.Dot(controller.WorldMatrix.Right) * dampK;
                    }
                    if (Math.Abs(pilotInput.Y) < 0.00001)
                    {
                        pilotInput.Y = -myVelocity.Dot(controller.WorldMatrix.Up) * dampK;
                    }
                    if (Math.Abs(pilotInput.Z) < 0.00001)
                    {
                        pilotInput.Z = -myVelocity.Dot(controller.WorldMatrix.Backward) * dampK;
                    }
                }
                Vector3D pilotInputW = Vector3D.Transform(pilotInput, controller.WorldMatrix.GetOrientation());

                foreach (IMyGravityGeneratorBase gravgen in gravGens)
                {
                    gravgen.GravityAcceleration = (float)pilotInputW.Dot(gravgen.WorldMatrix.Down) * 9.8f;
                }
            }

            public void Update()
            {
                if (CheckIntegrity())
                {
                    Operate();
                }
                else
                {
                    DisableAll();
                }
            }
        }

        /////////////////END OF SCRIPT///////////////////////////
    }

}

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

        IMyRemoteControl remoteControl;
        IMyCockpit cockpit;
        List<IMyThrust> thrusters;
        List<IMyCargoContainer> containers;
        List<IMyGyro> gyros;
        IMyTextPanel panel1;
        IMyTextPanel panel2;

        List<Vector3D> waypoints;

        float alt = 0;
        float shipMass = 1;

        int updateTick = 0;

        double prevSpeed = 0;

        double reqShipFWDspeed = 0;

        public Program()
        {
            thrusters = new List<IMyThrust>();
            containers = new List<IMyCargoContainer>();
            gyros = new List<IMyGyro>();
            waypoints = new List<Vector3D>();

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

                case "WW":
                    SaveThisPos();
                    break;
            }

            SlowUpdateFunctions();

            SetGyro(HoldDirections());
            FlyUp();
        }

        void FindComponetns()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, b => b.CubeGrid == Me.CubeGrid);
            cockpit = blocks.Where(b => b is IMyCockpit).First() as IMyCockpit;
            thrusters = blocks.Where(b => b is IMyThrust).Select(t => t as IMyThrust).ToList();
            containers = blocks.Where(b => b is IMyCargoContainer).Select(t => t as IMyCargoContainer).ToList();
            gyros = blocks.Where(b => b is IMyGyro).Select(t => t as IMyGyro).ToList();
            remoteControl = blocks.Where(b => b is IMyRemoteControl).First() as IMyRemoteControl;
            panel1 = blocks.Where(b => b is IMyTextPanel).Where(t => t.CustomName == "EngLcd").FirstOrDefault() as IMyTextPanel;
            panel2 = blocks.Where(b => b is IMyTextPanel).Where(t => t.CustomName == "MassLcd").FirstOrDefault() as IMyTextPanel;

        }

        void SlowUpdateFunctions()
        {
            updateTick++;
            if (updateTick > 100)
            {
                updateTick = 0;
                EchoInfo();
                ShipInfo();
            }

        }

        void EchoInfo()
        {
            if (remoteControl == null) 
            {
                Echo($"No remote control");
            }

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

            if (gyros?.Count == 0)
            {
                Echo($"No gyro");
            }
            else
            {
                Echo($"Total gyros {gyros.Count}");
            }

            Echo($"Ship mass: {shipMass}");

        }

        void ShipInfo()
        {
            shipMass = cockpit.CalculateShipMass().PhysicalMass;
   
        }

        public Vector3D HoldDirections()
        {

            var natGravity = Vector3D.Normalize(cockpit.GetNaturalGravity());
            Vector3D axis = natGravity.Cross(cockpit.WorldMatrix.Down);
            if (natGravity.Dot(cockpit.WorldMatrix.Down) < 0)
            {
                axis = Vector3D.Normalize(axis);
            }

            Vector3D signal = cockpit.WorldMatrix.Up * cockpit.RotationIndicator.Y;
            axis += signal;
            return axis;
        }

        public void SetGyro(Vector3D axis)
        {
            foreach (IMyGyro gyro in gyros)
            {
                gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up);
                gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right);
                gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward);
            }
        }

        public void SaveThisPos()
        {
            waypoints.Clear();
            remoteControl.ClearWaypoints();

            waypoints.Add(remoteControl.GetPosition() + remoteControl.WorldMatrix.Up*50);
            remoteControl.AddWaypoint(waypoints[0],"1");
          //  var downPos = remoteControl.GetPosition() + remoteControl.WorldMatrix.Down * 50;
           // remoteControl.AddWaypoint(downPos, "2");
        }

        public void FlyUp()
        {
            //var acc = thrusters.Where(b => b.Orientation.Forward == Base6Directions.GetOppositeDirection(remoteControl.Orientation.Up))
            //                   .Sum(t => t.MaxEffectiveThrust);
            shipMass = remoteControl.CalculateShipMass().PhysicalMass;
            var grav = remoteControl.GetNaturalGravity();
            double LeftThrust = 0;
            double RightThrust = 0;
            double ForwardThrust = 0;

            reqShipFWDspeed += cockpit.MoveIndicator.Z * -1;
            reqShipFWDspeed = Math.Min(Math.Max(-100, reqShipFWDspeed), 100);

            Vector3D ShipWeight = grav * shipMass;

            var speedUp = cockpit.GetShipVelocities().LinearVelocity.Dot(remoteControl.WorldMatrix.Up);
            var speedLeft = cockpit.GetShipVelocities().LinearVelocity.Dot(remoteControl.WorldMatrix.Left);
            var speedFwd = cockpit.GetShipVelocities().LinearVelocity.Dot(remoteControl.WorldMatrix.Forward);


            double UpThrust = 0;

            //необходимое ускорение для достмжения заданной скорости (v-v0)/t
            var accFwd = (speedFwd - reqShipFWDspeed) / 1;
            //тяга двигателей f=m*a
            ForwardThrust =accFwd * shipMass;

            UpThrust = -ShipWeight.Dot(remoteControl.WorldMatrix.Up - cockpit.GetShipVelocities().LinearVelocity);
            UpThrust *= Math.Max(1, cockpit.MoveIndicator.Y*10);

            if (cockpit.MoveIndicator.Y<0)
            {
                UpThrust = 0;

            }




            if (waypoints.Count > 0)
            {
                //var thrust = Vector3D.Reject(cockpit.WorldMatrix.Forward, Vector3D.Normalize(waypoints[0] * grav));

                //LeftThrust= Vector3D.Reject(cockpit.WorldMatrix.Forward, Vector3D.Normalize(waypoints[0] * grav)).Length();
                //RightThrust = Vector3D.Reject(cockpit.WorldMatrix.Forward, Vector3D.Normalize(waypoints[0] * grav)).Length();
                //LeftThrust = waypoints[0].Dot(remoteControl.WorldMatrix.Left);

                //ForwardThrust = waypoints[0].Dot(remoteControl.WorldMatrix.Forward);

            }

            var thrustersUp = thrusters.Where(b => b.GridThrustDirection.Y==-1).ToList();
            var accUp = thrustersUp.Sum(t => t.MaxEffectiveThrust);

            var thrustersDown = thrusters.Where(b => b.GridThrustDirection.Y == 1).ToList();
            var accDown = thrustersDown.Sum(t => t.MaxEffectiveThrust);


            var thrustersLeft = thrusters.Where(b => b.GridThrustDirection.X == -1).ToList();
            var accLeft = thrustersLeft.Sum(t => t.MaxEffectiveThrust);

            var thrustersRight = thrusters.Where(b => b.GridThrustDirection.X == 1).ToList();
            var accRight = thrustersRight.Sum(t => t.MaxEffectiveThrust);

            var thrustersForward = thrusters.Where(b => b.GridThrustDirection.Z == 1).ToList();
            var accForward = thrustersForward.Sum(t => t.MaxEffectiveThrust);

            var thrustersBackward = thrusters.Where(b => b.GridThrustDirection.Z == -1).ToList();
            var accBack = thrustersBackward.Sum(t => t.MaxEffectiveThrust);

            var forse = accUp / shipMass;
            var forse1ms = 1 * shipMass;


            var delta = (speedUp - prevSpeed) * 60;
            prevSpeed = speedUp;


            foreach (var tr in thrustersUp)
            {
                tr.ThrustOverridePercentage = (float)UpThrust / accUp;
            }

            foreach (var tr in thrustersDown)
            {
               // tr.ThrustOverridePercentage = (float)UpThrust / accDown;
            }


            foreach (var tr in thrustersLeft)
            {
                // tr.ThrustOverridePercentage = (float)(RightThrust)* forse1ms;
            }

            foreach (var tr in thrustersRight)
            {
               //  tr.ThrustOverridePercentage = (float)(LeftThrust) * forse1ms;
            }

            foreach (var tr in thrustersForward)
            {
                tr.ThrustOverridePercentage = - (float)ForwardThrust / accForward;
            }

            foreach (var tr in thrustersBackward)
            {
               tr.ThrustOverridePercentage = (float)ForwardThrust  / accBack;
            }


            panel1?.WriteText(
                $"FWdThrust: {accForward} N" +
                $"\nfwdSpd: {speedFwd} m/c" +
                $"\nFWDAcc m/c2: {accForward/shipMass}" +
                $"\nCalcAcc m/c2: {Math.Abs(accFwd)}" +
                $"\nReqThr: {ForwardThrust}" +
                $"\nReqSpd: {reqShipFWDspeed}", false);

            panel2?.WriteText(
                $"Mass: {shipMass} kg" +
                $"\nMTOW: {accUp/grav.Length()}", false);

        }

        /////////////END OF SCRIPT//////////////////////////
    }
       
}
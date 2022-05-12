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



namespace ShipManagers.Components.Thrusters
{
    public sealed class Program : MyGridProgram
    {

        /////////////////////////////////////////////////////////////

        ThrusterController Thruster;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Thruster = new ThrusterController(this);


        }

        public void Main(string args)
        {
            switch(args)
            {
                case "INIT":
                    Thruster.FindComponents();
                    break;
            }

            Thruster.Update();
        }


        public class ThrusterController
        {
            private IMyRemoteControl remoteControl;
            private IMyCockpit cockpit;

            private List<IMyThrust> thrusters;

            private List<IMyThrust> thrustersUp;
            private List<IMyThrust> thrustersDown;
            private List<IMyThrust> thrustersLeft;
            private List<IMyThrust> thrustersRight;
            private List<IMyThrust> thrustersForward;
            private List<IMyThrust> thrustersBackward;

            private List<IMyGyro> gyros;

            private double accUp;
            private double accDown;
            private double accLeft;
            private double accRight;
            private double accForward;
            private double accBack;

            private double totalMass = 0;

            private double radioAlt = 0;
            private double baroAlt = 0;
            private double deltaAlt = 0;
            private double altHolding = 100;

            private float rotationY;

            private Vector3D naturalGravity;
            private Vector3D localForward;
            private Vector3D localLeft;
            private Vector3D localUp;
            private Vector3D localDown;

            private Program mainProgram;

            private ControlType currentControlMode;


            enum ControlType
            {
                Cocpit,
                RemoteControl
            }

            public ThrusterController(Program progream)
            {
                mainProgram = progream;
                thrusters = new List<IMyThrust>();
            }

            public void FindComponents()
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                mainProgram.GridTerminalSystem.GetBlocksOfType(blocks, b => b.CubeGrid == mainProgram.Me.CubeGrid);

                thrusters = blocks.Where(b => b is IMyThrust).Select(t => t as IMyThrust).ToList();
                gyros = blocks.Where(b => b is IMyGyro).Select(t => t as IMyGyro).ToList();

                cockpit = blocks.Where(b => b is IMyCockpit).FirstOrDefault() as IMyCockpit;
                remoteControl = blocks.Where(b => b is IMyRemoteControl).FirstOrDefault() as IMyRemoteControl;

                if ((!thrusters.Any())|| (!gyros.Any()))
                {
                    mainProgram.Echo("Warning no detected thrusters or gyros");
                    return;
                }

                if ((cockpit == null) && (remoteControl == null))
                {
                    mainProgram.Echo("No cocpit or remote control detected");
                    return;
                }

                if (cockpit != null)
                {
                    currentControlMode = ControlType.Cocpit;
                }
                else
                {
                    currentControlMode = ControlType.RemoteControl;
                }

                var thrustersUp = thrusters.Where(b => b.GridThrustDirection.Y == -1).ToList();
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

            }

            public void Update()
            {
                var accUp = thrustersUp.Sum(t => t.MaxEffectiveThrust);
                var accDown = thrustersDown.Sum(t => t.MaxEffectiveThrust);
                var accLeft = thrustersLeft.Sum(t => t.MaxEffectiveThrust);
                var accRight = thrustersRight.Sum(t => t.MaxEffectiveThrust);
                var accForward = thrustersForward.Sum(t => t.MaxEffectiveThrust);
                var accBack = thrustersBackward.Sum(t => t.MaxEffectiveThrust);

                GetLocalParams();
                SetGyro(HoldHorizon());

            }

            private void GetLocalParams()
            {
                switch (currentControlMode)
                {
                    case ControlType.Cocpit:
                        totalMass = cockpit.CalculateShipMass().PhysicalMass;
                        naturalGravity = cockpit.GetNaturalGravity();

                        rotationY = cockpit.RotationIndicator.Y;

                        localForward = cockpit.WorldMatrix.Forward;
                        localLeft = cockpit.WorldMatrix.Left;
                        localUp = cockpit.WorldMatrix.Up;
                        localDown = cockpit.WorldMatrix.Down;
                        break;

                    case ControlType.RemoteControl:
                        totalMass = remoteControl.CalculateShipMass().PhysicalMass;
                        naturalGravity = remoteControl.GetNaturalGravity();

                        rotationY = remoteControl.RotationIndicator.Y;

                        localForward = remoteControl.WorldMatrix.Forward;
                        localLeft = remoteControl.WorldMatrix.Left;
                        localUp = remoteControl.WorldMatrix.Up;
                        localDown = remoteControl.WorldMatrix.Down;
                        break;
                }

            }


            private Vector3D HoldHorizon()
            {
                var natGravNorm = Vector3D.Normalize(naturalGravity);
                var axis = natGravNorm.Cross(localDown);

                if (natGravNorm.Dot(localDown) < 0)
                {
                    axis = Vector3D.Normalize(axis);
                }

                Vector3D signal = localUp * rotationY;
                axis += signal;

                return axis;

            }

            private void SetGyro(Vector3D axis)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up);
                    gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right);
                    gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward);
                }
            }



        }

     
        public class PIDRegulator
        {

            public double P = 0;
            public double D = 0;
            public double I = 0;

            public double Kp { get; set; } = 1;
            public double Kd { get; set; } = 1;
            public double Ki { get; set; } = 1;

            private double prevD = 0;
            public double DeltaTimer { get; set; } = 1;

            public PIDRegulator()
            {

            }

            /// <summary>
            /// Установка коэффициентов
            /// </summary>
            public PIDRegulator SetK(double _Kp, double _kD, double _kI)
            {
                Kp = _Kp;
                Kd = _kD;
                Ki = _kI;

                return this;
            }

            /// <summary>
            /// Установка регулирующего значения
            /// </summary>
            public PIDRegulator SetPID(double inputP, double inputD, double inputI, double deltaTimer)
            {
                DeltaTimer = deltaTimer;

                P = inputP;

                D = (inputD - prevD) / DeltaTimer;
                prevD = inputD;

                I = I + inputI * DeltaTimer;
                return this;
            }

            /// <summary>
            /// Возвращяем сигнал управления
            /// </summary>
            public double GetSignal()
            {
                double outSignal = P * Kp + D * Kd + I * Ki;

                return outSignal;
            }
        }

        ///////////////////////////////////////////////////////////
    }
}
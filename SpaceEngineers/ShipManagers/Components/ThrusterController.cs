﻿using System;
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
            Thruster.FindComponents();

        }

        public void Main(string args)
        {
            switch(args)
            {
                case "INIT":
                    Thruster.FindComponents();
                    break;
                case "CC":
                    Thruster.SwitchCruiseControl();
                    break;

                case "CCALT":
                    Thruster.SwitchAltHold();
                    break;

                case "HOR":
                    Thruster.ShitchHorizonHold();
                    break;
            }

            Thruster.Update();
        }


        public class ThrusterController
        {
            public bool CruiseControl { get; set; }
            public bool AltHold { get; set; }
            public int AltHildKP { get; set; }
            public int AltHildKD { get; set; }
            public bool HorizonHold { get; set; }

            private IMyRemoteControl remoteControl;
            private IMyCockpit cockpit;

            private IMyTextPanel cruiseControlLCD;
            private IMyTextSurface cruiseControlSurf;

            private List<IMyThrust> thrusters;

            private List<IMyThrust> thrustersUp;
            private List<IMyThrust> thrustersDown;
            private List<IMyThrust> thrustersLeft;
            private List<IMyThrust> thrustersRight;
            private List<IMyThrust> thrustersForward;
            private List<IMyThrust> thrustersBackward;

            private List<IMyGyro> gyros;

            private double accUp = 0;
            private double accDown = 0;
            private double accLeft = 0;
            private double accRight = 0;
            private double accForward = 0;
            private double accBack = 0;

            private double ThrustUp = 0;
            private double ThrustDown = 0;
            private double ThrustLeft = 0;
            private double ThrustRight = 0;
            private double ThrustForward = 0;
            private double ThrustBack = 0;

            private double totalMass = 0;
            private double maxTakeOffWheight = 0;

            private double radioAlt = 0;
            private double baroAlt = 0;
            private double deltaAlt = 0;
            private double altHolding = 100;
            private double requestedForwardSpeed = 0;

            private float rotationY = 0;

            private float moveZ = 0;
            private float moveY = 0;

            private Vector3D totalWheight;
            private Vector3D naturalGravity;
            private Vector3D lineraVelocity;
            private Vector3D localForward;
            private Vector3D localLeft;
            private Vector3D localUp;
            private Vector3D localDown;

            private double forwadSpeedComponent = 0;
            private double leftSpeedComponent = 0;
            private double upSpeedComponent = 0;

            private double deltaTime = 1;

            private Program mainProgram;

            private ControlType currentControlMode;

            private string cruiseControlLCDName = "CCLcd";


            enum ControlType
            {
                Cocpit,
                RemoteControl
            }

            public ThrusterController(Program program)
            {
                mainProgram = program;
                thrusters = new List<IMyThrust>();
                thrustersUp = new List<IMyThrust>();
                thrustersDown = new List<IMyThrust>();
                thrustersLeft = new List<IMyThrust>();
                thrustersRight = new List<IMyThrust>();
                thrustersForward = new List<IMyThrust>();
                thrustersBackward = new List<IMyThrust>();

                gyros = new List<IMyGyro>();

                AltHildKP = 1;
                AltHildKD = 1;

            }

            public void FindComponents()
            {
                mainProgram.Echo("-----Thruster controller init start---");

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                mainProgram.GridTerminalSystem.GetBlocksOfType(blocks, b => b.CubeGrid == mainProgram.Me.CubeGrid);

                thrusters = blocks.Where(b => b is IMyThrust).Select(t => t as IMyThrust).ToList();
                mainProgram.Echo($"Total thrusters: {thrusters.Count}");

                gyros = blocks.Where(b => b is IMyGyro).Select(t => t as IMyGyro).ToList();
                mainProgram.Echo($"Total gyros: {gyros.Count}");

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
                    cruiseControlLCD = blocks.Where(b => b is IMyTextPanel).Where(t => t.CustomName == cruiseControlLCDName).FirstOrDefault() as IMyTextPanel;

                    var text = cockpit as IMyTextSurfaceProvider;
                    if (text != null)
                    {
                        cruiseControlSurf = text.GetSurface(0);
                    }

                }
                else
                {
                    currentControlMode = ControlType.RemoteControl;
                    cruiseControlLCD = blocks.Where(b => b is IMyTextPanel).Where(t => t.CustomName == cruiseControlLCDName).FirstOrDefault() as IMyTextPanel;
                }

                thrustersUp = thrusters.Where(b => b.GridThrustDirection.Y == -1).ToList();
                accUp = thrustersUp.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr up: {thrustersUp.Count}");

                thrustersDown = thrusters.Where(b => b.GridThrustDirection.Y == 1).ToList();
                accDown = thrustersDown.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr down: {thrustersDown.Count}");

                thrustersLeft = thrusters.Where(b => b.GridThrustDirection.X == -1).ToList();
                accLeft = thrustersLeft.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr left: {thrustersLeft.Count}");

                thrustersRight = thrusters.Where(b => b.GridThrustDirection.X == 1).ToList();
                accRight = thrustersRight.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr right: {thrustersRight.Count}");

                thrustersForward = thrusters.Where(b => b.GridThrustDirection.Z == 1).ToList();
                accForward = thrustersForward.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr fwd: {thrustersForward.Count}");

                thrustersBackward = thrusters.Where(b => b.GridThrustDirection.Z == -1).ToList();
                accBack = thrustersBackward.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr back: {thrustersBackward.Count}");

                mainProgram.Echo("Init completed");

                ClearOverrideGyros();
                ClerarOverideEngines();
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


                if(HorizonHold)
                SetGyro(HoldHorizon());

                PrintData();

                if (CruiseControl)
                    CruiseControlSystem();

                if (AltHold)
                    AltHoldMode();
            }

            public void SwitchCruiseControl()
            {
                CruiseControl = !CruiseControl;
                ClerarOverideEngines();
            }

            public void SwitchAltHold()
            {
                AltHold = !AltHold;
                ClerarOverideEngines();
            }

            public void ShitchHorizonHold()
            {
                HorizonHold = !HorizonHold;
               if(HorizonHold)
                {
                    OverrideGyros();
                }
               else
                {
                    ClearOverrideGyros();
                }

            }

            public void ClerarOverideEngines()
            {
                foreach (var tr in thrustersUp)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersDown)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersLeft)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersRight)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersForward)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersBackward)
                {
                    tr.ThrustOverridePercentage = -1;
                }
            }

            public void ClearOverrideGyros()
            {
                foreach (var gyro in gyros)
                {
                    gyro.SetValueBool("Override", false);
                }
            }

            /// Приватные методы ниже


            private void PrintData()
            {
                cruiseControlLCD?.WriteText(
                    $"Mass: {totalMass} kg" +
                    $"\nMTOW: {maxTakeOffWheight}" +
                    $"\n{thrustersUp.Count}", false);


                cruiseControlSurf?.WriteText(
                    $"CC: {CruiseControl}" +
                    $"\nAltHold: {AltHold}" +
                    $"\nHorHold: {HorizonHold}" +
                    $"\nReqSpeed: {requestedForwardSpeed}" +
                    $"\nReqAlt: {altHolding}", false);
            }


            private void AltHoldMode()
            {
                double AltCorrThrust = 0;
                PIDRegulator pid = new PIDRegulator();
                deltaAlt = radioAlt - altHolding;
                altHolding += moveY;

                var upAcc = (2 * deltaAlt) / deltaTime;
                AltCorrThrust -= pid.SetK(AltHildKP, AltHildKD, 0).SetPID(upAcc, upAcc, upAcc, deltaTime).GetSignal() * totalMass;

                ThrustUp = -totalWheight.Dot(localUp - lineraVelocity) + AltCorrThrust;
                ThrustUp *= Math.Max(1, cockpit.MoveIndicator.Y * 10);

                if (deltaAlt > 1)
                {
                    ThrustDown = ThrustUp;
                }

                OverrideThrsters();

                //if (cockpit.MoveIndicator.Y < 0)
                //{
                //    UpThrust = 0;
                //    downThrust = UpThrust;

                //}

            }
            private void CruiseControlSystem()
            {
                requestedForwardSpeed += moveZ * -1;
                requestedForwardSpeed = Math.Min(Math.Max(-100, requestedForwardSpeed), 100);

                var reqAcc = (forwadSpeedComponent - requestedForwardSpeed) / deltaTime;
                ThrustForward = reqAcc * totalMass;

                OverrideThrsters();

            }

            private void OverrideThrsters()
            {
                foreach (var tr in thrustersUp)
                {
                    tr.ThrustOverridePercentage = (float)ThrustUp / (float)accUp;
                }

                foreach (var tr in thrustersDown)
                {
                    tr.ThrustOverridePercentage = -(float)ThrustDown / (float)accDown;
                }


                foreach (var tr in thrustersLeft)
                {
                    tr.ThrustOverridePercentage = (float)(ThrustLeft) / (float)accLeft;
                }

                foreach (var tr in thrustersRight)
                {
                    tr.ThrustOverridePercentage = -(float)(ThrustLeft) / (float)accRight;
                }

                foreach (var tr in thrustersForward)
                {
                    tr.ThrustOverridePercentage = -(float)ThrustForward / (float)accForward;
                }

                foreach (var tr in thrustersBackward)
                {
                    tr.ThrustOverridePercentage = (float)ThrustForward / (float)accBack;
                }
            }

            private void GetLocalParams()
            {
                switch (currentControlMode)
                {
                    case ControlType.Cocpit:
                        totalMass = cockpit.CalculateShipMass().PhysicalMass;
                        naturalGravity = cockpit.GetNaturalGravity();
                        totalWheight = totalMass * naturalGravity;
                        cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface,out radioAlt);
                        cockpit.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out baroAlt);

                        rotationY = cockpit.RotationIndicator.Y;

                        moveZ = cockpit.MoveIndicator.Z;
                        moveY = cockpit.MoveIndicator.Y;

                        localForward = cockpit.WorldMatrix.Forward;
                        localLeft = cockpit.WorldMatrix.Left;
                        localUp = cockpit.WorldMatrix.Up;
                        localDown = cockpit.WorldMatrix.Down;

                        lineraVelocity = cockpit.GetShipVelocities().LinearVelocity;
                        forwadSpeedComponent = lineraVelocity.Dot(localForward);
                        break;

                    case ControlType.RemoteControl:
                        totalMass = remoteControl.CalculateShipMass().PhysicalMass;
                        naturalGravity = remoteControl.GetNaturalGravity();
                        totalWheight = totalMass * naturalGravity;
                        remoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out radioAlt);
                        remoteControl.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out baroAlt);

                        rotationY = remoteControl.RotationIndicator.Y;

                        moveZ = remoteControl.MoveIndicator.Z;
                        moveY = remoteControl.MoveIndicator.Y;

                        localForward = remoteControl.WorldMatrix.Forward;
                        localLeft = remoteControl.WorldMatrix.Left;
                        localUp = remoteControl.WorldMatrix.Up;
                        localDown = remoteControl.WorldMatrix.Down;

                        lineraVelocity = remoteControl.GetShipVelocities().LinearVelocity;
                        forwadSpeedComponent = lineraVelocity.Dot(localForward);
                        break;
                }

                maxTakeOffWheight = accUp / naturalGravity.Length();

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

            private void OverrideGyros()
            {
                foreach (var gyro in gyros)
                {
                    gyro.SetValueBool("Override", true);
                }
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
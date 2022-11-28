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
using System.Diagnostics.Metrics;
using Sandbox.Game.Entities;
using Sandbox.Game.Replication;

namespace SpaceEngineers.Autominer.Autominer
{
    public sealed class Program : MyGridProgram
    {
       


        public Program()
        {

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
          

        }


        public void Main(string args, UpdateType updateType)
        {

          
            //if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            //    ShipComplexMonitor.Command(args);
        }

        public class MovementCommander
        {
            IMyShipController shipController;
            List<IMyThrust> thristers;

            Program program;

            public bool MoveToTarget { get; private set; }
            public bool PlanetDetected { get; private set; }
            public bool InGravity { get; private set; }

            public float DesiredSpeed { get; set; }
            float pathLen;
            float mass;
            float forwardChange, upChange, leftChange;
            const float IDLE_POWER = 0.0000001f;

            double elevationVelocity;
            double distanceToGround;
            double radius;
            const double TICK_TIME = 0.16666f;

            Vector3D position = new Vector3D();
            Vector3D targetPos = new Vector3D();
            Vector3D path = new Vector3D();
            Vector3D pathNormal = new Vector3D();
            Vector3D naturalGravity = new Vector3D();
            Vector3D linearVelocity = new Vector3D();

            Vector3D planetCenter = new Vector3D();

            Vector3D gravityUpVector = new Vector3D();
            Vector3D gravityDownVector = new Vector3D();
            Vector3D upVector = new Vector3D();
            Vector3D forwardVector = new Vector3D();
            Vector3D backwardVector = new Vector3D();
            Vector3D downVector = new Vector3D();
            Vector3D rightVector = new Vector3D();
            Vector3D leftVector = new Vector3D();
            MatrixD orientation = new MatrixD();
            Vector3D gridForwardVect = new Vector3D();
            Vector3D gridUpVect = new Vector3D();
            Vector3D gridLeftVect = new Vector3D();

            Vector3D maxThrottle = new Vector3D();

            Dictionary<Base6Directions.Direction, double> maxThrust = new Dictionary<Base6Directions.Direction, double>() { { Base6Directions.Direction.Backward, 0 }, { Base6Directions.Direction.Down, 0 }, { Base6Directions.Direction.Forward, 0 }, { Base6Directions.Direction.Left, 0 }, { Base6Directions.Direction.Right, 0 }, { Base6Directions.Direction.Up, 0 }, };


            public MovementCommander(Program mainPrgoram)
            {
                program = mainPrgoram;

                Init();
            }

            void Init()
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                program.GridTerminalSystem.GetBlocksOfType(blocks, (IMyTerminalBlock b) => b.CubeGrid == program.Me.CubeGrid);

                shipController = blocks.Where(b => b is IMyShipController)
                                       .Where(c => c.IsFunctional)
                                       .Select(t => t as IMyShipController).FirstOrDefault();

                thristers = blocks.Where(b => b is IMyThrust)
                                  .Where(c => c.IsFunctional)
                                  .Select(t => t as IMyThrust).ToList();
            }

            public void Update()
            {
                GetShipParams();
                RefreshEngines();
                ThrusterTick();
            }

            public void FlyTo(Vector3D pos)
            {
                if (!pos.IsZero())
                {
                    targetPos = pos;
                    MoveToTarget = true;
                }
            }

            void GetShipParams()
            {
                gridForwardVect = shipController.WorldMatrix.Forward;
                gridUpVect = shipController.WorldMatrix.Up;
                gridLeftVect = shipController.WorldMatrix.Left;

                forwardVector = shipController.WorldMatrix.Forward;
                backwardVector = shipController.WorldMatrix.Backward;
                rightVector = shipController.WorldMatrix.Right;
                leftVector = shipController.WorldMatrix.Left;
                upVector = shipController.WorldMatrix.Up;
                downVector = shipController.WorldMatrix.Down;

                mass = shipController.CalculateShipMass().PhysicalMass;

                position = shipController.CenterOfMass;
                orientation = shipController.WorldMatrix.GetOrientation();
                radius = shipController.CubeGrid.WorldVolume.Radius;
                linearVelocity = shipController.GetShipVelocities().LinearVelocity;
                elevationVelocity = Vector3D.Dot(linearVelocity, upVector);

                PlanetDetected = shipController.TryGetPlanetPosition(out planetCenter);
                naturalGravity = shipController.GetNaturalGravity();
                InGravity = naturalGravity.Length() >= 0.5;

                if (InGravity)
                {
                    shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out distanceToGround);
                    gravityDownVector = Vector3D.Normalize(naturalGravity);
                    gravityUpVector = -1 * gravityDownVector;
                }
                else
                {
                    distanceToGround = 500;
                    gravityDownVector = downVector;
                    gravityUpVector = upVector;
                }


                path = targetPos - position;
                pathLen = (float)path.Length();
                pathNormal = Vector3D.Normalize(path);
            }

            void RefreshEngines()
            {
                foreach (Base6Directions.Direction dir in maxThrust.Keys.ToList())
                {
                    maxThrust[dir] = 0;
                }
                foreach (IMyThrust thruster in thristers)
                {
                    if (!thruster.IsWorking)
                    {
                        continue;
                    }
                    maxThrust[thruster.Orientation.Forward] += thruster.MaxEffectiveThrust;
                }
            }


            double GetMaxThrust(Vector3D dir)
            {
                forwardChange = (float)Vector3D.Dot(dir, gridForwardVect);
                upChange = (float)Vector3D.Dot(dir, gridUpVect);
                leftChange = (float)Vector3D.Dot(dir, gridLeftVect);

                maxThrottle.X = forwardChange * maxThrust[(forwardChange > 0) ? Base6Directions.Direction.Forward : Base6Directions.Direction.Backward];
                maxThrottle.Y = upChange * maxThrust[(upChange > 0) ? Base6Directions.Direction.Up : Base6Directions.Direction.Down];
                maxThrottle.Z = leftChange * maxThrust[(leftChange > 0) ? Base6Directions.Direction.Left : Base6Directions.Direction.Right];

                return maxThrottle.Length();
            }

            float Drain(ref float remainingPower, float maxEffectiveThrust)
            {
                var applyPower = Math.Min(Math.Abs(remainingPower), maxEffectiveThrust);
                remainingPower = (remainingPower > 0) ? (remainingPower - applyPower) : (remainingPower + applyPower);
                return Math.Max(applyPower / maxEffectiveThrust, IDLE_POWER);
            }

            void ThrusterTick()
            {
                if (pathLen == 0.0f)
                {
                    return;
                }


                Vector3D force, directVel, directNormal, indirectVel;
                double ttt, maxFrc, maxVel, maxAcc, TIME_STEP = 2.5 * TICK_TIME, smooth;


                var massFix = mass;

                force = mass * naturalGravity;
                directVel = Vector3D.ProjectOnVector(ref linearVelocity, ref pathNormal);
                directNormal = Vector3D.Normalize(directVel);
                if (!directNormal.IsValid())
                {
                    directNormal = Vector3D.Zero;
                }
                maxFrc = GetMaxThrust(pathNormal) - ((Vector3D.Dot(force, pathNormal) > 0) ? Vector3D.ProjectOnVector(ref force, ref pathNormal).Length() : 0.0);
                maxVel = Math.Sqrt(2.0 * pathLen * maxFrc / massFix);
                smooth = Math.Min(Math.Max((DesiredSpeed + 1.0f - directVel.Length()) / 2.0f, 0.0f), 1.0f);
                maxAcc = 1.0f + (maxFrc / massFix) * smooth * smooth * (3.0f - 2.0f * smooth);
                ttt = Math.Max(TIME_STEP, Math.Abs(maxVel / maxAcc));
                force += massFix * -2.0 * (pathNormal * pathLen / ttt / ttt - directNormal * directVel.Length() / ttt);
                indirectVel = Vector3D.ProjectOnPlane(ref linearVelocity, ref pathNormal);
                force += massFix * indirectVel / TIME_STEP;

                forwardChange = (float)Vector3D.Dot(force, gridForwardVect);
                upChange = (float)Vector3D.Dot(force, gridUpVect);
                leftChange = (float)Vector3D.Dot(force, gridLeftVect);

                foreach (IMyThrust thruster in thristers)
                {
                    if (!thruster.IsWorking)
                    {
                        thruster.ThrustOverridePercentage = 0;
                        continue;
                    }
                    switch (thruster.Orientation.Forward)
                    {
                        case Base6Directions.Direction.Forward:
                            thruster.ThrustOverridePercentage = (forwardChange < 0) ? IDLE_POWER : Drain(ref forwardChange, thruster.MaxEffectiveThrust);
                            break;
                        case Base6Directions.Direction.Backward:
                            thruster.ThrustOverridePercentage = ((forwardChange > 0) ? IDLE_POWER : (Drain(ref forwardChange, thruster.MaxEffectiveThrust)));
                            break;
                        case Base6Directions.Direction.Up:
                            thruster.ThrustOverridePercentage = ((upChange < 0) ? IDLE_POWER : (Drain(ref upChange, thruster.MaxEffectiveThrust)));
                            break;
                        case Base6Directions.Direction.Down:
                            thruster.ThrustOverridePercentage = ((upChange > 0) ? IDLE_POWER : (Drain(ref upChange, thruster.MaxEffectiveThrust)));
                            break;
                        case Base6Directions.Direction.Left:
                            thruster.ThrustOverridePercentage = ((leftChange < 0) ? IDLE_POWER : (Drain(ref leftChange, thruster.MaxEffectiveThrust)));
                            break;
                        case Base6Directions.Direction.Right:
                            thruster.ThrustOverridePercentage = ((leftChange > 0) ? IDLE_POWER : (Drain(ref leftChange, thruster.MaxEffectiveThrust)));
                            break;
                    }
                }



            }
        }



        public class PIDRegulator
        {

            double P = 0;
            double D = 0;
            double I = 0;

            public double Kp { get; set; } = 1;
            public double Kd { get; set; } = 1;
            public double Ki { get; set; } = 1;

            public double DeltaTimer { get; set; } = 1;

            double prevD = 0;

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

                I += inputI * DeltaTimer;

                return this;
            }

            /// <summary>
            /// Установка регулирующего значения
            /// </summary>
            public PIDRegulator SetPID(double input)
            {
                P = input;

                D = (input - prevD) / DeltaTimer;
                prevD = input;

                I += input * DeltaTimer;

                return this;
            }

            /// <summary>
            /// Установка регулирующего значения
            /// </summary>
            public PIDRegulator SetPID(double input, double clampI)
            {
                P = input;

                D = (input - prevD) / DeltaTimer;
                prevD = input;

                I += input * DeltaTimer;
                I = MathHelper.Clamp(I, -clampI, clampI);

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

            public void Reset()
            {
                P = 0;
                D = 0;
                I = 0;
            }
        }



        ///END OF SCRIPT///////////////
    }

}

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
using System.Drawing;
using Sandbox.Game.WorldEnvironment.Modules;
using static VRage.Game.MyObjectBuilder_ControllerSchemaDefinition;
using System.Security.Cryptography;
using static SpaceEngineers.Autominer.Autominer.Program;
using static VRage.Game.MyObjectBuilder_CurveDefinition;

namespace SpaceEngineers.Autominer.Autominer
{

    public sealed class Program : MyGridProgram
    {

        #region mdk preserve
        //Комменты
        float miningSpeed = 1;
        #endregion

        PerformanceMonitor monitor;
        MovementCommander mover;
        Navigation navigation;

        Waypoint point;


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            monitor = new PerformanceMonitor(this, Me.GetSurface(1));
            mover = new MovementCommander(this);
            navigation = new Navigation(this);

            mover.MovingFinishedNotify += Mover_MovingFinishedNotify;
        }

        private void Mover_MovingFinishedNotify()
        {

        }

        public void Main(string args, UpdateType updateType)
        {
            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                Command(args);


            mover.Update();

     
            monitor.AddInstructions("");
            monitor.EndOfFrameCalc();
            monitor.Draw();
        }



        public void Command(string commands)
        {
            string comm = commands.ToUpper();

            switch (comm)
            {
                case "START":
                    break;

                case "STOP":
                    mover.FullStop();
                    break;

                case "FLY":
                    SavePosAndRotation();
                    break;

            }

        }

        public void SavePosAndRotation()
        {
            point = new Waypoint(mover.GetShipLocalDrift(Base6Directions.Direction.Forward, 100), mover.GetOrientation(), 10.0f);
            mover.FlyToWP(point);
            mover.ForwardMove(miningSpeed);
            mover.AlignOnWP(MovementCommander.RotateType.Matrix);
        }


      

        public class MovementCommander
        {
            IMyShipController shipController;

            //Двигатели
            List<IMyThrust> thrusters;
            List<IMyThrust> upThrusters = new List<IMyThrust>();
            List<IMyThrust> downThrusters = new List<IMyThrust>();
            List<IMyThrust> leftThrusters = new List<IMyThrust>();
            List<IMyThrust> rightThrusters = new List<IMyThrust>();
            List<IMyThrust> forwardThrusters = new List<IMyThrust>();
            List<IMyThrust> backwardThrusters = new List<IMyThrust>();

            List<IMyGyro> gyros = new List<IMyGyro>();

            Program program;

            public FlyType FlyMode { get; private set; }
            public RotateType RotateMode { get; private set; }
            //Основные параметры
            public bool MoveToTarget { get; private set; }
            public bool Rotate { get; private set; }
            public bool PlanetDetected { get; private set; }
            public bool InGravity { get; private set; }
            public float StoppingAccuracyDistance { get; set; }
            public float DesiredSpeed { get; set; }
            public float ForwardMiningSpeed { get; set; }
            public float PathLen { get; private set; }
            float mass;
            float forwardChange, upChange, leftChange;
            const float IDLE_POWER = 0.0000001f;

            double elevationVelocity;
            double distanceToGround;
            double radius;
            double forwadSpeedComponent;
            const double TICK_TIME = 0.16666f;
            double speed;

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

            //направление тяги двигателей
            Dictionary<Base6Directions.Direction, double> maxThrust = new Dictionary<Base6Directions.Direction, double>() { { Base6Directions.Direction.Backward, 0 }, { Base6Directions.Direction.Down, 0 }, { Base6Directions.Direction.Forward, 0 }, { Base6Directions.Direction.Left, 0 }, { Base6Directions.Direction.Right, 0 }, { Base6Directions.Direction.Up, 0 }, };

            public delegate void MoveFinishHandler();
            public event MoveFinishHandler MovingFinishedNotify;

            Matrix targetRotation;

            public enum FlyType
            {
                Normal,
                ForwardConst
            };

            public enum RotateType
            {
                Matrix,
                Vector
            };

            public MovementCommander(Program mainPrgoram)
            {
                program = mainPrgoram;

                Init();
            }

            void Init()
            {
                DesiredSpeed = 50;
                ForwardMiningSpeed = 0.5f;
                StoppingAccuracyDistance = 0.1f;

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                program.GridTerminalSystem.GetBlocksOfType(blocks, (IMyTerminalBlock b) => b.CubeGrid == program.Me.CubeGrid);

                shipController = blocks.Where(b => b is IMyShipController)
                                       .Where(c => c.IsFunctional)
                                       .Select(t => t as IMyShipController).FirstOrDefault();

                thrusters = blocks.Where(b => b is IMyThrust)
                                  .Where(c => c.IsFunctional)
                                  .Select(t => t as IMyThrust).ToList();

                gyros = blocks.Where(b => b is IMyGyro)
                                  .Where(c => c.IsFunctional)
                                  .Select(t => t as IMyGyro).ToList();

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
            }

            public void Update()
            {
                GetShipParams();

                if (MoveToTarget)
                {
                    ThrusterTick();
                }

                if(Rotate)
                {
                    switch(RotateMode)
                    {
                        case RotateType.Matrix:
                            RotateByMatrix(targetRotation);
                            break;

                        case RotateType.Vector:
                            RotateByVectorAndMatrix(targetPos, targetRotation);
                            break;
                    }
                }
            }

            /// <summary>
            /// Полет к заданной точке с заданной скоростью
            /// </summary>
            public void FlyTo(Vector3D pos, float speed)
            {
                if (!pos.IsZero())
                {
                    targetPos = pos;
                    DesiredSpeed = speed;
                    MoveToTarget = true;
                    FlyMode = FlyType.Normal;
                }
            }

            public void FlyToWP(Waypoint wp)
            {
                targetPos = wp.Position;
                DesiredSpeed = (float)wp.SpeedLimit;
                targetRotation = wp.Orientation;
                FlyMode = FlyType.Normal;
                MoveToTarget = true;
            }

            public void AlignOnWP(RotateType type)
            {
                Rotate = true;
                RotateMode = type;
                OverrideGyro(true);
            }

            /// <summary>
            /// Текущая позиция грида
            /// </summary>
            public Vector3D GetPosition()
            {
                return shipController.GetPosition();
            }

            public Matrix GetOrientation()
            {
                return shipController.WorldMatrix;
            }

            /// <summary>
            /// Целевая позиция грида
            /// </summary>
            public Vector3D GetTargetPosition()
            {
                return targetPos;
            }

            public double GetPlanetElevation()
            {
                return shipController.TryGetPlanetElevation(MyPlanetElevation.Surface, out distanceToGround) ? distanceToGround : 0;
            }

            public double GetSpeed()
            {
                return speed;
            }

            /// <summary>
            /// Локальное движение грида по заданным направлениям, направление и на сколько сдвинуть грид
            /// </summary>
            public Vector3D GetShipLocalDrift(Base6Directions.Direction dir, float distance)
            {
                switch (dir)
                {
                    case Base6Directions.Direction.Up:
                        return shipController.GetPosition() + shipController.WorldMatrix.Up * distance;

                    case Base6Directions.Direction.Down:
                        return shipController.GetPosition() + shipController.WorldMatrix.Down * distance;

                    case Base6Directions.Direction.Left:
                        return shipController.GetPosition() + shipController.WorldMatrix.Left * distance;

                    case Base6Directions.Direction.Right:
                        return shipController.GetPosition() + shipController.WorldMatrix.Right * distance;

                    case Base6Directions.Direction.Forward:
                        return shipController.GetPosition() + shipController.WorldMatrix.Forward * distance;

                    case Base6Directions.Direction.Backward:
                        return shipController.GetPosition() + shipController.WorldMatrix.Backward * distance;
                }
                return Vector3D.Zero;
            }

            /// <summary>
            /// Локальное движение грида по заданным направлениям, направление и на сколько сдвинуть грид
            /// </summary>
            public Vector3D GetShipGlobalDrift(Vector3D pos, float fwd, float up, float left)
            {
                return pos + shipController.WorldMatrix.Forward * fwd + shipController.WorldMatrix.Up * up + shipController.WorldMatrix.Left * left;
            }

            /// <summary>
            /// Движение грида впперед с постоянной скоростью
            /// </summary>
            public void ForwardMove(float speed)
            {
                MoveToTarget = true;
                ForwardMiningSpeed = speed;
                FlyMode = FlyType.ForwardConst;
            }

            /// <summary>
            /// Остановка грида
            /// </summary>
            public void FullStop()
            {
                MoveToTarget = false;
                Rotate = false;
                FlyMode = FlyType.Normal;

                ReleaseEngines();
                OverrideGyro(false);
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
                speed = shipController.GetShipSpeed();

                position = shipController.GetPosition();
                orientation = shipController.WorldMatrix.GetOrientation();
                radius = shipController.CubeGrid.WorldVolume.Radius;
                linearVelocity = shipController.GetShipVelocities().LinearVelocity;
                elevationVelocity = Vector3D.Dot(linearVelocity, upVector);

                forwadSpeedComponent = linearVelocity.Dot(forwardVector);

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
                    distanceToGround = 0;
                    gravityDownVector = downVector;
                    gravityUpVector = upVector;
                }

                path = targetPos - position;
                PathLen = (float)path.Length();
                pathNormal = Vector3D.Normalize(path);

            }

            void RefreshEngines()
            {
                //Реверс по осям для определения силы торможения 
                maxThrust[Base6Directions.Direction.Forward] = backwardThrusters.Sum(t => t.MaxEffectiveThrust);
                maxThrust[Base6Directions.Direction.Backward] = forwardThrusters.Sum(t => t.MaxEffectiveThrust);

                maxThrust[Base6Directions.Direction.Up] = downThrusters.Sum(t => t.MaxEffectiveThrust);
                maxThrust[Base6Directions.Direction.Down] = upThrusters.Sum(t => t.MaxEffectiveThrust);

                maxThrust[Base6Directions.Direction.Left] = rightThrusters.Sum(t => t.MaxEffectiveThrust);
                maxThrust[Base6Directions.Direction.Right] = leftThrusters.Sum(t => t.MaxEffectiveThrust);
            }

            void ReleaseEngines()
            {
                foreach (IMyThrust thruster in thrusters)
                {
                    thruster.ThrustOverride = 0;
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
                if (PathLen == 0.0f)
                {
                    return;
                }

                if (PathLen < StoppingAccuracyDistance)
                {
                    MovingFinishedNotify?.Invoke();
                    FullStop();
                    return;
                }

                RefreshEngines();

                Vector3D force, directVel, directNormal, indirectVel;
                double timeToEnd, maxFrc, maxVel, maxAcc, TIME_STEP = 2.5 * TICK_TIME, smooth;

                force = mass * naturalGravity;
                directVel = Vector3D.ProjectOnVector(ref linearVelocity, ref pathNormal);
                directNormal = Vector3D.Normalize(directVel);

                if (!directNormal.IsValid())
                {
                    directNormal = Vector3D.Zero;
                }

                maxFrc = GetMaxThrust(pathNormal) - ((Vector3D.Dot(force, pathNormal) > 0) ? Vector3D.ProjectOnVector(ref force, ref pathNormal).Length() : 0.0);
                maxVel = Math.Sqrt(2.0 * PathLen * maxFrc / mass);
                smooth = Math.Min(Math.Max((DesiredSpeed + 1.0f - directVel.Length()) / 2.0f, 0.0f), 1.0f);
                maxAcc = 1.0f + (maxFrc / mass) * smooth * smooth * (3.0f - 2.0f * smooth);
                timeToEnd = Math.Max(TIME_STEP, Math.Abs(maxVel / maxAcc));
                force += mass * -2.0 * (pathNormal * PathLen / timeToEnd / timeToEnd - directNormal * directVel.Length() / timeToEnd);
                indirectVel = Vector3D.ProjectOnPlane(ref linearVelocity, ref pathNormal);
                force += mass * indirectVel / TIME_STEP;

                forwardChange = (float)Vector3D.Dot(force, gridForwardVect);
                upChange = (float)Vector3D.Dot(force, gridUpVect);
                leftChange = (float)Vector3D.Dot(force, gridLeftVect);

                program.Echo($"L:{leftChange}\nU:{upChange}");

                if (FlyMode == FlyType.ForwardConst)
                {
                    if (InGravity)
                    {
                        forwardChange = (float)((float)((forwadSpeedComponent - ForwardMiningSpeed) * mass) - forwardVector.Dot(-naturalGravity) * mass);
                    }
                    else
                    {
                        forwardChange = (float)((forwadSpeedComponent - ForwardMiningSpeed) * mass);
                    }
                }

                FireThrusters();
            }

            void FireThrusters()
            {
                foreach (var tr in forwardThrusters)
                {
                    tr.ThrustOverridePercentage = (forwardChange > 0) ? IDLE_POWER : Drain(ref forwardChange, tr.MaxEffectiveThrust);
                }
                foreach (var tr in backwardThrusters)
                {
                    tr.ThrustOverridePercentage = (forwardChange < 0) ? IDLE_POWER : Drain(ref forwardChange, tr.MaxEffectiveThrust);
                }

                foreach (var tr in leftThrusters)
                {
                    tr.ThrustOverridePercentage = (leftChange > 0) ? IDLE_POWER : Drain(ref leftChange, tr.MaxEffectiveThrust);
                }
                foreach (var tr in rightThrusters)
                {
                    tr.ThrustOverridePercentage = (leftChange < 0) ? IDLE_POWER : Drain(ref leftChange, tr.MaxEffectiveThrust);
                }

                foreach (var tr in upThrusters)
                {
                    tr.ThrustOverridePercentage = (upChange > 0) ? IDLE_POWER : Drain(ref upChange, tr.MaxEffectiveThrust);
                }
                foreach (var tr in downThrusters)
                {
                    tr.ThrustOverridePercentage = (upChange < 0) ? IDLE_POWER : Drain(ref upChange, tr.MaxEffectiveThrust);
                }
            }

            public void RotateByMatrix(Matrix rotate)
            {
                var targetRoll = Vector3D.Dot(shipController.WorldMatrix.Left, Vector3D.Reject(Vector3D.Normalize(-rotate.Down), shipController.WorldMatrix.Forward));
                targetRoll = Math.Acos(targetRoll) - Math.PI / 2;

                var targetPitch = Vector3D.Dot(shipController.WorldMatrix.Up, Vector3D.Reject(Vector3D.Normalize(-rotate.Forward), shipController.WorldMatrix.Left));
                targetPitch = Math.Acos(targetPitch) - Math.PI / 2;

                var targetYaw = Vector3D.Dot(shipController.WorldMatrix.Forward, Vector3D.Reject(Vector3D.Normalize(-rotate.Left), shipController.WorldMatrix.Up));
                targetYaw = Math.Acos(targetYaw) - Math.PI / 2;

                var calcPosition = (position + rotate.Forward) - position;

                Vector3D resultVector = Vector3D.Normalize(calcPosition).Cross(shipController.WorldMatrix.Forward);
                resultVector += shipController.WorldMatrix.Backward * targetRoll;
                resultVector += shipController.WorldMatrix.Left * targetPitch;
                resultVector += shipController.WorldMatrix.Up * targetYaw;

                SetGyro(resultVector);
            }

            public void RotateByVectorAndMatrix(Vector3D point, Matrix rotate)
            {
                var targetRoll = Vector3D.Dot(shipController.WorldMatrix.Left, Vector3D.Reject(Vector3D.Normalize(-rotate.Down), shipController.WorldMatrix.Forward));
                targetRoll = Math.Acos(targetRoll) - Math.PI / 2;

                var calcPosition = point - position;

                Vector3D resultVector = Vector3D.Normalize(calcPosition).Cross(shipController.WorldMatrix.Forward);
                resultVector += shipController.WorldMatrix.Backward * targetRoll;

                SetGyro(resultVector);
            }

            void SetGyro(Vector3D axis)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up) * 1;
                    gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right) * 1;
                    gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward) * 1;
                }
            }

            void OverrideGyro(bool enable)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = enable;
                }
            }


        }

        public class Navigation
        {
            public bool Finish { get; private set; }
            public bool Reverse { get; private set; }
            public int CurrentWaypoint { get; private set; }
            public int TotalWaypoitns { get; private set; }

            Program main;
            List<Waypoint> waypoints;

            public Navigation(Program program)
            {
                main = program;
                waypoints = new List<Waypoint>();
                CurrentWaypoint = 0;

            }

            public int GetWaypointsCount()
            {
                return waypoints.Count;
            }

            public void WaypointAdd(Vector3D pos, Matrix orientation, float speed, WaypointType type = WaypointType.Navigating)
            {
                waypoints.Add(new Waypoint(pos, orientation, speed, type));
            }

            public void WaypointAdd(Waypoint WP)
            {
                waypoints.Add(WP);
            }

        }

        public class Waypoint
        {
            public float SpeedLimit { get; set; }
            public Vector3D Position { get; set; }
            public Matrix Orientation { get; set; }
            public WaypointType Type { get; set; }

            public Waypoint(Vector3D pos,Matrix orient, float speed, WaypointType type = WaypointType.Navigating)
            {
                Position = pos;
                Orientation = orient;
                SpeedLimit = speed;
                Type = type;
            }


        }

        public enum WaypointType
        {
            Navigating=0,
            Docking=1,
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

        public class PerformanceMonitor
        {
            public int TotalInstructions { get; private set; }
            public int MaxInstructions { get; private set; }
            public double UpdateTime { get; private set; }
            public int CallPerTick { get; private set; }
            public double AverageInstructionsPerTick { get; private set; }
            public double AverageTimePerTick { get; private set; }

            public double MaxInstructionsPerTick { get; private set; }

            private double avrInst;
            private double avrTime;

            private Program mainProgram;

            private IMyTextSurface mainDisplay;


            public PerformanceMonitor(Program main)
            {
                mainProgram = main;
                CallPerTick = 0;
                AverageInstructionsPerTick = 0;
                AverageTimePerTick = 0;
                avrInst = 0;
                avrTime = 0;

            }

            public PerformanceMonitor(Program main, IMyTextSurface display)
            {
                mainProgram = main;
                CallPerTick = 0;
                AverageInstructionsPerTick = 0;
                AverageTimePerTick = 0;
                avrInst = 0;
                avrTime = 0;
                mainDisplay = display;

            }


            public void AddInstructions(string methodName)
            {
                TotalInstructions = mainProgram.Runtime.CurrentInstructionCount;
                MaxInstructions = mainProgram.Runtime.MaxInstructionCount;
                avrInst += TotalInstructions;

                UpdateTime = mainProgram.Runtime.LastRunTimeMs;
                avrTime += UpdateTime;

                CallPerTick++;

                //var currMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            }

            public void EndOfFrameCalc()
            {
                AverageInstructionsPerTick = avrInst / CallPerTick;

                if (MaxInstructionsPerTick < avrInst)
                    MaxInstructionsPerTick = avrInst;



                avrInst = 0;

                AverageTimePerTick = avrTime / CallPerTick;
                avrTime = 0;

                CallPerTick = 0;

            }

            public void Draw()
            {
                mainDisplay?.WriteText("", false);
                mainDisplay?.WriteText($"CUR ins: {TotalInstructions} / Max: {MaxInstructions}" +
                                      $"\nAV inst: {AverageInstructionsPerTick} / {MaxInstructionsPerTick}" +
                                      $"\nAV time:{UpdateTime}", true);
            }

        }


        ///END OF SCRIPT///////////////
    }

}

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

namespace SpaceEngineers.Autominer.Autominer
{
    public sealed class Program : MyGridProgram
    {
        PerformanceMonitor monitor;
        MovementCommander mover;

        //TMP vars
        int state = 0;
        int nextDriftState = 0;
        float miningDist = 50;
        float shaftSize = 17;
        float miningSpeed = 0.5f;
        float miningDepth = 0;

        Vector3D initMiningPos = new Vector3D();
        Vector3D initShaftPos = new Vector3D();

        float leftShaftDrift = 0;
        float upShaftDrift = 0;
        float maxShafts = 1;



        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            monitor = new PerformanceMonitor(this, Me.GetSurface(1));
            mover = new MovementCommander(this);

            mover.MovingFinishedNotify += Mover_MovingFinishedNotify;
        }

        private void Mover_MovingFinishedNotify()
        {
            if (state == 0)//drift
            {
                state = 1;
                return;
            }

            if (state == 1)
            {
                state = 2;
                return;
            }

        }

        public void Main(string args, UpdateType updateType)
        {
            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                Command(args);

            if ((updateType & UpdateType.Update1) != 0)
            {
                //DriftingMiner();
                NormalMiner();
            }

            Echo($"Drill dist: {miningDist} m" +
                 $"\nCurrent depth: {miningDepth}" +
                 $"\nMining speed: {miningSpeed}" +
                 $"\nShaft size: {shaftSize} m" +
                 $"\nRadio alt: {mover.GetPlanetElevation()} m" +
                 $"\nState: {state}" +
                 $"\nShafts: {maxShafts * 2 + maxShafts} X {maxShafts * 2 + maxShafts} " +
                 $"\nNext:\nLeftDrift: {leftShaftDrift}" +
                 $"\nUpDrift: {upShaftDrift}");

            mover.Update();

            monitor.AddInstructions("");
            monitor.EndOfFrameCalc();
            monitor.Draw();
        }

        public void DriftingMiner()
        {
            if (mover.MoveToTarget && mover.FlyMode == MovementCommander.FlyType.ForwardConst)
            {
                miningDepth = (float)(initMiningPos - mover.GetPosition()).Length();

                if (miningDepth >= miningDist)
                {
                    mover.FullStop();
                    mover.FlyTo(initMiningPos, 5);
                }
            }

            if (!mover.MoveToTarget && state == 1)
            {
                Vector3D dir = new Vector3D();

                switch (nextDriftState)
                {
                    case 0:
                        dir = mover.GetShipLocalDrift(Base6Directions.Direction.Left, shaftSize);
                        nextDriftState = 1;
                        break;
                    case 1:
                        dir = mover.GetShipLocalDrift(Base6Directions.Direction.Down, shaftSize);
                        nextDriftState = 2;
                        break;
                    case 2:
                        dir = mover.GetShipLocalDrift(Base6Directions.Direction.Right, shaftSize);
                        nextDriftState = 3;
                        break;
                    case 3:
                        mover.FullStop();
                        nextDriftState = 0;
                        state = -1;
                        return;
                }

                mover.FlyTo(dir, 5);
                state = 2;
            }

            if (!mover.MoveToTarget && state == 2)
            {
                mover.ForwardMove(miningSpeed);
                initMiningPos = mover.GetPosition();
                state = 0;
            }
        }

        public void NormalMiner()
        {
            if (mover.MoveToTarget && mover.FlyMode == MovementCommander.FlyType.ForwardConst)
            {
                miningDepth = (float)(initShaftPos - mover.GetPosition()).Length();

                if (miningDepth >= miningDist)
                {
                    mover.FullStop();
                    mover.FlyTo(initShaftPos, 5);
                }
            }

            if (!mover.MoveToTarget && state == 1)
            {
                var dir = new Vector3D();
                dir = mover.GetShipGlobalDrift(initMiningPos, 0, upShaftDrift * shaftSize, leftShaftDrift * shaftSize);

                mover.FlyTo(dir, 5);
                state = 2;
                NextShaft();
            }

            if (!mover.MoveToTarget && state == 2) 
            {
                mover.ForwardMove(miningSpeed);
                initShaftPos = mover.GetPosition();
                state = 0;
            }

            if (!mover.MoveToTarget && state == 3)
            {
                mover.FlyTo(initMiningPos, 5);
                state = -1;
            }
        }

        public void NextShaft()
        {
            leftShaftDrift -= 1;

            if (Math.Abs(upShaftDrift) > maxShafts)
            {
                mover.FullStop();
                state = 3;
                return;
            }

            if (Math.Abs(leftShaftDrift) > maxShafts)
            {
                leftShaftDrift = 1 * maxShafts;
                upShaftDrift -= 1;
            }
        }
      

        public void Command(string commands)
        {
            string comm = commands.ToUpper();

            if (comm.Contains("SETMINEDEPTH"))
            {
                try
                {
                    var str = comm.Split(':');

                    if (str.Any())
                    {
                        int depth = 1;
                        if (int.TryParse(str[1], out depth))
                        {
                            miningDist = depth;
                            return;
                        }
                    }
                }
                catch
                {

                }
            }

            if (comm.Contains("SETSHAFTDRIFT"))
            {
                try
                {
                    var str = comm.Split(':');

                    if (str.Any())
                    {
                        int drift = 1;
                        if (int.TryParse(str[1], out drift))
                        {
                            shaftSize = drift;
                            return;
                        }
                    }
                }
                catch
                {

                }
            }

            if (comm.Contains("SETMINESPEED"))
            {
                try
                {
                    var str = comm.Split(':');

                    if (str.Any())
                    {
                        int speed = 1;
                        if (int.TryParse(str[1], out speed))
                        {
                            miningSpeed = speed;
                            return;
                        }
                    }
                }
                catch
                {

                }
            }

            if (comm.Contains("SETMSHAFTSQUARE"))
            {
                try
                {
                    var str = comm.Split(':');

                    if (str.Any())
                    {
                        int square = 1;
                        if (int.TryParse(str[1], out square))
                        {
                            maxShafts = square;
                            return;
                        }
                    }
                }
                catch
                {

                }
            }


            switch (comm)
            {
                case "MINE":
                   // mover.ForwardMove(miningSpeed);
                    initMiningPos = mover.GetPosition();
                    initShaftPos = initMiningPos;
                    state = 1;
                    upShaftDrift = 1 * maxShafts;
                    leftShaftDrift = 1 * maxShafts;
                    break;

                case "STOP":
                    mover.FullStop();
                    state = 0;
                    nextDriftState = 0;
                    break;
            }

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
            //Основные параметры
            public bool MoveToTarget { get; private set; }
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

            public enum FlyType
            {
                Normal,
                ForwardConst
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
                if (MoveToTarget)
                {
                    GetShipParams();
                    RefreshEngines();
                    ThrusterTick();
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

            /// <summary>
            /// Текущая позиция грида
            /// </summary>
            public Vector3D GetPosition()
            {
                return shipController.GetPosition();
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
            public Vector3D GetShipGlobalDrift(Vector3D pos, float fwd,float up,float left)
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
                targetPos = shipController.GetPosition() + shipController.WorldMatrix.Forward * 1000;
                FlyMode = FlyType.ForwardConst;
            }

            /// <summary>
            /// Остановка грида
            /// </summary>
            public void FullStop()
            {
                MoveToTarget = false;
                FlyMode = FlyType.Normal;

                ReleaseEngines();
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

                var size = shipController.CubeGrid.GridSize;
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
                    ReleaseEngines();
                    MoveToTarget = false;
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
                maxVel = Math.Sqrt(2.0 * PathLen * maxFrc / massFix);
                smooth = Math.Min(Math.Max((DesiredSpeed + 1.0f - directVel.Length()) / 2.0f, 0.0f), 1.0f);
                maxAcc = 1.0f + (maxFrc / massFix) * smooth * smooth * (3.0f - 2.0f * smooth);
                ttt = Math.Max(TIME_STEP, Math.Abs(maxVel / maxAcc));
                force += massFix * -2.0 * (pathNormal * PathLen / ttt / ttt - directNormal * directVel.Length() / ttt);
                indirectVel = Vector3D.ProjectOnPlane(ref linearVelocity, ref pathNormal);
                force += massFix * indirectVel / TIME_STEP;

                forwardChange = (float)Vector3D.Dot(force, gridForwardVect);
                upChange = (float)Vector3D.Dot(force, gridUpVect);
                leftChange = (float)Vector3D.Dot(force, gridLeftVect);

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
                mainDisplay.WriteText("", false);
                mainDisplay.WriteText($"CUR ins: {TotalInstructions} / Max: {MaxInstructions}" +
                                      $"\nAV inst: {AverageInstructionsPerTick} / {MaxInstructionsPerTick}" +
                                      $"\nAV time:{UpdateTime}", true);
            }

        }


        ///END OF SCRIPT///////////////
    }

}

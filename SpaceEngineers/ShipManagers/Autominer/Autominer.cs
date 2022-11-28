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

            public bool PlanetDetected { get; private set; }
            public bool InGravity { get; private set; }

            float pathLen;
            const float IDLE_POWER = 0.0000001f;

            double elevationVelocity;
            double distanceToGround;
            double radius;
            const double TICK_TIME = 0.16666f;

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

            Vector3D maxThrottle;

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

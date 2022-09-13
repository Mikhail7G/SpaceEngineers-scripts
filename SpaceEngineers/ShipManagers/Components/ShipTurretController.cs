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
using VRage.Game.ModAPI.Ingame.Utilities;

namespace SpaceEngineers.ShipManagers.Components.TurretController

{
    public sealed class Program : MyGridProgram
    {
        string antennaName = "Ant";
        string lightName = "TurretBeacon";
        string missileTagResiever = "ch1R";//Получаем данные от системы целеуказания по радиоканалу
        string TurretGroupName = "Turrets";

        string mainCocpit = "Main";

        IMyBroadcastListener listener;//слушаем эфир на получение данных о целях по радио

        IMyRadioAntenna antenna;

        IMyInteriorLight turretArmedStaLight;

        IMyShipController control;

        Vector3D targetPosition;
        Vector3D targetSpeed;

        ShipTurretController turretController;
        PerformanceMonitor monitor;
        MyIni dataSystem;

        public Program()
        {

            dataSystem = new MyIni();
            GetIniData();

            targetPosition = new Vector3D();
            targetSpeed = new Vector3D();

            targetPosition = Vector3D.Zero;

            listener = IGC.RegisterBroadcastListener(missileTagResiever);
            listener.SetMessageCallback(missileTagResiever);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
            turretArmedStaLight = GridTerminalSystem.GetBlockWithName(lightName) as IMyInteriorLight;
            control = GridTerminalSystem.GetBlockWithName(mainCocpit) as IMyShipController;


            var group = GridTerminalSystem.GetBlockGroupWithName(TurretGroupName);
            turretController = new ShipTurretController(group, this);
            turretController.ArmedNotify += ArmedNotify;

            monitor = new PerformanceMonitor(this, Me.GetSurface(1));


        }

        public void GetIniData()
        {
            InitCustomData();

            Echo($"Reading custom data");
            MyIniParseResult dataResult;
            if (!dataSystem.TryParse(Me.CustomData, out dataResult))
            {
                Echo($"CustomData error:\nLine {dataResult}");
            }
            else
            {

                antennaName = dataSystem.Get("Names", "AntennaName").ToString();
                lightName = dataSystem.Get("Names", "TurretBeacon").ToString();
                missileTagResiever = dataSystem.Get("Names", "RadioChannel").ToString();
                TurretGroupName = dataSystem.Get("Names", "TurretGroupName").ToString();
            }
        }

        public void InitCustomData()
        {
            var data = Me.CustomData;

            if (data.Length == 0)
            {
                Echo("Custom data empty!");

                dataSystem.AddSection("Names");
                dataSystem.Set("Names", "AntennaName", "Ant");
                dataSystem.Set("Names", "TurretBeacon", "TurretBeacon");
                dataSystem.Set("Names", "RadioChannel", "ch1R");
                dataSystem.Set("Names", "TurretGroupName", "Turrets");

                Me.CustomData = dataSystem.ToString();
            }

            Echo("Custom data ready");
        }

        private void ArmedNotify()
        {
            if(turretController.Armed)
            {
                if (turretArmedStaLight != null && !turretArmedStaLight.Closed)
                {
                    turretArmedStaLight.Color = Color.Red;
                }              
            }
            else
            {
                if (turretArmedStaLight != null && !turretArmedStaLight.Closed)
                {
                    turretArmedStaLight.Color = Color.White;
                }
            }
        }

        public void Main(string args, UpdateType updateType)
        {
            GetTargetByRadio();

            Update();

            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                turretController.Command(args);

            monitor.AddInstructions("");
            monitor.EndOfFrameCalc();
            monitor.Draw();

        }

        void Update()
        {
            SendTargetToController();
            turretController.UpdateTurrets();
        }

        void SendTargetToController()
        {
            if(!targetPosition.IsZero())
            {
                turretController.SetTarget(targetPosition, targetSpeed);
            }
        }

        public void Save()
        {

        }

        public void GetTargetByRadio()
        {
            while (listener.HasPendingMessage)
            {
                MyIGCMessage mess = listener.AcceptMessage();
                if (mess.Tag == missileTagResiever)
                {
                    string[] str = mess.Data.ToString().Split('|');
                    ///координаты цели
                    double.TryParse(str[0], out targetPosition.X);
                    double.TryParse(str[1], out targetPosition.Y);
                    double.TryParse(str[2], out targetPosition.Z);
                    ///его вектор скорости
                    double.TryParse(str[3], out targetSpeed.X);
                    double.TryParse(str[4], out targetSpeed.Y);
                    double.TryParse(str[5], out targetSpeed.Z);
                }
            }
        }



        /// <summary>
        /// Управление турелями вручную
        /// </summary>
        public class ShipTurretController
        {
            public bool Armed { set; get; }

            double distanceToTargetSqr;

            Vector3D targetPosition;
            Vector3D targetSpeed;

            IMyShipController controller;


            Program mainProgram;
            List<IMyLargeTurretBase> turrets;

            public delegate void ArmStatusHandler();
            public event ArmStatusHandler ArmedNotify;

            public ShipTurretController(IMyBlockGroup group, Program mainProg)
            {
                Armed = false;
                targetPosition = new Vector3D();
                targetSpeed = new Vector3D();

              

                turrets = new List<IMyLargeTurretBase>();
                mainProgram = mainProg;


                controller = mainProgram.GridTerminalSystem.GetBlockWithName("Main") as IMyShipController;

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                if (group == null)
                {

                }    
                group?.GetBlocks(blocks);

                turrets = blocks.Where(b => b is IMyLargeTurretBase).Select(b => b as IMyLargeTurretBase).ToList();

                mainProgram.Echo($"Total turrets: {turrets.Count}" +
                                 $"\nArmed: {Armed}");

            }

            public void Command(string command)
            {
                string com = command.ToUpper();

                switch (com)
                {
                    case "ARM":
                        ArmTurrets();
                        break;
                    case "DISARM":
                        DisarmTurrets();
                        break;

                    case "MAXRANGE":
                        EnableFiring();
                        break;

                    case "ZERORANGE":
                        DisabeFiring();
                        break;

                    case "RESET":
                        ResetRotation();
                        break;

                    case "DIST":
                        SyncOnDistance(0);
                        break;
                }
            }

            void ArmTurrets()
            {
                Armed = true;
                ArmedNotify.Invoke();
            }

            void DisarmTurrets()
            {
                Armed = false;
                ArmedNotify.Invoke();
                targetPosition = new Vector3D(0,0,0);
                ResetRotation();
            }

            void SyncOnDistance(float dist)
            {
                foreach (var turret in turrets)
                {
                    if (turret.Closed)
                        continue;

                    var dir = (controller.GetPosition() + controller.WorldMatrix.Forward * 800);

                   // dir = VectorTransform(dir, turret.WorldMatrix.GetOrientation());

                    float azimuth = (float)Math.Atan2(-dir.X, dir.Z);
                    float elevation = (float)Math.Asin(dir.Y / dir.Length());

                    turret.SetTarget(dir);
                    //turret.SetManualAzimuthAndElevation((float)(azimuth * 0.017), (float)(elevation * 0.017));
                    turret.SyncElevation();
                    turret.SyncAzimuth();
                }

            }

            public Vector3D VectorTransform(Vector3D vec, MatrixD orientation)
            {
                return new Vector3D(vec.Dot(orientation.Right), vec.Dot(orientation.Up), vec.Dot(orientation.Backward));
            }

            void EnableFiring()
            {
                foreach (var turret in turrets)
                {
                    turret.Range = 900000;
                }
            }

            void DisabeFiring()
            {
                foreach (var turret in turrets)
                {
                    if (turret.Closed)
                        continue;

                    turret.Range = 1;
                }
            }

            public void UpdateTurrets()
            {
                RotateAndFire();
            }

            public void SetTarget(Vector3D pos,Vector3D speed)
            {
                targetPosition = pos;
                targetSpeed = speed;
            }

            public void ResetRotation()
            {
                foreach (var turret in turrets)
                {
                    if (turret.Closed)
                        continue;

                    turret.Shoot = false;
                    turret.SetManualAzimuthAndElevation(0, 0);
                    turret.SyncElevation();
                    turret.SyncAzimuth();
                }
            }

            void RotateAndFire()
            {
                mainProgram.Echo($"Armed:{Armed}");

                if (!Armed)
                    return;

                distanceToTargetSqr = Vector3D.DistanceSquared(mainProgram.Me.GetPosition(), targetPosition);

                foreach (var turret in turrets)
                {
                    if (turret.Closed)
                        continue;

                    //Поворот на цель 
                    turret.TrackTarget(targetPosition, targetSpeed);

                    turret.SyncElevation();
                    turret.SyncAzimuth();

                    //Если радиуса хватает, то стрелять
                    if (distanceToTargetSqr < turret.Range * turret.Range)
                    {

                        if (turret.IsAimed)
                        {
                            turret.Shoot = true;
                        }
                        else
                        {
                            turret.Shoot = false;
                        }
                    }
                    else
                    {
                        turret.Shoot = false;
                    }
 
                }
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

                if (mainDisplay != null)
                {
                    mainDisplay.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    mainDisplay.FontSize = 1;
                }

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
                                      $"\nAV time:{AverageTimePerTick}", true);
            }

        }


        ///////////////////////////
    }
}
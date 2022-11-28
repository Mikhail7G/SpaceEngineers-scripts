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
using static VRageMath.Base6Directions;
using System.ComponentModel.DataAnnotations;

namespace SpaceEngineers.ShipManagers.Components.TurretController

{
    public sealed class Program : MyGridProgram
    {
        string antennaName = "Ant";
        string lightName = "TurretBeacon";
        string missileTagResiever = "ch1R";//Получаем данные от системы целеуказания по радиоканалу
        string TurretGroupName = "Turrets";

        string mainCocpit = "Main";

        string designatorTurretTag = "[d]";
        string designatorDisplayName = "DesRadar";
        float gyroMult = 5;
        float ammoAcc = 1;

        IMyBroadcastListener listener;//слушаем эфир на получение данных о целях по радио

        IMyRadioAntenna antenna;

        IMyInteriorLight turretArmedStaLight;

        IMyShipController control;

        Vector3D targetPosition;
        Vector3D targetSpeed;

        ShipTurretController turretController;
        ShipGyroController gyroController;
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

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);
            gyroController = new ShipGyroController(blocks, this);
            gyroController.DesignatorTagName = designatorTurretTag;
            gyroController.DesignatorDisplayName = designatorDisplayName;
            gyroController.GyroMult = gyroMult;
            gyroController.AmmoAcc = ammoAcc;

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

                designatorTurretTag = dataSystem.Get("AutoAlingSystem", "DesignatorTurretTag").ToString();
                designatorDisplayName = dataSystem.Get("AutoAlingSystem", "DesignatorDisplayName").ToString();
                gyroMult = dataSystem.Get("AutoAlingSystem", "GyroMult").ToInt32();
                ammoAcc = dataSystem.Get("AutoAlingSystem", "AmmoAcc").ToInt32();
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

                dataSystem.AddSection("AutoAlingSystem");
                dataSystem.Set("AutoAlingSystem", "DesignatorTurretTag", "[d]");
                dataSystem.Set("AutoAlingSystem", "DesignatorDisplayName", "DesRadar");
                dataSystem.Set("AutoAlingSystem", "GyroMult", 5);
                dataSystem.Set("AutoAlingSystem", "AmmoAcc", 1);

                Me.CustomData = dataSystem.ToString();
            }

            Echo("Custom data ready");
        }

        private void ArmedNotify()
        {
            if (turretController.Armed)
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
            {
                turretController.Command(args);
                gyroController.Command(args);
            }

            monitor.AddInstructions("");
            monitor.EndOfFrameCalc();
            monitor.Draw();

        }

        void Update()
        {
           // SendTargetToController();
            turretController.UpdateTurrets();
            gyroController.UpdateGyros();
        }

        void SendTargetToController()
        {
            if (!targetPosition.IsZero())
            {
                turretController.SetTarget(targetPosition, targetSpeed);
                gyroController.SetTarget(targetPosition, targetSpeed);
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

                    SendTargetToController();
                }
            }
        }



        /// <summary>
        /// Управление турелями вручную
        /// </summary>
        public class ShipTurretController
        {
            public bool Armed { set; get; }

            string mainCocpit = "Main";

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


                controller = mainProgram.GridTerminalSystem.GetBlockWithName(mainCocpit) as IMyShipController;
                string cocpitState = controller == null ? $"No cocpit with name: Main " : "Cocpit OK";

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                if (group == null)
                {

                }
                group?.GetBlocks(blocks);

                turrets = blocks.Where(b => (b is IMyLargeTurretBase)).Select(b => b as IMyLargeTurretBase).ToList();

                mainProgram.Echo($"Total turrets: {turrets.Count}" +
                                 $"\nArmed: {Armed}" +
                                 $"\n{cocpitState}");



            }

            public void Command(string command)
            {
                string com = command.ToUpper();

                if (com.Contains("DIST"))
                {
                    try
                    {
                        var dist = com.Split(':');

                        if (dist.Any())
                        {
                            float syncDist = 1500;
                            if (float.TryParse(dist[1], out syncDist))
                            {
                                SyncOnDistance(syncDist);
                            }
                        }
                    }
                    catch
                    {

                    }
                }

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

                        //case "DIST":
                        //    SyncOnDistance(0);
                        //    break;
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
                targetPosition = new Vector3D(0, 0, 0);
                ResetRotation();
            }

            void SyncOnDistance(float dist)
            {
                foreach (var turret in turrets)
                {
                    if (turret.Closed)
                        continue;


                    var dir = turret.GetPosition() - (controller.GetPosition() + controller.WorldMatrix.Forward * dist);
                    // var dir = Vector3D.Normalize(controller.WorldMatrix.Forward * 800 - turret.GetPosition());

                    dir = VectorTransform(dir, turret.WorldMatrix.GetOrientation());

                    float azimuth = (float)Math.Atan2(-dir.X, dir.Z);
                    float elevation = (float)Math.Asin(dir.Y / dir.Length());


                    turret.SetManualAzimuthAndElevation(-(float)(azimuth), -(float)(elevation));

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

            public void SetTarget(Vector3D pos, Vector3D speed)
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

        public class ShipGyroController
        {

            public string DesignatorTagName = "[d]";
            public string DesignatorDisplayName = "DesRadar";

            string mainCocpit = "Main";

            bool enableFollowing = false;
            bool designatorMode = true;

            public float GyroMult = 5;
            public float AmmoAcc = 1;

            float bulletSpeed = 5000;
           
            Vector3D targetPosition;
            Vector3D targetSpeed;

            Vector3D calcPosition;


            IMyShipController controller;

            IMyTextPanel designatorRadar;


            Program mainProgram;
            List<IMyGyro> gyros;
            List<IMyLargeTurretBase> designators;

            MyDetectedEntityInfo lastLockedTarget;
            Dictionary<long, MyDetectedEntityInfo> detectedTargets;


            public ShipGyroController(IMyBlockGroup group, Program mainProg)
            {
                mainProgram = mainProg;
                gyros = new List<IMyGyro>();
                designators = new List<IMyLargeTurretBase>();
                detectedTargets = new Dictionary<long, MyDetectedEntityInfo>();

                controller = mainProgram.GridTerminalSystem.GetBlockWithName(mainCocpit) as IMyShipController;
                string cocpitState = controller == null ? $"No cocpit with name: Main " : "Cocpit OK";

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                if (group == null)
                {

                }
                group?.GetBlocks(blocks);

                gyros = blocks.Where(b => (b is IMyGyro)).Select(b => b as IMyGyro).ToList();

                designators = blocks.Where(b => ((b is IMyLargeTurretBase) && (b.CustomName.Contains(DesignatorTagName)))).Select(b => b as IMyLargeTurretBase).ToList();

                designatorRadar = blocks.Where(b => ((b is IMyTextPanel) && (b.CustomName.Contains(DesignatorDisplayName)))).Select(b => b as IMyTextPanel).FirstOrDefault();

                if (designatorRadar != null) 
                {
                    designatorRadar.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    designatorRadar.FontSize = 1.1f;
                }

                mainProgram.Echo($"\n----Gyro controller-----" +
                                 $"\nTotal gyros: {gyros.Count}" +
                                 $"\n{cocpitState}");

            }

            public ShipGyroController(List<IMyTerminalBlock> blocks, Program mainProg)
            {
                mainProgram = mainProg;
                gyros = new List<IMyGyro>();
                designators = new List<IMyLargeTurretBase>();
                detectedTargets = new Dictionary<long, MyDetectedEntityInfo>();

                controller = mainProgram.GridTerminalSystem.GetBlockWithName(mainCocpit) as IMyShipController;
                string cocpitState = controller == null ? $"No cocpit with name: Main " : "Cocpit OK";

                gyros = blocks.Where(b => (b is IMyGyro)).Select(b => b as IMyGyro).ToList();

                designators = blocks.Where(b => ((b is IMyLargeTurretBase) && (b.CustomName.Contains(DesignatorTagName)))).Select(b => b as IMyLargeTurretBase).ToList();

                designatorRadar = blocks.Where(b => ((b is IMyTextPanel) && (b.CustomName.Contains(DesignatorDisplayName)))).Select(b => b as IMyTextPanel).FirstOrDefault();

                if (designatorRadar != null)
                {
                    designatorRadar.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    designatorRadar.FontSize = 1.1f;
                }

                mainProgram.Echo($"\n----Gyro controller-----" +
                                 $"\nTotal gyros: {gyros.Count}" +
                                 $"\n{cocpitState}");

            }

            public void Command(string command)
            {
                string com = command.ToUpper();

                if (com.Contains("RANGE"))
                {
                    try
                    {
                        var dist = com.Split(':');

                        if (dist.Any())
                        {
                            if (float.TryParse(dist[1], out bulletSpeed))
                            {
                                LockTarget();
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                switch (com)
                {
                    case "MODE":
                        designatorMode = !designatorMode;
                        break;
                }
            }

            public void SetTarget(Vector3D pos, Vector3D speed)
            {
                if (!designatorMode)
                {
                    targetPosition = pos;
                    targetSpeed = speed;
                }
            }

            public void SetTarget(MyDetectedEntityInfo info)
            {
                targetPosition = info.Position;
                targetSpeed = info.Velocity;
            }

            public void LockTarget()
            {
                if (designatorMode)
                {
                    if (detectedTargets.Any())
                    {
                        SetTarget(detectedTargets.First().Value);
                        lastLockedTarget = detectedTargets.First().Value;
                        enableFollowing = true;
                        OverrideGyros();
                    }
                }
                else
                {
                    if (!targetPosition.IsZero())
                    {
                        enableFollowing = true;
                        OverrideGyros();
                    }
                }
            }

            public void UpdateGyros()
            {
                UpdateDesignators();

                if (enableFollowing)
                {
                    calcPosition = GetPredictedTargetPosition(controller.GetPosition(), controller.GetShipVelocities().LinearVelocity, targetPosition, targetSpeed, bulletSpeed);
                    SetGyro(Vector3D.Normalize(calcPosition - controller.GetPosition()).Cross(controller.WorldMatrix.Forward));

                    MouseReset();
                }

            }

            public void UpdateDesignators()
            {
                mainProgram.Echo($"Total designators: {designators.Count}" +
                                 $"\nTrack: {enableFollowing}" +
                                 $"\nDesignator mode: {designatorMode}");

                designatorRadar?.WriteText("", false);

                detectedTargets.Clear();

                foreach (var des in designators)
                {
                    if (des.HasTarget)
                    {
                        var target = des.GetTargetedEntity();

                        if (!detectedTargets.ContainsKey(target.EntityId))
                        {
                            detectedTargets.Add(target.EntityId, target);
                        }
                    }
                }

                if(enableFollowing)
                {
                    if(detectedTargets.ContainsKey(lastLockedTarget.EntityId))
                    {
                        lastLockedTarget = detectedTargets[lastLockedTarget.EntityId];
                        SetTarget(lastLockedTarget);
                    }
                }


                designatorRadar?.WriteText($"Passive radar trg:{detectedTargets.Count} locked:{enableFollowing}", true);

                foreach (var entity in detectedTargets)
                {
                    var target = entity.Value;
                    var dir = target.Position - controller.GetPosition();
                    var dist = Vector3D.Distance(target.Position, controller.GetPosition());
                    var fwdDir = Vector3D.Normalize(dir).Dot(controller.WorldMatrix.Forward);
                    var speed = entity.Value.Velocity.Length();

                    designatorRadar?.WriteText($"\n-----Target------" +
                                               $"\nType:{target.Type}" +
                                               $"\nSignature:{target.BoundingBox.Volume}" +
                                               $"\nDir:{fwdDir}" +
                                               $"\nDist:{dist}" +
                                               $"\nSpeed:{speed}", true);
                }
            }


            public void MouseReset()
            {
                if (controller.RotationIndicator != Vector2.Zero)
                {
                    ClearGyros();
                }
            }


            public void OverrideGyros()
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.GyroOverride = true;
                }
            }

            public void ClearGyros()
            {
                enableFollowing = false;

                foreach (IMyGyro gyro in gyros)
                {
                    gyro.GyroOverride = false;
                }
            }

            public Vector3D GetPredictedTargetPosition(Vector3D myPos, Vector3 mySpeed, Vector3D targetPos, Vector3D targetSpeed, float shotSpeed)
            {

                var dist = Vector3D.Distance(myPos, targetPos);

                float newSpeed = (float)Math.Sqrt(2 * AmmoAcc * dist + shotSpeed * shotSpeed);

                shotSpeed = newSpeed;

                Vector3D predictedPosition = targetPos;
                Vector3D dirToTarget = Vector3D.Normalize(predictedPosition - myPos);


                Vector3 targetVelocity = targetSpeed;
                targetVelocity -= mySpeed;
                Vector3 targetVelOrth = Vector3.Dot(targetVelocity, dirToTarget) * dirToTarget;
                Vector3 targetVelTang = targetVelocity - targetVelOrth;
                Vector3 shotVelTang = targetVelTang;
                float shotVelSpeed = shotVelTang.Length();

                if (shotVelSpeed > shotSpeed)
                {

                    return Vector3.Normalize(targetSpeed) * shotSpeed;
                }
                else
                {

                    float shotSpeedOrth = (float)Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed);
                    Vector3 shotVelOrth = dirToTarget * shotSpeedOrth;
                    float timeDiff = shotVelOrth.Length() - targetVelOrth.Length();
                    var timeToCollision = timeDiff != 0 ? ((myPos - targetPos).Length()) / timeDiff : 0;
                    Vector3 shotVel = shotVelOrth + shotVelTang;
                    predictedPosition = timeToCollision > 0.01f ? myPos + (Vector3D)shotVel * timeToCollision : predictedPosition;
                    return predictedPosition;
                }
            }

            public void SetGyro(Vector3D axis)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up) * GyroMult;
                    gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right) * GyroMult;
                    gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward) * GyroMult;
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

                AverageTimePerTick = mainProgram.Runtime.LastRunTimeMs;
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
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

namespace SpaceEngineers.ShipManagers.Components
{
    public sealed class Program : MyGridProgram
    {

        /////////////////////////////////////////////////////////////

        const UpdateType CommandUpdate = UpdateType.Trigger | UpdateType.Terminal;

        string antennaName = "Ant";
        string missileTagSender = "ch1R";//Отправляем в канал ракет координаты цели
        string beeperName = "Beeper";

        //для радара 
        string radarCameraGroupName = "radarCams";
        string lcdRadarStatus = "RadarSta";
        string lcdTargetStatus = "RadarTarget";
        string observerCameraName = "radarCamOBS";

        string radarAlarmNorm = "Тревога 1";
        string radarAlarmTargetLost = "Тревога 2";

        double scanDistance = 10000;

        CameraRadar Radar;
        IMyRadioAntenna antenna;//антенна для передачи данных ракете, правильный метод
        IMySoundBlock beeper;//звуковое информирование о захвате цели

        MyIni dataSystem;

        int radarSoundTimer = 0;

        PerformanceMonitor monitor;

        public Program()
        {
            dataSystem = new MyIni();
            GetIniData();

            Radar = new CameraRadar(this)
            {
                RadarCameraGroupName = radarCameraGroupName,
                LcdRadarStatus = lcdRadarStatus,
                LcdTargetStatus = lcdTargetStatus,
                ObserverCameraName = observerCameraName,
                ScanDistance = scanDistance
            };
            Radar.RadarNotify += RadarNotify;

            Runtime.UpdateFrequency = UpdateFrequency.Update1;


            antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
            beeper = GridTerminalSystem.GetBlockWithName(beeperName) as IMySoundBlock;

            monitor = new PerformanceMonitor(this, Me.GetSurface(1));

        }

        /// <summary>
        /// Функция отправляет данные по радио, только в случае обнаружения цели от радара
        /// </summary>
        void RadarNotify()
        {
            SendMessageRadio();
        }

        public void Main(string args, UpdateType updateType)
        {
            if ((updateType & CommandUpdate) != 0)
                Commands(args);

            Radar.RadarUpdate();
            //SendMessageRadio();
            RadarSound();

            monitor.AddInstructions("");
            monitor.EndOfFrameCalc();
            monitor.Draw();

        }

        public void Commands(string command)
        {
            string comm = command.ToUpper();

            switch (comm)
            {
                case "RADAR.INIT":
                    Radar.InitRadar();
                    break;
                case "RADAR.SCAN":
                    Radar.ScanOnce();
                    break;
                case "RADAR.LOCK":
                    Radar.TargetPrecisionFollowing();
                    break;
                case "RADAR.LOCKCENTER":
                    Radar.TargetFollowing();
                    break;
            }
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
                radarCameraGroupName = dataSystem.Get("Names", "radarCameraGroupName").ToString();
                lcdRadarStatus = dataSystem.Get("Names", "lcdRadarStatus").ToString();
                lcdTargetStatus = dataSystem.Get("Names", "lcdTargetStatus").ToString();
                observerCameraName = dataSystem.Get("Names", "observerCameraName").ToString();
                missileTagSender = dataSystem.Get("Names", "radioSenderTag").ToString();

                scanDistance = dataSystem.Get("Variables", "ScanDistance").ToDouble();
            }
        }

        public void InitCustomData()
        {
            var data = Me.CustomData;

            if (data.Length == 0)
            {
                Echo("Custom data empty!");

                dataSystem.AddSection("Names");
                dataSystem.Set("Names", "radarCameraGroupName", "radarCams");
                dataSystem.Set("Names", "lcdRadarStatus", "RadarSta");
                dataSystem.Set("Names", "lcdTargetStatus", "RadarTarget");
                dataSystem.Set("Names", "observerCameraName", "radarCamOBS");
                dataSystem.Set("Names", "radioSenderTag", "ch1R");

                dataSystem.AddSection("Variables");
                dataSystem.Set("Variables", "ScanDistance", 10000);

                Me.CustomData = dataSystem.ToString();
            }

            Echo("Custom data ready");
        }

        public void RadarSound()
        {
            if (Radar.TrackTarget)
            {
                radarSoundTimer++;

                if (radarSoundTimer > 120)
                {
                    beeper.Play();
                    radarSoundTimer = 0;
                }
                if (Radar.LOCClosed)
                {
                    beeper.SelectedSound = radarAlarmNorm;

                }
                else
                {
                    beeper.SelectedSound = radarAlarmTargetLost;

                }
            }
            else
            {
                beeper.Stop();
                radarSoundTimer = 120;
            }

        }

        void SendMessageRadio()
        {
            if (!Radar.TargetInfo.IsEmpty())
            {
                Vector3D pos = new Vector3D();
                Vector3D speed = new Vector3D();
                Radar.GetPositonAndSpeed(out pos, out speed);

                ///координаты цели
                string sendingSignal = pos.X.ToString() +
                                       "|" + pos.Y.ToString() +
                                       "|" + pos.Z.ToString() +

                                       "|" + speed.X.ToString() + //скорость цели
                                       "|" + speed.Y.ToString() +
                                       "|" + speed.Z.ToString();

                IGC.SendBroadcastMessage(missileTagSender, sendingSignal, TransmissionDistance.TransmissionDistanceMax);
            }
           
        }

        public class CameraRadar
        {
            public string RadarCameraGroupName = "radarCams";
            public string LcdRadarStatus = "RadarSta";
            public string LcdTargetStatus = "RadarTarget";
            public string ObserverCameraName = "radarCamOBS";

            /// <summary>
            /// Проверка на перекрытие линии прицеливания
            /// </summary>
            public bool LOCClosed { get; private set; }

            /// <summary>
            /// Точное наведение на выбранную часть корабля
            /// </summary>
            public bool PrecisionFollowing { get; private set; }

            /// <summary>
            /// Отслеживание цели
            /// </summary>
            public bool TrackTarget { get; private set; }

            /// <summary>
            /// Дистацния сканирования 
            /// </summary>
            public double ScanDistance { get; set; }

            /// <summary>
            /// Сколько времени не было цели на прямой видимости в тиках
            /// </summary>
            public int LOCClosedTime { get; private set; }

            /// <summary>
            /// Текущая активная камера
            /// </summary>
            public IMyCameraBlock Camera { get; private set; }

            /// <summary>
            /// Скорость цели
            /// </summary>
            public Vector3D TargetSpeed { get; private set; }

            /// <summary>
            /// Координаты цели
            /// </summary>
            public Vector3D TargetPos { get; private set; }

            /// <summary>
            /// Место попадания луча
            /// </summary>
            public Vector3D HitPos { get; private set; }

            /// <summary>
            /// Рассчитанная точка для сопровождения цели
            /// </summary>
            public Vector3D CalculatedTargetPos { get; private set; }

            /// <summary>
            /// Выходной параметр для дальнейшего взаимодействия с целью
            /// </summary>
            public Vector3D CalculatedPosition { get; private set; }

            /// <summary>
            /// ID цели
            /// </summary>
            public double TargetId { get; private set; }

            /// <summary>
            /// Расстояние до цели
            /// </summary>
            public Vector3D DistanceToTarget { get; private set; }

            /// <summary>
            /// Текущая информация о цели
            /// </summary>
            public MyDetectedEntityInfo TargetInfo { get; private set; }

            /// <summary>
            /// Камеры, которые в текущий момент могут следить за целью
            /// </summary>
            public int CurrentAviableCameras { get; private set; }

            double avrCamDist = 0;

            private IMyTextPanel lcd;
            private IMyTextPanel lcdInfo;
            private List<IMyTerminalBlock> cameras;

            private IMyCameraBlock observerCamera;

            private int scanTick;
            private double tickLimit;

            private Vector3D HitInvert;

            private Program mainProgram;

            public delegate void RadarScanHandler();
            /// <summary>
            /// Сигнализатор захвата цели
            /// </summary>
            public event RadarScanHandler RadarNotify;

            public CameraRadar(Program program)
            {
                mainProgram = program;
                cameras = new List<IMyTerminalBlock>();
                TrackTarget = false;
                scanTick = 0;
                ScanDistance = 10000;

            }

            public void InitRadar()
            {
                lcd = mainProgram.GridTerminalSystem.GetBlockWithName(LcdRadarStatus) as IMyTextPanel;
                lcdInfo = mainProgram.GridTerminalSystem.GetBlockWithName(LcdTargetStatus) as IMyTextPanel;
                observerCamera = mainProgram.GridTerminalSystem.GetBlockWithName(ObserverCameraName) as IMyCameraBlock;

                if (lcd != null) 
                {
                    lcd.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    lcd.FontSize = 1.5f;
                }

                if (lcdInfo != null)
                {
                    lcdInfo.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    lcdInfo.FontSize = 1.5f;
                }


                IMyBlockGroup radarGroup;
                radarGroup = mainProgram.GridTerminalSystem.GetBlockGroupWithName(RadarCameraGroupName);
                radarGroup.GetBlocks(cameras);

                SetCameras();

                mainProgram.Echo("----Camera radar setup started----");

                mainProgram.Echo($"Total scan cameras: {cameras.Count}");

                if (cameras.Count == 0)
                {
                    return;
                }
                else
                {
                    Camera = cameras[0] as IMyCameraBlock;
                }
            }

            /// <summary>
            /// Однокатное сканирование
            /// </summary>
            public void ScanOnce()
            {
                TryScanForward();
                TrackTarget = false;
            }

            /// <summary>
            /// Наведение на конкретный компонент корабля
            /// </summary>
            public void TargetPrecisionFollowing()
            {
                TryScanForward();
                TrackTarget = true;
                PrecisionFollowing = true;
            }

            /// <summary>
            /// Наведение на центр корабля
            /// </summary>
            public void TargetFollowing()
            {
                TryScanForward();
                TrackTarget = true;
                PrecisionFollowing = false;
            }

            /// <summary>
            /// Функция, выполняется каждый кадр
            /// </summary>
            public void RadarUpdate()
            {
                TryTrackTarget();
                PrintData();
            }

            /// <summary>
            /// Получить параметры цели
            /// </summary>
            public void GetPositonAndSpeed(out Vector3D pos, out Vector3D speed)
            {
                pos = CalculatedPosition;
                speed = TargetSpeed;
            }

            /// <summary>
            /// Вывод отладочной информации
            /// </summary>
            public void PrintData()
            {
                if (cameras.Any())
                    avrCamDist = cameras.Select(c => c as IMyCameraBlock).Sum(cam => cam.AvailableScanRange) / cameras.Count;

                mainProgram.Echo("----Radar vorking normally----");
                mainProgram.Echo($"Total cameras: {cameras?.Count}" +
                                 $"\nOBS cam dist: {observerCamera?.AvailableScanRange}" +
                                 $"\nAvr cams range: {avrCamDist}" +
                                 $"\nScan distanse: {ScanDistance} m");


                if (!TargetInfo.IsEmpty())
                {
                    lcd?.WriteText($"Distance: {DistanceToTarget.Length()}" +
                               $"\nSpeed: {TargetSpeed.Length()}" +
                               $"\nTracked: {TrackTarget}" +
                               $"\nPrecision: {PrecisionFollowing}" +
                               $"\nLOC closed: {LOCClosed}" +
                               $"\nLOC time: {LOCClosedTime}", false);
                }
                else
                {
                    lcd?.WriteText("   \n\n NO DATA", false);
                }

                lcdInfo?.WriteText($"Camera: {Camera.CustomName}" +
                                   $"\nAvailDist: {Camera.AvailableScanRange}" +
                                   $"\nTotalCameras: {cameras.Count}" +
                                   $"\n/AvailCams: {CurrentAviableCameras}" +
                                   $"\nOBScam: {observerCamera.AvailableScanRange}", false);

            }

            private void TryScanForward()
            {
                if (ScanDistance > 0)
                {
                    // if (CameraSelect()) 
                    {
                        TargetInfo = observerCamera.Raycast(ScanDistance);

                        if (!TargetInfo.IsEmpty())
                        {
                            HitPos = TargetInfo.HitPosition.Value - TargetInfo.Position + Vector3D.Normalize(TargetInfo.HitPosition.Value - Camera.GetPosition()) * 1;

                            TargetPos = TargetInfo.Position;
                            TargetSpeed = TargetInfo.Velocity;
                            TargetId = TargetInfo.EntityId;

                            MatrixD invMatrix = MatrixD.Invert(TargetInfo.Orientation);
                            HitInvert = Vector3D.Transform(HitPos, invMatrix);

                            HitPos = Vector3D.Transform(HitInvert, TargetInfo.Orientation);
                            CalculatedPosition = TargetInfo.HitPosition.Value;

                            DistanceToTarget = TargetInfo.HitPosition.Value - Camera.GetPosition();

                            RadarNotify.Invoke();
                        }
                        else
                        {
                           
                        }
                    }
                }
            }

            private void TryFollowTarget()
            {
                if (ScanDistance > 0)
                {
                    if (CameraSelect(CalculatedTargetPos))
                    {
                        var currentTarget = Camera.Raycast(CalculatedTargetPos);
                        //TargetInfo = Camera.Raycast(CalculatedTargetPos);
                        if (!currentTarget.IsEmpty())
                        {
                            if (TargetInfo.EntityId == currentTarget.EntityId)
                            {
                                TargetInfo = currentTarget;
                                LOCClosed = false;
                                LOCClosedTime = 0;

                                TargetPos = TargetInfo.Position;
                                TargetSpeed = TargetInfo.Velocity;
                                TargetId = TargetInfo.EntityId;

                                HitPos = Vector3D.Transform(HitInvert, TargetInfo.Orientation);

                                if (PrecisionFollowing)
                                {
                                    CalculatedPosition = TargetPos + HitPos;
                                }
                                else
                                {
                                    CalculatedPosition = TargetPos;
                                }
                                DistanceToTarget = TargetPos - Camera.GetPosition();

                                RadarNotify.Invoke();
                            }
                            else
                            {
                                LOCClosed = true;
                                LOCClosedTime++;
                                //ппроверка на LOC
                            }
                        }
                        else
                        {
                            HitPos = Vector3D.Zero;
                            TargetPos = Vector3D.Zero;
                            TargetSpeed = Vector3D.Zero;
                            TargetId = 0;
                            TargetInfo = Camera.Raycast(Vector3D.Zero);
                        }

                    }
                }
            }
            private void TryTrackTarget()
            {
                if (TrackTarget)
                {
                    tickLimit = (DistanceToTarget.Length() + 10) / 2000 * 60 / cameras.Count;
                    CalculatedTargetPos = CalculatedPosition + TargetSpeed * ((int)tickLimit + LOCClosedTime) / 60;

                    if (TargetInfo.IsEmpty())
                    {
                        TrackTarget = false;
                        LOCClosed = false;
                        LOCClosedTime = 0;
                    }

                    if (scanTick > tickLimit)
                    {
                        scanTick = 0;
                        TryFollowTarget();
                    }

                    scanTick++;
                }
            }

            private void SetCameras()
            {
                observerCamera.EnableRaycast = true;

                for (int i = 0; i < cameras.Count; i++)
                {
                    IMyCameraBlock cam = cameras[i] as IMyCameraBlock;
                    if (cam != null)
                        cam.EnableRaycast = true;
                }
            }

            private bool CameraSelect()
            {
                var availCamera = cameras.Select(c => c as IMyCameraBlock).Where(c => c.CanScan(ScanDistance));
                if (availCamera.Any())
                {
                    Camera = availCamera.First();
                    return true;
                }

                return false;
            }
            private bool CameraSelect(Vector3D scanPoz)
            {
                var availCamera = cameras.Select(c => c as IMyCameraBlock).Where(c => c.CanScan(scanPoz));
                if (availCamera.Any())
                {
                    Camera = availCamera.First();
                    CurrentAviableCameras = availCamera.Count();
                    return true;
                }

                return false;
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


        ///////////////////////////////////////////////////////////
    }
}
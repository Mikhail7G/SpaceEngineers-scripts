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

namespace SpaceEngineers.GroundMissileV1
{
    /// <summary>
    /// Модуль сканирования с помощью камеры и рэйкаста
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        /// <summary>
        /// Комманды для захвата целей:
        /// SCAN - одиночное сканирование без захвата цели на стандартуню дистанцию 1500 м
        /// SCAN:5000 - одиночное сканирование на дистанцию 5000 м
        /// SCANSEND - отправка координат по каналу для наведения ракет + дистанция через ":дист"
        /// RADARLOCK - захват цели в геометрическом центре
        /// RADARLOCKHIGHT - захват конкретной точки цели
        /// </summary>


        string observerCameraName = "radarCamOBS";
        string radarCameraGroupName = "radarCams";
        string antennaName = "Ant";
        string lcdName = "LCD1";
        string lcdInfoName = "LCDData";
        string beeperName = "Beeper";

        ///для управления через роторы
        string controlGroupName = "RadarGroup";
        string controlStationName = "Capitan";
        string statorHorizontalName = "RadarStatorH";
        string statorVerticalName = "RadarStatorV";

        string missileTagSender = "ch1R";//Отправляем в канал ракет координаты цели

        IMyCameraBlock observerCamera;//
        IMyCameraBlock camera;//активная в текущий момент камера
        IMySoundBlock beeper;//звуковое информирование о захвате цели
        IMyRadioAntenna antenna;//антенна для передачи данных ракете, правильный метод
        IMyTextPanel lcd;//дисплей инфы цели
        IMyTextPanel lcdInfo;//дисплей инфы камер



        IMyBlockGroup group;
        IMyShipController control;
        IMyMotorAdvancedStator statorHorizontal;
        IMyMotorAdvancedStator statorVertical;
        IMyTextSurface radarSurface;

        /// <summary>
        /// Параметры цели
        /// </summary>
        Vector3D targetSpeed;
        Vector3D targetVec;
        Vector3D hitPos;
        Vector3D calculatedTargetPos;//расчетная точка нахождения цели
        MyDetectedEntityInfo info;

        bool useRotors = false;


        /// <summary>
        /// Параметры сканирования
        /// </summary>
        double scanTick;
        double tickLimit = 1;
        bool beeperState = false;
        bool hightAccuracy = false;//захват с высокой точностью
        bool hasTarget = false;//захвачена ли цель?
        long targetId = 0;
        int currentCameraIndex = 0;
        float scanDistance;//дистанция сканирования
        double distanceToTarget;

        float rotateModifier = 1;//модификатор скорости поворота для роторной установки

        List<IMyTerminalBlock> cameras;


        public Program()
        {
            camera = GridTerminalSystem.GetBlockWithName(observerCameraName) as IMyCameraBlock;
            observerCamera = GridTerminalSystem.GetBlockWithName(observerCameraName) as IMyCameraBlock;
            antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
            lcd = GridTerminalSystem.GetBlockWithName(lcdName) as IMyTextPanel;
            lcdInfo = GridTerminalSystem.GetBlockWithName(lcdInfoName) as IMyTextPanel;
            beeper = GridTerminalSystem.GetBlockWithName(beeperName) as IMySoundBlock;

            cameras = new List<IMyTerminalBlock>();
            // GridTerminalSystem.SearchBlocksOfName(radarCameraGroupName, cameras);
            IMyBlockGroup radarGroup;
            radarGroup = GridTerminalSystem.GetBlockGroupWithName(radarCameraGroupName);
            radarGroup.GetBlocks(cameras);

            SetCameras();
            currentCameraIndex = cameras.Count;

            camera.EnableRaycast = true;
            scanDistance = 1500;
            scanTick = 0;
            distanceToTarget = 100;
            calculatedTargetPos = new Vector3D(0, 0, 0);

            targetVec = new Vector3D(0, 0, 0);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            if (cameras.Count > 0)
            {
                camera = cameras[0] as IMyCameraBlock;
                Echo($"Total camera in array: {cameras.Count}");
            }
            else
            {
                Echo($"No camera array with name {radarCameraGroupName} not detected");
            }

            if (camera != null)
            {
                Echo("Observer camera OK");
            }
            else
            {
                Echo($"No observer camera with name: {observerCameraName}");
            }

            if (antenna != null)
            {
                Echo("Antenna OK");
            }
            else
            {
                Echo($"No antenna with name: {antennaName}");
            }

            if (lcd != null)
            {
                Echo("LCD1 OK");
            }
            else
            {
                Echo($"No LCD1 name: {lcdName}");
            }

            if (lcdInfo != null)
            {
                Echo("LCD1 OK");
            }
            else
            {
                Echo($"No LCD1 name: {lcdInfoName}");
            }

            if (beeper != null)
            {
                Echo("Beeper Ok");
            }
            else
            {
                Echo($"No Beeper name: {beeperName}");
            }
        }

        public void Main(string args, UpdateType updateSource)
        {
            //пример комманды*    ScanD:1500:SCAN  -  означает установить дальность 1500м и провести сканирование
            //                    ScanD:1500:SCAN:LOCK  -  означает установить дальность 1500м и провести сканирование и захватить цель
            //                    ScanD:1500:SCAN:LOCKH - захват цели с высокой точностью, для захвата кораблей странной формы
            //                    ScanD:1500:SCAN:SEND - скан и отправка данных ракетам
            string[] argRes = args.Split(':');

            int lenght = argRes.Length;
            if (lenght > 0) 
            {
                string action = argRes[0].ToUpper();
                switch(action)
                {
                    case "SCAN": //режим одиночного сканирования
                        float.TryParse(argRes[1], out scanDistance);
                        StopBeeper();
                        TryGetGroundPos();
                        break;

                    case "SCANSEND": //режим одиночного сканирования и отправки данных
                        float.TryParse(argRes[1], out scanDistance);
                        StopBeeper();
                        TryGetGroundPos();
                        SendMessageRadio();
                        break;

                    case "RADARLOCK": //режим неточного захвата цели и сопровождения
                        float.TryParse(argRes[1], out scanDistance);
                        StopBeeper();
                        TryScanCamera();
                        hasTarget = true;
                        break;

                    case "RADARLOCKHIGHT": //режим неточного захвата цели и сопровождения
                        float.TryParse(argRes[1], out scanDistance);
                        StopBeeper();

                        hightAccuracy = true;
                        TryScanCameraHigthAccuracy();
                        hasTarget = true;
                        break;

                    case "SETTAG": //Смена тега для отправки данных о целе ракетам
                       if(argRes.Length>1)
                        {
                            missileTagSender = argRes[1];
                            Echo("New radio transmitted tag setted:" + missileTagSender);
                        }
                        break;

                    case "USEROTOR"://использовать роторы для вращения радара
                        Echo("-----Rotors setup start----");
                        SetUpRotors();
                        break;

                    case "ROTORSPEED":
                        if (argRes.Length > 1)
                        {
                            float.TryParse(argRes[1], out rotateModifier);
                        }
                        break;
                }
            }

            CameraInfo();
            TryTrackTarget();

            if (useRotors)
                UpdateRotation();
        }

        /// <summary>
        /// Настройка системы для вращения радара через роторы
        /// </summary>
      public void SetUpRotors()
        {
            Echo("-----Rotors setup start----");
            group = GridTerminalSystem.GetBlockGroupWithName(controlGroupName);
            Echo(group.Name);
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            group.GetBlocks(blocks);

            if (blocks.Count > 0)
            {
                foreach (var block in blocks)
                {
                    if (block is IMyShipController)
                    {
                        control = block as IMyShipController;

                        var text = control as IMyTextSurfaceProvider;
                        if(text!=null)
                        {
                            radarSurface = text.GetSurface(0);
                        }

                        Echo($"Cocpit: {block.CustomName}");
                    }

                    if (block is IMyMotorAdvancedStator)
                    {
                        if (block.CustomName == statorHorizontalName)
                        {
                            statorHorizontal = block as IMyMotorAdvancedStator;
                            Echo($"Horizontal: {block.CustomName}");
                        }

                        if (block.CustomName == statorVerticalName)
                        {
                            statorVertical = block as IMyMotorAdvancedStator;
                            Echo($"Vertical: {block.CustomName}");
                        }
                    }
                }
            }
            if(control == null)
            {
                Echo($"No Cocpit: {controlStationName}");
                return;
            }

            if (statorHorizontal == null)
            {
                Echo($"No Horizontal: {statorHorizontalName}");
                return;
            }

            if (statorVertical == null)
            {
                Echo($"No Vertical: {statorVerticalName}");
                return;
            }

            Echo("-----Rotors setup completed----");
            useRotors = true;
        }

        /// <summary>
        /// Вращение радаром при просмотре через камеру обзора
        /// </summary>
        public void UpdateRotation()
        {
            if ((!control.Closed) || (!statorHorizontal.Closed) || (!statorVertical.Closed))
            {
                Echo($"RotorAzimuth: {Math.Round(statorHorizontal.Angle * 180 / 3.14,2)} \nRotorElevation: {Math.Round(statorVertical.Angle * 180 / 3.14,2)}" +
                     $"\nRotate modifier: {Math.Round(rotateModifier,2)}");

                if ((control.IsUnderControl) && (observerCamera.IsActive))
                {
                    var rot = control.RotationIndicator;
                    statorHorizontal.TargetVelocityRPM = rot.Y * rotateModifier;
                    statorVertical.TargetVelocityRPM = -rot.X * rotateModifier;
                }
                else
                {
                    statorHorizontal.TargetVelocityRPM = 0;
                    statorVertical.TargetVelocityRPM = 0;
                }
            }

        }

        /// <summary>
        /// Начальная настройка камер
        /// </summary>
        public void SetCameras()
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                IMyCameraBlock cam = cameras[i] as IMyCameraBlock;
                if (cam != null)
                    cam.EnableRaycast = true;
            }
        }

        /// <summary>
        /// Выбор камеры, у которой есть возможность рейкаста до заданной дистанции Всегда true!!!
        /// </summary>
        bool  CameraSelect()
        {
          if (!camera.CanScan(scanDistance))
            {
                currentCameraIndex++;
                if (currentCameraIndex >= cameras.Count)
                    currentCameraIndex = 0;
                camera = cameras[currentCameraIndex] as IMyCameraBlock;
            }

            return true;
        }

        /// <summary>
        /// Отправляет ракете точку на поверхности планеты
        /// </summary>
        void TryGetGroundPos()
        {
            TryScanCamera();

            if (!info.IsEmpty())
            {
                calculatedTargetPos = info.HitPosition.Value;
                distanceToTarget = Vector3D.Distance(camera.GetPosition(), info.HitPosition.Value);
            }
        }

        /// <summary>
        /// Сканиреум точку на наличие чего либо
        /// </summary>
        void TryScanCamera()
        {
            if(scanDistance>0)
            {

                if (CameraSelect()) 
                {
                    //Если нет цели сканируем прямо от камеры лучем, иначе ищем цель в расчетной точке
                    if (!hasTarget)
                    {
                         info = camera.Raycast(scanDistance);
                    }
                    else
                    {
                         info = camera.Raycast(calculatedTargetPos);
                    }
                  
                    if (!info.IsEmpty())
                    {
                        //Ишем объект в геометрическом центре
                        hitPos = info.HitPosition.Value - info.Position;
                        targetVec = info.Position;
                        targetSpeed = info.Velocity;

                        distanceToTarget = Vector3D.Distance(targetVec, camera.GetPosition());
                        targetId = info.EntityId;
                    }
                }
            }
        }

        /// <summary>
        /// Поиск цели с учетом локальных преобразований координат, для поиска кораблей нестандартной формы
        /// </summary>
        void TryScanCameraHigthAccuracy()
        {
            if (scanDistance > 0)
            {
                if (CameraSelect())
                {
                    if (!hasTarget)
                    {
                        info = camera.Raycast(scanDistance);
                    }
                    else
                    {
                        info = camera.Raycast(calculatedTargetPos);
                    }

                    if (!info.IsEmpty())
                    {
                        //Сохраняем локальную точку на корабле и трансформируем ее в глобальную сетку координат
                        hitPos = info.HitPosition.Value - info.Position + Vector3D.Normalize(info.HitPosition.Value - camera.GetPosition()) * 1.5;

                        MatrixD invMatrix = MatrixD.Invert(info.Orientation);
                        hitPos = Vector3D.Transform(hitPos, invMatrix);
                        targetVec = info.Position + Vector3D.Transform(hitPos, info.Orientation);

                        targetSpeed = info.Velocity;

                        distanceToTarget = Vector3D.Distance(targetVec, camera.GetPosition());
                        targetId = info.EntityId;
                    }
                }
            }
        }

        /// <summary>
        /// Отключаем пищалку и убираем захват цели
        /// </summary>
        void StopBeeper()
        {
            if (beeper != null)
            {
                beeper.Stop();
                beeperState = false;
            }
            hightAccuracy = false;
            hasTarget = false;
        }

        /// <summary>
        /// Функция отслеживания цели
        /// </summary>
        void TryTrackTarget()
        {
            if (!info.IsEmpty())
            {
                //Расчитывает тик активации камеры с учетом регенерации дистанции захвата, каждую секунду 2000м
                scanTick++;
                tickLimit = distanceToTarget / 2000 * 60 / cameras.Count;
                if (hightAccuracy)
                {
                    calculatedTargetPos = info.Position + (info.Velocity * (int)tickLimit / 60) + Vector3D.Transform(hitPos, info.Orientation);
                }
                else
                {
                    calculatedTargetPos = info.Position + (info.Velocity * (int)tickLimit / 60);
                }
              
            }
            else
            {
                hasTarget = false;
                if (beeper != null)
                {
                    beeper.Stop();
                    beeperState = false;
                }
            }

            if((scanTick > tickLimit) && (hasTarget))
            {
                scanTick = 0;

                if (!beeperState)
                {
                    if (beeper != null)
                    {
                        beeper.Play();
                        beeperState = true;
                    }
                }

                //Тип сканирования, точный и не точный
                if (hightAccuracy)
                {
                    TryScanCameraHigthAccuracy();
                }
                else
                {
                    TryScanCamera();
                }
                SendMessageRadio();
            }   
        }

        /// <summary>
        /// Вывод инфорации по цели и статус камер
        /// </summary>
        void CameraInfo()
        {
            lcdInfo?.WriteText("", false);
            lcdInfo?.WriteText($"MaxRange: {Math.Round(camera.AvailableScanRange)} m" +
                               $"\nTick/MaxTick: {scanTick}/{Math.Round(tickLimit, 2)}" +
                               $"\nTarg/higAcc: {hasTarget}/{hightAccuracy}" +
                               $"\nRadio: {missileTagSender}" +
                               $"\nCurCam/MaxCams: {currentCameraIndex}/{cameras.Count}", true);

            if (!info.IsEmpty())
            {
                lcd?.WriteText("", false);
                lcd?.WriteText($"Dist/Speed: {Math.Round(distanceToTarget)}/{Math.Round(info.Velocity.Length())}" +
                              $"\n{info.Type.ToString()}" +
                              $"\n{info.EntityId}", true);

                radarSurface?.WriteText("", false);
                radarSurface?.WriteText($"Target:\nDist/Speed: {Math.Round(distanceToTarget)}/{Math.Round(info.Velocity.Length())}" +
                              $"\n{info.Type}" +
                              $"\n{info.EntityId}", true);
            }
           else
            {
                lcd?.WriteText("        NO DATA", false);
            }
        }

        /// <summary>
        /// Отправка координат цели через антенну
        /// </summary>
        void SendMessageRadio()
        {
            if (!info.IsEmpty())
            {                          ///координаты цели
                string sendingSignal = (calculatedTargetPos.X).ToString() +
                                       "|" + (calculatedTargetPos.Y).ToString() +
                                       "|" + (calculatedTargetPos.Z).ToString() +

                                       "|" + (targetSpeed.X).ToString() + //скорость цели
                                       "|" + (targetSpeed.Y).ToString() +
                                       "|" + (targetSpeed.Z).ToString();

                IGC.SendBroadcastMessage(missileTagSender, sendingSignal, TransmissionDistance.TransmissionDistanceMax);
            }
        }

        //////////////////END OF SCRIPT////////////////////////////////////////

    }
}

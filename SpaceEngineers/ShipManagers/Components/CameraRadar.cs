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



namespace ShipManagers.Components.Radar
{
    public sealed class Program : MyGridProgram
    {

        /////////////////////////////////////////////////////////////

        string antennaName = "Ant";
        string missileTagSender = "ch1R";//Отправляем в канал ракет координаты цели
        string beeperName = "Beeper";

        CameraRadar Radar;
        IMyRadioAntenna antenna;//антенна для передачи данных ракете, правильный метод
        IMySoundBlock beeper;//звуковое информирование о захвате цели

        bool beeperEnabled;

        public Program()
        {
            Radar = new CameraRadar(this);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;


            antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
            beeper = GridTerminalSystem.GetBlockWithName(beeperName) as IMySoundBlock;

        }

        public void Main(string args)
        {
            switch(args)
            {
                case "init":
                    Radar.InitRadar();
                    break;
                case "scan":
                    Radar.ScanOnce();
                    break;
                case "lock":
                    Radar.TargetPrecisionFollowing();
                    break;
                case "lockcenter":
                    Radar.TargetFollowing();
                    break;
            }

            Radar.RadarUpdate();
            SendMessageRadio();

            if (Radar.TrackTarget)
            {
                if(beeperEnabled==false)
                {
                    beeperEnabled = true;
                    beeper.Play();
                }
            }
            else
            {
                beeperEnabled = false;
                beeper.Stop();

            }

        }

        void SendMessageRadio()
        {
            if (!Radar.TargetInfo.IsEmpty())
            {
                var pos = Radar.CalculatedPosition;
                var speed = Radar.TargetSpeed;

                ///координаты цели
                string sendingSignal = (pos.X).ToString() +
                                       "|" + (pos.Y).ToString() +
                                       "|" + (pos.Z).ToString() +

                                       "|" + (speed.X).ToString() + //скорость цели
                                       "|" + (speed.Y).ToString() +
                                       "|" + (speed.Z).ToString();

                IGC.SendBroadcastMessage(missileTagSender, sendingSignal, TransmissionDistance.TransmissionDistanceMax);
            }
        }

        public class CameraRadar
        {
            public string RadarCameraGroupName = "radarCams";
            public string LcdRadarStatus = "RadarSta";
            public string LcdTargetStatus = "RadarTarget";

            public bool PrecisionFollowing { get; private set; }
            public bool TrackTarget { get; private set; }
            public float ScanDistance { get; set; }
            public IMyCameraBlock Camera { get; private set; }

            public Vector3D TargetSpeed { get; private set; }
            public Vector3D TargetPos { get; private set; }
            public Vector3D HitPos { get; private set; }
            public Vector3D CalculatedTargetPos { get; private set; }
            public double TargetId { get; private set; }
            public Vector3D DistanceToTarget { get; private set; }
            public Vector3D CalculatedPosition { get; private set; }
            public MyDetectedEntityInfo TargetInfo { get; private set; }

            private IMyTextPanel lcd;
            private IMyTextPanel lcdInfo;
            private List<IMyTerminalBlock> cameras;

            private int scanTick;
            private double tickLimit;

            private Vector3D HitInvert;

            private Program mainProgram;

            public CameraRadar(Program program)
            {
                mainProgram = program;
                cameras = new List<IMyTerminalBlock>();
                TrackTarget = false;
                scanTick = 0;

            }

            public void InitRadar()
            {
                lcd = mainProgram.GridTerminalSystem.GetBlockWithName(LcdRadarStatus) as IMyTextPanel;
                lcdInfo = mainProgram.GridTerminalSystem.GetBlockWithName(LcdTargetStatus) as IMyTextPanel;

                IMyBlockGroup radarGroup;
                radarGroup = mainProgram.GridTerminalSystem.GetBlockGroupWithName(RadarCameraGroupName);
                radarGroup.GetBlocks(cameras);

                SetCameras();
                ScanDistance = 10000;

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

            public void ScanOnce()
            {
                TryScanForward();
                TrackTarget = false;
            }

            public void TargetPrecisionFollowing()
            {
                TryScanForward();
                TrackTarget = true;
                PrecisionFollowing = true;
            }

            public void TargetFollowing()
            {
                TryScanForward();
                TrackTarget = true;
                PrecisionFollowing = false;
            }

            public void RadarUpdate()
            {
                TryTrackTarget();
                PrintData();
            }

            public void PrintData()
            {
                lcd?.WriteText($"Distance: {DistanceToTarget.Length()}" +
                               $"\nId: {TargetId}" +
                               $"\nTracked: {TrackTarget}" +
                               $"\nPrecision: {PrecisionFollowing}", false);

                lcdInfo?.WriteText($"Camera: {Camera.CustomName}" +
                                   $"\nAvailDist: {Camera.AvailableScanRange}" +
                                   $"\nTotalCameras: {cameras.Count}", false);

            }

            private void TryScanForward()
            {
                if (ScanDistance > 0) 
                {
                    if (CameraSelect()) 
                    {
                        TargetInfo = Camera.Raycast(ScanDistance);
                    
                        if(!TargetInfo.IsEmpty())
                        {
                            HitPos = TargetInfo.HitPosition.Value - TargetInfo.Position + Vector3D.Normalize(TargetInfo.HitPosition.Value - Camera.GetPosition()) * 1;

                            TargetPos = TargetInfo.Position;
                            TargetSpeed = TargetInfo.Velocity;
                            TargetId = TargetInfo.EntityId;

                            MatrixD invMatrix = MatrixD.Invert(TargetInfo.Orientation);
                            HitInvert = Vector3D.Transform(HitPos, invMatrix);

                            HitPos = Vector3D.Transform(HitInvert, TargetInfo.Orientation);
                            CalculatedPosition = TargetPos + HitPos;

                            DistanceToTarget = CalculatedPosition - Camera.GetPosition();       
                        }
                        else
                        {
                            HitPos = Vector3D.Zero;
                            TargetPos = Vector3D.Zero;
                            TargetSpeed = Vector3D.Zero;
                            TargetId = 0;
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
                            }
                            else
                            {
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
                if(TrackTarget)
                {
                    tickLimit = (DistanceToTarget.Length()+10) / 2000 * 60 / cameras.Count;
                    CalculatedTargetPos = CalculatedPosition + (TargetSpeed * (int)tickLimit / 60);

                    if (TargetInfo.IsEmpty())
                    {
                        TrackTarget = false;
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
                    return true;
                }
                                         
                return false;
            }

        }


        ///////////////////////////////////////////////////////////
    }
}
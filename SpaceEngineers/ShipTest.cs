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



namespace ShipTests
{
    public sealed class Program : MyGridProgram
    {
        IMyGyro gyro;
        IMyRemoteControl RemoteCon;
        IMyTextPanel myTextPanel;
        IMyCameraBlock camera;

        IMyProgrammableBlock block;
        MyDetectedEntityInfo info;

        IMyRadioAntenna antenna;
        string missileTagSender = "ch1R";


        Vector3D targetVec;

        int timerTick = 0;
        double dist = 0;
        bool terrainAlert = false;


        public Program()
        {
            gyro = GridTerminalSystem.GetBlockWithName("gyro") as IMyGyro;
            RemoteCon = GridTerminalSystem.GetBlockWithName("radio") as IMyRemoteControl;
            myTextPanel = GridTerminalSystem.GetBlockWithName("LCD1") as IMyTextPanel;
            antenna = GridTerminalSystem.GetBlockWithName("Ant") as IMyRadioAntenna;

            camera = GridTerminalSystem.GetBlockWithName("Cam") as IMyCameraBlock;
            camera.EnableRaycast = true;

            targetVec = new Vector3D(-36701.45, -22535.07, -43526.47);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

        }

        public void Main(string args)
        {
            // Vector3D vec = GetAngels(targetVec);

            double alt = 0;
            Vector3D pos = new Vector3D();
            RemoteCon.TryGetPlanetElevation(MyPlanetElevation.Surface, out alt);

            pos = RemoteCon.GetPosition();
            double airdDist = Vector3D.Distance(targetVec, pos);

            pos = new Vector3D(pos.X, targetVec.Y, pos.Z);
            double groundDist = Vector3D.Distance(targetVec, pos);

            myTextPanel.WriteText("", false);
            myTextPanel.WriteText(Math.Round(alt).ToString() + "\n" +
                                  Math.Round(gyro.Roll, 2) + "\n" +
                                  Math.Round(gyro.Yaw, 2) + "\n" +
                                  Math.Round(gyro.Pitch, 2) + "\n" +
                                  Math.Round(groundDist) + "\n" +
                                  Math.Round(airdDist), true);

            if (!info.IsEmpty())
            {
                dist = Vector3D.Distance(info.HitPosition.Value, pos);
                myTextPanel.WriteText(info.HitPosition.ToString() + 
                                     "\n" + Math.Round(dist) + 
                                     "\n" + info.Type.ToString(), true);

                string sendingSignal = Math.Round(info.HitPosition.Value.X).ToString() +
                                       "|" + Math.Round(info.HitPosition.Value.Y).ToString() +
                                       "|" + Math.Round(info.HitPosition.Value.Z).ToString();

                IGC.SendBroadcastMessage(missileTagSender, sendingSignal, TransmissionDistance.TransmissionDistanceMax);

                if(dist<300)
                {
                    terrainAlert = true;
                }

            }
            else
            {
                terrainAlert = false;
            }

 


            SetGyro(true, GetAngels(targetVec) * 10, 1);
            CameraRays();


        }

        public void Save()
        {

        }

        public Vector3D GetAngels(Vector3D vec)
        {

            Vector3D pos = RemoteCon.GetPosition();
            Vector3D fwd = RemoteCon.WorldMatrix.Forward;
            Vector3D up = RemoteCon.WorldMatrix.Up;
            Vector3D left = RemoteCon.WorldMatrix.Left;


         

            Vector3D distanseNormalized = Vector3D.Normalize(vec - pos);

            double targetPitch = Math.Acos(Vector3D.Dot(up, distanseNormalized)) - (Math.PI / 2);
            double targetYaw = Math.Acos(Vector3D.Dot(left, Vector3D.Normalize(Vector3D.Reject(distanseNormalized, up)))) - (Math.PI / 2);
            double targetRoll = 0;
            targetRoll = Math.Acos(Vector3D.Dot(left, Vector3D.Reject(Vector3D.Normalize(-RemoteCon.GetNaturalGravity()), fwd))) - (Math.PI / 2);

            var grav = RemoteCon.CubeGrid.WorldToGridInteger(RemoteCon.GetNaturalGravity());
            var az = Math.Atan2(grav.Z, -grav.Y);

            if (!terrainAlert)
            {
                targetPitch = -(float)az * 2;
            }
            else
            {
                targetPitch = -1;
            }

            return new Vector3D(targetYaw, targetPitch, -targetRoll);
        }

        void SetGyro(bool overdrideONOFF, Vector3D vec, float power)
        {
            gyro.SetValueFloat("Power", power);
            gyro.SetValueFloat("Yaw", (float)vec.GetDim(0));
            gyro.SetValueFloat("Pitch", (float)vec.GetDim(1));
            gyro.SetValueFloat("Roll", (float)vec.GetDim(2));
        }

        void CameraRays()
        {
            timerTick++;
            if (timerTick > 50) 
            {
                timerTick = 0;
                if (camera.CanScan(1500))
                    info = camera.Raycast(1500, -15, 0);
            }
        }

    }
}

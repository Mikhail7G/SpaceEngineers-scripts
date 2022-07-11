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

namespace SpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        IMyEntity entity;
        IMyGyro gyro;
        IMyRemoteControl RemoteCon;
        IMyTextPanel myTextPanel;
        IMyTextPanel missilePanel;

        IMyRadioAntenna antenna;
        IMyProgrammableBlock block;
        List<IMyThrust> trusters;

        IMyBroadcastListener listener;

        bool init = false;

        string tag = "ch1";
        string tagResieved = "ch1R";
        string message;
        string messageResieved;

        Vector3D targetVec;
        double targetDistanse;
        double flightAltitude;

        int timerTick;

        public Program()
        {
            trusters = new List<IMyThrust>();
            targetDistanse = 0;

            targetVec = new Vector3D(-36701.45, -22535.07, -43526.47);
            timerTick = 0;
            flightAltitude = 0;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            IGC.RegisterBroadcastListener(tag);
            listener = IGC.RegisterBroadcastListener(tagResieved);
            listener.SetMessageCallback(tagResieved);
        }

        public void Main(string args)
        {

            if (!init)
            {
                entity = GridTerminalSystem.GetBlockWithName("radio") as IMyEntity;
                RemoteCon = GridTerminalSystem.GetBlockWithName("radio") as IMyRemoteControl;
                myTextPanel = GridTerminalSystem.GetBlockWithName("Test") as IMyTextPanel;
                missilePanel = GridTerminalSystem.GetBlockWithName("MissileLCD") as IMyTextPanel;
                block = GridTerminalSystem.GetBlockWithName("PC") as IMyProgrammableBlock;
                antenna = GridTerminalSystem.GetBlockWithName("Ant") as IMyRadioAntenna;
                GridTerminalSystem.GetBlocksOfType(trusters);


                gyro = GridTerminalSystem.GetBlockWithName("gyro") as IMyGyro;
                gyro.SetValueBool("Override", true);

                foreach (IMyThrust trust in trusters)
                {
                    trust.SetValueBool("OnOff", true);
                }
                init = true;
            }

            targetDistanse = Vector3D.Distance(targetVec, RemoteCon.GetPosition());
            RemoteCon.TryGetPlanetElevation(MyPlanetElevation.Surface, out flightAltitude);

            message = Math.Round(block.GetPosition().X).ToString() +
                "|" + Math.Round(block.GetPosition().Y).ToString() +
                "|" + Math.Round(block.GetPosition().Z).ToString() +
                "|" + Math.Round(flightAltitude) +
                "|" + Math.Round(targetDistanse);

            myTextPanel.WriteText("FlightTime: " + timerTick.ToString() +
                                  "\nDistance To Target: " + Math.Round(targetDistanse) +
                                  "\nMissile Alt: " + Math.Round(flightAltitude));

            IGC.SendBroadcastMessage(tag, message, TransmissionDistance.TransmissionDistanceMax);

            while (listener.HasPendingMessage)
            {
                MyIGCMessage mess = listener.AcceptMessage();
                if (mess.Tag == tagResieved)
                {
                    missilePanel.WriteText("", false);
                    missilePanel.WriteText(mess.Data.ToString(), true);
                    string[] str = mess.Data.ToString().Split('|');
                    double.TryParse(str[0], out targetVec.X);
                    double.TryParse(str[1], out targetVec.Y);
                    double.TryParse(str[2], out targetVec.Z);


                }
            }


            if (timerTick > 1500)
            {
                SetGyro(GetAngels(targetVec) * 10, 1);
            }
            else
            {

            }
            timerTick++;
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

            double targetPitch = Math.Acos(Vector3D.Dot(up, distanseNormalized)) - Math.PI / 2;
            double targetYaw = Math.Acos(Vector3D.Dot(left, Vector3D.Normalize(Vector3D.Reject(distanseNormalized, up)))) - Math.PI / 2;
            double targetRoll = Math.Acos(Vector3D.Dot(left, Vector3D.Reject(Vector3D.Normalize(-RemoteCon.GetNaturalGravity()), fwd))) - Math.PI / 2;

            return new Vector3D(targetYaw, -targetPitch, targetRoll);
        }

        void SetGyro(Vector3D vec, float power)
        {
            gyro.SetValueFloat("Power", power);
            gyro.SetValueFloat("Yaw", (float)vec.GetDim(0));
            gyro.SetValueFloat("Pitch", (float)vec.GetDim(1));
            gyro.SetValueFloat("Roll", (float)vec.GetDim(2));

        }

    }
}

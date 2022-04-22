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

namespace SpaceEngineers.GroundMissileV1GtoG
{
    public sealed class Program : MyGridProgram
    {
        /// <summary>
        /// Статус ракеты на всех этапах существования
        /// </summary>
        enum MissileState
        {
            Idle,
            Prearm,
            Disarm,
            TakeOff
        }


        MissileState currentState;
        List<IMyGyro> gyros;
        List<IMyThrust> trusters;

        IMyRemoteControl remotControl;
        IMyTextPanel groundPanel;
        IMyRadioAntenna radioAnt;
        IMyBroadcastListener listener;

        string tag = "ch1";//отправка телеметрии
        string tagResieved = "ch1R";//получение комманд
        string messageSended;
        string messageResieved;

        Vector3D targetVector;

        double targetDistance;
        double radioAltitude;
        double flightAltitude;
        float flightTime;



        public Program()
        {
            currentState = MissileState.Idle;
            gyros = new List<IMyGyro>();
            trusters = new List<IMyThrust>();
            targetVector = new Vector3D(0, 0, 0);
            targetDistance = 0;
            radioAltitude = 0;
            flightAltitude = 1500;
            flightTime = 0;
            messageSended = " ";
            messageResieved = " ";

            GridTerminalSystem.GetBlocksOfType(gyros);
            GridTerminalSystem.GetBlocksOfType(trusters);

            remotControl = GridTerminalSystem.GetBlockWithName("radio") as IMyRemoteControl;
            groundPanel = GridTerminalSystem.GetBlockWithName("GroundLCD") as IMyTextPanel;
            radioAnt = GridTerminalSystem.GetBlockWithName("Ant") as IMyRadioAntenna;

            IGC.RegisterBroadcastListener(tag);
            listener = IGC.RegisterBroadcastListener(tagResieved);
            listener.SetMessageCallback(tagResieved);

            SetEngines(false);

        }

        public void Main(string args, UpdateType updateSource)
        {
            //Строки аргументов: PREARM, PREARM|5000 - установка высоты начального наборта по радиовысотомеру
            string[] argument = args.Split('|');

            if (argument.Length > 0)
            {
                string state = argument[0];

                switch (state)
                {
                    case "PREARM":
                        Prearm();
                        if (argument.Length > 1) 
                            double.TryParse(argument[1], out flightAltitude);
                        break;
                    case "DISARM":
                        Disarm();
                        break;
                }
            }

            switch (currentState)
            {
                case MissileState.Prearm:
                    GetMissileTelemetryByModule();
                    GetTargetByRadio();
                    SendTelemetryByRadio();
                    break;
            }
           
        }

        public void Save()
        {

        }

        /// <summary>
        /// Запуск или остановка двигателей
        /// </summary>
        public void SetEngines(bool activate)
        {
            foreach (IMyThrust truster in trusters)
            {
                truster.SetValueBool("OnOff", activate);
                truster.ThrustOverridePercentage = 1;

            }
        }

        /// <summary>
        /// Подготовка ракеты ко взлету
        /// </summary>
        public void Prearm()
        {
            if(currentState == MissileState.Idle)
            {
                SetEngines(true);
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
                currentState = MissileState.Prearm;
            }
        }

        /// <summary>
        /// Отключение ракеты, отмена пуска
        /// </summary>
        public void Disarm()
        {
            if (currentState == MissileState.Prearm)
            {
                SetEngines(false);
                Runtime.UpdateFrequency = UpdateFrequency.Once;
                currentState = MissileState.Idle;
            }
        }

        /// <summary>
        /// Получение телеметрии ракеты через модуль ссылку
        /// </summary>
        public void GetMissileTelemetryByModule()
        {
            groundPanel.WriteText("", true);
            targetDistance = Vector3D.Distance(targetVector, remotControl.GetPosition());
            remotControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out radioAltitude);

            groundPanel.WriteText("Flight Time: " + flightTime + 
                                  "\nDistanse to target: " + Math.Round(targetDistance) + 
                                   "\nRadio altimiter: " + Math.Round(radioAltitude) + 
                                   "\nInitial altitude: " + flightAltitude, false);
        }

        /// <summary>
        /// Отправка телеметрии через антенну
        /// </summary>
        public void SendTelemetryByRadio()
        {
            messageSended = Math.Round(remotControl.GetPosition().X).ToString() +
               "|" + Math.Round(remotControl.GetPosition().Y).ToString() +
               "|" + Math.Round(remotControl.GetPosition().Z).ToString() +
               "|" + Math.Round(radioAltitude) +
               "|" + Math.Round(targetDistance);

            IGC.SendBroadcastMessage(tag, messageSended, TransmissionDistance.TransmissionDistanceMax);
        }

        /// <summary>
        /// Получение координат цели через антенну
        /// </summary>
        public void GetTargetByRadio()
        {
            while(listener.HasPendingMessage)
            {
                MyIGCMessage mess = listener.AcceptMessage();
                if (mess.Tag == tagResieved)
                {
                    string[] str = mess.Data.ToString().Split('|');
                    double.TryParse(str[0], out targetVector.X);
                    double.TryParse(str[1], out targetVector.Y);
                    double.TryParse(str[2], out targetVector.Z);
                }
            }
        }

    }

}

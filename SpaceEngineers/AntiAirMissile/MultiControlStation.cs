﻿using System;
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


namespace SpaceEngineers.MultiControlStation
{
    public sealed class Program : MyGridProgram
    {
        /// <summary>
        /// Максимальное время полета ракеты, т.е. время контроля ракеты станцией
        /// </summary>
        int maxAviableFlightTime = 2500;

        string remoteControlName = "Radio";//
        string decouplerName = "Decoupler";//
        string missileEnginesName = "MissileEng";//
        string missileGyrosName = "MissileGyro";//
        string missileGravityEngineName = "Gravity";
        string missileManevricEngineName = "Manv";
        string missileWarheadName = "Warhead";

        string missileTagSender = "ch1R";//Получаем данные от системы целеуказания по радиоканалу
        double distanceToTargetFromBase = 0;
        Vector3D targetPosition;//положение цели
        Vector3D targetSpeed;//скрость цели
       
        int missileCounter = 0;
        string missileTagFinder = null;
        IMyRadioAntenna antenna;
        IMyTextPanel panel;
        IMyBroadcastListener listener;//слушаем эфир на получение данных о целях по радио

        List<ControlledMissile> missileList;
        List<ControlledMissile> missileInFlightList;

        Vector3D mPos = new Vector3D(0,0,0);


        public Program()
        {
            targetPosition = new Vector3D(0,0,0);
            targetSpeed = new Vector3D(0, 0, 0);

            missileList = new List<ControlledMissile>();
            missileInFlightList = new List<ControlledMissile>();

            antenna =  GridTerminalSystem.GetBlockWithName("!AntGround") as IMyRadioAntenna;
            panel = GridTerminalSystem.GetBlockWithName("!MissileMonitor") as IMyTextPanel;

            listener = IGC.RegisterBroadcastListener(missileTagSender);
            listener.SetMessageCallback(missileTagSender);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string args)
        {
            string[] arguments = args.Split('|');
            if(arguments.Length>0)
            {
                string arg = arguments[0].ToUpper();
                switch(arg)
                {
                    case "FIRE":
                        foreach(var missile in missileList)
                        {
                            if (missile != null)
                            {
                                missileInFlightList.Add(missile);
                                missile.MissileFire();
                                
                            }
                        }
                        missileList.Clear();
                        break;

                    case "BUILD":
                        FindMissile();
                        break;
                }

            }

            GetTargetByRadio();
            CalculateParametres();

            UpdateFlithtMissile();

            panel.WriteText("", true);
            panel.WriteText("Missiles in bay/In Flight: " + missileList.Count + "/"+ missileInFlightList.Count + 
                            "\nTarget Pos: " + targetPosition.ToString() +
                            "\nTarget speed: " + Math.Round(targetSpeed.Length()).ToString() +
                            "\nDist from base: " + Math.Round(distanceToTargetFromBase), false);

            // panel.WriteText(testPos.ToString(), false);

        }

        public void Save()
        {

        }

        public void FindMissile()
        {
            missileTagFinder = Me.CustomData;
            missileList.Clear();
            missileCounter = 0;

            if (missileTagFinder != null)
            {
                Echo("Try to find with tag: " + missileTagFinder);

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType(blocks);

                foreach(var block in blocks)
                {
                    if(block.CustomName.Contains("Radio"))
                    {
                        missileCounter++;
                    }

                }

                for(int i=0;i< missileCounter;i++)
                {
                    ControlledMissile missile = new ControlledMissile();

                    foreach (var block in blocks)
                    {
                        if (block.CustomName.Contains(missileTagFinder + i + remoteControlName))
                        {
                          if(block is IMyRemoteControl)
                            {
                                missile.RemotControl = block as IMyRemoteControl;
                            }
                        }

                        if (block.CustomName.Contains(missileTagFinder + i + decouplerName))
                        {
                            if (block is IMyTerminalBlock)
                            {
                                missile.MergeBlock = block as IMyTerminalBlock;
                            }
                        }

                        if (block.CustomName.Contains(missileTagFinder + i + missileEnginesName))
                        {
                            if (block is IMyThrust)
                            {
                                missile.Trusters.Add(block as IMyThrust);
                            }
                        }

                        if (block.CustomName.Contains(missileTagFinder + i + missileGravityEngineName))
                        {
                            if (block is IMyThrust)
                            {
                                missile.GravityTrusters.Add(block as IMyThrust);
                            }
                        }

                        if (block.CustomName.Contains(missileTagFinder + i + missileGyrosName))
                        {
                            if (block is IMyGyro)
                            {
                                missile.Gyros.Add(block as IMyGyro);
                            }
                        }

                        if (block.CustomName.Contains(missileTagFinder + i + missileWarheadName))
                        {
                            if (block is IMyWarhead)
                            {
                                missile.Warheads.Add(block as IMyWarhead);
                            }
                        }


                    }

                    if(missile.MissileReady())
                    {
                        missileList.Add(missile);
                    }
                    else
                    {
                        Echo("Some misssile have problem with build");
                        break;
                    }

                }
                Echo("Missile detection completed! Missiles: " + missileList.Count);

            }
            else
            {
                Echo("No tag! Enter custom data!");
            }
        }

        /// <summary>
        /// Получение координат цели через антенну
        /// </summary>
        public void GetTargetByRadio()
        {
            while (listener.HasPendingMessage)
            {
                panel.WriteText("Rado wait for target!", false);

                MyIGCMessage mess = listener.AcceptMessage();
                if (mess.Tag == missileTagSender)
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

        //расчет параметров для ракет
        public void CalculateParametres()
        {
            distanceToTargetFromBase = (Me.CubeGrid.GetPosition() - targetPosition).Length();
        }

        public Vector3D CalcInterceptPos(Vector3D missilePos, double missileSpeed, Vector3D targetPos, Vector3D targetSpeed)
        {

            Vector3D directionToTarget = Vector3D.Normalize(targetPos - missilePos);
            Vector3D targetVelOrto = Vector3D.Dot(targetSpeed, directionToTarget) * directionToTarget;

            // Vector3D targetVelTang = targetSpeed - targetVelOrto;
            Vector3D targetVelTang = Vector3D.Reject(targetSpeed, directionToTarget);
            Vector3D shootVelTang = targetVelTang;
            double shootVelSpeed = shootVelTang.Length();


            if (shootVelSpeed > missileSpeed)
            {
                return Vector3D.Normalize(targetSpeed) * missileSpeed;
            }
            else
            {
                double shootSpeedOrto = Math.Sqrt(missileSpeed * missileSpeed - shootVelSpeed * shootVelSpeed);
                Vector3D shootVelOrto = directionToTarget * shootSpeedOrto;
               // impactTime = Math.Abs(directionToTarget.Length()) / (Math.Abs(targetVelTang.Length()) - Math.Abs(shootVelOrto.Length()));

                return shootVelOrto + shootVelTang;
            }
        }

        public Vector3D RottateMissileToTarget(Vector3D targetPos, IMyGyro gyro,IMyRemoteControl ctr)
        {
            Vector3D fwd = gyro.WorldMatrix.Forward;
            Vector3D up = gyro.WorldMatrix.Up;
            Vector3D left = gyro.WorldMatrix.Left;

            Vector3D targetNormal = Vector3D.Normalize(targetPos);
            Vector3D vecReject = Vector3D.Reject(Vector3D.Normalize(ctr.GetShipVelocities().LinearVelocity), targetNormal);
            Vector3D correctVect = Vector3D.Normalize(targetNormal - vecReject * 2);

            double targetPitch = Vector3D.Dot(up, Vector3D.Normalize(Vector3D.Reject(correctVect, left)));
            targetPitch = Math.Acos(targetPitch) - Math.PI / 2;
            double targetYaw = Vector3D.Dot(left, Vector3D.Normalize(Vector3D.Reject(correctVect, up)));
            targetYaw = Math.Acos(targetYaw) - Math.PI / 2;
            double targetRoll = Vector3D.Dot(left, Vector3D.Reject(Vector3D.Normalize(-ctr.GetNaturalGravity()), fwd));
            targetRoll = Math.Acos(targetRoll) - Math.PI / 2;
            return new Vector3D(targetYaw, -targetPitch, targetRoll);

        }

        /// <summary>
        /// Вращение гироскоров ракеты
        /// </summary>
        void SetGyro(IMyGyro gyro,Vector3D vec, float power)
        {

            gyro.SetValueFloat("Power", power);
            gyro.SetValueFloat("Yaw", (float)vec.GetDim(0));
            gyro.SetValueFloat("Pitch", (float)vec.GetDim(1));
            gyro.SetValueFloat("Roll", (float)vec.GetDim(2));
            
        }


        public void UpdateFlithtMissile()
        {
            for (int i = 0; i < missileInFlightList.Count; i++) 
            {
                if(missileInFlightList[i].RemotControl !=null)
                {
                    missileInFlightList[i].UpdateFlightTimer();

                    mPos = missileInFlightList[i].RemotControl.GetPosition();
                    double mSpeed = missileInFlightList[i].RemotControl.GetShipVelocities().LinearVelocity.Length();

                    IMyGyro gyro = missileInFlightList[i].Gyros[0];
                    IMyRemoteControl ctr = missileInFlightList[i].RemotControl;

                    //расчет точки перехвата цели и поворот её гироскопа на цель
                    Vector3D interPos = CalcInterceptPos(mPos, mSpeed, targetPosition, targetSpeed);
                    SetGyro(gyro, RottateMissileToTarget(interPos, gyro, ctr) * 5, 1);

                    //Время жизни ракеты, для того чтоб не забивать лист ракет в полете
                    if (missileInFlightList[i].FlightTime > maxAviableFlightTime) 
                    {
                        missileInFlightList.RemoveAt(i);
                    }

                }
                else
                {
                    missileInFlightList.RemoveAt(i);//это запускается только когда игра запускает свой GC
                }
            }
        }


        public class ControlledMissile
        {

            public int FlightTime { private set; get; }

            public List<IMyGyro> Gyros;
            public List<IMyThrust> Trusters;//двигатели вперед
            public List<IMyThrust> GravityTrusters;//двигатели для гашения гравитации
            public List<IMyThrust> ManevricTrusters;//маневровые двигаетли
            public List<IMyWarhead> Warheads;//боеголовки

            public IMyRemoteControl RemotControl;//дистанционный контроль
            public IMyTerminalBlock MergeBlock;//блок соеденителя

            public ControlledMissile()
            {
                FlightTime = 0;

                Gyros = new List<IMyGyro>();
                Trusters = new List<IMyThrust>();
                GravityTrusters = new List<IMyThrust>();
                ManevricTrusters = new List<IMyThrust>();
                Warheads = new List<IMyWarhead>();
            }

            /// <summary>
            /// Проверка целостности ракеты, установлены ли все модули необходимые для полета 
            /// </summary>
            public bool MissileReady()
            {
                if (Gyros.Count < 1) 
                    return false;
                if (Trusters.Count < 1)
                    return false;
                if (RemotControl == null)
                    return false;
                if (MergeBlock == null)
                    return false;
                return true;
            }

            /// <summary>
            /// Пуск ракеты
            /// </summary>
            public void MissileFire()
            {
                foreach(var truster in Trusters)
                {
                    truster.SetValueBool("OnOff", true);
                    truster.ThrustOverridePercentage = 1;
                }

                foreach (var truster in GravityTrusters)
                {
                    truster.SetValueBool("OnOff", true);
  
                }

                foreach (IMyWarhead head in Warheads)
                {
                    head.IsArmed = true;
                }

                MergeBlock.SetValueBool("OnOff", false);
            }

            public void UpdateFlightTimer()
            {
                FlightTime++;
            }

        }

        //////////////END OF SCRIPT/////////////////////////////

    }
}

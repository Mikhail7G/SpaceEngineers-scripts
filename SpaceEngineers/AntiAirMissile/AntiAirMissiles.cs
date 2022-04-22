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

namespace SpaceEngineers.AntiAirMissile
{
    public sealed class Program : MyGridProgram
    {
        ///Класс управления ракетой с ручным наведением, может работать как в поле гравитации так и в космосе
        ///для работы нелбходимы: удаленный контроль 1 шт, прогрг блок 1 шт, антенна 1 шт, блок соеденитель 1 шт.
        ///После сборки ракеты в соих данных необходимо указать тэг ракеты пример Missile.1
        ///выполнить комманду rename - компоненты ракеты будут переименованны
        ///выполнить комманду build - покажет все ли впорядке
        ///для полетов в поле гравитации вручную переименовать двигатель в тэг missileGravutyEngineName
        ///комманды ракеты:
        ///rename deletenames - переименовть/ убрать имена
        ///build - инициализация ракеты
        ///prearm - запуск двигателей, включение детонаторов
        ///fire - запуск ракеты, отцепка от базы
        /// 
        /// <summary>
        /// Статус ракеты на всех этапах существования
        /// </summary>
        enum MissileState
        {
            Idle,
            Prearm,
            Disarm,
            TakeOff,
            Fly
        }


        MissileState currentState;


        List<IMyGyro> gyros;

        List<IMyThrust> trusters;//двигатели вперед
        List<IMyThrust> gravityTrusters;//двигатели для гашения гравитации
        List<IMyThrust> manevricTrusters;//маневровые двигаетли
        List<IMyWarhead> warheads;//боеголовки




        IMyRemoteControl remotControl;//
        IMyProgrammableBlock missilePC;//
        IMyTextPanel groundPanel;
        IMyRadioAntenna radioAnt;//
        IMyBroadcastListener listener;
        IMyTerminalBlock mergeBlock;

        string defaultTag = "ch1";//отправка телеметрии
        string tag = "ch1";//отправка телеметрии
        string tagResieved = "ch1R";//получение комманд
        string messageSended;
        string messageResieved;


        static string SPECIAL_NAME_SYMBOL = "!";
        string missileCustomName;

        string remoteControlName = "Radio";//
        string radioAntennaName = "Ant";//
        string decouplerName = "Decoupler";//
        string missileControllerName = "MissilePC";//
        string missileEnginesName = "MissileEng";//
        string missileGyrosName = "MissileGyro";//
        string missileGroundLCDName = "GroundLCD";
        string missileGravityEngineName = "Gravity";
        string missileManevricEngineName = "Manv";
        bool needManevric = false; //использовать ли маневровые двигатели при приближении к цели

  
        /// <summary>
        /// Параметры ракеты
        /// </summary>
        Vector3D missilePosition;
        Vector3D targetVector;
        Vector3D targetVelocity;
        MyShipVelocities shipVelocities;
        double missileSpeed;

        double targetDistance;//дистанция до цели
        double radioAltitude;//выстота по радиовысотомеру
        double flightAltitude;//безопасаня высота начала маневров ракеты
        float flightTime;//время полета ракеты после взлета
        float acitveTime;//время прошедшее после запуска ракеты и наведением ее на цель
        double impactTime; // время до столкновения

        double manevrTick = 0;
        int currentManevrEngIndex = 0;
        int prevManevrEngIndex = 0;


        public Program()
        {
            currentState = MissileState.Idle;

            gyros = new List<IMyGyro>();
            trusters = new List<IMyThrust>();
            gravityTrusters = new List<IMyThrust>();
            warheads = new List<IMyWarhead>();
            manevricTrusters = new List<IMyThrust>();

            targetVector = new Vector3D(0, 0, 0);
            missilePosition = new Vector3D(0, 0, 0);
            targetVelocity = new Vector3D(0, 0, 0);

            targetDistance = 0;
            radioAltitude = 0;
            flightAltitude = 150;
            flightTime = 0;
            acitveTime = 150;
            missileSpeed = 0;

            messageSended = " ";
            messageResieved = " ";
            missileCustomName = "";
 
            IGC.RegisterBroadcastListener(tag);
            listener = IGC.RegisterBroadcastListener(tagResieved);
            listener.SetMessageCallback(tagResieved);

        }

        public void Main(string args, UpdateType updateSource)
        {
            //Строки аргументов: PREARM - подготовка ракеты.
            //BUILD - Сторительство ракеты, выполняется на блоке управления ракетами

            string[] argument = args.Split('|');

            if (argument.Length > 0)
            {
                string state = argument[0].ToUpper();


                switch (state)
                {
                    case "RENAME":
                        ComponentsRename();
                    break;
                    case "DELETENAMES":
                        ClearComponentsRename();
                        break;
                    case "BUILD":
                        {
                            MissileInit(null);
                        }
                        break;

                    case "PREARM":
                        Prearm();
                        if (argument.Length > 1)
                            double.TryParse(argument[1], out flightAltitude);
                        break;
                    case "DISARM":
                        Disarm();
                        break;
                    case "FIRE":
                        TakeOff();
                        break;
                }
            }

            switch (currentState)
            {
                case MissileState.Prearm:
                    MissileTelemetry();
                    break;
                case MissileState.TakeOff:
                    MissileTelemetry();

                    UpdateFlightTimer();
                    break;

                case MissileState.Fly:
                    MissileTelemetry();

                    UpdateFlightTimer();
                    FlyToTarget();
                    Manevric();
                    break;
            }

        }

        public void Save()
        {

        }

        /// <summary>
        /// Смена названий компонентов ракеты для множественного копирования и контроля
        /// </summary>
        public void ComponentsRename()
        {
            List<IMyProgrammableBlock> terms = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(terms, b=>b.CubeGrid == Me.CubeGrid);

            foreach(var term in terms)
            {
                if (term != null)
                {
                    missileCustomName = term.CustomData;
                    term.CustomName = SPECIAL_NAME_SYMBOL + missileCustomName + missileControllerName + " " + term.EntityId;
                }
            }

            List<IMyRadioAntenna> ants = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType(ants);
            foreach(var ant in ants)
            {
                if (ant != null)
                {
                    ant.CustomName = SPECIAL_NAME_SYMBOL + missileCustomName + radioAntennaName;
                }
            }

            List<IMyRemoteControl> remConts = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remConts);
            foreach(var rem in remConts)
            {
                if (rem != null)
                {
                    rem.CustomName = SPECIAL_NAME_SYMBOL + missileCustomName + remoteControlName;
                }
            }

            List<IMyShipMergeBlock> merges = new List<IMyShipMergeBlock>();
            GridTerminalSystem.GetBlocksOfType(merges);
            foreach(var merg in merges)
            {
                if (merg != null) 
                merg.CustomName = SPECIAL_NAME_SYMBOL + missileCustomName + decouplerName;
            }

            List<IMyThrust> trusts = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(trusts);
            int i = 0;
            foreach(var trust in trusts)
            {
                if (trust != null)
                trust.CustomName = SPECIAL_NAME_SYMBOL + missileCustomName + missileEnginesName + " " + i;
                i++;
            }

            List<IMyGyro> gyrs = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(gyrs);
            i = 1;
            foreach(var gyr in gyrs)
            {
                if (gyr != null)
                gyr.CustomName = SPECIAL_NAME_SYMBOL + missileCustomName + missileGyrosName + " " + i;
                i++;
            }

        }

        /// <summary>
        /// Сбрасывает переименование компонентов до начального
        /// </summary>
        public void ClearComponentsRename()
        {
            //настройка имени для прогр блока
            List<IMyProgrammableBlock> contr = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(contr, b => b.CubeGrid == Me.CubeGrid);
            foreach (var ctr in contr)
            {
                if (ctr != null)
                {
                    ctr.CustomName = missileControllerName;
                }
                else
                {
                    Echo("No Programmable block\n");
                }

            }
            //натсройка имени блока радиоуправления
            List<IMyRemoteControl> remctr = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remctr);
            if (remctr.Count > 0)
            {
                foreach (var rem in remctr)
                {

                    rem.CustomName = remoteControlName;
                }
            }
            else
            {
                Echo("No remote control block\n");
            }
            //Антенна
            List<IMyRadioAntenna> radants = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType(radants);
            if (radants.Count > 0)
            {
                foreach (var ant in radants)
                {
                    ant.CustomName = radioAntennaName;
                }
            }
            else
            {
                Echo("No antenna\n");
            }

            List<IMyShipMergeBlock> decops = new List<IMyShipMergeBlock>();
            GridTerminalSystem.GetBlocksOfType(decops);
            if (decops.Count > 0)
            {
                foreach (var dec in decops)
                {
                    dec.CustomName = decouplerName;
                }
            }
            else
            {
                Echo("No Decoupler\n");
            }

            List<IMyThrust> trusts = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(trusts);
            if (trusts.Count > 0)
            {
                foreach (var tr in trusts)
                {
                    tr.CustomName = missileEnginesName;
                }
            }
            else
            {
                Echo("No Engines\n");
            }

            List<IMyGyro> gyrs = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(gyrs);
            if (gyrs.Count > 0)
            {
                foreach (var gyr in gyrs)
                {
                    gyr.CustomName = missileGyrosName;
                }
            }
            else
            {
                Echo("No Gyro\n");
            }
        }

        /// <summary>
        /// Первоначальная настрйка ракеты, поиск блоков
        /// </summary>
        public void MissileInit(string customName)
        {
            missileCustomName = Me.CustomData;
            trusters.Clear();
            gravityTrusters.Clear();
            gyros.Clear();
            warheads.Clear();
            manevricTrusters.Clear();

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, b => b.CubeGrid == Me.CubeGrid);

            foreach(var block in blocks)
            {
                if (block.CustomName == (SPECIAL_NAME_SYMBOL + missileCustomName + remoteControlName))
                {
                    var conv = block as IMyRemoteControl;
                    if (conv != null)
                        remotControl = conv;
                }

                if (block.CustomName == (SPECIAL_NAME_SYMBOL + missileCustomName + radioAntennaName))
                {
                    var conv = block as IMyRadioAntenna;
                    if (conv != null)
                        radioAnt = conv;
                }

                if (block.CustomName == (SPECIAL_NAME_SYMBOL + missileCustomName + decouplerName))
                {
                    var conv = block as IMyTerminalBlock;
                    if (conv != null)
                        mergeBlock = conv;
                }

                if (block.CustomName.Contains(SPECIAL_NAME_SYMBOL + missileCustomName + missileEnginesName))
                {
                    var conv = block as IMyThrust;
                    if (conv != null)
                    {
                        trusters.Add(conv);
                    }
                }

                if (block.CustomName.Contains(SPECIAL_NAME_SYMBOL + missileCustomName + missileGravityEngineName))
                {
                    var conv = block as IMyThrust;
                    if (conv != null)
                    {
                        gravityTrusters.Add(conv);
                    }
                }

             
                if (block.CustomName.Contains(SPECIAL_NAME_SYMBOL + missileCustomName + missileGyrosName))
                {
                    var conv = block as IMyGyro;
                    if (conv != null)
                    {
                        conv.SetValueBool("Override", true);
                        gyros.Add(conv);
                    }
                }

                if (block.CustomName.Contains(SPECIAL_NAME_SYMBOL + missileCustomName + missileManevricEngineName))
                {
                    var conv = block as IMyThrust;
                    if (conv != null)
                    {
                        manevricTrusters.Add(conv);
                    }
                }

                if (block is IMyWarhead)
                {
                    var conv = block as IMyWarhead;
                    if (conv != null)
                        warheads.Add(conv);
                }


            }

            groundPanel = GridTerminalSystem.GetBlockWithName("missileGroundLCDName") as IMyTextPanel;

            blocks.Clear();

            if (remotControl == null)
                Echo("No Remote Control module");
            if (radioAnt == null)
                Echo("No Antenna");
            if (mergeBlock == null)
                Echo("No Merge block");

            if (groundPanel != null)
            {
                Echo("LCD telemetry finded");
            }


            // tag = missileCustomName + tag;

            Echo(remotControl?.CustomName + "\n" + radioAnt?.CustomName + "\n" + mergeBlock?.CustomName);
            Echo("Engines: " + trusters.Count.ToString());
            Echo("Gravity Engines: " + gravityTrusters.Count.ToString());
            Echo("Manevr Engines: " + manevricTrusters.Count.ToString());
            Echo("Gyros: " + gyros.Count.ToString());
            Echo("Warheads: " + warheads.Count.ToString());
            Echo("Ground telemetry tag: " + tag);
            Echo("Radio command tag: " + tagResieved);



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

            foreach (IMyThrust truster in gravityTrusters)
            {
                truster.SetValueBool("OnOff", activate);
            }

            foreach (IMyThrust truster in manevricTrusters)
            {
                truster.SetValueBool("OnOff", activate);
            }

            Random rnd = new Random();
            currentManevrEngIndex = rnd.Next(0, manevricTrusters.Count);

            foreach (IMyWarhead head in warheads)
            {
                head.IsArmed = true;
            }
        }

        /// <summary>
        /// Подготовка ракеты ко взлету
        /// </summary>
        public void Prearm()
        {
            if (currentState == MissileState.Idle)
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
        /// Взлет ракеты, отстыковка
        /// </summary>
        public void TakeOff()
        {
            if(currentState == MissileState.Prearm)
            {
                currentState = MissileState.TakeOff;
                //расцепка замков ракеты
                mergeBlock.SetValueBool("OnOff", false);
            }
        }

        /// <summary>
        /// Ракета выполняет маневры при приближении к цели
        /// </summary>
        public void Manevric()
        {

            if ((targetDistance < 1000) && (needManevric)) 
            {
                manevrTick++;

                if(manevrTick>90)
                {
                    manevrTick = 0;
                    if (manevricTrusters[currentManevrEngIndex] != null)
                    {
                        manevricTrusters[prevManevrEngIndex].ThrustOverridePercentage = 0;
                        manevricTrusters[currentManevrEngIndex].ThrustOverridePercentage = 1;
                    }
                    prevManevrEngIndex = currentManevrEngIndex;
                    currentManevrEngIndex++;
                    if (currentManevrEngIndex == manevricTrusters.Capacity)
                    {
                        currentManevrEngIndex = 0;
                    }
                }
            }

        }

        /// <summary>
        /// Расчет точки перехвата ракеты при стрельбе по движущейся цели
        /// </summary>
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
                impactTime = Math.Abs(directionToTarget.Length()) / (Math.Abs(targetVelTang.Length()) - Math.Abs(shootVelOrto.Length()));

                return shootVelOrto + shootVelTang;
            }
        }

        public Vector3D RottateMissileToTarget(Vector3D targetPos)
        {
            Vector3D fwd = gyros[0].WorldMatrix.Forward;
            Vector3D up = gyros[0].WorldMatrix.Up;
            Vector3D left = gyros[0].WorldMatrix.Left;

            Vector3D targetNormal = Vector3D.Normalize(targetPos);
            Vector3D vecReject = Vector3D.Reject(Vector3D.Normalize(shipVelocities.LinearVelocity), targetNormal);
            Vector3D correctVect = Vector3D.Normalize(targetNormal - vecReject * 2);

            double targetPitch = Vector3D.Dot(up, Vector3D.Normalize(Vector3D.Reject(correctVect, left)));
            targetPitch = Math.Acos(targetPitch) - Math.PI / 2;
            double targetYaw = Vector3D.Dot(left, Vector3D.Normalize(Vector3D.Reject(correctVect, up)));
            targetYaw = Math.Acos(targetYaw) - Math.PI / 2;
            double targetRoll = Vector3D.Dot(left, Vector3D.Reject(Vector3D.Normalize(-remotControl.GetNaturalGravity()), fwd));
            targetRoll = Math.Acos(targetRoll) - Math.PI / 2;
            return new Vector3D(targetYaw, -targetPitch, targetRoll);

        }

       
        void SetGyro(Vector3D vec, float power)
        {

            foreach (IMyGyro gyro in gyros)
            {
                gyro.SetValueFloat("Power", power);
                gyro.SetValueFloat("Yaw", (float)vec.GetDim(0));
                gyro.SetValueFloat("Pitch", (float)vec.GetDim(1));
                gyro.SetValueFloat("Roll", (float)vec.GetDim(2));
            }
        }

        void FlyToTarget()
        {

            Vector3D newVec = CalcInterceptPos(missilePosition, missileSpeed, targetVector, targetVelocity);
             SetGyro(RottateMissileToTarget(newVec) * 5, 1);
        }

        /// <summary>
        /// Полетное время ракеты с момента отстыковки
        /// </summary>
        public void UpdateFlightTimer()
        {
            flightTime++;

            if ((currentState == MissileState.TakeOff) && (flightTime > acitveTime))
                currentState = MissileState.Fly;
                   
        }

        /// <summary>
        /// Получение телеметрии ракеты скорость, координаты
        /// </summary>
        public void GetMissileTelemetry()
        {

            targetDistance = Vector3D.Distance(targetVector, remotControl.GetPosition());
            remotControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out radioAltitude);

            missilePosition = remotControl.GetPosition();

            shipVelocities = remotControl.GetShipVelocities();
            missileSpeed = shipVelocities.LinearVelocity.Length();

            if (groundPanel != null)
            {
                groundPanel.WriteText("", true);
                groundPanel.WriteText("Flight Time: " + flightTime +
                                      "\nDistanse to target: " + Math.Round(targetDistance) +
                                      "\nRadio altimiter: " + Math.Round(radioAltitude) +
                                      "\nSpeed: " + Math.Round(missileSpeed) +
                                      "\nTargetSpeed:" + Math.Round(targetVelocity.Length()) +
                                      "\nImapacTime: " + impactTime, false);
            }

        }

        /// <summary>
        /// Отправка телеметрии через антенну
        /// </summary>
        public void SendTelemetryByRadio()
        {
            messageSended = Math.Round(missilePosition.X).ToString() +
               "|" + Math.Round(missilePosition.Y).ToString() +
               "|" + Math.Round(missilePosition.Z).ToString() +
               "|" + Math.Round(radioAltitude) +
               "|" + Math.Round(targetDistance);


            IGC.SendBroadcastMessage(tag, messageSended, TransmissionDistance.TransmissionDistanceMax);
        }

        /// <summary>
        /// Получение координат цели через антенну
        /// </summary>
        public void GetTargetByRadio()
        {
            while (listener.HasPendingMessage)
            {
                MyIGCMessage mess = listener.AcceptMessage();
                if (mess.Tag == tagResieved)
                {
                    string[] str = mess.Data.ToString().Split('|');
                    ///координаты цели
                    double.TryParse(str[0], out targetVector.X);
                    double.TryParse(str[1], out targetVector.Y);
                    double.TryParse(str[2], out targetVector.Z);
                    ///его вектор скоротси
                    double.TryParse(str[3], out targetVelocity.X);
                    double.TryParse(str[4], out targetVelocity.Y);
                    double.TryParse(str[5], out targetVelocity.Z);
                }
            }
        }

        /// <summary>
        /// Обработка параметров ракеты
        /// </summary>
        public void MissileTelemetry()
        {
            GetMissileTelemetry();

            GetTargetByRadio();
            SendTelemetryByRadio();
        }

    }

}

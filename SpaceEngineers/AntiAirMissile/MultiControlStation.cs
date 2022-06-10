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


namespace SpaceEngineers.MultiControlStation
{
    public sealed class Program : MyGridProgram
    {
        /// <summary>
        /// Максимальное время полета ракеты, т.е. время контроля ракеты станцией в секундах
        /// </summary>
        int maxAviableFlightTime = 120; //seconds

        /// <summary>
        /// Дистанция потрыва ракеты в метрах
        /// </summary>
        public int missileRadioExplosionDistance = 10;

        /// <summary>
        /// Назавания компонентов наземной станции
        /// </summary>
        string antennaName = "!AntGround";
        string textMonitorName = "!MissileMonitor";
        string textMonitorMissileName = "!MissileInfo";

        /// <summary>
        /// Название компонентов блоков ракет
        /// </summary>
        string remoteControlName = "Radio";//
        string decouplerName = "Decoupler";//
        string missileEnginesName = "MissileEng";//
        string missileGyrosName = "MissileGyro";//
        string missileGravityEngineName = "Gravity";
        string missileManevricEngineName = "Manv";
        string missileWarheadName = "Warhead";

        string missileTagResiever = "ch1R";//Получаем данные от системы целеуказания по радиоканалу

        /// <summary>
        ///////////// DO NOT EDIT BELLOW THE LINE  ////////////////////
        /// </summary>

        int currentSelectedMissile = 0;//текущая выбранная ракета
        double distanceToTargetFromBase = 0;
        Vector3D targetPosition;//положение цели
        Vector3D targetSpeed;//скрость цели

        bool scriptEnabled = false;//работает ли скрипт каждый тик
        string missileTagFinder = null;//тэг поиска ракет

        //компоненты пусковой базы
        IMyRadioAntenna antenna;
        IMyTextPanel panel;
        IMyTextPanel panelInfo;
        IMyBroadcastListener listener;//слушаем эфир на получение данных о целях по радио

        //Ракеты на базе и в полете
        List<ControlledMissile> missileList;
        List<ControlledMissile> missileInFlightList;

        public Program()
        {
            targetPosition = new Vector3D(0,0,0);
            targetSpeed = new Vector3D(0, 0, 0);

            missileList = new List<ControlledMissile>();
            missileInFlightList = new List<ControlledMissile>();

            antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
            panel = GridTerminalSystem.GetBlockWithName(textMonitorName) as IMyTextPanel;
            panelInfo = GridTerminalSystem.GetBlockWithName(textMonitorMissileName) as IMyTextPanel;

            listener = IGC.RegisterBroadcastListener(missileTagResiever);
            listener.SetMessageCallback(missileTagResiever);

            WriteMissileStaticParams();
        }

        public void Main(string args)
        {
            string[] arguments = args.Split('|');
            if(arguments.Length>0)
            {
                string arg = arguments[0].ToUpper();
                switch(arg)
                {
                    case "FIRE"://стрельба залпом
                        FireAll();
                        DrawMissileInfo();
                        break;

                    case "FIREONCE"://стрельба по одной
                        FireOne();
                        DrawMissileInfo();
                        break;

                    case "BUILD"://поиск всех ракет по тэгам
                        FindMissile();
                        DrawMissileInfo();
                        break;

                    case "ENABLE"://запуск скрипта
                        Enable();
                        break;

                    case "DISABLE"://остановка скрипта
                        Disable();
                        break;

                    case "NEXT"://выбро след. ракеты
                        currentSelectedMissile++;
                        DrawMissileInfo();
                        break;
                }
            }

            GetTargetByRadio();
            CalculateParametres();

            UpdateFlightMissile();

            DrawInfo();
        }

        /// <summary>
        /// Запускает постоянное обновление скрипта
        /// </summary>
        public void Enable()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            scriptEnabled = true;
        }

        /// <summary>
        /// Откоючает обновление скрипта для экономии ресурсов
        /// </summary>
        public void Disable()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            scriptEnabled = false;
        }

        public void WriteMissileStaticParams()
        {
            ControlledMissile.MissileRadioExpDistance = missileRadioExplosionDistance;
        }

        /// <summary>
        /// Поиск компонентов с названиями по группам, для сборки ракет в субгридах
        /// </summary>
        public void FindMissile()
        {
            missileTagFinder = Me.CustomData;//тэг для поиска ракет
            missileList.Clear();

            currentSelectedMissile = 0;

            if (missileTagFinder != null)
            {
                Echo("Try to find with tag: " + missileTagFinder);

                //Получаем все группы блоков с тэгом и формируем ракеты добавляя блоки из групп
                List<IMyBlockGroup> group = new List<IMyBlockGroup>();
                GridTerminalSystem.GetBlockGroups(group, g => g.Name.Contains(missileTagFinder));
                Echo("D_groups: " + group.Count);

                for (int i = 0; i < group.Count; i++)
                {
                    List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
                    group[i].GetBlocks(groupBlocks);

                    ControlledMissile missile = new ControlledMissile();
                    missile.MissileName = group[i].Name;

                    //Приск блоков ракеты и проверка на проварку всех блоков
                    if (missile.BuildMissile(groupBlocks).MissileReady()) 
                    {
                        missileList.Add(missile);
                    }
                    else
                    {
                        Echo("Some misssile have problem with build: " + group[i].Name);
                    }

                }//for

                Echo("Missile detection completed! Missiles: " + missileList.Count);
                missileList = missileList.OrderBy(m => m.MissileName).ToList();
            }
            else
            {
                Echo("No tag! Enter custom data!");
            }

            GetMissileInfo();
        }

        /// <summary>
        /// Вывод отладочной информации в блок
        /// </summary>
        public void GetMissileInfo()
        {
            Echo($"Radio target rcv channel: {missileTagResiever}");
            Echo($"Station control time: {maxAviableFlightTime} sec");

            if (antenna != null) 
            {
                Echo("Antenna ready");
            }
            else
            {
                Echo("No radio antenna!");
            }

            if (panel != null) 
            {
                Echo("Text panel OK");
            }
            else
            {
                Echo($"No text panel with name: {textMonitorName}");
            }

            if (panelInfo != null)
            {
                Echo("Bay status panel OK");
            }
            else
            {
                Echo($"No status panel with name: {textMonitorMissileName}");
            }

            Echo("-----Missiles info------");

            foreach(var missile in missileList)
            {
                missile.DrawMissileInfo(this);
            }
        }

        /// <summary>
        /// Получение координат цели через антенну
        /// </summary>
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
        /// Расчет некоторых параметров
        /// </summary>
        public void CalculateParametres()
        {
            distanceToTargetFromBase = (Me.CubeGrid.GetPosition() - targetPosition).Length();
        }

        /// <summary>
        /// Обновление всех ракет
        /// </summary>
        public void UpdateFlightMissile()
        {
            //Отключаем обновление блока если нет связей с ракетами
            if (missileInFlightList.Count == 0)
            {
                Disable();
                return;
            }

            for (int i = 0; i < missileInFlightList.Count; i++) 
            {
                if(missileInFlightList[i].MissileAlive())
                {
                    missileInFlightList[i].UpdateMissile(targetPosition, targetSpeed);

                    //Время жизни ракеты, для того чтоб не забивать лист ракет в полете
                    if (missileInFlightList[i].FlightTime > maxAviableFlightTime * 60) 
                    {
                        missileInFlightList[i].Detonate();
                        missileInFlightList.RemoveAt(i);
                    }

                }
                else
                {
                    missileInFlightList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Вывод информации о состоянии ракет и цели
        /// </summary>
        public void DrawInfo()
        {

            panel?.WriteText("", true);
            panel?.WriteText("Missiles in bay/In Flight: " + missileList.Count + "/" + missileInFlightList.Count +
                            "\nTarget speed: " + Math.Round(targetSpeed.Length()).ToString() +
                            "\nDist from base: " + Math.Round(distanceToTargetFromBase) +
                            "\nCalcStatus: " + scriptEnabled, false);
        }

        /// <summary>
        /// Вывод информации по выбранной ракете на дисплей
        /// </summary>
        public void DrawMissileInfo()
        {
            if (missileList.Count == 0)
            {
                panelInfo?.WriteText("     NO MISSILES IN BAY     ", false);
                return;
            }

            if (currentSelectedMissile < missileList.Count)
            {
            }
            else
            {
                currentSelectedMissile = 0;
            }

            var missile = missileList[currentSelectedMissile];

            panelInfo?.WriteText("", false);
            panelInfo?.WriteText("Missile: ", true);

            for (int i = 0; i < missileList.Count; i++)
            {
                int missileIndex = 0;
                int.TryParse(string.Join(string.Empty, missileList[i].MissileName.Where(c => char.IsDigit(c))), out missileIndex);

                if (i == currentSelectedMissile)
                {
                    panelInfo?.WriteText($" [{missileIndex}] ", true);
                }
                else
                {
                    panelInfo?.WriteText($" {missileIndex} ", true);
                }
            }

            panelInfo?.WriteText($"\nReady: {missile.MissileReady()}" +
                $"\nSize: {missile.RemotControl.CubeGrid.GridSizeEnum}" +
                $"\nRadio expl dist {missileRadioExplosionDistance} m" +
                $"\n----PowerSystems-----" +
                $"\nAcc powered: {missile.TotalBatteryPower * 100 / missile.MaxBatteryPower} % " +
                $"\n-------------------------", true);  
        }

        /// <summary>
        /// Запуск сразу всех ракет залпом
        /// </summary>
        public void FireAll()
        {
            if(missileList.Count==0)
            {
                FindMissile();
                return;
            }

            Enable();
            foreach (var missile in missileList)
            {
                if (missile != null)
                {
                    missileInFlightList.Add(missile);
                    missile.MissileFire();
                }
            }
            missileList.Clear();
        }
      
        /// <summary>
        /// Выстрел по одной выбранной ракете
        /// </summary>
        public void FireOne()
        {
            if (missileList.Count > 0)
            {
                if (missileList[currentSelectedMissile].MissileReady())
                {
                    missileInFlightList.Add(missileList[currentSelectedMissile]);
                    missileList[currentSelectedMissile].MissileFire();
                    missileList.RemoveAt(currentSelectedMissile);
                    currentSelectedMissile = 0;

                    Enable();
                }
            }
            else
            {
                FindMissile();
            }
        }


        public class ControlledMissile
        {
            public int FlightTime { private set; get; }
            public string MissileName { set; get; }
            public bool UseHirozonCorrector { set; get; }

            public List<IMyGyro> Gyros;
            public List<IMyThrust> Trusters;//двигатели вперед
            public List<IMyThrust> GravityTrusters;//двигатели для гашения гравитации
            public List<IMyThrust> ManevricTrusters;//маневровые двигаетли
            public List<IMyWarhead> Warheads;//боеголовки
            public List<IMyBatteryBlock> Batteries;//аккумуляторы

            public IMyRemoteControl RemotControl;//дистанционный контроль
            public IMyShipMergeBlock MergeBlock;//блок соеденителя
            public IMySensorBlock SensorBlock;

            public float TotalBatteryPower { set; get; } = 0;
            public float MaxBatteryPower { set; get; } = 0;

            public float EffectiveEngineThrust { set; get; } = 0;

            private double missileMass = 0;

            private Vector3D linearVel = new Vector3D();
            private Vector3D natGravity = new Vector3D();

            private int minimumSafeTakeOffTimer = 150;

            private int wanderTime = 0;
            private int spiralTime = 0;

            private double sqrDistance;

            private Vector3D wander = new Vector3D();

            public static int MissileRadioExpDistance = 5;

            public ControlledMissile()
            {
                FlightTime = 0;
                UseHirozonCorrector = false;

                Gyros = new List<IMyGyro>();
                Trusters = new List<IMyThrust>();
                GravityTrusters = new List<IMyThrust>();
                ManevricTrusters = new List<IMyThrust>();
                Warheads = new List<IMyWarhead>();
                Batteries = new List<IMyBatteryBlock>();

                Random rnd;
                rnd = new Random();
                wanderTime = rnd.Next(0,100) - 25;
            }

            /// <summary>
            /// Сборка ракеты из модулей в группе
            /// </summary>
            public ControlledMissile BuildMissile(List<IMyTerminalBlock> groupBlocks)
            {
                RemotControl = groupBlocks.FirstOrDefault(b => b is IMyRemoteControl) as IMyRemoteControl;
                MergeBlock = groupBlocks.FirstOrDefault(b => b is IMyShipMergeBlock) as IMyShipMergeBlock;
                SensorBlock = groupBlocks.FirstOrDefault(b => b is IMySensorBlock) as IMySensorBlock;

                Trusters = groupBlocks.Where(b => b is IMyThrust)
                                               .Where(b => b.Orientation.Forward == Base6Directions.GetOppositeDirection(RemotControl.Orientation.Forward))
                                               .Select(t => t as IMyThrust).ToList();

                ManevricTrusters = groupBlocks.Where(b => b is IMyThrust)
                                              .Where(b => b.Orientation.Forward != Base6Directions.GetOppositeDirection(RemotControl.Orientation.Forward))
                                              .Select(t => t as IMyThrust).ToList();

                Gyros = groupBlocks.Where(b => b is IMyGyro).Select(g => g as IMyGyro).ToList();
                Warheads = groupBlocks.Where(b => b is IMyWarhead).Select(w => w as IMyWarhead).ToList();

                Batteries = groupBlocks.Where(b => b is IMyBatteryBlock).Select(g => g as IMyBatteryBlock).ToList();

                MaxBatteryPower = Batteries.Sum(b => b.MaxStoredPower);
                TotalBatteryPower = Batteries.Sum(b => b.CurrentStoredPower);

                EffectiveEngineThrust = Trusters.Sum(b => b.MaxEffectiveThrust);

                foreach(var batt in Batteries)
                {
                    batt.ChargeMode = ChargeMode.Recharge;
                }

                foreach (var truster in Trusters)
                {
                    truster.SetValueBool("OnOff", false);
                    truster.ThrustOverridePercentage = -1;
                }

                foreach (var truster in ManevricTrusters)
                {
                    truster.SetValueBool("OnOff", false);
                }


                if (SensorBlock != null)
                {
                    SensorBlock.Enabled = false;
                    SensorBlock.FrontExtend = MissileRadioExpDistance;
                    SensorBlock.BackExtend = MissileRadioExpDistance;
                    SensorBlock.LeftExtend = MissileRadioExpDistance;
                    SensorBlock.RightExtend = MissileRadioExpDistance;
                    SensorBlock.BackExtend = MissileRadioExpDistance;
                    SensorBlock.TopExtend = MissileRadioExpDistance;
                    SensorBlock.BottomExtend = MissileRadioExpDistance;

                    SensorBlock.DetectStations = true;
                    SensorBlock.DetectNeutral = true;
                    SensorBlock.DetectEnemy = true;
                    SensorBlock.DetectFriendly = false;
                    SensorBlock.DetectFloatingObjects = true;
                    SensorBlock.DetectLargeShips = true;
                    SensorBlock.DetectSmallShips = true;
                    SensorBlock.DetectPlayers = true;
                    SensorBlock.DetectOwner = false;
                }


                return this;
            }

            /// <summary>
            /// Вывод информации в блок управления
            /// </summary>
            public void DrawMissileInfo(MyGridProgram EchoMonitor)
            {
                EchoMonitor.Echo("------------------");
                EchoMonitor.Echo($"Size: {RemotControl.CubeGrid.GridSizeEnum}" +
                   $"\nEngines: {Trusters.Count}" +
                   $"\nGyros: {Gyros.Count}" +
                   $"\nManevrEngines: {ManevricTrusters.Count}" +
                   $"\nWarheads: {Warheads.Count}" +
                   $"\nRadio expl dist {MissileRadioExpDistance} m");

                if (SensorBlock != null)
                    EchoMonitor.Echo("Sencor block: OK");

                EchoMonitor.Echo($"----PowerSystems-----" +
                $"\nTotal batt: {Batteries.Count}" +
                $"\nAcc powered: {TotalBatteryPower * 100 / MaxBatteryPower} % " +
                $"\nMass/Wheight: {Math.Round(RemotControl.CalculateShipMass().TotalMass, 2)} kg" +
                $"/{Math.Round(RemotControl.CalculateShipMass().TotalMass * RemotControl.GetNaturalGravity().Length(), 2)} N" +
                $"\nEng effective thrust: {EffectiveEngineThrust} kN");
                EchoMonitor.Echo("------------------");
            }

            /// <summary>
            /// Проверка целостности ракеты, установлены ли все модули необходимые для полета 
            /// </summary>
            public bool MissileReady()
            {
                if ((Gyros.Count == 0) || Gyros.Any(b => !b.IsFunctional))
                    return false;
                if ((Trusters.Count == 0) || Trusters.Any(b => !b.IsFunctional)) 
                    return false;
                if ((RemotControl == null) || (!RemotControl.IsFunctional)) 
                    return false;
                if ((MergeBlock == null) || (!MergeBlock.IsFunctional)) 
                    return false;
                if ((Batteries.Count == 0) || Batteries.Any(b => !b.IsFunctional))
                    return false;

                MaxBatteryPower = Batteries.Sum(b => b.MaxStoredPower);
                TotalBatteryPower = Batteries.Sum(b => b.CurrentStoredPower);
                EffectiveEngineThrust = Trusters.Sum(b => b.MaxEffectiveThrust);
                missileMass = RemotControl.CalculateShipMass().PhysicalMass;

                return true;
            }

            public bool MissileAlive()
            {
                if (Gyros.Any(g => g.Closed))
                    return false;
                if (Trusters.Any(t => t.Closed))
                    return false;
                if (RemotControl.Closed)
                    return false;

                return true;
            }

            /// <summary>
            /// Точка перехвата для движущихся целей
            /// </summary>
            public Vector3D CalcInterceptPos(Vector3D missilePos, double missileSpeed, Vector3D targetPos, Vector3D targetSpeed)
            {
                Vector3D directionToTarget = Vector3D.Normalize(targetPos - missilePos);
                Vector3D targetVelOrto = Vector3D.Dot(targetSpeed, directionToTarget) * directionToTarget;

                //Vector3D targetVelTang = targetSpeed - targetVelOrto;
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

            public Vector3D RottateMissileToTargetNew(Vector3D targetPos)
            {
                double targetRoll = 0;
                linearVel = RemotControl.GetShipVelocities().LinearVelocity;
                natGravity = RemotControl.GetNaturalGravity();

                Vector3D targetNormal = Vector3D.Normalize(targetPos);
                Vector3D vecReject = Vector3D.Reject(Vector3D.Normalize(linearVel), targetNormal);
                Vector3D correctVect = Vector3D.Normalize(targetNormal - vecReject * 2);


                if (UseHirozonCorrector)
                {
                    targetRoll = Vector3D.Dot(RemotControl.WorldMatrix.Left, Vector3D.Reject(Vector3D.Normalize(-natGravity), RemotControl.WorldMatrix.Forward));
                    targetRoll = Math.Acos(targetRoll) - Math.PI / 2;
                }

                Vector3D resultVector = Vector3D.Normalize(correctVect).Cross(RemotControl.WorldMatrix.Forward);

                if ((UseHirozonCorrector) && (!natGravity.IsZero())) 
                    resultVector += RemotControl.WorldMatrix.Backward * targetRoll;

                return resultVector;
            }
          
            public void SetGyro(Vector3D axis)
            {
                foreach (IMyGyro gyro in Gyros)
                {
                    gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up);
                    gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right);
                    gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward);
                }
            }

            public void Detonate()
            {
                foreach(var warhead in Warheads)
                {
                    if(!warhead.Closed)
                    {
                        warhead.Detonate();
                    }
                }
            }

            public void SensorDetection()
            {
                if (SensorBlock != null)
                {
                    SensorBlock.Enabled = true;
                    var det = SensorBlock.LastDetectedEntity;

                    if (!det.IsEmpty())
                    {
                        Detonate();
                    }
                }
            }

            /// <summary>
            /// Пуск ракеты
            /// </summary>
            public void MissileFire()
            {
                if (MissileReady())
                {
                    foreach (var batt in Batteries)
                    {
                        batt.ChargeMode = ChargeMode.Auto;
                    }

                    foreach (var truster in Trusters)
                    {
                        truster.SetValueBool("OnOff", true);
                        truster.ThrustOverridePercentage = 1;
                    }

                    foreach (var gyro in Gyros)
                    {
                        gyro.SetValueBool("Override", true);
                    }

                    foreach (var truster in ManevricTrusters)
                    {
                        truster.SetValueBool("OnOff", true);
                    }

                    foreach (IMyWarhead head in Warheads)
                    {
                        head.IsArmed = true;
                    }

                    MergeBlock.SetValueBool("OnOff", false);
                }
            }

            public void UpdateMissile(Vector3D targetPosition,Vector3D targetSpeed)
            {
                FlightTime++;

                if (FlightTime > minimumSafeTakeOffTimer)
                {
                    var mPos = RemotControl.GetPosition();
                    var mSpeed = RemotControl.GetShipVelocities().LinearVelocity.Length();
                    sqrDistance = Vector3D.DistanceSquared(mPos, targetPosition);

                    SensorDetection();
                    //if(sqrDistance < MissileExplTimer)
                    //{
                    //    Detonate();
                    //}

                    //расчет точки перехвата цели и поворот гироскопа на цель
                    Vector3D interPos = CalcInterceptPos(mPos, mSpeed, targetPosition, targetSpeed);

                    if (sqrDistance > 1000 * 1000) 
                    {
                        interPos += Wander(targetPosition);
                    }
                    else
                    {
                        interPos += Spiral(targetPosition);
                    }

                    SetGyro(RottateMissileToTargetNew(interPos));
                }
            }

            /// <summary>
            /// Режим случайного маневрирования
            /// </summary>
            public Vector3D Wander(Vector3D dist)
            {
                wanderTime++;
                if (wanderTime > 50)
                {
                    wanderTime = 0;

                    Random rnd = new Random();
                    var angle = rnd.Next(0, 40) - 20;

                    wander = RemotControl.WorldMatrix.Forward * 45 + RemotControl.WorldMatrix.Left * angle + RemotControl.WorldMatrix.Up * angle;
                    return wander;
                }
                else
                {
                    return wander;
                }
            }

            /// <summary>
            /// Движение по спирали
            /// </summary>
            public Vector3D Spiral(Vector3D dist)
            {
                double radius = 10;
                double maxSpiralTime = 300;

                spiralTime++;
                if (spiralTime > maxSpiralTime)
                    spiralTime = 0;

                var fwd = RemotControl.WorldMatrix.Forward * radius;
                var up = RemotControl.WorldMatrix.Up * radius;
                var left = RemotControl.WorldMatrix.Left * radius;

                double angle = 2 * Math.PI * spiralTime / maxSpiralTime;

                Vector3D spiral = fwd + (left * Math.Cos(angle) + up * Math.Sin(angle));

                return spiral;
            }

        }

        //////////////END OF SCRIPT/////////////////////////////

    }
}

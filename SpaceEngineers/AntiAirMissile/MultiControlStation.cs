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


namespace SpaceEngineers.AntiAirMissile
{
    public sealed class Program : MyGridProgram
    {
        /// <summary>
        /// Максимальное время полета ракеты, т.е. время контроля ракеты станцией в секундах
        /// </summary>
        int maxAviableFlightTime = 120; //seconds

        int missileTakeOffSafeyTimer = 150; //Ticks

        /// <summary>
        /// Дистанция потрыва ракеты в метрах
        /// </summary>
        int missileRadioExplosionDistance = 10;

        /// <summary>
        /// Назавания компонентов наземной станции
        /// </summary>
        string antennaName = "!AntGround";
        string textMonitorName = "!MissileMonitor";
        string textMonitorMissileName = "!MissileInfo";


        string missileTagResiever = "ch1R";//Получаем данные от системы целеуказания по радиоканалу

        /// <summary>
        ///////////// DO NOT EDIT BELLOW THE LINE  ////////////////////
        /// </summary>

        int currentSelectedMissile = 0;//текущая выбранная ракета
        double distanceToTargetFromBase = 0;
        Vector3D targetPosition;//положение цели
        Vector3D targetSpeed;//скрость цели

        bool scriptEnabled = false;//работает ли скрипт каждый тик
        string missileTagFinder = "";//тэг поиска ракет

        bool missileUseWander = true;
        bool missileUseSpiral = true;

        //компоненты пусковой базы
        IMyRadioAntenna antenna;
        IMyTextPanel panel;
        IMyTextPanel panelInfo;
        IMyBroadcastListener listener;//слушаем эфир на получение данных о целях по радио

        //Ракеты на базе и в полете
        List<ControlledMissile> missileList;
        List<ControlledMissile> missileInFlightList;

        MyIni dataSystem;
        PerformanceMonitor perfMonitor;

        public Program()
        {
            dataSystem = new MyIni();
            ReadIni();

            targetPosition = new Vector3D(0, 0, 0);
            targetSpeed = new Vector3D(0, 0, 0);

            missileList = new List<ControlledMissile>();
            missileInFlightList = new List<ControlledMissile>();

            antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
            panel = GridTerminalSystem.GetBlockWithName(textMonitorName) as IMyTextPanel;
            panelInfo = GridTerminalSystem.GetBlockWithName(textMonitorMissileName) as IMyTextPanel;

            listener = IGC.RegisterBroadcastListener(missileTagResiever);
            listener.SetMessageCallback(missileTagResiever);

            perfMonitor = new PerformanceMonitor(this, Me.GetSurface(1));

        }

        public void ReadIni()
        {
            InitData();

            MyIniParseResult dataResult;
            if (!dataSystem.TryParse(Me.CustomData, out dataResult))
            {
                Echo($"CustomData error:\nLine {dataResult}");
            }
            else
            {
                missileTagFinder = dataSystem.Get("Names", "MissileGroupName").ToString();
                antennaName = dataSystem.Get("Names", "StationAntennaName").ToString();
                textMonitorName = dataSystem.Get("Names", "StationMissileMonitor").ToString();
                textMonitorMissileName = dataSystem.Get("Names", "StationMissileInfoMonitor").ToString();
                missileTagResiever = dataSystem.Get("Names", "RadioResieveChannel").ToString();

                maxAviableFlightTime = dataSystem.Get("Params", "StationControlTime").ToInt32();
                missileTakeOffSafeyTimer = dataSystem.Get("Params", "MissileTakeOffSafeyTicks").ToInt32();
                missileRadioExplosionDistance = dataSystem.Get("Params", "RadioExplDistance").ToInt32();
                missileUseWander = dataSystem.Get("Params", "UseWander").ToBoolean();
                missileUseSpiral = dataSystem.Get("Params", "UseSpiral").ToBoolean();
            }

        }

        public void InitData()
        {
            var data = Me.CustomData;

            if(!data.Any())
            {
                dataSystem.AddSection("Names");
                dataSystem.Set("Names", "MissileGroupName", "Missile.");
                dataSystem.Set("Names", "RadioResieveChannel", "ch1R");

                dataSystem.Set("Names", "StationAntennaName", "!AntGround");
                dataSystem.Set("Names", "StationMissileMonitor", "!MissileMonitor");
                dataSystem.Set("Names", "StationMissileInfoMonitor", "!MissileInfo");

                dataSystem.AddSection("Params");
                dataSystem.Set("Params", "StationControlTime", 120);
                dataSystem.Set("Params", "MissileTakeOffSafeyTicks", 150);
                dataSystem.Set("Params", "RadioExplDistance", 10);
                dataSystem.Set("Params", "UseWander", true);
                dataSystem.Set("Params", "UseSpiral", true);

                Me.CustomData = dataSystem.ToString();
            }
        }

        public void Main(string args)
        {
            string[] arguments = args.Split('|');
            if (arguments.Length > 0)
            {
                string arg = arguments[0].ToUpper();
                switch (arg)
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

            perfMonitor.EndOfFrameCalc();
            perfMonitor.Draw();
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


        /// <summary>
        /// Поиск компонентов с названиями по группам, для сборки ракет в субгридах
        /// </summary>
        public void FindMissile()
        {
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

                    ControlledMissile missile = new ControlledMissile
                    {
                        MissileName = group[i].Name,
                        UseSpiral = missileUseSpiral,
                        UseWander = missileUseWander,
                        MissileRadioExpDistance = missileRadioExplosionDistance,
                        MinimumSafeTakeOffTimer = missileTakeOffSafeyTimer
                    };
                    //missile.MissileName = group[i].Name;
                    //missile.UseSpiral = missileUseSpiral;
                    //missile.UseWander = missileUseWander;
                    //missile.MissileRadioExpDistance = missileRadioExplosionDistance;
                    //missile.MinimumSafeTakeOffTimer = missileTakeOffSafeyTimer;

                    //Поиск блоков ракеты и проверка на проварку всех блоков
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

            GetStationInfo();
            perfMonitor.AddInstructions("");
        }

        /// <summary>
        /// Вывод отладочной информации в блок
        /// </summary>
        public void GetStationInfo()
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

            foreach (var missile in missileList)
            {
                missile.DrawMissileInfo(this);
            }

            perfMonitor.AddInstructions("");
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

            perfMonitor.AddInstructions("");
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
                if (missileInFlightList[i].MissileAlive())
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

            perfMonitor.AddInstructions("");
        }

        /// <summary>
        /// Вывод информации о состоянии ракет и цели
        /// </summary>
        public void DrawInfo()
        {

            panel?.WriteText("", true);
            panel?.WriteText($"Missiles in bay/In Flight: {missileList.Count} / {missileInFlightList.Count}" +
                             $"\nTarget speed: {Math.Round(targetSpeed.Length())}" +
                             $"\nDist from base: {Math.Round(distanceToTargetFromBase)}" +
                             $"\nCalcStatus: {scriptEnabled}", false);
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
            if (missileList.Count == 0)
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
            public int FlightTime { private set; get; }//время полета ракеты с момента запуска
            public string MissileName { set; get; }//имя
            public bool UseHirozonCorrector { set; get; }//использовать ли корректировку гироскопа для ориентации по гравитации
            public bool UseWander { set; get; } = true;//использование случайной траектории
            public bool UseSpiral { set; get; } = true;//финальная спиральная траектория

            public List<IMyGyro> Gyros;
            public List<IMyThrust> Thrusters;//двигатели вперед
            public List<IMyThrust> GravityTrusters;//двигатели для гашения гравитации
            public List<IMyThrust> ManevricThrusters;//маневровые двигаетли
            public List<IMyWarhead> Warheads;//боеголовки
            public List<IMyBatteryBlock> Batteries;//аккумуляторы

            public IMyRemoteControl RemotControl;//дистанционный контроль
            public IMyShipMergeBlock MergeBlock;//блок соеденителя
            public IMySensorBlock SensorBlock;

            public double FinalApproachDistance { set; get; } = 1000;//дистанция финального наведения на цель
            public float TotalBatteryPower { set; get; } = 0;//заряд батарей
            public float MaxBatteryPower { set; get; } = 0;//отдача батарей
            public float EffectiveEngineThrust { set; get; } = 0;//эффективная тяга двигателей
            public int MissileRadioExpDistance { set; get; } = 5;//дистанция срабатывания сенсора на детонацию
            public double SpiralRadius { set; get; } = 10;//радиус спирали
            public double SpiralMaxTime { set; get; } = 300;//время спирали
            public int MinimumSafeTakeOffTimer { set; get; } = 150;//время в тиках безопасного взлета ракеты после запуска

            private Vector3D linearVel = new Vector3D();
            private Vector3D natGravity = new Vector3D();

            private int wanderTime = 0;
            private int spiralTime = 0;

            private double sqrDistance;

            private Vector3D wander = new Vector3D();

            //Настройки  ПИД регулятора
            private PIDRegulator yawPID;
            private PIDRegulator rollPID;
            private PIDRegulator pitchPID;

            private double pK = 1;
            private double dK = 0;
            private double iK = 0;

            public ControlledMissile()
            {
                FlightTime = 0;
                UseHirozonCorrector = false;

                Gyros = new List<IMyGyro>();
                Thrusters = new List<IMyThrust>();
                GravityTrusters = new List<IMyThrust>();
                ManevricThrusters = new List<IMyThrust>();
                Warheads = new List<IMyWarhead>();
                Batteries = new List<IMyBatteryBlock>();

                Random rnd;
                rnd = new Random();
                wanderTime = rnd.Next(0, 100) - 25;

                yawPID = new PIDRegulator();
                rollPID = new PIDRegulator();
                pitchPID = new PIDRegulator();

                yawPID.SetK(pK, dK, iK);
                rollPID.SetK(pK, dK, iK);
                pitchPID.SetK(pK, dK, iK);

            }

            /// <summary>
            /// Сборка ракеты из модулей в группе
            /// </summary>
            public ControlledMissile BuildMissile(List<IMyTerminalBlock> groupBlocks)
            {
                RemotControl = groupBlocks.FirstOrDefault(b => b is IMyRemoteControl) as IMyRemoteControl;
                MergeBlock = groupBlocks.FirstOrDefault(b => b is IMyShipMergeBlock) as IMyShipMergeBlock;
                SensorBlock = groupBlocks.FirstOrDefault(b => b is IMySensorBlock) as IMySensorBlock;

                if (RemotControl != null)
                {

                    Thrusters = groupBlocks.Where(b => b is IMyThrust)
                                                   .Where(b => b.Orientation.Forward == Base6Directions.GetOppositeDirection(RemotControl.Orientation.Forward))
                                                   .Select(t => t as IMyThrust).ToList();

                    ManevricThrusters = groupBlocks.Where(b => b is IMyThrust)
                                                  .Where(b => b.Orientation.Forward != Base6Directions.GetOppositeDirection(RemotControl.Orientation.Forward))
                                                  .Select(t => t as IMyThrust).ToList();
                }

                Gyros = groupBlocks.Where(b => b is IMyGyro).Select(g => g as IMyGyro).ToList();
                Warheads = groupBlocks.Where(b => b is IMyWarhead).Select(w => w as IMyWarhead).ToList();

                Batteries = groupBlocks.Where(b => b is IMyBatteryBlock).Select(g => g as IMyBatteryBlock).ToList();

                MaxBatteryPower = Batteries.Sum(b => b.MaxStoredPower);
                TotalBatteryPower = Batteries.Sum(b => b.CurrentStoredPower);

                EffectiveEngineThrust = Thrusters.Sum(b => b.MaxEffectiveThrust);

                foreach (var batt in Batteries)
                {
                    batt.ChargeMode = ChargeMode.Recharge;
                }

                foreach (var truster in Thrusters)
                {
                    //truster.SetValueBool("OnOff", false);
                    truster.Enabled = false;
                    truster.ThrustOverridePercentage = -1;
                }

                foreach (var truster in ManevricThrusters)
                {
                    // truster.SetValueBool("OnOff", false);
                    truster.Enabled = false;
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
                   $"\nEngines: {Thrusters.Count}" +
                   $"\nGyros: {Gyros.Count}" +
                   $"\nManevrEngines: {ManevricThrusters.Count}" +
                   $"\nWarheads: {Warheads.Count}" +
                   $"\nRadio expl dist {MissileRadioExpDistance} m");

                string sensorSta = SensorBlock != null ? "Sencor block: OK" : "No Sencor";
                EchoMonitor.Echo(sensorSta);

                EchoMonitor.Echo($"----PowerSystems-----" +
                $"\nTotal batt: {Batteries.Count}" +
                $"\nAcc powered: {TotalBatteryPower * 100 / MaxBatteryPower} % " +
                $"\nEng effective thrust: {EffectiveEngineThrust} kN");
                EchoMonitor.Echo("------------------");
            }

            /// <summary>
            /// Проверка целостности ракеты, установлены ли все модули необходимые для полета 
            /// </summary>
            public bool MissileReady()
            {
                if (Gyros.Count == 0 || Gyros.Any(b => !b.IsFunctional))
                    return false;
                if (Thrusters.Count == 0 || Thrusters.Any(b => !b.IsFunctional))
                    return false;
                if (RemotControl == null || !RemotControl.IsFunctional)
                    return false;
                if (MergeBlock == null || !MergeBlock.IsFunctional)
                    return false;
                if (Batteries.Count == 0 || Batteries.Any(b => !b.IsFunctional))
                    return false;

                MaxBatteryPower = Batteries.Sum(b => b.MaxStoredPower);
                TotalBatteryPower = Batteries.Sum(b => b.CurrentStoredPower);
                EffectiveEngineThrust = Thrusters.Sum(b => b.MaxEffectiveThrust);

                return true;
            }

            public bool MissileAlive()
            {
                if (Gyros.Any(g => g.Closed))
                    return false;
                if (Thrusters.Any(t => t.Closed))
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

                Vector3D resultVector = Vector3D.Normalize(correctVect).Cross(RemotControl.WorldMatrix.Forward);

                if (UseHirozonCorrector && !natGravity.IsZero())
                {
                    targetRoll = Vector3D.Dot(RemotControl.WorldMatrix.Left, Vector3D.Reject(Vector3D.Normalize(-natGravity), RemotControl.WorldMatrix.Forward));
                    targetRoll = Math.Acos(targetRoll) - Math.PI / 2;

                    resultVector += RemotControl.WorldMatrix.Backward * targetRoll;
                }

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
                foreach (var warhead in Warheads)
                {
                    if (!warhead.Closed)
                    {
                        warhead.Detonate();
                    }
                }
            }

            public void SensorDetection()
            {
                if (SensorBlock != null && !SensorBlock.Closed)
                {
                    //var det = SensorBlock.LastDetectedEntity;

                    // if (!det.IsEmpty())
                    if (SensorBlock.IsActive)
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

                    foreach (var thruster in Thrusters)
                    {
                        //thruster.SetValueBool("OnOff", true);
                        thruster.Enabled = true;
                        thruster.ThrustOverridePercentage = 1;
                    }

                    foreach (var gyro in Gyros)
                    {
                       // gyro.SetValueBool("Override", true);
                        gyro.GyroOverride = true;
                    }

                    foreach (var thruster in ManevricThrusters)
                    {
                        //thruster.SetValueBool("OnOff", true);
                        thruster.Enabled = true;
                    }

                    foreach (IMyWarhead head in Warheads)
                    {
                        head.IsArmed = true;
                    }

                    if(SensorBlock!=null)
                    {
                        SensorBlock.Enabled = true;
                    }

                   // MergeBlock.SetValueBool("OnOff", false);
                    MergeBlock.Enabled = false;
                }
            }

            public void UpdateMissile(Vector3D targetPosition, Vector3D targetSpeed)
            {
                FlightTime++;

                if (FlightTime > MinimumSafeTakeOffTimer)
                {
                    var mPos = RemotControl.GetPosition();
                    var mSpeed = RemotControl.GetShipVelocities().LinearVelocity.Length();
                    sqrDistance = Vector3D.DistanceSquared(mPos, targetPosition);

                    SensorDetection();

                    //расчет точки перехвата цели и поворот гироскопа на цель
                    Vector3D interPos = CalcInterceptPos(mPos, mSpeed, targetPosition, targetSpeed);

                    if (sqrDistance > FinalApproachDistance * FinalApproachDistance)
                    {
                        interPos += Wander();
                    }
                    else
                    {
                        interPos += Spiral();
                    }

                    SetGyro(RottateMissileToTargetNew(interPos));
                }
            }

            /// <summary>
            /// Режим случайного маневрирования
            /// </summary>
            public Vector3D Wander()
            {
                if (!UseWander)
                    return Vector3D.Zero;

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
            public Vector3D Spiral()
            {
                if(!UseSpiral)
                    return Vector3D.Zero;

                spiralTime++;
                if (spiralTime > SpiralMaxTime)
                    spiralTime = 0;

                var fwd = RemotControl.WorldMatrix.Forward * SpiralRadius;
                var up = RemotControl.WorldMatrix.Up * SpiralRadius;
                var left = RemotControl.WorldMatrix.Left * SpiralRadius;

                double angle = 2 * Math.PI * spiralTime / SpiralMaxTime;

                Vector3D spiral = fwd + left * Math.Cos(angle) + up * Math.Sin(angle);

                return spiral;
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
                mainDisplay?.WriteText($"CUR ins: {TotalInstructions}" +
                                      $"\nAV inst: {AverageInstructionsPerTick} / {MaxInstructionsPerTick}" +
                                      $"\nAV time:{AverageTimePerTick}", true);

            }

        }

        public class PIDRegulator
        {

            double P = 0;
            double D = 0;
            double I = 0;

            public double Kp { get; set; } = 1;
            public double Kd { get; set; } = 1;
            public double Ki { get; set; } = 1;

            public double DeltaTimer { get; set; } = 1;

            double prevD = 0;

            public PIDRegulator()
            {

            }

            /// <summary>
            /// Установка коэффициентов
            /// </summary>
            public PIDRegulator SetK(double _Kp, double _kD, double _kI)
            {
                Kp = _Kp;
                Kd = _kD;
                Ki = _kI;

                return this;
            }

            /// <summary>
            /// Установка регулирующего значения
            /// </summary>
            public PIDRegulator SetPID(double inputP, double inputD, double inputI, double deltaTimer)
            {
                DeltaTimer = deltaTimer;

                P = inputP;

                D = (inputD - prevD) / DeltaTimer;
                prevD = inputD;

                I += inputI * DeltaTimer;

                return this;
            }

            /// <summary>
            /// Установка регулирующего значения
            /// </summary>
            public PIDRegulator SetPID(double input)
            {
                P = input;

                D = (input - prevD) / DeltaTimer;
                prevD = input;

                I += input * DeltaTimer;

                return this;
            }

            /// <summary>
            /// Установка регулирующего значения
            /// </summary>
            public PIDRegulator SetPID(double input,double clampI)
            {
                P = input;

                D = (input - prevD) / DeltaTimer;
                prevD = input;

                I += input * DeltaTimer;
                I = MathHelper.Clamp(I, -clampI, clampI);

                return this;
            }

            /// <summary>
            /// Возвращяем сигнал управления
            /// </summary>
            public double GetSignal()
            {
                double outSignal = P * Kp + D * Kd + I * Ki;

                return outSignal;
            }

            public void Reset()
            {
                P = 0;
                D = 0;
                I = 0;
            }
        }

        //////////////END OF SCRIPT/////////////////////////////

    }
}

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


////// Модууль управления двигателями и гироскопом

namespace SpaceEngineers.ShipManagers.INOP
{
    public sealed class Program : MyGridProgram
    {

        /////////////////////////////////////////////////////////////

        private Vector3D testTarget = new Vector3D(7364.16, -20008.05, 12525.12);
        private Vector3D convPosz = new Vector3D();
        private Vector3D dockPosz = new Vector3D();
        private ThrusterController Thruster;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Thruster = new ThrusterController(this);
            Thruster.FindComponents();

        }

        public void Main(string args)
        {
            /// Комманды для управления двигателями, использовать только их

            switch (args)
            {
                case "INIT":
                    Thruster.FindComponents();
                    break;
                case "CC":
                    Thruster.SwitchCruiseControl();
                    break;
                case "CCSPEED":
                    Thruster.SetCrusieSpeed(1);
                    break;

                case "CCALT":
                    Thruster.SwitchAltHold();
                    break;

                case "HOR":
                    Thruster.SwitchHorizonHold();
                    break;

                case "DIR":
                    Thruster.LockOn(testTarget);
                    break;

                case "GYROS":
                    Thruster.SwhitchGyrosHold();
                    break;

                case "FLY":
                    Thruster.FlyTO(testTarget);
                    break;

                case "HOLD":
                    Thruster.HoldPos();
                    Thruster.SetVerticalSpeed(0);
                    break;

                case "UP":
                    Thruster.SetVerticalSpeed(1);
                    break;

                case "DOWN":
                    Thruster.SetVerticalSpeed(-0.5);
                    break;
                case "STOP":
                    Thruster.SetVerticalSpeed(0);
                    break;

                case "SAVE":
                    Thruster.SaveConnector();
                    convPosz = Thruster.GetConectorPos();
                    dockPosz = Thruster.GetConectorPos();
                    break;
                case "CONV":
                    Thruster.FlyTO(convPosz);
                    break;
                case "DOCK":
                    Thruster.FlyTO(dockPosz);
                    break;
                case "PON":
                    Thruster.EnableConnector();
                    break;
                case "POFF":
                    Thruster.DisableConnector();
                    break;

            }

            Thruster.Update();
        }


        public class ThrusterController
        {
            /// <summary>
            /// Включен ли круиз контроль?
            /// </summary>
            public bool CruiseControl { get; private set; }
            /// <summary>
            /// Режим авто удержания высоты полета
            /// </summary>
            public bool AltHold { get; private set; }
            /// <summary>
            /// Режим перехвата управления гироскопами
            /// </summary>
            public bool GyrosHold { get; private set; }
            /// <summary>
            /// Удержание горизонта
            /// </summary>
            public bool HorizonHold { get; private set; }
            /// <summary>
            /// Режим удержания точки, для автомайнера
            /// </summary>
            public bool VerticalAPHold { get; private set; }

            public bool DirectToTarget { get; private set; }

            /// <summary>
            /// Летит ли в заданную точку?
            /// </summary>
            public bool FlyToPoint { get; set; }

            /// <summary>
            /// Пропорцианальный коэффициент для тяги двигателей не более 5
            /// </summary>
            public double PidKP { get; set; }
            /// <summary>
            /// Дифференциальный коэф, степень торможения, чем выше тем быстрее замедляюся двигатели по мере достижения цели
            /// </summary>
            public double PidKD { get; set; }
            /// <summary>
            /// Не используется всегда 0
            /// </summary>
            public double PidKI { get; set; }

            /// <summary>
            /// Коэффициенты для авто корректировки высоты
            /// </summary>
            public int AltHoldKP { get; set; }
            /// <summary>
            /// Коэффициенты для авто корректировки высоты
            /// </summary>
            public int AltHoldKD { get; set; }
            /// <summary>
            /// Леимт вертикальной скорости, работает в режиме VerticalAPHold для автомайнера
            /// </summary>
            public double VertSpeedLimit { get; private set; }

            /// <summary>
            /// Максимальное ограничение скорости сервера дефолт=100
            /// </summary>
            public double MaxSpeedServerLimit { get; set; }

            public Vector3D DirectionToTarget { get; set; }

            ///Блоки управления, пока выбор идет из двух, в дальнейшем убрать кокпит
            public IMyRemoteControl remoteControl;//пока public
            private IMyCockpit cockpit;
            private IMyShipConnector targetConnector;

            private IMyTextPanel cruiseControlLCD;
            private IMyTextPanel verticalControlLCD;
            private IMyTextSurface cruiseControlSurf;

            //Двигатели и гироскопы
            private List<IMyThrust> thrusters;

            private List<IMyThrust> thrustersUp;
            private List<IMyThrust> thrustersDown;
            private List<IMyThrust> thrustersLeft;
            private List<IMyThrust> thrustersRight;
            private List<IMyThrust> thrustersForward;
            private List<IMyThrust> thrustersBackward;

            private List<IMyShipConnector> connectors;

            private List<IMyGyro> gyros;

            // Тага по всем двигателям и сумма тяг двигателей
            private double accUp = 0;
            private double accDown = 0;
            private double accLeft = 0;
            private double accRight = 0;
            private double accForward = 0;
            private double accBack = 0;

            private double calcAccUp = 0;
            private double calcAccDown = 0;
            private double calcAccLeft = 0;
            private double calcAccRight = 0;
            private double calcAccForward = 0;
            private double calcAccBack = 0;

            private double ThrustUp = 0;
            private double ThrustDown = 0;
            private double ThrustLeft = 0;
            private double ThrustRight = 0;
            private double ThrustForward = 0;
            private double ThrustBack = 0;

            private double totalMass = 0;
            private double maxTakeOffWheight = 0;

            //Параметры автопилота
            private double radioAlt = 0;
            private double baroAlt = 0;
            private double deltaAlt = 0;
            private double altHolding = 100;
            private double requestedForwardSpeed = 0;
            private double distSqrToPoint = 0;

            //Данные от кокпита или Радио блока о управлении вращешием и движением по осям
            private float rotationY = 0;

            private float moveZ = 0;
            private float moveY = 0;

            private Vector3D totalWheight;
            private Vector3D naturalGravity;
            private Vector3D lineraVelocity;
            private Vector3D currentPosition;
            private Vector3D targetPosition;
            private Vector3D localForward;
            private Vector3D localLeft;
            private Vector3D localUp;
            private Vector3D localDown;

            private double forwadSpeedComponent = 0;
            private double leftSpeedComponent = 0;
            private double upSpeedComponent = 0;
            private double deltaTime = 1;

            //обязательна программа, которая запускает этот скрипт
            private Program mainProgram;

            //Тип управления, пока 2 так как испольую либо кокпит(для ручного использования автопилота) и Радио блока для автомайнера
            private ControlType currentControlMode;

            //Названия дисплеев, потом переделать из Custom Data блока
            private string cruiseControlLCDName = "CCLcd";
            private string verticalControlLCDName = "MinerLCD";

            //ПД регуляторы распределения тяги по двигателям
            private PIDRegulator ForwardPid = new PIDRegulator();
            private PIDRegulator LeftPid = new PIDRegulator();
            private PIDRegulator UpPid = new PIDRegulator();

            enum ControlType
            {
                Cocpit,
                RemoteControl
            }

            public ThrusterController(Program program)
            {
                mainProgram = program;
                thrusters = new List<IMyThrust>();
                thrustersUp = new List<IMyThrust>();
                thrustersDown = new List<IMyThrust>();
                thrustersLeft = new List<IMyThrust>();
                thrustersRight = new List<IMyThrust>();
                thrustersForward = new List<IMyThrust>();
                thrustersBackward = new List<IMyThrust>();

                connectors = new List<IMyShipConnector>();

                gyros = new List<IMyGyro>();

                AltHoldKP = 1;
                AltHoldKD = 1;

                PidKP = 1;
                PidKD = 100;
                PidKI = 1;
                MaxSpeedServerLimit = 200;


            }

            /// <summary>
            /// Инищиализация всех компонентов, ОБЯЗАТЕЛЬНО выполнить при наличии пилота в кокпите или радио блоке, распределение тяги по двигателям только так работает????
            /// </summary>
            public void FindComponents()
            {
                mainProgram.Echo("-----Thruster setup init---");

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                mainProgram.GridTerminalSystem.GetBlocksOfType(blocks, b => b.CubeGrid == mainProgram.Me.CubeGrid);

                thrusters = blocks.Where(b => b is IMyThrust).Where(t => !t.CustomName.Contains("Brake")).Select(t => t as IMyThrust).ToList();
                mainProgram.Echo($"Total thrusters: {thrusters.Count}");

                gyros = blocks.Where(b => b is IMyGyro).Select(t => t as IMyGyro).ToList();
                mainProgram.Echo($"Total Gyro: {gyros.Count}");

                cockpit = blocks.Where(b => b is IMyCockpit).FirstOrDefault() as IMyCockpit;
                remoteControl = blocks.Where(b => b is IMyRemoteControl).FirstOrDefault() as IMyRemoteControl;

                connectors = blocks.Where(b => b is IMyShipConnector).Select(t => t as IMyShipConnector).ToList();

                if (!thrusters.Any() || !gyros.Any())
                {
                    mainProgram.Echo("No thrusters or gyro, init FAIL");
                    return;
                }

                if (cockpit == null && remoteControl == null)
                {
                    mainProgram.Echo("No cocpit or remote ctr, init FAIL");
                    return;
                }

                if (cockpit != null)
                {
                    currentControlMode = ControlType.Cocpit;
                    cruiseControlLCD = blocks.Where(b => b is IMyTextPanel).Where(t => t.CustomName == cruiseControlLCDName).FirstOrDefault() as IMyTextPanel;
                    verticalControlLCD = blocks.Where(b => b is IMyTextPanel).Where(t => t.CustomName == verticalControlLCDName).FirstOrDefault() as IMyTextPanel;

                    var text = cockpit as IMyTextSurfaceProvider;
                    if (text != null)
                    {
                        cruiseControlSurf = text.GetSurface(0);
                    }

                }
                else
                {
                    currentControlMode = ControlType.RemoteControl;
                    cruiseControlLCD = blocks.Where(b => b is IMyTextPanel).Where(t => t.CustomName == cruiseControlLCDName).FirstOrDefault() as IMyTextPanel;
                }

                thrustersUp = thrusters.Where(b => b.GridThrustDirection.Y == -1).ToList();
                accUp = thrustersUp.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr up: {thrustersUp.Count}");

                thrustersDown = thrusters.Where(b => b.GridThrustDirection.Y == 1).ToList();
                accDown = thrustersDown.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr down: {thrustersDown.Count}");

                thrustersLeft = thrusters.Where(b => b.GridThrustDirection.X == -1).ToList();
                accLeft = thrustersLeft.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr left: {thrustersLeft.Count}");

                thrustersRight = thrusters.Where(b => b.GridThrustDirection.X == 1).ToList();
                accRight = thrustersRight.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr right: {thrustersRight.Count}");

                thrustersForward = thrusters.Where(b => b.GridThrustDirection.Z == 1).ToList();
                accForward = thrustersForward.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr fwd: {thrustersForward.Count}");

                thrustersBackward = thrusters.Where(b => b.GridThrustDirection.Z == -1).ToList();
                accBack = thrustersBackward.Sum(t => t.MaxEffectiveThrust);
                mainProgram.Echo($"Thr back: {thrustersBackward.Count}");

                totalMass = cockpit.CalculateShipMass().TotalMass;
                calcAccUp = accUp / totalMass;
                calcAccDown = accDown / totalMass;
                calcAccLeft = accLeft / totalMass;
                calcAccRight = accRight / totalMass;
                calcAccForward = accForward / totalMass;
                calcAccBack = accBack / totalMass;

                mainProgram.Echo("------Init completed----------");

                ClearOverrideGyros();
                ClerarOverideEngines();
            }

            /// <summary>
            /// Функция обновления, вызывается каждый кадр
            /// </summary>
            public void Update()
            {

                GetLocalParams();


                if (GyrosHold)
                    SetGyro(HoldGyros());

                PrintData();

                if (CruiseControl)
                    CruiseControlSystem();

                if (AltHold)
                    AltHoldMode();

                if (FlyToPoint)
                    Fly();

                if (VerticalAPHold)
                    VerticalHold();
            }

            /// <summary>
            /// Включение и выключение круиз контроля
            /// </summary>
            public void SwitchCruiseControl()
            {
                CruiseControl = !CruiseControl;
                ClerarOverideEngines();
            }

            /// <summary>
            /// Вклбчение и выключение удержания высоты
            /// </summary>
            public void SwitchAltHold()
            {
                AltHold = !AltHold;
                ClerarOverideEngines();
            }

            /// <summary>
            /// Режим перехвата управления гироскопами
            /// </summary>
            public void SwhitchGyrosHold()
            {
                GyrosHold = !GyrosHold;
                if (GyrosHold)
                {
                    OverrideGyros();
                }
                else
                {
                    ClearOverrideGyros();
                }

            }

            public void SwitchHorizonHold()
            {
                HorizonHold = !HorizonHold;
                if (!GyrosHold)
                    SwhitchGyrosHold();

            }


            public void SaveConnector()
            {
                var actConn = connectors.Where(c => c.Status == MyShipConnectorStatus.Connected).FirstOrDefault();
                if (actConn != null)
                {
                    targetConnector = actConn.OtherConnector;
                }

            }

            public IMyShipConnector GetSavedConnector()
            {
                if (targetConnector != null)
                    return targetConnector;
                return null;
            }

            public Vector3D GetConectorPos()
            {
                if (targetConnector != null)
                    return cockpit.GetPosition();
                return Vector3D.Zero;
            }

            public void EnableConnector()
            {
                if (targetConnector != null)
                    targetConnector.Enabled = true;
            }

            public void DisableConnector()
            {
                if (targetConnector != null)
                    targetConnector.Enabled = false;
            }

            /// <summary>
            /// Сброс переопределения тяги двигателей
            /// </summary>
            public void ClerarOverideEngines()
            {
                foreach (var tr in thrustersUp)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersDown)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersLeft)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersRight)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersForward)
                {
                    tr.ThrustOverridePercentage = -1;
                }

                foreach (var tr in thrustersBackward)
                {
                    tr.ThrustOverridePercentage = -1;
                }
            }

            /// <summary>
            /// Сброс переопределения гироскопов
            /// </summary>
            public void ClearOverrideGyros()
            {
                DirectToTarget = false;
                HorizonHold = false;

                foreach (var gyro in gyros)
                {
                    gyro.SetValueBool("Override", false);
                }
            }

            /// <summary>
            /// Отправляет в полет на указанную точку без ориентации на нее
            /// </summary>
            public void FlyTO(Vector3D point)
            {
                targetPosition = point;
                FlyToPoint = true;
            }

            /// <summary>
            /// Ориентация корабля на точку
            /// </summary>
            public void LockOn(Vector3D point)
            {
                DirectToTarget = true;
                targetPosition = point;
                if (!GyrosHold)
                    SwhitchGyrosHold();

            }

            /// <summary>
            /// Удержание точки для автомайнера
            /// </summary>
            public void HoldPos()
            {
                targetPosition = currentPosition;
                VerticalAPHold = !VerticalAPHold;
                ClerarOverideEngines();
            }


            /// <summary>
            /// Регулировка вертикальной скорости для автомайнера '+' - вверх '-' - вниз
            /// </summary>
            public void SetVerticalSpeed(double Vspeed)
            {
                VertSpeedLimit = Vspeed;
            }

            /// <summary>
            /// Получить текущую позицию
            /// </summary>
            /// <returns></returns>
            public Vector3D GetPosition()
            {
                switch (currentControlMode)
                {
                    case ControlType.Cocpit:
                        return new Vector3D(cockpit.GetPosition());


                    case ControlType.RemoteControl:
                        return new Vector3D(remoteControl.GetPosition());

                }

                return new Vector3D(0, 0, 0);
            }

            /// <summary>
            /// Установить скорость для круиз контроля
            /// </summary>
            public void SetCrusieSpeed(double speed)
            {
                requestedForwardSpeed = speed;
                requestedForwardSpeed = Math.Min(Math.Max(-MaxSpeedServerLimit, requestedForwardSpeed), MaxSpeedServerLimit);
            }

            // Приватные методы ниже

            /// <summary>
            /// Вывод информации на дисплеи, не обязательная функция
            /// </summary>
            private void PrintData()
            {
                cruiseControlLCD?.WriteText(
                    $"Mass: {totalMass} kg" +
                    $"\nMTOW: {maxTakeOffWheight}" +
                    $"\nPayload:{Math.Round(totalMass / maxTakeOffWheight * 100, 1)} %" +
                    $"ACCs:\nU:{calcAccUp}\nD:{calcAccDown}\nL:{calcAccLeft}\nR:{calcAccRight}\nF:{calcAccForward}\nB:{calcAccBack} ", false);

                cruiseControlSurf?.WriteText(
                    $"CC: {CruiseControl}" +
                    $"\nAltHold: {AltHold}" +
                    $"\nHorHold: {GyrosHold}" +
                    $"\nFlytoPont {FlyToPoint}" +
                    $"\nHoldPos {VerticalAPHold}" +
                    $"\nHGorHold: {HorizonHold}" +
                    $"\nDirTo: {DirectToTarget}" +
                    $"\n{DirectionToTarget.Dot(localForward)}", false);

                verticalControlLCD?.WriteText(
                    $"VertSpeed: {VertSpeedLimit}" +
                    $"\nDist {Math.Sqrt(distSqrToPoint)} ", false);
            }


            /// <summary>
            /// Полет на заданную точку
            /// </summary>
            private void Fly()
            {
                double AltCorrThrust = 0;

                ThrustForward = 0;
                ThrustLeft = 0;
                ThrustUp = 0;

                PidKP = 1;
                PidKD = 80;
                PidKI = 1;


                var targetPos = targetPosition - currentPosition;
                distSqrToPoint = Vector3D.DistanceSquared(targetPosition, currentPosition);

                // Отколения от осей цели и локальных направлений для расчета ускорений
                var fwdDir = targetPos.Dot(localForward);
                var dirLeft = targetPos.Dot(localLeft);
                var dirUp = targetPos.Dot(localUp);

                var accFwd = 2 * fwdDir / deltaTime;//Ускорение по осям
                ForwardPid.ClampI = fwdDir;
                ThrustForward = -ForwardPid.SetK(PidKP, PidKD, PidKI).SetPID(accFwd, accFwd, accFwd, deltaTime).GetSignal() * totalMass;//Тяга в Н

                var accLeft = 2 * dirLeft / deltaTime;
                LeftPid.ClampI = dirLeft;
                ThrustLeft = -LeftPid.SetK(PidKP, PidKD, PidKI).SetPID(accLeft, accLeft, accLeft, deltaTime).GetSignal() * totalMass;

                var accUp = 2 * dirUp / deltaTime;
                UpPid.ClampI = dirUp;
                AltCorrThrust = UpPid.SetK(PidKP, PidKD, PidKI).SetPID(accUp, accUp, accUp, deltaTime).GetSignal() * totalMass;

                ThrustUp = -totalWheight.Dot(localUp) + AltCorrThrust;

                //Нужно для включения моторов даже если игрок не отключил гасители
                if (accUp < 0)
                {
                    ThrustDown = ThrustUp;
                }

                if (distSqrToPoint < 1)
                {
                    FlyToPoint = false;
                    ClerarOverideEngines();
                    return;
                }

                OverrideThrsters();
            }

            /// <summary>
            /// Поварачивает корабль на цель
            /// </summary>
            private Vector3D DirectTo()
            {
                var dir = Vector3D.Normalize(targetPosition - currentPosition);
                DirectionToTarget = Vector3D.Normalize(dir).Cross(localForward);
                return DirectionToTarget;
            }

            /// <summary>
            /// Вертикальный режим для автомайнера
            /// </summary>
            private void VerticalHold()
            {

                ThrustForward = 0;
                ThrustLeft = 0;
                ThrustUp = 0;
                double KP = 0.5;
                double KD = 150;
                double KI = 0;

                var targetPos = targetPosition - currentPosition;
                distSqrToPoint = Vector3D.DistanceSquared(targetPosition, currentPosition);

                var fwdDir = targetPos.Dot(localForward);
                var dirLeft = targetPos.Dot(localLeft);

                var accFwd = 2 * fwdDir / deltaTime;
                ThrustForward = -ForwardPid.SetK(KP, KD, KI).SetPID(accFwd, accFwd, accFwd, deltaTime).GetSignal() * totalMass;

                var accLeft = 2 * dirLeft / deltaTime;
                ThrustLeft = -LeftPid.SetK(KP, KD, KI).SetPID(accLeft, accLeft, accLeft, deltaTime).GetSignal() * totalMass;


                double deltaVSpeed = 0;
                var vertSpeed = VertSpeedLimit;
                double AltCorrThrust = 0;
                PIDRegulator pid = new PIDRegulator();
                deltaVSpeed = vertSpeed - upSpeedComponent;

                var upAcc = 2 * deltaVSpeed / deltaTime;
                AltCorrThrust = pid.SetK(AltHoldKP, AltHoldKD, 0).SetPID(upAcc, upAcc, upAcc, deltaTime).GetSignal() * totalMass;

                ThrustUp = -totalWheight.Dot(localUp) + AltCorrThrust;

                if (vertSpeed < 0)
                {
                    ThrustDown = ThrustUp;
                }

                OverrideThrsters();
            }


            /// <summary>
            /// Режим удержания высоты для круиз контроля
            /// </summary>
            private void AltHoldMode()
            {
                double AltCorrThrust = 0;
                PIDRegulator pid = new PIDRegulator();
                deltaAlt = radioAlt - altHolding;
                altHolding += moveY;

                var upAcc = 2 * deltaAlt / deltaTime;
                AltCorrThrust -= pid.SetK(AltHoldKP, AltHoldKD, 0).SetPID(upAcc, upAcc, upAcc, deltaTime).GetSignal() * totalMass;

                ThrustUp = -totalWheight.Dot(localUp - lineraVelocity) + AltCorrThrust;
                ThrustUp *= Math.Max(1, cockpit.MoveIndicator.Y * 10);

                if (deltaAlt > 1)
                {
                    ThrustDown = ThrustUp;
                }

                OverrideThrsters();
            }

            /// <summary>
            /// Круиз контроль
            /// </summary>
            private void CruiseControlSystem()
            {
                requestedForwardSpeed += moveZ * -1;
                requestedForwardSpeed = Math.Min(Math.Max(-MaxSpeedServerLimit, requestedForwardSpeed), MaxSpeedServerLimit);

                var reqAcc = (forwadSpeedComponent - requestedForwardSpeed) / deltaTime;
                ThrustForward = reqAcc * totalMass;

                OverrideThrsters();

            }

            private void OverrideThrsters()
            {
                foreach (var tr in thrustersUp)
                {
                    tr.ThrustOverridePercentage = (float)ThrustUp / (float)accUp;
                }

                foreach (var tr in thrustersDown)
                {
                    tr.ThrustOverridePercentage = -(float)ThrustDown / (float)accDown;
                }


                foreach (var tr in thrustersLeft)
                {
                    tr.ThrustOverridePercentage = (float)ThrustLeft / (float)accLeft;
                }

                foreach (var tr in thrustersRight)
                {
                    tr.ThrustOverridePercentage = -(float)ThrustLeft / (float)accRight;
                }

                foreach (var tr in thrustersForward)
                {
                    tr.ThrustOverridePercentage = -(float)ThrustForward / (float)accForward;
                }

                foreach (var tr in thrustersBackward)
                {
                    tr.ThrustOverridePercentage = (float)ThrustForward / (float)accBack;
                }
            }

            private void GetLocalParams()
            {
                switch (currentControlMode)
                {
                    case ControlType.Cocpit:
                        totalMass = cockpit.CalculateShipMass().PhysicalMass;
                        naturalGravity = cockpit.GetNaturalGravity();
                        totalWheight = totalMass * naturalGravity;
                        cockpit.TryGetPlanetElevation(MyPlanetElevation.Surface, out radioAlt);
                        cockpit.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out baroAlt);

                        currentPosition = cockpit.GetPosition();

                        rotationY = cockpit.RotationIndicator.Y;

                        moveZ = cockpit.MoveIndicator.Z;
                        moveY = cockpit.MoveIndicator.Y;

                        localForward = cockpit.WorldMatrix.Forward;
                        localLeft = cockpit.WorldMatrix.Left;
                        localUp = cockpit.WorldMatrix.Up;
                        localDown = cockpit.WorldMatrix.Down;

                        lineraVelocity = cockpit.GetShipVelocities().LinearVelocity;

                        forwadSpeedComponent = lineraVelocity.Dot(localForward);
                        leftSpeedComponent = lineraVelocity.Dot(localLeft);
                        upSpeedComponent = lineraVelocity.Dot(localUp);
                        break;

                    case ControlType.RemoteControl:
                        totalMass = remoteControl.CalculateShipMass().PhysicalMass;
                        naturalGravity = remoteControl.GetNaturalGravity();
                        totalWheight = totalMass * naturalGravity;
                        remoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out radioAlt);
                        remoteControl.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out baroAlt);
                        currentPosition = remoteControl.GetPosition();

                        rotationY = remoteControl.RotationIndicator.Y;

                        moveZ = remoteControl.MoveIndicator.Z;
                        moveY = remoteControl.MoveIndicator.Y;

                        localForward = remoteControl.WorldMatrix.Forward;
                        localLeft = remoteControl.WorldMatrix.Left;
                        localUp = remoteControl.WorldMatrix.Up;
                        localDown = remoteControl.WorldMatrix.Down;

                        lineraVelocity = remoteControl.GetShipVelocities().LinearVelocity;

                        forwadSpeedComponent = lineraVelocity.Dot(localForward);
                        leftSpeedComponent = lineraVelocity.Dot(localLeft);
                        upSpeedComponent = lineraVelocity.Dot(localUp);
                        break;
                }

                if (!naturalGravity.IsZero())
                {
                    maxTakeOffWheight = accUp / naturalGravity.Length();
                }
                else
                {
                    maxTakeOffWheight = accUp;
                }

            }

            /// <summary>
            /// Перехват управления гироскопами
            /// </summary>
            private Vector3D HoldGyros()
            {

                Vector3D axis = new Vector3D(0, 0, 0);

                if (DirectToTarget)
                    axis += DirectTo();

                if (HorizonHold)
                {
                    var natGravNorm = Vector3D.Normalize(naturalGravity);

                    if (!DirectToTarget)
                    {
                        axis = natGravNorm.Cross(cockpit.WorldMatrix.Down);
                        if (natGravNorm.Dot(cockpit.WorldMatrix.Down) < 0)
                        {
                            axis = Vector3D.Normalize(axis);
                        }
                    }

                    double targetRoll = Vector3D.Dot(localLeft, Vector3D.Reject(Vector3D.Normalize(-natGravNorm), localForward));
                    targetRoll = Math.Acos(targetRoll) - Math.PI / 2;
                    axis += -1 * localForward * targetRoll;
                }

                Vector3D signal = localUp * rotationY;
                axis += signal;

                return axis;
            }

            /// <summary>
            /// Управление гироскопами в авто режиме
            /// </summary>
            private void OverrideGyros()
            {
                foreach (var gyro in gyros)
                {
                    gyro.SetValueBool("Override", true);
                }
            }

            /// <summary>
            /// Установка гироскопов
            /// </summary>
            private void SetGyro(Vector3D axis)
            {
                foreach (IMyGyro gyro in gyros)
                {
                    gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up);
                    gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right);
                    gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward);
                }
            }

        }

        public class PIDRegulator
        {

            public double P = 0;
            public double D = 0;
            public double I = 0;

            public double Kp { get; set; } = 1;
            public double Kd { get; set; } = 1;
            public double Ki { get; set; } = 1;

            public double ClampI { get; set; } = 0;

            private double prevD = 0;
            public double DeltaTimer { get; set; } = 1;

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
                I = MathHelper.Clamp(I, -ClampI, ClampI);

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
        }

        ///////////////////////////////////////////////////////////
    }
}
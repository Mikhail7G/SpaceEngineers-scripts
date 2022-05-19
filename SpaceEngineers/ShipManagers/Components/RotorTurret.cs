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


namespace SpaceEngineers.ShipManagers.Components.RotorTurret

{
    public sealed class Program : MyGridProgram
    {
        public RotorBase Base;
        public Vector3D TestPoint = new Vector3D(-38797.81, -38669.89, -27402.11);

        IMyLargeTurretBase designator;

        string antennaName = "AntGround";
        string missileTagResiever = "ch1R";//Получаем данные от системы целеуказания по радиоканалу
        IMyBroadcastListener listener;//слушаем эфир на получение данных о целях по радио
        IMyRadioAntenna antenna;

        private Vector3D targetPosition;
        private Vector3D targetSpeed;

       // private Func<IMyTerminalBlock, int, MyDetectedEntityInfo> _getWeaponTarget;



        public Program()
        {
            Base = new RotorBase(this);
            Base.SetUpRotors();
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
            designator = GridTerminalSystem.GetBlockWithName("des") as IMyLargeTurretBase;

            listener = IGC.RegisterBroadcastListener(missileTagResiever);
            listener.SetMessageCallback(missileTagResiever);

 

        }

        //public MyDetectedEntityInfo? GetWeaponTarget(IMyTerminalBlock weapon, int weaponId = 0) =>
        //       _getWeaponTarget?.Invoke(weapon, weaponId) ?? null;

        public void Main(string args, UpdateType updateSource)
        {
            switch(args)
            {
                case "init":
                    Base.SetUpRotors();
                    break;
            }

            Base.Update();
            // GetTargetByRadio();


            if (designator == null)
            {
                return;
            }
            if (designator.HasTarget)
            {
                var taget = designator.GetTargetedEntity();
                Base.SetTarget(taget);
            }
            else
            {
                Base.HasTarget = false;
            }

        }

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

                   // Base.SetTarget(targetPosition, targetSpeed);
                }
            }
        }

        public class RotorBase
        {
            public string controlGroupName = "RadarGroup";
            public string controlStationName = "Capitan";
            public string statorHorizontalName = "RadarStatorH";
            public string statorVerticalName = "RadarStatorV";

            public bool HasTarget { get; set; }
            public bool UnderControl { get; private set; }
            public int ElevationModifier { get; set; }
            public int RotateModifier { get; set; }
            public int BulletSpeed { get; set; }

            private IMyBlockGroup group;
            private IMyShipController control;
            private IMyCameraBlock camera;
            private IMyMotorAdvancedStator statorHorizontal;
            private IMyMotorAdvancedStator statorVertical;

            private List<IMyUserControllableGun> turrets;

            private Vector3D targetPosition;
            private Vector3D targetSpeed;
            private Vector3D gravity;

            private Program mainPrgoram;

            private double verticalDegrees;
            private double horizontalDegrees;

            private float azimuthDelta;
            private float elevationDelta;

            private MyDetectedEntityInfo target;

            private int shootTime = 0;
            private int cannonId = 0;
            private int spiralTime;

            private double impactTime = 0;
            private double deltaGravity = 0;

            public RotorBase(Program _mainPrgoram)
            {
                mainPrgoram = _mainPrgoram;
                RotateModifier = 5;
                ElevationModifier = -3;
                BulletSpeed = 400;
            }

            public void SetUpRotors()
            {
                mainPrgoram.Echo("-----Rotors setup start----");
                group = mainPrgoram.GridTerminalSystem.GetBlockGroupWithName(controlGroupName);
                if (group == null)
                {
                    mainPrgoram.Echo($"No group detected:{controlGroupName}");
                    return;
                }
                mainPrgoram.Echo(group?.Name);

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                group?.GetBlocks(blocks);

                control = mainPrgoram.GridTerminalSystem.GetBlockWithName(controlStationName) as IMyShipController;

                if (blocks.Count > 0)
                {
                    statorHorizontal = (IMyMotorAdvancedStator)blocks.Where(b => b is IMyMotorAdvancedStator).First(m => m.CustomName == statorHorizontalName);
                    statorVertical = (IMyMotorAdvancedStator)blocks.Where(b => b is IMyMotorAdvancedStator).First(m => m.CustomName == statorVerticalName);
                    camera = (IMyCameraBlock)blocks.Where(b => b is IMyCameraBlock).FirstOrDefault();

                    turrets = blocks.Where(b => b is IMyUserControllableGun).Where(r => r.IsFunctional).Select(t => t as IMyUserControllableGun).ToList();
                }

                if (statorHorizontal == null)
                {
                    mainPrgoram.Echo($"No Horizontal: {statorHorizontalName}");
                    return;
                }

                if (statorVertical == null)
                {
                    mainPrgoram.Echo($"No Vertical: {statorVerticalName}");
                    return;
                }

                mainPrgoram.Echo("-----Rotors setup completed----");
            }

            public void Update()
            {
                mainPrgoram.Echo("-----Rotors system vorking----");
                mainPrgoram.Echo($"Horizontal: {statorHorizontal.CustomName}");
                mainPrgoram.Echo($"Vertical: {statorVertical.CustomName}");

                verticalDegrees = statorVertical.Angle;
                horizontalDegrees = statorHorizontal.Angle;

                mainPrgoram.Echo($"Horizontal: {horizontalDegrees * 180 / 3.14}");
                mainPrgoram.Echo($"Vertical: {verticalDegrees * 180 / 3.14}");

                mainPrgoram.Echo($"Under Control: {UnderControl}");
                mainPrgoram.Echo($"Camera: {camera?.CustomName}");
                mainPrgoram.Echo($"Control: {control?.CustomName}");
                mainPrgoram.Echo($"Has target: {HasTarget}");
                mainPrgoram.Echo($"Time: {impactTime}");


                ManualControl();

                if (!UnderControl)
                {
                    if (HasTarget)
                    {
                        if(target.IsEmpty())
                        {
                            HasTarget = false;

                            foreach(var tur in turrets)
                            {
                                tur.Shoot = false;
                            }
                        }
                        targetPosition = target.HitPosition.Value;
                        targetSpeed = target.Velocity;


                            //   mainPrgoram.Echo($"BB: {}");



                            AutoRotation();

                        var dev = Math.Abs(azimuthDelta) + Math.Abs(elevationDelta);
                        if (dev<2)
                        {
                            //shootTime++;
                            //if (shootTime > 60) 
                            //{
                            //    shootTime = 0;
                            //    cannonId++;
                            //    turrets[cannonId].ApplyAction("ShootOnce");

                            //    if (cannonId == turrets.Count-1)
                            //        cannonId = 0;
                            //}
                            foreach (var tur in turrets)
                            {
                                tur.Shoot = true;
                            }
                        }
                    }
                    else
                    {
                        HasTarget = false;
                        foreach (var tur in turrets)
                        {
                            tur.Shoot = false;
                        }
                    }
                }

            }

            public void ManualControl()
            {
                if ((!control.Closed) || (!statorHorizontal.Closed) || (!statorVertical.Closed))
                {
                   if ((control.IsUnderControl) && (camera.IsActive))
                    {
                        //UnderControl = true;
                        //var rotIndicator = control.RotationIndicator;
                        //statorHorizontal.TargetVelocityRPM = rotIndicator.Y * RotateModifier;
                        //statorVertical.TargetVelocityRPM = -rotIndicator.X * RotateModifier;
                    }
                    else
                    {
                        UnderControl = false;
                        statorHorizontal.TargetVelocityRPM = 0;
                        statorVertical.TargetVelocityRPM = 0;
                    }
                }
            }

            public void SetTarget(Vector3D pos, Vector3D speed)
            {
                targetPosition = pos;
                targetSpeed = speed;            
            }

            public void SetTarget(MyDetectedEntityInfo info)
            {
                target = info;
                targetPosition = target.Position;
                targetSpeed = target.Velocity;
                HasTarget = true;
            }

            private void AutoRotation()
            {
                gravity = control.GetNaturalGravity();
                var myPos = statorHorizontal.GetPosition() - statorHorizontal.WorldMatrix.Up * ElevationModifier;

                var calcTargetPos = CalcInterceptPos(myPos, 300, targetPosition, targetSpeed) + Spiral(myPos) + GravityCompensation(myPos, targetPosition);

                calcTargetPos = VectorTransform(calcTargetPos, statorHorizontal.WorldMatrix.GetOrientation());
                float azimuth = (float)Math.Atan2(-calcTargetPos.X, calcTargetPos.Z);
                float elevation = (float)Math.Asin(calcTargetPos.Y / calcTargetPos.Length());

                azimuthDelta = Rotate(azimuth, statorHorizontal.Angle);
                elevationDelta = Rotate(elevation, statorVertical.Angle);

                statorHorizontal.TargetVelocityRad = azimuthDelta * RotateModifier;
                statorVertical.TargetVelocityRad = (elevationDelta - (float)Math.PI / 2) * RotateModifier;
            }

            private Vector3D CalcInterceptPos(Vector3D basePos, double buletSpeed, Vector3D targetPos, Vector3D targetSpeed)
            {
                Vector3D directionToTarget = Vector3D.Normalize(targetPos - basePos);

                Vector3D targetVelTang = Vector3D.Reject(targetSpeed, directionToTarget);
                Vector3D shootVelTang = targetVelTang;
                double shootVelSpeed = shootVelTang.Length();

                if (shootVelSpeed > buletSpeed)
                {
                    return Vector3D.Normalize(targetSpeed) * buletSpeed;
                }
                else
                {
                    double shootSpeedOrto = Math.Sqrt(buletSpeed * buletSpeed - shootVelSpeed * shootVelSpeed);
                    Vector3D shootVelOrto = directionToTarget * shootSpeedOrto;
                    return shootVelOrto + shootVelTang;
                }
            }

            private Vector3D GravityCompensation(Vector3D basePos, Vector3D targetPos)
            {
                var dir = (targetPos - basePos);

                mainPrgoram.Echo($"dir: {dir.Length()}");
                impactTime = dir.Length() / BulletSpeed;

                deltaGravity = (0.5 * control.GetNaturalGravity().Length() * impactTime * impactTime) / 2;
                mainPrgoram.Echo($"DELTA: {deltaGravity}");

                return (-1) * Vector3D.Normalize(gravity) * deltaGravity;

            }

            public Vector3D Spiral(Vector3D dist)
            {
                double radius = 1;
                double maxSpiralTime = 50;

                spiralTime++;
                if (spiralTime > maxSpiralTime)
                    spiralTime = 0;

                var fwd = statorHorizontal.WorldMatrix.Forward * radius;
                var up = statorHorizontal.WorldMatrix.Up * radius;
                var left = (statorHorizontal.WorldMatrix.Left) * radius;

                double angle = 2 * Math.PI * spiralTime / maxSpiralTime;

                Vector3D spiral = fwd + (left * Math.Cos(angle) + up * Math.Sin(angle));

                return spiral;
            }

            private float Rotate(float targetAngle, float currentAngle)
            {
                float angle = targetAngle - currentAngle;
                if (angle < -Math.PI) angle += 2 * (float)Math.PI;
                else if (angle > Math.PI) angle -= 2 * (float)Math.PI;
                return angle;
            }

            public Vector3D VectorTransform(Vector3D vec, MatrixD orientation)
            {
                return new Vector3D(vec.Dot(orientation.Right), vec.Dot(orientation.Up), vec.Dot(orientation.Backward));
            }

        }

        //////////////////END OF SCRIPT////////////////////////////////////////

    }

}


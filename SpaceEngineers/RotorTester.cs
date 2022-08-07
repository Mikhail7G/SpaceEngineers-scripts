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

namespace SpaceEngineers.TurretTester
{
    public sealed class Program : MyGridProgram
    {
        string missileTagResiever = "ch1R";//Получаем данные от системы целеуказания по радиоканалу
        IMyBroadcastListener listener;//слушаем эфир на получение данных о целях по радио

        Vector3D targetPosition;
        Vector3D targetSpeed;

        IMyRadioAntenna antenna;

        List<IMyLargeTurretBase> turrets;


        public Program()
        {
            targetPosition = new Vector3D();
            targetSpeed = new Vector3D();

            turrets = new List<IMyLargeTurretBase>();

            listener = IGC.RegisterBroadcastListener(missileTagResiever);
            listener.SetMessageCallback(missileTagResiever);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;


            antenna = GridTerminalSystem.GetBlockWithName("Ant") as IMyRadioAntenna;


            var group = GridTerminalSystem.GetBlockGroupWithName("Turrets");
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            group?.GetBlocks(blocks);

            turrets = blocks.Where(b => b is IMyLargeTurretBase).Select(b => b as IMyLargeTurretBase).ToList();

        }

        public void Main(string args)
        {
            GetTargetByRadio();



            foreach (var turret in turrets)
            {

                // turret.SetTarget(targetPosition);
                turret.TrackTarget(targetPosition, targetSpeed);

                turret.SyncElevation();
                turret.SyncAzimuth();

                if (turret.IsAimed)
                {
                    turret.Shoot = true;
                }
                else
                {
                    turret.Shoot = false;
                }


            }
        }

        public void Save()
        {

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
                }
            }
        }

    }
}
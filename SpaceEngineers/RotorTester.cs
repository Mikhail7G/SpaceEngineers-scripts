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

        /// <summary>
        /// Управление поворотной системой мышкой из кокпита, для работы надо кокпит, два ротора и монитор по желанию
        /// </summary>


        IMyShipController control;
        IMyTextPanel panel;
        IMyMotorAdvancedStator statorHorizontal;
        IMyMotorAdvancedStator statorVertical;
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

        IMyBlockGroup group;

        //название группы компонентов и необходимых модулей в них
        string controlGroup = "2XRotorGrpup";
        string controlName = "Oper";
        string panelName = "LCD";
        string statorHorizontalName = "StatorH";
        string statorVerticalName = "StatorV";

        float rotateModifier = 1;//модификатор скорости вращения

        public Program()
        {
            PrepareModules();
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

        }

        public void Main(string args)
        {
            string[] argRes = args.Split(':');
            int lenght = argRes.Length;
            if (argRes[0].Length > 1)
            {
                string state = argRes[0].ToUpper();
                switch (state)
                {
                    case "ROTATEMODIFIER":
                        if (lenght > 1)
                        {
                            float.TryParse(argRes[1], out rotateModifier);
                        }
                        break;
                }
            }


            UpdateRotation();
        }

        public void PrepareModules()
        {
            Echo("-----RotorsScriptData----");

            panel = GridTerminalSystem.GetBlockWithName(panelName) as IMyTextPanel;
            if (panel != null)
                Echo("LCD finded:" + panel.CustomName);


            group = GridTerminalSystem.GetBlockGroupWithName(controlGroup);
            Echo(group.Name);
            group.GetBlocks(blocks);
            if (blocks.Count > 0)
            {

                foreach (var block in blocks)
                {
                    if (block is IMyShipController)
                    {
                        control = block as IMyShipController;
                        Echo("Cocpit: " + block.CustomName);
                    }

                    if (block is IMyMotorAdvancedStator)
                    {
                        if (block.CustomName == statorHorizontalName)
                        {
                            statorHorizontal = block as IMyMotorAdvancedStator;
                            Echo("Horizontal: " + block.CustomName);
                        }

                        if (block.CustomName == statorVerticalName)
                        {
                            statorVertical = block as IMyMotorAdvancedStator;
                            Echo("Vertical: " + block.CustomName);
                        }
                    }
                }
            }
        }

        public void UpdateRotation()
        {
            if (control.IsUnderControl)
            {
                var rot = control.RotationIndicator;
                statorHorizontal.TargetVelocityRPM = rot.Y * rotateModifier;
                statorVertical.TargetVelocityRPM = -rot.X * rotateModifier;
            }
            else
            {
                statorHorizontal.TargetVelocityRPM = 0;
                statorVertical.TargetVelocityRPM = 0;
            }

        }

        public void Save()
        {

        }

    }
}
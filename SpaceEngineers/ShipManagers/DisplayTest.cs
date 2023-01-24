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
using static SpaceEngineers.ShipManagers.INOP.Nanodrill.Program;

namespace SpaceEngineers.ShipManagers.DisplayTester
{
    public sealed class Program : MyGridProgram
    {

        /////////////////////////////////////////////////////////////

        string guidanceBlockNameLarge = "LB_Torpedo_Payload_GuidanceBlock_Large";

        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> guidances = new List<IMyTerminalBlock>();

        public Program()
        {
           GetGuidances();

        }

        public void Main(string args, UpdateType updateType)
        {

            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                Commands(args);

        }

        void Commands(string args)
        {
            string com = args.ToUpper();

            switch (com)
            {
                case "LOCK":
                    LockTarget();
                    break;

                case "FIRE":
                    FireOnce();
                    break;
            }
        }

        void LockTarget()
        {
            foreach(var guidance in guidances)
            {
                if (guidance.Closed)
                    continue;

                guidance.ApplyAction("Adn.ActionLockOnTarget");
            }
        }

        void FireOnce()
        {
            GetGuidances();

            if (guidances.Any())
            {
                guidances[0].ApplyAction("Adn.ActionLaunchMissile");
            }
        }

        void GetGuidances()
        {
            blocks.Clear();
            guidances.Clear();

            GridTerminalSystem.GetBlocks(blocks);

            if (Me.CubeGrid.GridSizeEnum == MyCubeSize.Large)
            {
                guidances = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == guidanceBlockNameLarge).ToList();
            }

            Echo($"Guidances:{guidances.Count}");
        }







        ///////////////////////////////////////////////////////////
    }
}

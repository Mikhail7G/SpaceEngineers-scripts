using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Definitions;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;


namespace SpaceEngineers.ShipManagers.Components.Nanodrill

{
    public sealed class Program : MyGridProgram
    {

        NanodrillSystem drills;


        public Program()
        {
            drills = new NanodrillSystem(this);
           // Runtime.UpdateFrequency = UpdateFrequency.Update1;

        }



        public void Main(string args, UpdateType updateSource)
        {
            drills.Init();
            drills.Update();
        }

        public class NanodrillSystem
        {

            public string LargeNanoName = "SELtdLargeNanobotDrillSystem";
            public string SmallNanoName = "SELtdSmallNanobotDrillSystem";

            public string NanoInfoDisplayName = "LCD Nano";

            public bool HideIce = true;

            public List<NanoDrill> Drills;
            private Program mainProg;
            private List<IMyTerminalBlock> nanoDrill;

            private IMyTextPanel nanoStatus;
            private List<List<object>> miningFields;

         

            public NanodrillSystem(Program main)
            {
                mainProg = main;
                miningFields = new List<List<object>>();
                Drills = new List<NanoDrill>();
            }

            public void Init()
            {
                Drills.Clear();
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                mainProg.GridTerminalSystem.GetBlocks(blocks);

                if(mainProg.Me.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == LargeNanoName).ToList();
                }
                else if(mainProg.Me.CubeGrid.GridSizeEnum == MyCubeSize.Small)
                {
                    nanoDrill = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == SmallNanoName).ToList();
                }

                nanoStatus = blocks.Where(b => b.CustomName == NanoInfoDisplayName).FirstOrDefault() as IMyTextPanel;

                if (nanoDrill.Count == 0) 
                {
                    mainProg.Echo("Can't find drill system");
                    return;
                }

                foreach(var dr in nanoDrill)
                {
                    Drills.Add(new NanoDrill(dr));
                }

                mainProg.Echo($"Total drills: {nanoDrill.Count}");


            }

            public void Start()
            {
               
            }

            public void Stop()
            {
               
            }

            public void Update()
            {
                GetNearlestOres();
                PrintData();
            }

            private void GetNearlestOres()
            {
                foreach (var drill in Drills) 
                {
                    drill.FindOres();
                }
              
            }

            private void PrintData()
            {
                nanoStatus?.WriteText("", false);
                nanoStatus?.WriteText($"Total drills: {nanoDrill.Count}", true);

                foreach (var drill in Drills)
                {
                    miningFields = drill.GetOreList();

                    nanoStatus?.WriteText($"\nDrill:", true);

                    if (HideIce)
                    {

                        foreach (var ore in miningFields.Where(o => !o[3].ToString().Contains("Snow")))
                        {
                            string oreName = ore[3].ToString().Remove(0, 40);
                            string cuant = ore[4].ToString();
                            nanoStatus?.WriteText($"\nOre: {oreName} X {cuant}", true);
                        }
                    }
                    else
                    {
                        foreach (var ore in miningFields)
                        {
                            string oreName = ore[3].ToString().Remove(0, 40);
                            string cuant = ore[4].ToString();
                            nanoStatus?.WriteText($"\nOre: {oreName} X {cuant}", true);

                        }
                    }

                }



                //foreach(var ore in miningFields)
                //{
                //    string oreName = ore[3].ToString().Remove(0, 40);
                //    string cuant = ore[4].ToString();

                //    nanoStatus?.WriteText($"\nOre: {oreName} X {cuant}", true);

                //}



                //if (miningFields.Any())
                //{
                //    var nnn = nanoDrill[0] as IMyShipDrill;
                //    nnn?.SetValue<object>("Drill.CurrentPickedDrillTarget", null);
                //}



                //foreach (var ore in miningFields)
                //{
                //    string oreName = ore[3].ToString().Remove(0, 40);
                //    string cuant = ore[4].ToString();

                //    nanoStatus?.WriteText($"\nOre: {oreName} X {cuant}", true);
                //}

                // nanoDrill[0].ApplyAction("OnOff_Off");

                //nanoDrill[0].ApplyAction("AreaOffsetLeftRight_Increase");
                //nanoDrill[0].ApplyAction("ShowArea_On");

                // nanoDrill[0].ApplyAction("ShowArea_On");AreaHeight Increase

                //string oreName = mining[0][3].ToString().Remove(0, 40);

                //nanoStatus?.WriteText($"Total mining: {mining.Count}", true);

                //nanoStatus?.WriteText($"\nCurrent: {current}", true);

                //nanoStatus?.WriteText($"\n{mining[0][0]}", true);//сам объект
                //nanoStatus?.WriteText($"\n{mining[0][1]}", true);//INOP
                //nanoStatus?.WriteText($"\n{mining[0][2]}", true);//количество
                //nanoStatus?.WriteText($"\n{mining[0][3]}", true);//имя
                //nanoStatus?.WriteText($"\n{mining[0][4]}", true);//INOP

                //nanoStatus?.WriteText($"\n{mining[0][0]}", true);//сам объект
                //nanoStatus?.WriteText($"\n{mining[0][1]}", true);//INOP
                //nanoStatus?.WriteText($"\n{mining[0][2]}", true);//количество
                //nanoStatus?.WriteText($"\n{oreName}", true);//имя
                //nanoStatus?.WriteText($"\n{mining[0][4]}", true);//INOP

                //if (mining.Any())
                //{
                //    var nnn = nanoDrill[0] as IMyShipDrill;
                //    nnn?.SetValue<object>("Drill.CurrentPickedDrillTarget", mining[0][0]);
                //}

                //            acts: AreaOffsetLeftRight Increase
                //acts: AreaOffsetLeftRight Decrease
                //acts: AreaOffsetUpDown Increase
                //acts: AreaOffsetUpDown Decrease
                //acts: AreaOffsetFrontBack Increase
                //acts: AreaOffsetFrontBack Decrease


            }

        }

        public class NanoDrill
        {
            private List<List<object>> miningFields;
            private IMyTerminalBlock drill;

            public NanoDrill(IMyTerminalBlock _drill)
            {
                drill = _drill;
                if (drill == null)
                    return;

                miningFields = new List<List<object>>();
            }

            public void FindOres()
            {
                if (drill == null)
                    return;

                miningFields = drill.GetValue<List<List<object>>>("Drill.PossibleDrillTargets");

               // drill.SetValue<object>("Drill.CurrentPickedDrillTarget", miningFields[0][0]);
            }

            public List<List<object>> GetOreList()
            {
                return miningFields;
            }
        }

       
       

        //////////////////END OF SCRIPT////////////////////////////////////////

    }

}


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

        NanodrillSystem DrillsSys;


        public Program()
        {
            DrillsSys = new NanodrillSystem(this);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;


        }



        public void Main(string args, UpdateType updateSource)
        {
            DrillsSys.Update();

            string arg = args.ToUpper();
          
            switch(arg)
            {
                case "SCAN":
                    DrillsSys.GetNearlestOres();
                    break;

                case "CL":
                    DrillsSys.ClearMiningTargets();
                    break;

                case "START":
                    DrillsSys.Start();
                    break;
                case "STOP":
                    DrillsSys.Stop();
                    break;

                case "PWRON":
                    DrillsSys.PowerOn();
                    break;
                case "PWROFF":
                    DrillsSys.PowerOff();
                    break;
            }

        }

        public class NanodrillSystem
        {
            public string LargeNanoName = "SELtdLargeNanobotDrillSystem";
            public string SmallNanoName = "SELtdSmallNanobotDrillSystem";

            public string NanoInfoDisplayName = "LCD Nano";

            public bool HideIce = true;
            public bool StartMining { get; private set; }
            public bool Powered { get; private set; }

            public List<NanoDrill> Drills;

            private Program mainProg;
            private List<IMyTerminalBlock> nanoDrill;

            private IMyTextPanel nanoStatus;
            private List<List<object>> miningFields;

            private int currentTick = 0;

   
            public NanodrillSystem(Program main)
            {
                mainProg = main;
                miningFields = new List<List<object>>();
                Drills = new List<NanoDrill>();
                StartMining = false;
                Powered = false;
                Init();
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

                PowerOff();

                mainProg.Echo($"Total drills: {nanoDrill.Count}");
                nanoStatus?.WriteText($"Nanodrill system powered: {Powered}", false);


            }

            public void Start()
            {
                StartMining = true;
            }

            public void Stop()
            {
                StartMining = false;
                ClearMiningTargets();
            }

            public void PowerOn()
            {
                Powered = true;
                foreach (var drill in Drills)
                {
                    drill.PowerOn();
                    drill.TakeControl();
                }
            }

            public void PowerOff()
            {
                Powered = false;
                Stop();
                foreach (var drill in Drills)
                {
                    drill.PowerOff();
                }
                nanoStatus?.WriteText($"Nanodrill system powered: {Powered}", false);
            }

            public void Update()
            {
                if (!Powered)
                    return;

                currentTick++;
                MiningUpdate();

                if (currentTick == 20) 
                {
                    GetNearlestOres();
                    PrintData();
                    currentTick = 0;
                }
            }

            public void GetNearlestOres()
            {
                foreach (var drill in Drills) 
                {
                    drill.FindOres();
                }
            }

            public void ClearMiningTargets()
            {
                foreach (var drill in Drills)
                {
                    drill.ClearMiningTarget();
                }
            }

            private void MiningUpdate()
            {
                if (!StartMining)
                    return;

                foreach (var drill in Drills)
                {
                    drill.Mining();
                }
            }


            public void PrintData()
            {
                nanoStatus?.WriteText("", false);
                nanoStatus?.WriteText($"Total drills: {nanoDrill.Count}", true);

                foreach (var drill in Drills)
                {
                    miningFields = drill.GetOreList();

                    nanoStatus?.WriteText($"\nDrill: {drill.GetCurrentMiningTarget()}", true);

                    if (HideIce)
                    {
                        foreach (var ore in miningFields.Where(o => !o[3].ToString().Contains("Ice") && !o[3].ToString().Contains("Snow")))
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

                //List<ITerminalAction> act = new List<ITerminalAction>();
                //Drills[0].Drill.GetActions(act);
                //foreach (var a in act)
                //{
                //    nanoStatus?.WriteText($"\nacts: {a.Name}", true);
                //}
                // nanoDrill[0].ApplyAction("OnOff_Off");

                //nanoDrill[0].ApplyAction("AreaOffsetLeftRight_Increase");
                //nanoDrill[0].ApplyAction("ShowArea_On");
                //acts: AreaOffsetLeftRight Increase
                //acts: AreaOffsetLeftRight Decrease
                //acts: AreaOffsetUpDown Increase
                //acts: AreaOffsetUpDown Decrease
                //acts: AreaOffsetFrontBack Increase
                //acts: AreaOffsetFrontBack Decrease


            }

        }

        public class NanoDrill
        {
            public IMyTerminalBlock Drill;

            private List<List<object>> miningFields;

            private MyIni dataSystem;

            public Dictionary<string, bool> TargetOres = new Dictionary<string, bool>()
                                                                                    {   {"Silicon",true},
                                                                                        {"Marble",true},
                                                                                        {"Copper",true},
                                                                                        {"Cobalt",true },
                                                                                        {"Iron",true },
                                                                                        {"Silver",true }
                                                                                    };

            public NanoDrill(IMyTerminalBlock _drill)
            {
                Drill = _drill;
                if (Drill == null) 
                    return;

                miningFields = new List<List<object>>();
                dataSystem = new MyIni();
                InitCustomData();
            }

            public void InitCustomData()
            {
                var data = Drill.CustomData;

                if (data.Length == 0)
                {   
                    dataSystem.AddSection("Ores");
                    dataSystem.Set("Ores", "Silicon", true);
                    dataSystem.Set("Ores", "Marble", true);
                    dataSystem.Set("Ores", "Copper", true);
                    dataSystem.Set("Ores", "Cobalt", true);
                    dataSystem.Set("Ores", "Iron", true);
                    dataSystem.Set("Ores", "Silver", true);
                    dataSystem.Set("Ores", "4", true);

                    Drill.CustomData = dataSystem.ToString();
                }
            }

            public void GetIniData()
            {
                // TargetOres["Silicon"] = 

                MyIniParseResult dataResult;
                if (!dataSystem.TryParse(Drill.CustomData, out dataResult))
                {

                }
                else
                {
                    TargetOres["Silicon"] = dataSystem.Get("Ores", "Silicon").ToBoolean();
                    TargetOres["Marble"] = dataSystem.Get("Ores", "Marble").ToBoolean();
                    TargetOres["Copper"] = dataSystem.Get("Ores", "Copper").ToBoolean();
                    TargetOres["Cobalt"] = dataSystem.Get("Ores", "Cobalt").ToBoolean();
                    TargetOres["Iron"] = dataSystem.Get("Ores", "Iron").ToBoolean();
                    TargetOres["Silver"] = dataSystem.Get("Ores", "Silver").ToBoolean();
                }
            }


            public void FindOres()
            {
                if (Drill == null)
                    return;

                miningFields = Drill.GetValue<List<List<object>>>("Drill.PossibleDrillTargets");
            }



            public void Mining()
            {
                FindOres();
                if (miningFields.Any())
                {

                    foreach (var ore in miningFields)
                    {
                        bool trg = false;
                        foreach (var trgore in TargetOres)
                        {
                            if (trgore.Value == true)
                                trg = ore[3].ToString().Contains(trgore.Key);

                            if (trg)
                            {
                                Drill.SetValue<object>("Drill.CurrentPickedDrillTarget", ore[0]);
                                return;
                            }
                        }
                    }
                }
            }

            public void ClearMiningTarget()
            {
                Drill?.SetValue<object>("Drill.CurrentPickedDrillTarget", null);
            }

            public List<List<object>> GetOreList()
            {
                return miningFields;
            }

            public object GetCurrentMiningTarget()
            {
                return Drill.GetValue<object>("Drill.CurrentPickedDrillTarget");
            }

            public void PowerOn()
            {
                Drill.ApplyAction("OnOff_On");
                GetIniData();
            }

            public void PowerOff()
            {
                Drill.ApplyAction("OnOff_Off");
            }

            public void TakeControl()
            {
                Drill.ApplyAction("ScriptControlled_On");
            }

        }

       
       

        //////////////////END OF SCRIPT////////////////////////////////////////

    }

}


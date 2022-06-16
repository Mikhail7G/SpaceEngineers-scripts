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
            if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                DrillsSys.Arguments(args);

            DrillsSys.Update();

        }

        /// <summary>
        /// Класс управления системами автобурения
        /// </summary>
        public class NanodrillSystem
        {
            public string LargeNanoName = "SELtdLargeNanobotDrillSystem";
            public string SmallNanoName = "SELtdSmallNanobotDrillSystem";

            public string NanoInfoDisplayName = "LCD Nano";

            public int DisplayUpdateInterval { get; set; }

            /// <summary>
            /// Скрывать лед при выводе на экран
            /// </summary>
            public bool HideIce = true;

            /// <summary>
            /// Начать бурение
            /// </summary>
            public bool StartMining { get; private set; }

            /// <summary>
            /// Включение питания модулей бурения
            /// </summary>
            public bool Powered { get; private set; }

            /// <summary>
            /// Лист всех установленных модулей на корабле
            /// </summary>
            public List<NanoDrill> Drills;

            private Program mainProg;
            private List<IMyTerminalBlock> nanoDrill;

            private IMyTextPanel nanoStatus;
            private List<List<object>> miningFields;

            private int currentTick = 0;
            private MyIni dataSystem;


            public NanodrillSystem(Program main)
            {
                mainProg = main;
                miningFields = new List<List<object>>();
                Drills = new List<NanoDrill>();
                StartMining = false;
                Powered = false;
                DisplayUpdateInterval = 20;
                dataSystem = new MyIni();

                Init();
            }

            public void InitCustomData()
            {
                var data = mainProg.Me.CustomData;

                if (data.Length == 0)
                {
                    dataSystem.AddSection("Settings");
                    dataSystem.Set("Settings", "DisplayUpdateRate", 20);

                    mainProg.Me.CustomData = dataSystem.ToString();
                }
            }

            /// <summary>
            /// Считывание при подаче питания на модуль
            /// </summary>
            public void GetIniData()
            {

                MyIniParseResult dataResult;
                if (!dataSystem.TryParse(mainProg.Me.CustomData, out dataResult))
                {

                }
                else
                {
                    DisplayUpdateInterval = dataSystem.Get("Settings", "DisplayUpdateRate").ToInt32();
                    if (DisplayUpdateInterval == 0)
                        DisplayUpdateInterval = 20;
                }
            }

            public void Init()
            {
                Drills.Clear();
                InitCustomData();
                GetIniData();

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                mainProg.GridTerminalSystem.GetBlocks(blocks);

                //Малая и большая сетки имеют разные названия
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
                nanoStatus?.WriteText($"Nanodrill system powered: {Powered}", false);
            }

            public void Arguments(string args)
            {
                string arg = args.ToUpper();

                switch (arg)
                {
                    case "NANO.SCAN":
                        GetNearlestOres();
                        break;

                    case "NANO.CL":
                        ClearMiningTargets();
                        break;

                    case "NANO.START":
                        Start();
                        break;
                    case "NANO.STOP":
                        Stop();
                        break;

                    case "NANO.PWRON":
                        PowerOn();
                        break;
                    case "NANO.PWROFF":
                        PowerOff();
                        break;
                }

            }

            /// <summary>
            /// Запуск скрипта
            /// </summary>
            public void Start()
            {
                StartMining = true;
            }

            /// <summary>
            /// Остановка скрипта
            /// </summary>
            public void Stop()
            {
                StartMining = false;
                ClearMiningTargets();
            }

            /// <summary>
            /// Включение модулей бурения
            /// </summary>
            public void PowerOn()
            {
                Powered = true;
                foreach (var drill in Drills)
                {
                    drill.PowerOn();
                }
            }

            /// <summary>
            /// Выключение модулей бурения
            /// </summary>
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

            /// <summary>
            /// Обновление модулей выполняется каждый тик
            /// </summary>
            public void Update()
            {
                if (!Powered)
                    return;

                currentTick++;
                MiningUpdate();

                if (currentTick == DisplayUpdateInterval) 
                {
                    GetNearlestOres();
                    PrintData();
                    currentTick = 0;
                }
            }

            /// <summary>
            /// Поиск ближайших доступных руд
            /// </summary>
            public void GetNearlestOres()
            {
                foreach (var drill in Drills) 
                {
                    drill.FindOres();
                }
            }

            /// <summary>
            /// Сброс целей для буров
            /// </summary>
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

            /// <summary>
            /// Вывод информации на дисплеи, интервал обновления == DisplayUpdateInterval
            /// </summary>
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

        /// <summary>
        /// Модуль бурения
        /// </summary>
        public class NanoDrill
        {
            /// <summary>
            /// Сам блок
            /// </summary>
            public IMyTerminalBlock Drill;

            /// <summary>
            /// Ближайшие воксели с рудой
            /// </summary>
            private List<List<object>> miningFields;

            /// <summary>
            /// Словарь руд для копки
            /// </summary>
            public Dictionary<string, bool> TargetOres = new Dictionary<string, bool>()
                                                                                    {   {"Silicon",true},
                                                                                        {"Marble",true},
                                                                                        {"Copper",true},
                                                                                        {"Cobalt",true },
                                                                                        {"Iron",true },
                                                                                        {"Silver",true },
                                                                                        {"Ammonium_Hydroxide",true },
                                                                                        {"Uraninite",true },
                                                                                        {"Magnesium",true },
                                                                                        {"Calcium",true },
                                                                                        {"Nickel",true },
                                                                                        {"Lead",true },
                                                                                        {"Gold",true },
                                                                                        {"Platinum",true },
                                                                                        {"Tylium",true },
                                                                                        {"Methane_Hydrate",true },
                                                                                        {"CHON",true },
                                                                                        {"Zinc",true },
                                                                                        {"Nadrit",true },
                                                                                        {"CarbonTetrachloride_AminoAcids",true }
                                                                                    };

            private MyIni dataSystem;

            public NanoDrill(IMyTerminalBlock _drill)
            {
                Drill = _drill;
                if (Drill == null) 
                    return;

                miningFields = new List<List<object>>();
                dataSystem = new MyIni();
                InitCustomData();
            }

            /// <summary>
            /// У каждого блока персональные настройки в Custom Data
            /// </summary>
            public void InitCustomData()
            {
                var data = Drill.CustomData;

                if (data.Length == 0)
                {   
                    dataSystem.AddSection("Ores");
                    dataSystem.AddSection("Ice");
                    //ORE
                    dataSystem.Set("Ores", "Silicon", true);
                    dataSystem.Set("Ores", "Marble", true);
                    dataSystem.Set("Ores", "Copper", true);
                    dataSystem.Set("Ores", "Cobalt", true);
                    dataSystem.Set("Ores", "Iron", true);
                    dataSystem.Set("Ores", "Silver", true); 
                    dataSystem.Set("Ores", "Uraninite", true);
                    dataSystem.Set("Ores", "Magnesium", true);
                    dataSystem.Set("Ores", "Calcium", true);
                    dataSystem.Set("Ores", "Nickel", true);
                    dataSystem.Set("Ores", "Lead", true);
                    dataSystem.Set("Ores", "Gold", true);
                    dataSystem.Set("Ores", "Platinum", true);
                    dataSystem.Set("Ores", "Nadrit", true);
                    //ICE
                    dataSystem.Set("Ice", "Ammonium_Hydroxide", true);
                    dataSystem.Set("Ice", "Tylium", true);
                    dataSystem.Set("Ice", "Methane_Hydrate", true);
                    dataSystem.Set("Ice", "CHON", true);
                    dataSystem.Set("Ice", "CarbonTetrachloride_AminoAcids", true);


                    Drill.CustomData = dataSystem.ToString();
                }
            }

            /// <summary>
            /// Считывание при подаче питания на модуль
            /// </summary>
            public void GetIniData()
            {

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
                    TargetOres["Uraninite"] = dataSystem.Get("Ores", "Uraninite").ToBoolean();
                    TargetOres["Magnesium"] = dataSystem.Get("Ores", "Magnesium").ToBoolean();
                    TargetOres["Calcium"] = dataSystem.Get("Ores", "Calcium").ToBoolean();
                    TargetOres["Nickel"] = dataSystem.Get("Ores", "Nickel").ToBoolean();
                    TargetOres["Lead"] = dataSystem.Get("Ores", "Lead").ToBoolean();
                    TargetOres["Gold"] = dataSystem.Get("Ores", "Gold").ToBoolean();
                    TargetOres["Platinum"] = dataSystem.Get("Ores", "Platinum").ToBoolean();
                    TargetOres["Nadrit"] = dataSystem.Get("Ores", "Nadrit").ToBoolean();

                    TargetOres["Ammonium_Hydroxide"] = dataSystem.Get("Ice", "Ammonium_Hydroxide").ToBoolean();
                    TargetOres["Tylium"] = dataSystem.Get("Ice", "Tylium").ToBoolean();
                    TargetOres["Methane_Hydrate"] = dataSystem.Get("Ice", "Methane_Hydrate").ToBoolean();
                    TargetOres["CHON"] = dataSystem.Get("Ice", "CHON").ToBoolean();
                    TargetOres["CarbonTetrachloride_AminoAcids"] = dataSystem.Get("Ice", "CarbonTetrachloride_AminoAcids").ToBoolean();
                }
            }

            /// <summary>
            /// Поиск вокселей
            /// </summary>
            public void FindOres()
            {
                if (Drill == null)
                    return;

                miningFields = Drill.GetValue<List<List<object>>>("Drill.PossibleDrillTargets");
            }

            /// <summary>
            /// Процесс копки
            /// </summary>
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

            public void ShowMiningArea()
            {
                Drill.ApplyAction("ShowArea_On");
            }

            public void HideMiningArea()
            {
                Drill.ApplyAction("ShowArea_Off");
            }

            /// <summary>
            /// Сброс цели
            /// </summary>
            public void ClearMiningTarget()
            {
                Drill.SetValue<object>("Drill.CurrentPickedDrillTarget", null);
            }

            /// <summary>
            /// Возвращяет лист с вокселями для копки
            /// </summary>
            /// <returns></returns>
            public List<List<object>> GetOreList()
            {
                return miningFields;
            }

            /// <summary>
            /// Текущая цель копки
            /// </summary>
            /// <returns></returns>
            public object GetCurrentMiningTarget()
            {
                return Drill.GetValue<object>("Drill.CurrentPickedDrillTarget");
            }

            /// <summary>
            /// Подать питание
            /// </summary>
            public void PowerOn()
            {
                Drill.ApplyAction("OnOff_On");
                TakeControl();
                GetIniData();
            }

            /// <summary>
            /// Отключение
            /// </summary>
            public void PowerOff()
            {
                Drill.ApplyAction("OnOff_Off");
            }

            /// <summary>
            /// Установка флага "управляется скриптом"
            /// </summary>
            public void TakeControl()
            {
                Drill.ApplyAction("ScriptControlled_On");
            }
        }

        //////////////////END OF SCRIPT////////////////////////////////////////

    }

}


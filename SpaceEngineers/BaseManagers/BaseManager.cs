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
using System.Linq;

namespace SpaceEngineers.BaseManagers
{
    public sealed class Program : MyGridProgram
    {
        string oreStorageName = "Ore";
        string ingnotStorageName = "Ingnot";
        string componentsStorageName = "Parts";

        string lcdInventoryIngnotsName = "LCD Inventory";
        string lcdPowerSystemName = "LCD Power";
        string lcdPartsName = "LCD Parts";
        string lcdInventoryDebugName = "LCD Debug";
        string lcdPowerDetailedName = "LCD Power full";
        string lcdNanobotName = "LCD Nano";
        string lcdRefinereyName = "LCD Refinerey";

        string assemblersSpecialOperationsName = "[sp]";


        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        PerformanceMonitor monitor;
        MyIni dataSystem;
        //дисплеи
        IMyTextPanel debugPanel;
        IMyTextPanel ingnotPanel;
        IMyTextPanel powerPanel;
        IMyTextPanel detailedPowerPanel;
        IMyTextPanel partsPanel;
        IMyTextPanel nanobotDisplay;
        IMyTextPanel refinereysDisplay;

        IMyTextSurface mainDisplay;


        //все объекты, содержащие инвентарь
        IEnumerable<IMyInventory> inventories;

        //сборщики, печки, контейнера
        List<IMyRefinery> refinereys;
        List<IMyAssembler> assemblers;
        List<IMyCargoContainer> containers;
        List<IMyBatteryBlock> batteries;
        List<IMyGasTank> gasTanks;
        List<IMyPowerProducer> generators;

        List<IMyAssembler> specialAssemblers;

        IMyTerminalBlock nanobotBuildModule;

        bool needReplaceIngnots = false;
        bool needReplaceParts = false;
        bool usePowerManagmentSystem = false;
        bool useDetailedPowerMonitoring = false;
        bool useAutoBuildSystem = false;
        bool getOreFromTransports = false;
        bool useNanobotAutoBuild = false;
        bool useRefinereysOperations = false;

        int totalIngnotStorageVolume = 0;
        int freeIngnotStorageVolume = 0;

        int totalPartsStorageVolume = 0;
        int freePartsStorageVolume = 0;

        int currentTick = 0;

        float maxStoredPower = 0;
        float currentStoredPower = 0;

        float inputPower = 0;
        float outputPower = 0;

        float generatorsMaxOutputPower = 0;
        float generatorsOutputPower = 0;


        int reactorMinFuel = 100;

        //словарь готовых компонентов и словарь запросов на автосборку компонентов
        Dictionary<string, int> ingnotsDict;
        Dictionary<string, int> partsDictionary;
        Dictionary<string, int> partsRequester;

        //Печки
        Dictionary<IMyRefinery, float> refinereyEfectivity;
        Dictionary<string, float> refsUpgradeList;
        List<MyProductionItem> refinereysItems;

        Dictionary<MyDefinitionId, int> nanobotBuildQueue;

        //Список стандартных названий чертежей
        List<string> blueprintDataBase = new List<string>{ "MyObjectBuilder_BlueprintDefinition/BulletproofGlass",
                                                           "MyObjectBuilder_BlueprintDefinition/ComputerComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/ConstructionComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/DetectorComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/Display",
                                                            "MyObjectBuilder_BlueprintDefinition/ExplosivesComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/GirderComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/InteriorPlate",
                                                            "MyObjectBuilder_BlueprintDefinition/LargeTube",
                                                            "MyObjectBuilder_BlueprintDefinition/MedicalComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/MetalGrid",
                                                            "MyObjectBuilder_BlueprintDefinition/Missile200mm",
                                                            "MyObjectBuilder_BlueprintDefinition/MotorComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/NATO_25x184mmMagazine",
                                                            "MyObjectBuilder_BlueprintDefinition/NATO_5p56x45mmMagazine",
                                                            "MyObjectBuilder_BlueprintDefinition/PowerCell",
                                                            "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/ReactorComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/SmallTube",
                                                            "MyObjectBuilder_BlueprintDefinition/SolarCell",
                                                            "MyObjectBuilder_BlueprintDefinition/SteelPlate",
                                                            "MyObjectBuilder_BlueprintDefinition/Superconductor",
                                                            "MyObjectBuilder_BlueprintDefinition/ThrustComponent",
                                                            "MyObjectBuilder_BlueprintDefinition/AngleGrinder",
                                                            "MyObjectBuilder_BlueprintDefinition/AngleGrinder2",
                                                            "MyObjectBuilder_BlueprintDefinition/AngleGrinder3",
                                                            "MyObjectBuilder_BlueprintDefinition/AngleGrinder4",
                                                            "MyObjectBuilder_BlueprintDefinition/HandDrill",
                                                            "MyObjectBuilder_BlueprintDefinition/HandDrill2",
                                                            "MyObjectBuilder_BlueprintDefinition/HandDrill3",
                                                            "MyObjectBuilder_BlueprintDefinition/HandDrill4",
                                                            "MyObjectBuilder_BlueprintDefinition/Welder",
                                                            "MyObjectBuilder_BlueprintDefinition/Welder2",
                                                            "MyObjectBuilder_BlueprintDefinition/Welder3",
                                                            "MyObjectBuilder_BlueprintDefinition/Welder4",
                                                            "MyObjectBuilder_BlueprintDefinition/AutomaticRifle",
                                                            "MyObjectBuilder_BlueprintDefinition/PreciseAutomaticRifle",
                                                            "MyObjectBuilder_BlueprintDefinition/RapidFireAutomaticRifle",
                                                            "MyObjectBuilder_BlueprintDefinition/UltimateAutomaticRifle",
                                                            "MyObjectBuilder_BlueprintDefinition/HydrogenBottle",
                                                            "MyObjectBuilder_BlueprintDefinition/OxygenBottle"};


        /// <summary>
        /// Инициализация компонентов 1 раз при создании объекта
        /// </summary>
        public Program()
        {
            Echo($"Script first init starting");
            Runtime.UpdateFrequency = UpdateFrequency.None;

            mainDisplay = Me.GetSurface(1);

            inventories = new List<IMyInventory>();
            refinereys = new List<IMyRefinery>();
            assemblers = new List<IMyAssembler>();
            containers = new List<IMyCargoContainer>();
            batteries = new List<IMyBatteryBlock>();
            gasTanks = new List<IMyGasTank>();
            specialAssemblers = new List<IMyAssembler>();

            ingnotsDict = new Dictionary<string, int>();
            partsDictionary = new Dictionary<string, int>();
            partsRequester = new Dictionary<string, int>();
            nanobotBuildQueue = new Dictionary<MyDefinitionId, int>();

            refsUpgradeList = new Dictionary<string, float>();
            refinereysItems = new List<MyProductionItem>();
            refinereyEfectivity = new Dictionary<IMyRefinery, float>();

            dataSystem = new MyIni();
            monitor = new PerformanceMonitor(this);
            GetIniData();

        }

        /// <summary>
        /// Функия выполняется каждые 100 тиков
        /// </summary>
        public void Main(string args, UpdateType updateType)
        {

            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                Commands(args);

            Update();
        }

        public void Commands(string str)
        {
            string argument = str.ToUpper();

            switch (argument)
            {
                case "START":
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    Echo($"Script running");
                    break;
                case "STOP":
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    Echo($"Script stopped");
                    break;
                case "PRINTBPS":
                    Echo("Try print pbs names");
                    PrintAllBluepritnsNames();
                    break;
                case "NANO":
                    SwitchNanobotMode();
                    break;
                case "INGNOT":
                    SwitchIngnotMode();
                    break;
                case "PART":
                    SwitchPartsMode();
                    break;
                case "ORE":
                    SwitchOreMode();
                    break;
                case "POWER":
                    SwitchPowerMode();
                    break;

            }

        }


        public void Update()
        {
            FindLcds();
            FindInventories();


            switch (currentTick)
            {
                case 0:
                    ReplaceIgnots();
                    DisplayIngnots();
                    break;
                case 1:
                    ReplaceParts();
                    DisplayParts();
                    break;
                case 2:
                    PowerMangment();
                    PrintPowerStatus();
                    break;
                case 3:
                    GetOreFromTransport();
                    NanobotOperations();
                    PrintNanobotQueue();
                    break;
                case 4:
                    RefinereysPrintData();
                    break;
            }

            currentTick++;
            if (currentTick == 5)
                currentTick = 0;

            monitor.EndOfFrameCalc();

            mainDisplay.WriteText("", false);
            mainDisplay.WriteText($"AV inst: {monitor.AverageInstructionsPerTick} / {monitor.MaxInstructionsPerTick}" +
                                  $"\nAV time:{monitor.AverageTimePerTick}", true);

        }


        public void GetIniData()
        {
            InitCustomData();

            Echo($"Reading custom data");
            MyIniParseResult dataResult;
            if (!dataSystem.TryParse(Me.CustomData, out dataResult))
            {
                Echo($"CustomData error:\nLine {dataResult}");
            }
            else
            {
                needReplaceIngnots = dataSystem.Get("Operations", "ReplaceIngnots").ToBoolean();
                needReplaceParts = dataSystem.Get("Operations", "ReplaceParts").ToBoolean();
                usePowerManagmentSystem = dataSystem.Get("Operations", "PowerManagmentSystem").ToBoolean();
                useDetailedPowerMonitoring = dataSystem.Get("Operations", "DetailedPowerMonitoring").ToBoolean();
                useAutoBuildSystem = dataSystem.Get("Operations", "AutoBuildSystem").ToBoolean();
                getOreFromTransports = dataSystem.Get("Operations", "TransferOreFromTransports").ToBoolean();
                useNanobotAutoBuild = dataSystem.Get("Operations", "UseNanobotAutoBuild").ToBoolean();
                useRefinereysOperations = dataSystem.Get("Operations", "UseRefinereyOperations").ToBoolean();

                //Containers
                oreStorageName = dataSystem.Get("ContainerNames", "oreStorageName").ToString();
                ingnotStorageName = dataSystem.Get("ContainerNames", "ingnotStorageName").ToString();
                componentsStorageName = dataSystem.Get("ContainerNames", "componentsStorageName").ToString();

                //Displays
                lcdInventoryIngnotsName = dataSystem.Get("DisplaysNames", "lcdInventoryIngnotsName").ToString();
                lcdPowerSystemName = dataSystem.Get("DisplaysNames", "lcdPowerSystemName").ToString();
                lcdPartsName = dataSystem.Get("DisplaysNames", "lcdPartsName").ToString();
                lcdInventoryDebugName = dataSystem.Get("DisplaysNames", "lcdInventoryDebugName").ToString();
                lcdPowerDetailedName = dataSystem.Get("DisplaysNames", "lcdPowerDetailedName").ToString();
                lcdNanobotName = dataSystem.Get("DisplaysNames", "NanobotDisplayName").ToString();
                lcdRefinereyName = dataSystem.Get("DisplaysNames", "lcdRefinereyName").ToString();

                //Tags
                assemblersSpecialOperationsName = dataSystem.Get("TagsNames", "assemblersSpecialOperationsTagName").ToString();
            }

            Echo("Script ready to run");
        }

        public void InitCustomData()
        {
            var data = Me.CustomData;

            if (data.Length == 0)
            {
                Echo("Custom data empty!");

                dataSystem.AddSection("Operations");
                dataSystem.Set("Operations", "ReplaceIngnots", false);
                dataSystem.Set("Operations", "ReplaceParts", false);
                dataSystem.Set("Operations", "PowerManagmentSystem", false);
                dataSystem.Set("Operations", "DetailedPowerMonitoring", false);
                dataSystem.Set("Operations", "AutoBuildSystem", false);
                dataSystem.Set("Operations", "TransferOreFromTransports", false);
                dataSystem.Set("Operations", "UseNanobotAutoBuild", false);
                dataSystem.Set("Operations", "UseRefinereyOperations", false);

                dataSystem.AddSection("DisplaysNames");
                dataSystem.Set("DisplaysNames", "lcdInventoryIngnotsName", "LCD Inventory");
                dataSystem.Set("DisplaysNames", "lcdPowerSystemName", "LCD Power");
                dataSystem.Set("DisplaysNames", "lcdPowerDetailedName", "LCD Power full");
                dataSystem.Set("DisplaysNames", "lcdPartsName", "LCD Parts");
                dataSystem.Set("DisplaysNames", "NanobotDisplayName", "LCD Nano");
                dataSystem.Set("DisplaysNames", "lcdInventoryDebugName", "LCD Debug");
                dataSystem.Set("DisplaysNames", "lcdRefinereyName", "LCD Refinerey");

                dataSystem.AddSection("ContainerNames");
                dataSystem.Set("ContainerNames", "oreStorageName", "Ore");
                dataSystem.Set("ContainerNames", "ingnotStorageName", "Ingnot");
                dataSystem.Set("ContainerNames", "componentsStorageName", "Parts");

                dataSystem.AddSection("TagsNames");
                dataSystem.Set("TagsNames", "assemblersSpecialOperationsTagName", "[sp]");

                Me.CustomData = dataSystem.ToString();
            }

            Echo("Custom data ready");
        }

        public void PrintAllBluepritnsNames()
        {
            //debugPanel?.WriteText("", false);
            //debugPanel?.WriteText("\n<--------Production blocks--------->", true);

            //var blueprints = new List<MyProductionItem>();
            //var ass = assemblers.Where(q => !q.IsQueueEmpty).ToList();
            //foreach (var a in ass)
            //{
            //    a.GetQueue(blueprints);

            //    foreach (var bp in blueprints)
            //    {
            //        debugPanel?.WriteText($"{bp.BlueprintId}\n", true);
            //    }
            //}

            //debugPanel?.WriteText("\n<--------Ore blocks--------->", true);
            //debugPanel?.WriteText("\n<--------UPGRADES--------->", true);

            //List<MyProductionItem> items = new List<MyProductionItem>();

            //var refs = refinereys.ToList();
            //foreach (var r in refs)
            //{
            //    r.GetQueue(items);
            //    debugPanel?.WriteText($"\nQQ:{items.Count}", true);

            //    var upg = r as IMyUpgradableBlock;
            //    Dictionary<string, float> upgradeList = new Dictionary<string, float>();
            //    upg?.GetUpgrades(out upgradeList);

            //    debugPanel?.WriteText($"\n{r.CustomName}", true);
            //    foreach (var bp in upgradeList)
            //    {
            //        debugPanel?.WriteText($"\n{bp}", true);
            //    }
            //}
            //debugPanel?.WriteText("\n<--------END--------->", true);

            //List<ITerminalAction> act = new List<ITerminalAction>();
            //refs[0].GetActions(act);

            //List<ITerminalProperty> prop = new List<ITerminalProperty>();
            //refs[0].GetProperties(prop);
            //foreach (var a in act)
            //{
            //    debugPanel?.WriteText($"\nacts: {a.Name}", true);
            //}
            //debugPanel?.WriteText("\n<--------PRP--------->", true);
            //foreach (var a in prop)
            //{
            //    debugPanel?.WriteText($"\nPROP: {a}", true);
            //}

        }

        /// <summary>
        /// Поиск необходимых дисплеев, можно без них
        /// </summary>
        public void FindLcds()
        {
            if ((debugPanel == null) || (debugPanel.Closed))
            {
                debugPanel = GridTerminalSystem.GetBlockWithName(lcdInventoryDebugName) as IMyTextPanel;
            }
            else
            {
                Echo($"Debug LCDs found:{lcdInventoryDebugName}");
            }

            if ((ingnotPanel == null) || (ingnotPanel.Closed))
            {
                Echo($"Try find:{lcdInventoryIngnotsName}");
                ingnotPanel = GridTerminalSystem.GetBlockWithName(lcdInventoryIngnotsName) as IMyTextPanel;
            }
            else
            {
                Echo($"Ingnot LCDs found:{lcdInventoryIngnotsName}");
            }

            if ((powerPanel == null) || (powerPanel.Closed))
            {
                Echo($"Try find:{lcdPowerSystemName}");
                powerPanel = GridTerminalSystem.GetBlockWithName(lcdPowerSystemName) as IMyTextPanel;
            }
            else
            {
                Echo($"Power LCDs found:{lcdPowerSystemName}");
            }

            if ((detailedPowerPanel == null) || (detailedPowerPanel.Closed))
            {
                Echo($"Try find:{lcdPowerDetailedName}");
                detailedPowerPanel = GridTerminalSystem.GetBlockWithName(lcdPowerDetailedName) as IMyTextPanel;
            }
            else
            {
                Echo($"Full power LCDs found:{lcdPowerDetailedName}");
            }


            if ((partsPanel == null) || (partsPanel.Closed))
            {
                Echo($"Try find:{lcdPartsName}");
                partsPanel = GridTerminalSystem.GetBlockWithName(lcdPartsName) as IMyTextPanel;
            }
            else
            {
                Echo($"Parts LCDs found:{lcdPartsName}");
            }

            if ((nanobotDisplay == null) || (nanobotDisplay.Closed))
            {
                Echo($"Try find:{lcdNanobotName}");
                nanobotDisplay = GridTerminalSystem.GetBlockWithName(lcdNanobotName) as IMyTextPanel;
            }
            else
            {
                Echo($"NANOBOT LCDs found:{lcdNanobotName}");
            }

            if ((refinereysDisplay == null) || (refinereysDisplay.Closed))
            {
                Echo($"Try find:{lcdRefinereyName}");
                refinereysDisplay = GridTerminalSystem.GetBlockWithName(lcdRefinereyName) as IMyTextPanel;
            }
            else
            {
                Echo($"Refinerey LCDs found:{lcdRefinereyName}");
            }

        }

        /// <summary>
        /// Отладка
        /// </summary>
        public void WriteDebugText()
        {
            debugPanel?.WriteText("", false);

            //List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            //GridTerminalSystem.GetBlocksOfType(blocks);

            //debugPanel?.WriteText("", false);

            //foreach (var b in blocks)
            //{
            //    debugPanel?.WriteText($"\n{b.BlockDefinition}", true);
            //}

            var targetInventory = containers.Where(c => c.CustomName.Contains(oreStorageName))
                                            .Select(i => i.GetInventory(0)).ToList();

            foreach (var inventory in targetInventory)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);

                foreach (var item in items)
                {

                    debugPanel?.WriteText($"\n{item.Type.TypeId} x {item.Type.SubtypeId}", true);

                    //if (item.Type.TypeId == "MyObjectBuilder_Component")//части
                    //{
                    //    if (partsDictionary.ContainsKey(item.Type.SubtypeId))
                    //    {
                    //        partsDictionary[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                    //    }
                    //    else
                    //    {
                    //        partsDictionary.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                    //    }
                    //}
                }
            }


        }


        /// <summary>
        /// Поиск всех обьектов, печек, сборщиков, ящиков
        /// </summary>
        public void FindInventories()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);
            inventories = blocks.Where(b => b.HasInventory)
                                .Select(b => b.GetInventory(b.InventoryCount - 1));//берем из инвентаря готовой продукции


            refinereys = blocks.Where(b => b is IMyRefinery).Where(r => r.IsFunctional).Where(b=>b.CubeGrid==Me.CubeGrid).Select(t => t as IMyRefinery).ToList();
            assemblers = blocks.Where(b => b is IMyAssembler).Where(a => a.IsFunctional).Where(b => b.CubeGrid == Me.CubeGrid).Select(t => t as IMyAssembler).ToList();
            containers = blocks.Where(b => b is IMyCargoContainer).Where(c => c.IsFunctional).Select(t => t as IMyCargoContainer).ToList();
            batteries = blocks.Where(b => b is IMyBatteryBlock).Where(b => b.IsFunctional).Select(t => t as IMyBatteryBlock).ToList();
            gasTanks = blocks.Where(b => b is IMyGasTank).Where(g => g.IsFunctional).Select(t => t as IMyGasTank).ToList();

            generators = blocks.Where(b => b is IMyPowerProducer).Where(r => r.IsFunctional).Select(t => t as IMyPowerProducer).ToList();

            nanobotBuildModule = blocks.Where(g => g.BlockDefinition.SubtypeName.ToString() == "SELtdLargeNanobotBuildAndRepairSystem").FirstOrDefault();

            specialAssemblers = assemblers.Where(a => a.CustomName.Contains(assemblersSpecialOperationsName)).ToList();


            foreach (var refs in refinereys.Where(refs => refs is IMyUpgradableBlock))
            {
                var upgradeBlock = refs as IMyUpgradableBlock;
                upgradeBlock?.GetUpgrades(out refsUpgradeList);

                if (refinereyEfectivity.ContainsKey(refs))
                {
                    refinereyEfectivity[refs] = refsUpgradeList["Effectiveness"];
                }
                else
                {
                    refinereyEfectivity.Add(refs, refsUpgradeList["Effectiveness"]);
                }
            }

            Echo(">>>-------------------------------<<<");
            Echo($"Refinereys found:{refinereys.Count}");
            Echo($"Assemblers found:{assemblers.Count}");
            Echo($"Special assemblers found:{specialAssemblers.Count}");
            Echo($"Containers found my/conn: {containers.Where(c => c.CubeGrid == Me.CubeGrid).Count()}/" +
                                           $"{containers.Where(c => c.CubeGrid != Me.CubeGrid).Count()}");

            Echo($"Battery found my/conn: {batteries.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
                                        $"{batteries.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");

            Echo($"Generators found my/conn: {generators.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
                                           $"{generators.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");

            Echo($"Gas found my/conn: {gasTanks.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
                                         $"{gasTanks.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");

            string nanoFinded = nanobotBuildModule != null ? "OK" : "NO module";
            Echo($"Nanobot:{nanoFinded}:{nanobotBuildModule.CustomName}");

            Echo(">>>-------------------------------<<<");

            Echo($"Nanobot system: {useNanobotAutoBuild}");
            Echo($"Ingnot replace system: {needReplaceIngnots}");
            Echo($"Parts replace system: {needReplaceParts}");
            Echo($"Power mng system: {usePowerManagmentSystem}");
            Echo($"Get ore frm outer: {getOreFromTransports}");
            Echo($"Refinerey ops: {useRefinereysOperations}");

            Echo(">>>-------------------------------<<<");

            monitor.AddInstructions("");
        }

        public void SwitchNanobotMode()
        {
            useNanobotAutoBuild = !useNanobotAutoBuild;
        }

        public void SwitchIngnotMode()
        {
            needReplaceIngnots = !needReplaceIngnots;
        }

        public void SwitchOreMode()
        {
            getOreFromTransports = !getOreFromTransports;
        }

        public void SwitchPartsMode()
        {
            needReplaceParts = !needReplaceParts;
        }

        public void SwitchPowerMode()
        {
            usePowerManagmentSystem = !usePowerManagmentSystem;
        }


        /// <summary>
        /// Вывод информации о заполнении печек и наличию модов
        /// </summary>
        public void RefinereysPrintData()
        {

            if (refinereysDisplay == null)
                return;

            refinereysDisplay?.WriteText("", false);
            refinereysDisplay?.WriteText("<<-----------ORES----------->>", false);

            foreach (var refs in refinereyEfectivity)
            {
                refs.Key.GetQueue(refinereysItems);
                refinereysDisplay?.WriteText($"\n{refs.Key.CustomName} effectivity: {refs.Value}", true);

                foreach (var bp in refinereysItems)
                {
                    //string pbsName = bp.BlueprintId.SubtypeName.Substring(0,bp.BlueprintId.SubtypeName.LastIndexOf("OreToIngot"));
                    refinereysDisplay?.WriteText($"\n{bp.BlueprintId.SubtypeName} X Def:{bp.Amount} / EFF:{bp.Amount * refs.Value}", true);
                }
                refinereysDisplay?.WriteText("\n----------", true);

            }
            monitor.AddInstructions("");
        }

        /// <summary>
        /// INOP
        /// </summary>
        public void LoadRefinereysManually()
        {
            var oreInventory = containers.Where(c => c.CustomName.Contains(oreStorageName))
                                         .Select(i => i.GetInventory(0))
                                         .Where(i => i.ItemCount > 0).ToList();


            if (!oreInventory.Any())
                return;

           
            foreach (var inv in oreInventory)
            {

 
            }
            monitor.AddInstructions("");
        }






        /// <summary>
        /// Перекладываем слитки из печек по контейнерам
        /// </summary>
        public void ReplaceIgnots()
        {
            if (!needReplaceIngnots)
                return;

            var targetInventory = containers.Where(c => c.CustomName.Contains(ingnotStorageName))
                                            .Select(i => i.GetInventory(0))
                                            .Where(i => !i.IsFull);

            var refsInventory = refinereys.Select(i => i.GetInventory(1))
                                           .Where(i => i.ItemCount > 0);

            Echo("------Replace ingnots starting------");

            if ((!targetInventory.Any()) || (!refsInventory.Any()))
            {
                Echo("------No items to transfer-----");
                return;
            }

            Echo($"Total ingnot conts:{targetInventory.Count()}");

            foreach (var refs in refsInventory)
            {
                //var availConts = targetInventory.Where(inv => inv.CanTransferItemTo(refs, MyItemType.MakeIngot("MyObjectBuilder_Ingot")));
                var availConts = targetInventory.Where(inv => inv.IsConnectedTo(refs));

                if (!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }
                var item = refs.GetItemAt(0);
                var targInv = availConts.First().Owner as IMyCargoContainer;

                if (refs.TransferItemTo(availConts.First(), 0, null, true))
                {
                    Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                }
                else
                {
                    Echo($"Transer FAILED!: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                }

            }
            monitor.AddInstructions("");
        }

        /// <summary>
        /// Вывод количества слитков в ящиках на дисплее
        /// </summary>
        public void DisplayIngnots()
        {
            if (ingnotPanel == null)
            {
                return;
            }

            totalIngnotStorageVolume = 0;
            freeIngnotStorageVolume = 0;
            ingnotsDict.Clear();

            var ingnotInventorys = containers.Where(c => c.CustomName.Contains(ingnotStorageName))
                                             .Select(i => i.GetInventory(0));

            freeIngnotStorageVolume = ingnotInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalIngnotStorageVolume = ingnotInventorys.Sum(i => i.MaxVolume.ToIntSafe());

            foreach (var inventory in ingnotInventorys)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);

                foreach (var item in items)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Ingot")//слитки
                    {
                        if (ingnotsDict.ContainsKey(item.Type.SubtypeId))
                        {
                            ingnotsDict[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            ingnotsDict.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                        }
                    }
                }
            }

            ingnotPanel?.WriteText("", true);
            ingnotPanel?.WriteText($"Total/max ingnot cont volume: {freeIngnotStorageVolume} / {totalIngnotStorageVolume} T", false);

            foreach (var dict in ingnotsDict.OrderBy(k => k.Key))
            {
                ingnotPanel?.WriteText($"\n{dict.Key} : {dict.Value} ", true);
            }

            monitor.AddInstructions("");
        }//DisplayIngnots()


        /// <summary>
        /// Выгрузка руды из подключенных к базе кораблей
        /// </summary>
        public void GetOreFromTransport()
        {
            if (!getOreFromTransports)
                return;

            Echo("------Replase Ore from transport------");
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);

            var externalContainers = blocks.Where(b => b is IMyCargoContainer)
                                           .Where(c => c.IsFunctional)
                                           .Where(c => c.CubeGrid != Me.CubeGrid)
                                           .Select(t => t.GetInventory(0)).ToList();

            var targetInventory = containers.Where(c => c.CustomName.Contains(oreStorageName))
                                           .Select(i => i.GetInventory(0))
                                           .Where(i => !i.IsFull);

            Echo($"Ext conts {externalContainers.Count}");

            if ((!targetInventory.Any()) || (!externalContainers.Any()))
            {
                Echo("------No items to transfer-----");
                return;
            }

            foreach (var cargo in externalContainers)
            {

                var availConts = targetInventory.Where(inv => inv.IsConnectedTo(cargo));

                if (!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }

                var currentCargo = cargo.ItemCount;

                var targInv = availConts.First().Owner as IMyCargoContainer;

                for (int i = 0; i <= currentCargo; i++)
                {
                    var item = cargo.GetItemAt(i);

                    if (item == null)
                        continue;

                    if (item.Value.Type.TypeId == "MyObjectBuilder_Ore")
                    {
                        if (cargo.TransferItemTo(availConts.First(), i, null, true))
                        {
                            Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                        }
                        else
                        {
                            Echo($"Transer FAILED: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                        }
                    }
                    else
                    {
                        Echo($"No ore in cargo");
                    }

                }
 
            }

            monitor.AddInstructions("");
        }


        /// <summary>
        /// Перекладка запчастей из сбощиков в контейнеры
        /// </summary>
        public void ReplaceParts()
        {
            if (!needReplaceParts)
                return;

            var targetInventory = containers.Where(c => c.CustomName.Contains(componentsStorageName))
                                            .Select(i => i.GetInventory(0))
                                            .Where(i => !i.IsFull);

            var assInventory = assemblers.Select(i => i.GetInventory(1))
                                         .Where(i => i.ItemCount > 0);

            Echo("------Replace parts starting------");

            if ((!targetInventory.Any()) || (!assInventory.Any()))
            {
                Echo("------No items to transfer-----");
                return;
            }

            Echo($"Total parts conts:{targetInventory.Count()}");

            foreach (var ass in assInventory)
            {
                // var availConts = targetInventory.Where(inv => inv.CanTransferItemTo(ass, MyItemType.MakeComponent("MyObjectBuilder_Component")));
                var availConts = targetInventory.Where(inv => inv.IsConnectedTo(ass));

                if (!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }
                var item = ass.GetItemAt(0);
                var targInv = availConts.First().Owner as IMyCargoContainer;


                if (ass.TransferItemTo(availConts.First(), 0, null, true))
                {
                    Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                }
                else
                {
                    Echo($"Transer FAILED: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                }
            }

            monitor.AddInstructions("");
        }

        /// <summary>
        /// Вывод информации о компонентах на дисплей
        /// </summary>
        public void DisplayParts()
        {
            if (partsPanel == null)
            {
                return;
            }

            totalPartsStorageVolume = 0;
            freePartsStorageVolume = 0;
            partsDictionary.Clear();

            var partsInventorys = containers.Where(c => c.CustomName.Contains(componentsStorageName))
                                            .Select(i => i.GetInventory(0));

            freePartsStorageVolume = partsInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalPartsStorageVolume = partsInventorys.Sum(i => i.MaxVolume.ToIntSafe());

            foreach (var inventory in partsInventorys)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);

                foreach (var item in items)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Component")//части
                    {
                        if (partsDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            partsDictionary[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            partsDictionary.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                        }
                    }
                }
            }

            partsPanel?.WriteText("", true);
            partsPanel?.WriteText($"Total/max parts cont volume: {freePartsStorageVolume} / {totalPartsStorageVolume} T", false);

            foreach (var dict in partsDictionary.OrderBy(k => k.Key))
            {
                if (partsRequester.ContainsKey(dict.Key))
                {
                    partsPanel?.WriteText($"\n{dict.Key} : {dict.Value} / {partsRequester[dict.Key]}", true);
                }
                else
                {
                    partsPanel?.WriteText($"\n{dict.Key} : {dict.Value} ", true);
                }
            }

            monitor.AddInstructions("");
        }//DisplayParts()

        /// <summary>
        /// Система управления питанием базы
        /// </summary>
        public void PowerMangment()
        {
            if (!usePowerManagmentSystem)
                return;


            Echo("------Power managment system-------");
            Echo($"Batt:{batteries.Count}");
            Echo($"Gens:{generators.Count}");

            maxStoredPower = batteries.Sum(b => b.MaxStoredPower);
            currentStoredPower = batteries.Sum(b => b.CurrentStoredPower);

            inputPower = batteries.Sum(b => b.CurrentInput);
            outputPower = batteries.Sum(b => b.CurrentOutput);

            generatorsMaxOutputPower = generators.Sum(g => g.MaxOutput);
            generatorsOutputPower = generators.Sum(g => g.CurrentOutput);

            PowerSystemDetailed();

            monitor.AddInstructions("");
        }

        /// <summary>
        /// Детальная информация о электросистемах
        /// </summary>
        public void PowerSystemDetailed()
        {
            if (!useDetailedPowerMonitoring)
                return;

            detailedPowerPanel?.WriteText("", false);

            List<MyInventoryItem> items = new List<MyInventoryItem>();

            var reactorInventory = generators.Where(g => g.HasInventory).Select(g => g.GetInventory(0)).ToList();
            int reactorsCount = generators.Where(g => g is IMyReactor).Count();
            int windCount = generators.Where(g => g.BlockDefinition.TypeId.ToString() == "MyObjectBuilder_WindTurbine").Count();
            int gasCount = generators.Where(g => g.BlockDefinition.TypeId.ToString() == "MyObjectBuilder_HydrogenEngine").Count();

            detailedPowerPanel?.WriteText($"Wind: {windCount} React: {reactorsCount} Gas: {gasCount}", true);

            foreach (var react in reactorInventory)
            {
                items.Clear();
                react.GetItems(items);

                if (items.Any())
                {
                    string lowCount = items[0].Amount < reactorMinFuel ? "TO LOW" : "";
                    detailedPowerPanel?.WriteText($"\nR: {items[0].Type.SubtypeId} / {items[0].Amount} {lowCount}", true);
                }
                else
                {
                    var targInv = react.Owner as IMyTerminalBlock;
                    detailedPowerPanel?.WriteText($"\nR:{targInv?.CustomName} EMPTY!!", true);
                }
            }

            monitor.AddInstructions("");
        }

        /// <summary>
        /// Вывод информации о состоянии энергии
        /// </summary>
        public void PrintPowerStatus()
        {
            powerPanel?.WriteText("", false);
            powerPanel?.WriteText($"BatteryStatus:\nTotal/Max power:{Math.Round(currentStoredPower, 2)} / {maxStoredPower} MWt {Math.Round(currentStoredPower / maxStoredPower * 100, 1)} %"
                                 + $"\nInput/Output:{Math.Round(inputPower, 2)} / {Math.Round(outputPower, 2)} {(inputPower > outputPower ? "+" : "-")} MWt/h "
                                 + $"\nGens maxOut/Out: {Math.Round(generatorsMaxOutputPower, 2)} / {Math.Round(generatorsOutputPower, 2)} MWT", true);

        }

        /// <summary>
        /// Сислема заказа производства недостающих компонентов INOP
        /// </summary>
        public void PartsAutoBuild()
        {
            //if (!useAutoBuildSystem)
            //    return;

            //Echo("------Auto build system-------");
            //partsRequester.Clear();

            //string[] lines = Me.CustomData.Split('\n');

            //foreach (var line in lines)
            //{
            //    string[] itemName = line.Split('=');

            //    if (!partsRequester.ContainsKey(itemName[0]))
            //    {
            //        int count = 0;

            //        if (int.TryParse(itemName[1], out count))
            //        {
            //            partsRequester.Add(itemName[0], count);
            //        }
            //    }

            //}
            //Echo($"Detected auto build componetns: {partsRequester.Count}");

            //var workingAssemblers = assemblers.Where(ass => !ass.IsQueueEmpty).ToList();

            //if (workingAssemblers.Count == 0)
            //{
            //    foreach (var req in partsRequester)
            //    {
            //        if (partsDictionary.ContainsKey(req.Key))
            //        {
            //            int needComponents = req.Value - partsDictionary[req.Key];
            //            if (needComponents > 0)
            //            {
            //                string name = "MyObjectBuilder_BlueprintDefinition/" + req.Key;//Название компонента для строительсвта
            //                var parser = blueprintDataBase.Where(n => n.Contains(req.Key)).FirstOrDefault();//Если компонент стандартный ищем его в готовом списке

            //                if (parser != null)
            //                    name = parser;

            //                Echo($"Start build: {req.Key} X {needComponents}");
            //                Echo($"D_name: {name}");
            //                Echo("\n");

            //                MyDefinitionId blueprint;
            //                if (!MyDefinitionId.TryParse(name, out blueprint))
            //                    Echo($"WARNING cant parse: {name}");

            //                var assemblersCanBuildThis = assemblers.Where(a => a.CanUseBlueprint(blueprint)).ToList();
            //                var count = needComponents / assemblersCanBuildThis.Count;
            //                if (count < 1)
            //                    count = 1;

            //                foreach (var asembler in assemblersCanBuildThis)
            //                {
            //                    VRage.MyFixedPoint amount = (VRage.MyFixedPoint)count;
            //                    asembler.AddQueueItem(blueprint, amount);
            //                    Echo($"Assemblers starts: {req.Key}");
            //                }
            //            }
            //        }
            //    }
            //}
            //AddInstructions();
        }//PartsAutoBuild()

        /// <summary>
        /// Автосборка с помощью мода Nanobot
        /// </summary>
        public void NanobotOperations()
        {
            if (nanobotBuildModule == null)
                return;
            if (useNanobotAutoBuild == false)
                return;

            Echo("------Nanobot system working-------");

            nanobotBuildQueue.Clear();

            nanobotBuildQueue = nanobotBuildModule.GetValue<Dictionary<MyDefinitionId, int>>("BuildAndRepair.MissingComponents");
            Echo($"Nanobot total components:{nanobotBuildQueue.Count}");

            AddNanobotPartsToProduct();

            monitor.AddInstructions("");
        }

        public void AddNanobotPartsToProduct()
        {
            if (useNanobotAutoBuild == false)
                return;

            foreach (var ass in specialAssemblers)
            {
                ass.ClearQueue();
            }

            var freeAssemblers = specialAssemblers.FirstOrDefault(ass => ass.IsQueueEmpty);
            if (freeAssemblers == null)
                return;

            foreach (var bps in nanobotBuildQueue)
            {
                string pbsName = bps.Key.ToString().Remove(0, 26);
                string name = "MyObjectBuilder_BlueprintDefinition/" + pbsName;
                var parser = blueprintDataBase.Where(n => n.Contains(pbsName)).FirstOrDefault();//Если компонент стандартный ищем его в готовом списке

                if (parser != null)
                    name = parser;

                VRage.MyFixedPoint amount = (VRage.MyFixedPoint)bps.Value;
                MyDefinitionId blueprint;

                if (!MyDefinitionId.TryParse(name, out blueprint))
                {
                    Echo($"WARNING cant parse: {name}");
                    continue;
                }

                if (freeAssemblers.CanUseBlueprint(blueprint))
                {
                    freeAssemblers.AddQueueItem(blueprint, amount);
                }
            }

            monitor.AddInstructions("");
        }

        public void PrintNanobotQueue()
        {
            if ((nanobotBuildModule == null) || (nanobotBuildModule.Closed) || (nanobotDisplay == null) || (nanobotDisplay.Closed))
            {
                return;
            }

            nanobotDisplay.WriteText("", false);
            nanobotDisplay?.WriteText($"Block:{nanobotBuildModule.CustomName} auto mode: {useNanobotAutoBuild}", true);

            foreach (var comp in nanobotBuildQueue.OrderBy(c => c.Key.ToString()))
            {
                string name = comp.Key.ToString().Remove(0, 26);
                nanobotDisplay?.WriteText($"\n{name} X {comp.Value}", true);
            }

            monitor.AddInstructions("");
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


            public PerformanceMonitor(Program main)
            {
                mainProgram = main;
                CallPerTick = 0;
                AverageInstructionsPerTick = 0;
                AverageTimePerTick = 0;
                avrInst = 0;
                avrTime = 0;

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

                AverageTimePerTick = avrTime /  CallPerTick;
                avrTime = 0;

                CallPerTick = 0;

            }

        }
        

        ///END OF SCRIPT///////////////
    }

}

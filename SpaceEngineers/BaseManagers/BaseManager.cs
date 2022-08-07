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

        string lcdInventoryOresName = "LCD Ore";
        string lcdInventoryIngnotsName = "LCD Inventory";
        string lcdPowerSystemName = "LCD Power";
        string lcdPartsName = "LCD Parts";
        string lcdInventoryDebugName = "LCD Debug";
        string lcdPowerDetailedName = "LCD Power full";
        string lcdNanobotName = "LCD Nano";
        string lcdRefinereyName = "LCD Refinerey";

        string assemblersSpecialOperationsName = "[sp]";
        string assemblersBlueprintLeanerName = "[bps]";


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
        IMyTextPanel oreDisplay;

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

        bool autorun = false;
        bool needReplaceIngnots = false;
        bool needReplaceParts = false;
        bool usePowerManagmentSystem = false;
        bool useDetailedPowerMonitoring = false;
        bool useAutoBuildSystem = false;
        bool getOreFromTransports = false;
        bool useNanobotAutoBuild = false;
        bool useRefinereysOperations = false;
        bool reactorPayloadLimitter = false;

        int totalOreStorageVolume = 0;
        int freeOreStorageVolume = 0;

        int totalIngnotStorageVolume = 0;
        int freeIngnotStorageVolume = 0;

        int totalPartsStorageVolume = 0;
        int freePartsStorageVolume = 0;

        int currentTick = 0;
        int assemblersDeadlockTick = 0;

        int refinereyReloadPrecentage = 70;
        int maxVolumeContainerPercentage = 95;

        float maxStoredPower = 0;
        float currentStoredPower = 0;

        float inputPower = 0;
        float outputPower = 0;

        float generatorsMaxOutputPower = 0;
        float generatorsOutputPower = 0;
        float powerLoadPercentage = 0;

        bool nanobuildReady = true;

        bool assemblerBlueprintGetter = false;
        string SpecialAssemblerLastName = "";

        //словарь готовых компонентов и словарь запросов на автосборку компонентов
        Dictionary<string, int> oresDict;
        Dictionary<string, ItemBalanser> ingnotsDict;
        Dictionary<string, ItemBalanser> partsDictionary;
        Dictionary<string, int> partsIngnotAndOresDictionary;
        Dictionary<string, ItemBalanser> ammoDictionary;
        Dictionary<string, ItemBalanser> buildedIngnotsDictionary;
        Dictionary<string, int> reactorFuelLimitter;

        Dictionary<string, string> blueprintData;

       // List<string> orePriority;
        Dictionary<string, int> orePriority;

        //Печки
        Dictionary<IMyRefinery, float> refinereyEfectivity;
        Dictionary<string, float> refsUpgradeList;
        List<MyInventoryItem> oreItems;
        List<MyInventoryItem> ingnotItems;
        List<MyProductionItem> refinereysItems;
        List<MyInventoryItem> productionItems;

        StringBuilder partsDisplayData;


        //Поиск чертежей
        MyProductionItem lastDetectedBlueprintItem;
        MyInventoryItem? lastDetectedConstructItem;

        Dictionary<MyDefinitionId, int> nanobotBuildQueue;

        System.Text.RegularExpressions.Regex NameRegex = new System.Text.RegularExpressions.Regex(@"(?<Name>\w*\-?\w*)\s:");//(?<Name>\w*)\s: || (?<Name>^\w*\-*\s*\w*)\s: - c пробелами и подчеркиваниями
        System.Text.RegularExpressions.Regex AmountRegex = new System.Text.RegularExpressions.Regex(@"(?<Amount>\d+)$");


        /// <summary>
        /// Инициализация компонентов 1 раз при создании объекта
        /// </summary>
        public Program()
        {
            Echo($"Script first init starting");
            Runtime.UpdateFrequency = UpdateFrequency.None;

            inventories = new List<IMyInventory>();
            oreItems = new List<MyInventoryItem>();
            refinereys = new List<IMyRefinery>();
            assemblers = new List<IMyAssembler>();
            containers = new List<IMyCargoContainer>();
            batteries = new List<IMyBatteryBlock>();
            generators = new List<IMyPowerProducer>();
            gasTanks = new List<IMyGasTank>();
            specialAssemblers = new List<IMyAssembler>();

            partsDisplayData = new StringBuilder();

            oresDict = new Dictionary<string, int>();
            ingnotsDict = new Dictionary<string, ItemBalanser>();
            partsDictionary = new Dictionary<string, ItemBalanser>();
            partsIngnotAndOresDictionary = new Dictionary<string, int>();
            nanobotBuildQueue = new Dictionary<MyDefinitionId, int>();
            ammoDictionary = new Dictionary<string, ItemBalanser>();
            buildedIngnotsDictionary = new Dictionary<string, ItemBalanser>();
            reactorFuelLimitter = new Dictionary<string, int>();

            blueprintData = new Dictionary<string, string>();

            refsUpgradeList = new Dictionary<string, float>();
            refinereysItems = new List<MyProductionItem>();
            productionItems = new List<MyInventoryItem>();
            ingnotItems = new List<MyInventoryItem>();
            refinereyEfectivity = new Dictionary<IMyRefinery, float>();
            orePriority = new Dictionary<string, int>();
            

            dataSystem = new MyIni();
            monitor = new PerformanceMonitor(this, Me.GetSurface(1));

            GetIniData();

            if (autorun)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                Echo($"Script running");
            }

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
                case "SAVEBPS":
                    Echo("Try save bps names");
                    SwitchBlueprintGetter();
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
                case "BUILD":
                    SwitchAutoBuildMode();
                    break;
            }
        }


        public void Update()
        {
            //FindLcds();
            //FindInventories();

            // WriteDebugText();
            // ServiceInfo();

            DrawEcho();

            SaveAllBlueprintsNames();

            switch (currentTick)
            {
                case 0:
                    FindLcds();
                    FindInventories();
                    break;
                case 1:
                    ClearRefinereys();
                    ReplaceIgnots();
                    DisplayIngnots();
                    break;
                case 2:
                    ReplaceParts();
                    DisplayParts();
                    break;
                case 3:
                    PowerMangment();
                    PrintPowerStatus();
                    PartsAutoBuild();
                    break;
                case 4:
                    GetOreFromTransport();
                    NanobotOperations();
                    PrintNanobotQueue();
                    break;
                case 5:
                    LoadRefinereys();
                    RefinereysPrintData();
                    DisplayOres();
                    AssemblersClear();
                    break;
            }

            currentTick++;
            if (currentTick == 6)
                currentTick = 0;

            monitor.EndOfFrameCalc();
            monitor.Draw();
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

                autorun = dataSystem.Get("Operations", "Autorun").ToBoolean();

                needReplaceIngnots = dataSystem.Get("Operations", "ReplaceIngnots").ToBoolean();
                needReplaceParts = dataSystem.Get("Operations", "ReplaceParts").ToBoolean();
                usePowerManagmentSystem = dataSystem.Get("Operations", "PowerManagmentSystem").ToBoolean();
                useDetailedPowerMonitoring = dataSystem.Get("Operations", "DetailedPowerMonitoring").ToBoolean();
                useAutoBuildSystem = dataSystem.Get("Operations", "AutoBuildSystem").ToBoolean();
                getOreFromTransports = dataSystem.Get("Operations", "TransferOreFromTransports").ToBoolean();
                useNanobotAutoBuild = dataSystem.Get("Operations", "UseNanobotAutoBuild").ToBoolean();
                useRefinereysOperations = dataSystem.Get("Operations", "UseRefinereyOperations").ToBoolean();
                reactorPayloadLimitter = dataSystem.Get("Operations", "ReactorFuelLimitter").ToBoolean();

                //Containers 
                oreStorageName = dataSystem.Get("ContainerNames", "oreStorageName").ToString();
                ingnotStorageName = dataSystem.Get("ContainerNames", "ingnotStorageName").ToString();
                componentsStorageName = dataSystem.Get("ContainerNames", "componentsStorageName").ToString();

                //Displays 
                lcdInventoryOresName = dataSystem.Get("DisplaysNames", "lcdInventoryOresName").ToString();
                lcdInventoryIngnotsName = dataSystem.Get("DisplaysNames", "lcdInventoryIngnotsName").ToString();
                lcdPowerSystemName = dataSystem.Get("DisplaysNames", "lcdPowerSystemName").ToString();
                lcdPartsName = dataSystem.Get("DisplaysNames", "lcdPartsName").ToString();
                lcdInventoryDebugName = dataSystem.Get("DisplaysNames", "lcdInventoryDebugName").ToString();
                lcdPowerDetailedName = dataSystem.Get("DisplaysNames", "lcdPowerDetailedName").ToString();
                lcdNanobotName = dataSystem.Get("DisplaysNames", "NanobotDisplayName").ToString();
                lcdRefinereyName = dataSystem.Get("DisplaysNames", "lcdRefinereyName").ToString();

                //Tags
                assemblersSpecialOperationsName = dataSystem.Get("TagsNames", "assemblersSpecialOperationsTagName").ToString();
                assemblersBlueprintLeanerName = dataSystem.Get("TagsNames", "assemblersBlueprintLeanerName").ToString();

                // OrePriority
                List<MyIniKey> keys = new List<MyIniKey>();
                dataSystem.GetKeys("OrePriority", keys);

                foreach (var key in keys)
                {
                    orePriority.Add(key.Name, dataSystem.Get(key).ToInt32());
                }

                keys.Clear();

                // Blueprints
                //List<MyIniKey> keys = new List<MyIniKey>();
                dataSystem.GetKeys("Blueprints", keys);

                foreach (var key in keys)
                {
                    if (blueprintData.ContainsKey(key.Name))
                    {
                        blueprintData[key.Name] = dataSystem.Get(key).ToString();
                    }
                    else
                    {
                        blueprintData.Add(key.Name, dataSystem.Get(key).ToString());
                    }
                }

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
                dataSystem.Set("Operations", "Autorun", false);
                dataSystem.Set("Operations", "ReplaceIngnots", false);
                dataSystem.Set("Operations", "ReplaceParts", false);
                dataSystem.Set("Operations", "PowerManagmentSystem", false);
                dataSystem.Set("Operations", "DetailedPowerMonitoring", false);
                dataSystem.Set("Operations", "AutoBuildSystem", false);
                dataSystem.Set("Operations", "TransferOreFromTransports", false);
                dataSystem.Set("Operations", "UseNanobotAutoBuild", false);
                dataSystem.Set("Operations", "UseRefinereyOperations", false);
                dataSystem.Set("Operations", "ReactorFuelLimitter", false);

                dataSystem.AddSection("DisplaysNames");
                dataSystem.Set("DisplaysNames", "lcdInventoryOresName", "LCD Ore");
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
                dataSystem.Set("TagsNames", "assemblersBlueprintLeanerName", "[bps]");

                dataSystem.AddSection("OrePriority");

                dataSystem.AddSection("Blueprints");

                Me.CustomData = dataSystem.ToString();
            }

            Echo("Custom data ready");
        }

        public void ReloadData()
        {
            dataSystem.Set("Operations", "Autorun", autorun);
            dataSystem.Set("Operations", "ReplaceIngnots", needReplaceIngnots);
            dataSystem.Set("Operations", "ReplaceParts", needReplaceParts);
            dataSystem.Set("Operations", "PowerManagmentSystem", usePowerManagmentSystem);
            dataSystem.Set("Operations", "DetailedPowerMonitoring", useDetailedPowerMonitoring);
            dataSystem.Set("Operations", "AutoBuildSystem", useAutoBuildSystem);
            dataSystem.Set("Operations", "TransferOreFromTransports", getOreFromTransports);
            dataSystem.Set("Operations", "UseNanobotAutoBuild", useNanobotAutoBuild);
            dataSystem.Set("Operations", "UseRefinereyOperations", useRefinereysOperations);

            Me.CustomData = dataSystem.ToString();
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
                //Echo($"Debug LCDs found:{lcdInventoryDebugName}");
            }

            if ((ingnotPanel == null) || (ingnotPanel.Closed))
            {
                //Echo($"Try find:{lcdInventoryIngnotsName}");
                ingnotPanel = GridTerminalSystem.GetBlockWithName(lcdInventoryIngnotsName) as IMyTextPanel;
            }
            else
            {
               // Echo($"Ingnot LCDs found:{lcdInventoryIngnotsName}");
            }

            if ((powerPanel == null) || (powerPanel.Closed))
            {
               // Echo($"Try find:{lcdPowerSystemName}");
                powerPanel = GridTerminalSystem.GetBlockWithName(lcdPowerSystemName) as IMyTextPanel;
            }
            else
            {
                //Echo($"Power LCDs found:{lcdPowerSystemName}");
            }

            if ((detailedPowerPanel == null) || (detailedPowerPanel.Closed))
            {
                //Echo($"Try find:{lcdPowerDetailedName}");
                detailedPowerPanel = GridTerminalSystem.GetBlockWithName(lcdPowerDetailedName) as IMyTextPanel;
            }
            else
            {
                //Echo($"Full power LCDs found:{lcdPowerDetailedName}");
            }


            if ((partsPanel == null) || (partsPanel.Closed))
            {
                //Echo($"Try find:{lcdPartsName}");
                partsPanel = GridTerminalSystem.GetBlockWithName(lcdPartsName) as IMyTextPanel;
            }
            else
            {
                ///Echo($"Parts LCDs found:{lcdPartsName}");
            }

            if ((nanobotDisplay == null) || (nanobotDisplay.Closed))
            {
               // Echo($"Try find:{lcdNanobotName}");
                nanobotDisplay = GridTerminalSystem.GetBlockWithName(lcdNanobotName) as IMyTextPanel;
            }
            else
            {
               // Echo($"NANOBOT LCDs found:{lcdNanobotName}");
            }

            if ((refinereysDisplay == null) || (refinereysDisplay.Closed))
            {
               // Echo($"Try find:{lcdRefinereyName}");
                refinereysDisplay = GridTerminalSystem.GetBlockWithName(lcdRefinereyName) as IMyTextPanel;
            }
            else
            {
                //Echo($"Refinerey LCDs found:{lcdRefinereyName}");
            }

            if ((oreDisplay == null) || (oreDisplay.Closed))
            {
               // Echo($"Try find:{lcdInventoryOresName}");
                oreDisplay = GridTerminalSystem.GetBlockWithName(lcdInventoryOresName) as IMyTextPanel;
            }
            else
            {
               // Echo($"Ores LCDs found:{lcdInventoryOresName}");
            }

        }

        //public void ServiceInfo()
        //{
        //    debugPanel?.WriteText("", false);
        //    debugPanel?.WriteText($"\nrefinereys {refinereys.Count}x{refinereys.Capacity}" +
        //                          $"\nassemblers {assemblers.Count}x{assemblers.Capacity}" +
        //                          $"\ncontainers {containers.Count}x{containers.Capacity}" +
        //                          $"\nbatteries {batteries.Count}x{batteries.Capacity}" +
        //                          $"\ngasTanks {gasTanks.Count}x{gasTanks.Capacity}" +
        //                          $"\ngenerators {generators.Count}x{generators.Capacity}" +
        //                          $"\nspecialAssemblers {specialAssemblers.Count}x{specialAssemblers.Capacity}" +
        //                          $"\noresDict {oresDict.Count}" +
        //                          $"\ningnotsDict {ingnotsDict.Count}" +
        //                          $"\npartsDictionary {partsDictionary.Count}" +
        //                          $"\npartsIngnotAndOresDictionary {partsIngnotAndOresDictionary.Count}" +
        //                          $"\nammoDictionary {ammoDictionary.Count}" +
        //                          $"\nbuildedIngnotsDictionary {buildedIngnotsDictionary.Count}" +
        //                          $"\nrefinereyEfectivity {refinereyEfectivity.Count}" +
        //                          $"\nrefsUpgradeList {refsUpgradeList.Count}" +
        //                          $"\noreItems {oreItems.Count}x{oreItems.Capacity}" +
        //                          $"\ningnotItems {ingnotItems.Count}x{ingnotItems.Capacity}" +
        //                          $"\nrefinereysItems {refinereysItems.Count}x{refinereysItems.Capacity}" +
        //                          $"\nproductionItems {productionItems.Count}x{productionItems.Capacity}" +
        //                          $"\nnanobotBuildQueue {nanobotBuildQueue.Count}" +
        //                          $" ", true);
        //}

        /// <summary>
        /// Отладка
        /// </summary>
        public void WriteDebugText()
        {
            debugPanel?.WriteText("", false);

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);

            debugPanel?.WriteText("", false);

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

        public void DrawEcho()
        {
            Echo(">>>-------------------------------<<<");
            Echo($"Current frame: {currentTick}");
            Echo($"Refinereys found:{refinereys.Count}");
            Echo($"Assemblers found:{assemblers.Count}");
            Echo($"Special assemblers found:{specialAssemblers.Count}");
            Echo($"Containers:{containers.Count}");
            Echo($"Battery:{batteries.Count}");
            Echo($"Generators:{generators.Count}");
            Echo($"Gas tanks:{gasTanks.Count}");

           
            if (nanobotBuildModule != null)
            {
                string nanoFinded = nanobotBuildModule != null ? "OK" : "NO module";
                Echo($"Nanobot:{nanoFinded}:{nanobotBuildModule?.CustomName}");
            }

            Echo(">>>-------------------------------<<<");

            Echo($"Auto build system: {useAutoBuildSystem}");
            Echo($"Nanobot system: {useNanobotAutoBuild}");
            Echo($"Ingnot replace system: {needReplaceIngnots}");
            Echo($"Parts replace system: {needReplaceParts}");
            Echo($"Power mng system: {usePowerManagmentSystem}");
            Echo($"Get ore frm outer: {getOreFromTransports}");
            Echo($"Refinerey ops: {useRefinereysOperations}");
            Echo($"Scan blueprints: {assemblerBlueprintGetter}");

            Echo(">>>-------------------------------<<<");
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

            nanobotBuildModule = blocks.Where(b => b.IsFunctional)
                                       .Where(g => g.BlockDefinition.SubtypeName.ToString() == "SELtdLargeNanobotBuildAndRepairSystem")
                                       .Where(n => n.CubeGrid == Me.CubeGrid).FirstOrDefault();

            specialAssemblers = assemblers.Where(a => a.CustomName.Contains(assemblersSpecialOperationsName)).ToList();


            //Echo(">>>-------------------------------<<<");
            //Echo($"Current frame: {currentTick}");
            //Echo($"Refinereys found:{refinereys.Count}");
            //Echo($"Assemblers found:{assemblers.Count}");
            //Echo($"Special assemblers found:{specialAssemblers.Count}");
            //Echo($"Containers found my/conn: {containers.Where(c => c.CubeGrid == Me.CubeGrid).Count()}/" +
            //                               $"{containers.Where(c => c.CubeGrid != Me.CubeGrid).Count()}");

            //Echo($"Battery found my/conn: {batteries.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
            //                            $"{batteries.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");

            //Echo($"Generators found my/conn: {generators.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
            //                               $"{generators.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");

            //Echo($"Gas found my/conn: {gasTanks.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
            //                        $"{gasTanks.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");

            //string nanoFinded = nanobotBuildModule != null ? "OK" : "NO module";
            //Echo($"Nanobot:{nanoFinded}:{nanobotBuildModule.CustomName}");

            //Echo(">>>-------------------------------<<<");

            //Echo($"Nanobot system: {useNanobotAutoBuild}");
            //Echo($"Ingnot replace system: {needReplaceIngnots}");
            //Echo($"Parts replace system: {needReplaceParts}");
            //Echo($"Power mng system: {usePowerManagmentSystem}");
            //Echo($"Get ore frm outer: {getOreFromTransports}");
            //Echo($"Refinerey ops: {useRefinereysOperations}");
            //Echo($"Scan blueprints: {assemblerBlueprintGetter}");

            //Echo(">>>-------------------------------<<<");

            monitor.AddInstructions("");
        }

        public void SwitchNanobotMode()
        {
            useNanobotAutoBuild = !useNanobotAutoBuild;
            if(useNanobotAutoBuild)
            {
                if (nanobotBuildModule != null)
                    nanobotBuildModule.SetValueBool("OnOff", true);
            }
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

        public void SwitchAutoBuildMode()
        {
            useAutoBuildSystem = !useAutoBuildSystem;
        }


        /// <summary>
        /// Вывод информации о заполнении печек и наличию модов
        /// </summary>
        public void RefinereysPrintData()
        {
            if (refinereysDisplay == null)
                return;

            foreach (var refs in refinereys.Where(refs => (!refs.Closed) && (refs is IMyUpgradableBlock)))
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

            refinereysDisplay?.WriteText("", false);
            refinereysDisplay?.WriteText("<<---------------Refinereys-------------->>", true);

            foreach (var refs in refinereyEfectivity)
            {
                double loadInput = (double)refs.Key.InputInventory.CurrentVolume.ToIntSafe() / (double)refs.Key.InputInventory.MaxVolume.ToIntSafe() * 100;
                double loadOuptut = (double)refs.Key.OutputInventory.CurrentVolume.ToIntSafe() / (double)refs.Key.OutputInventory.MaxVolume.ToIntSafe() * 100;
               
                refs.Key.GetQueue(refinereysItems);
                refinereysDisplay?.WriteText($"\n{refs.Key.CustomName}:" +
                                             $"\nEffectivity: {refs.Value} Load: {loadInput} / {loadOuptut} %", true);

                foreach (var bp in refinereysItems)
                {
                    //string pbsName = bp.BlueprintId.SubtypeName.Substring(0,bp.BlueprintId.SubtypeName.LastIndexOf("OreToIngot"));
                    refinereysDisplay?.WriteText($"\n{bp.BlueprintId.SubtypeName} X Def:{bp.Amount} / EFF:{bp.Amount * refs.Value}", true);
                }
                refinereysDisplay?.WriteText("\n----------", true);

            }
            refinereysItems.Clear();

            monitor.AddInstructions("");
        }

        /// <summary>
        /// Выгрузка низкоприоритетной руды из печей
        /// </summary>
        public void ClearRefinereys()
        {
            if (!useRefinereysOperations)
                return;

            var oreInventory = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(oreStorageName)))
                                         .Select(i => i.GetInventory(0))
                                         .Where(i => i.ItemCount > 0).ToList();

            var targetOreInventory = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(oreStorageName)))
                                               .Select(i => i.GetInventory(0))
                                               .Where(i => !i.IsFull).ToList();

            //foreach (var oreInv in oreInventory)
            //{
            //    var count = oreInv.ItemCount;

            //    for (int i = 0; i <= count; i++)
            //    {
            //        var item = oreInv.GetItemAt(i);

            //        if (item == null)
            //            continue;

            //        var priorItem = orePriority.ContainsKey(item.Value.Type.SubtypeId);
            //        //if (!priorItem)


            //    }
            //}

            //if (detectNewOre)
            //{
            //    foreach (var refs in refinereys)
            //    {
            //        var count = refs.InputInventory.ItemCount;

            //        for (var i = 0; i <= count; i++)
            //        {
            //            var item = refs.InputInventory.GetItemAt(i);

            //            if (item == null)
            //                continue;

            //            var priorItem = orePriority.ContainsKey(item.Value.Type.SubtypeId);

            //            if (priorItem)
            //            {
            //                foreach(var targInv in targetOreInventory)
            //                {
            //                    if (refs.InputInventory.TransferItemTo(targInv, i, null, true))
            //                        break;
            //                }

            //            }

            //        }
            //    }
            //}

            monitor.AddInstructions("");
        }

        /// <summary>
        /// INOP
        /// </summary>
        public void LoadRefinereys()
        {
            if (!useRefinereysOperations)
                return;

            var oreInventory = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(oreStorageName)))
                                         .Select(i => i.GetInventory(0))
                                         .Where(i => i.ItemCount > 0).ToList();

            if (!oreInventory.Any())
                return;

            foreach (var refs in refinereys)
            {
                if (!refs.Closed)
                {
                    refs.UseConveyorSystem = false;

                    if (refs.InputInventory.ItemCount == 0)
                    {
                        //Балансировка руды по приоритетам,сачала та, что быстро плавится
                        foreach (var inv in oreInventory)
                        {
                            var count = inv.ItemCount;

                            for (int i = 0; i <= count; i++)
                            {
                                var item = inv.GetItemAt(i);

                                if (item == null)
                                    continue;

                                var lowPriorItem = orePriority.ContainsKey(item.Value.Type.SubtypeId);

                                if (!lowPriorItem)
                                {
                                    if (inv.TransferItemTo(refs.InputInventory, i, null, true))
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        //Если осталась только низкоприоитетная руда
                        if (refs.InputInventory.ItemCount == 0)
                        {
                            oreInventory.FirstOrDefault().TransferItemTo(refs.InputInventory, 0, null, true);
                        }
                    }
                    else
                    {
                        var load = (double)refs.InputInventory.CurrentVolume * 100 / (double)refs.InputInventory.MaxVolume;
                        if (load < refinereyReloadPrecentage)
                        {
                            var refsItem = refs.InputInventory.GetItemAt(0);

                            if (refsItem != null)
                            {
                                foreach (var inv in oreInventory)
                                {
                                    var count = inv.ItemCount;

                                    for (int i = 0; i <= count; i++)
                                    {
                                        var item = inv.GetItemAt(i);

                                        if (item == null)
                                            continue;

                                        if (item.Value.Type.SubtypeId == refsItem.Value.Type.SubtypeId)
                                        {
                                            if (!inv.TransferItemTo(refs.InputInventory, i, null, true))
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            monitor.AddInstructions("");
        }


        /// <summary>
        /// Отображение руды на базе в контейнерах
        /// </summary>
        public void DisplayOres()
        {
            if (oreDisplay == null)
                return;
       
            var oreInventory = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(oreStorageName)))
                                        .Select(i => i.GetInventory(0))
                                        .Where(i => i.ItemCount > 0).ToList();

            if (!oreInventory.Any())
                return;

            oresDict.Clear();

            freeOreStorageVolume = oreInventory.Sum(i => i.CurrentVolume.ToIntSafe());
            totalOreStorageVolume = oreInventory.Sum(i => i.MaxVolume.ToIntSafe());

            double precentageVolume = Math.Round(((double)freeOreStorageVolume / (double)totalOreStorageVolume) * 100, 1);


            foreach (var inv in oreInventory)
            {
                inv.GetItems(oreItems);

                foreach(var item in oreItems)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Ore")//построенная руда  
                    {
                        if (oresDict.ContainsKey(item.Type.SubtypeId))
                        {
                            oresDict[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            oresDict.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                        }
                    }
                }
                oreItems.Clear();
            }

            oreDisplay?.WriteText("", false);
            oreDisplay?.WriteText($"<<-----------Ores----------->>{oreItems.Count} x {oreItems.Capacity}" +
                                   $"\nContainers:{oreInventory.Count()}" +
                                   $"\nVolume: {precentageVolume} % {freeOreStorageVolume} / {totalOreStorageVolume} T", true);

            foreach (var dict in oresDict.OrderBy(k => k.Key))
            {
                oreDisplay?.WriteText($"\n{dict.Key} : {dict.Value} ", true);
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

            var targetInventory = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(ingnotStorageName)))
                                            .Select(i => i.GetInventory(0))
                                            .Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            var refsInventory = refinereys.Where(r => !r.Closed)
                                          .Select(i => i.GetInventory(1))
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
                var availConts = targetInventory.Where(inv => inv.IsConnectedTo(refs));

                if (!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }

                var currentCargo = refs.ItemCount;
                var targInv = availConts.First().Owner as IMyCargoContainer;


                for (int i = 0; i <= currentCargo; i++)
                {
                    var item = refs.GetItemAt(i);

                    if (item == null)
                        continue;

                    if (refs.TransferItemTo(availConts.First(), i, null, true))
                    {
                        Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                    }
                    else
                    {
                        Echo($"Transer FAILED!: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                    }
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

            foreach(var dict in ingnotsDict.ToList())
            {
                dict.Value.Current = 0;
            }
         
            totalIngnotStorageVolume = 0;
            freeIngnotStorageVolume = 0;
           // ingnotsDict.Clear();
            partsIngnotAndOresDictionary.Clear();

            var ingnotInventorys = containers.Where(c => (!c.Closed) && c.CustomName.Contains(ingnotStorageName))
                                             .Select(i => i.GetInventory(0));

            freeIngnotStorageVolume = ingnotInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalIngnotStorageVolume = ingnotInventorys.Sum(i => i.MaxVolume.ToIntSafe());

            double precentageVolume = Math.Round(((double)freeIngnotStorageVolume / (double)totalIngnotStorageVolume) * 100,1);

            foreach (var inventory in ingnotInventorys)
            {
                inventory.GetItems(ingnotItems);

                foreach (var item in ingnotItems)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Ingot")//слитки 
                    {
                        if (ingnotsDict.ContainsKey(item.Type.SubtypeId))
                        {
                            ingnotsDict[item.Type.SubtypeId].Current += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            ingnotsDict.Add(item.Type.SubtypeId, new ItemBalanser { Current = item.Amount.ToIntSafe() });
                        }
                    }

                    if (item.Type.TypeId == "MyObjectBuilder_Ore")//построенная руда  
                    {
                        if (partsIngnotAndOresDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            partsIngnotAndOresDictionary[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            partsIngnotAndOresDictionary.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                        }
                    }

                }
                ingnotItems.Clear();
            }

            ingnotPanel?.WriteText("", false);
            ingnotPanel?.WriteText($"<<-----------Ingnots----------->>" +
                                   $"\nContainers:{ingnotInventorys.Count()}" +
                                   $"\nVolume: {precentageVolume} % {freeIngnotStorageVolume} / {totalIngnotStorageVolume} T", true);

            ingnotPanel?.WriteText("\n<<-----------Ingnots----------->>", true);

            foreach (var dict in ingnotsDict.OrderBy(k => k.Key))
            {
                ingnotPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} ", true);
            }

            ingnotPanel?.WriteText("\n<<-----------Ores----------->>", true);

            foreach (var dict in partsIngnotAndOresDictionary.OrderBy(k => k.Key))
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

            var externalInventory = containers.Where(c => c.IsFunctional)
                                              .Where(c => c.CubeGrid != Me.CubeGrid)
                                              .Select(i => i.GetInventory(0)).ToList();

            var targetInventory = containers.Where(c => (!c.Closed) && c.CustomName.Contains(oreStorageName))
                                            .Select(i => i.GetInventory(0))
                                            .Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            Echo($"Ext conts {externalInventory.Count}");

            if ((!targetInventory.Any()) || (!externalInventory.Any()))
            {
                Echo("------No items to transfer-----");
                return;
            }

            foreach (var cargo in externalInventory)
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

            var targetInventory = containers.Where(c => (!c.Closed) && c.CustomName.Contains(componentsStorageName))
                                            .Select(i => i.GetInventory(0))
                                            .Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            var assInventory = assemblers.Where(a => !a.Closed)
                                         .Select(i => i.GetInventory(1))
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
          
            foreach(var dict in partsDictionary)
            {
                dict.Value.Current = 0;
            }
            foreach(var dict in ammoDictionary)
            {
                dict.Value.Current = 0;
            }
            foreach (var dict in buildedIngnotsDictionary)
            {
                dict.Value.Current = 0;
            }

            // partsDictionary.Clear();
            // ammoDictionary.Clear();
            //buildedIngnotsDictionary.Clear();

            var partsInventorys = containers.Where(c => (!c.Closed) && c.CustomName.Contains(componentsStorageName))
                                            .Select(i => i.GetInventory(0));

            freePartsStorageVolume = partsInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalPartsStorageVolume = partsInventorys.Sum(i => i.MaxVolume.ToIntSafe());

            double precentageVolume = Math.Round(((double)freePartsStorageVolume / (double)totalPartsStorageVolume) * 100, 1);

            foreach (var inventory in partsInventorys)
            {
                inventory.GetItems(productionItems);

                foreach (var item in productionItems)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Component")//части
                    {
                        if (partsDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            partsDictionary[item.Type.SubtypeId].Current += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            partsDictionary.Add(item.Type.SubtypeId, new ItemBalanser { Current = item.Amount.ToIntSafe() });
                        }
                    }

                    if (item.Type.TypeId == "MyObjectBuilder_AmmoMagazine")//Боеприпасы
                    {
                        if (ammoDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            ammoDictionary[item.Type.SubtypeId].Current += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            ammoDictionary.Add(item.Type.SubtypeId, new ItemBalanser { Current = item.Amount.ToIntSafe() });
                        }
                    }

                    if ((item.Type.TypeId == "MyObjectBuilder_Ingot") || (item.Type.TypeId == "MyObjectBuilder_Ore"))//Построенные слитки 
                    {
                        if (buildedIngnotsDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            buildedIngnotsDictionary[item.Type.SubtypeId].Current += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            buildedIngnotsDictionary.Add(item.Type.SubtypeId, new ItemBalanser { Current = item.Amount.ToIntSafe() });
                        }
                    }
                }
                productionItems.Clear();
            }

            //Блок считывания и заполнения данных о количестве компонентов с дисплея
            partsDisplayData.Clear();
            partsPanel?.ReadText(partsDisplayData);

            var str = partsDisplayData.ToString().Split('\n');

            //debugPanel?.WriteText("", false);
            foreach (var st in str)
            {
                string name = "";
                string count = "";

                System.Text.RegularExpressions.Match match = NameRegex.Match(st);

                if (match.Success)
                {
                    name = match.Groups["Name"].Value;
                }

                match = AmountRegex.Match(st);

                if (match.Success)
                {
                    count = match.Groups["Amount"].Value;
                }

                int amount = 0;

                if (int.TryParse(count, out amount))
                {
                   // debugPanel?.WriteText("\n" + name + "" + count + "XX" + amount, true);

                    if (partsDictionary.ContainsKey(name))
                    {
                        partsDictionary[name].Requested = amount;
                    }

                    if (buildedIngnotsDictionary.ContainsKey(name))
                    {
                        buildedIngnotsDictionary[name].Requested = amount;
                    }

                }
            }

            string sysState = useAutoBuildSystem == true ? "Auto mode ON" : "Auto mode OFF";
            partsPanel?.WriteText("", false);
            partsPanel?.WriteText($"<<-------------Production------------->>" +
                                  $"\n{sysState}" +
                                  $"\nContainers:{partsInventorys.Count()}" +
                                  $"\nVolume: {precentageVolume} % {freePartsStorageVolume} / {totalPartsStorageVolume} T" +
                                  "\n<<-----------Parts----------->>", true);


            foreach (var dict in partsDictionary.OrderBy(k => k.Key))
            {
                if (blueprintData.ContainsKey(dict.Key))
                {
                    partsPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current}. / {dict.Value.Requested}", true);
                }
                else
                {
                    partsPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} / {dict.Value.Requested}", true);
                }
            }

            partsPanel?.WriteText("\n<<-----------Ammo----------->>", true);

            foreach (var dict in ammoDictionary.OrderBy(k => k.Key))
            {
                if (blueprintData.ContainsKey(dict.Key))
                {
                    partsPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current}. / {dict.Value.Requested}", true);
                }
                else
                {
                    partsPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} / {dict.Value.Requested}", true);
                }
            }

            partsPanel?.WriteText("\n<<-----------As Ingnot----------->>", true);

            foreach (var dict in buildedIngnotsDictionary.OrderBy(k => k.Key))
            {
                if (blueprintData.ContainsKey(dict.Key))
                {
                    partsPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current}. / {dict.Value.Requested}", true);
                }
                else
                {
                    partsPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} / {dict.Value.Requested}", true);
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

            maxStoredPower = batteries.Where(b => !b.Closed).Sum(b => b.MaxStoredPower);
            currentStoredPower = batteries.Where(b => !b.Closed).Sum(b => b.CurrentStoredPower);

            inputPower = batteries.Where(b => !b.Closed).Sum(b => b.CurrentInput);
            outputPower = batteries.Where(b => !b.Closed).Sum(b => b.CurrentOutput);

            generatorsMaxOutputPower = generators.Where(g => !g.Closed).Sum(g => g.MaxOutput);
            generatorsOutputPower = generators.Where(g => !g.Closed).Sum(g => g.CurrentOutput);

            powerLoadPercentage = (float)Math.Round(generatorsOutputPower / generatorsMaxOutputPower * 100, 1);

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

            var reactorInventory = generators.Where(g => !g.Closed && g.HasInventory).Select(g => g.GetInventory(0)).ToList();
            int reactorsCount = generators.Where(g => g is IMyReactor).Count();
            int windCount = generators.Where(g => g.BlockDefinition.TypeId.ToString() == "MyObjectBuilder_WindTurbine").Count();
            int gasCount = generators.Where(g => g.BlockDefinition.TypeId.ToString() == "MyObjectBuilder_HydrogenEngine").Count();

            detailedPowerPanel?.WriteText("<--------Gens status--------->", true);
            detailedPowerPanel?.WriteText($"\nWind: {windCount} React: {reactorsCount} Gas: {gasCount}", true);

            foreach (var react in reactorInventory)
            {
                if (react.ItemCount != 0)
                {
                    var block = react.Owner as IMyReactor;

                    var item = react.GetItemAt(0);

                    if (item != null)
                    {
                        detailedPowerPanel?.WriteText($"\nR:{item.Value.Type.SubtypeId} / {item.Value.Amount}", true);

                        if (reactorPayloadLimitter)
                        {
                            //if (item.Value.Type.SubtypeId == "Uranium")
                            //{
                            //    if (item.Value.Amount > maxReactorPayload)
                            //    {
                            //        block.UseConveyorSystem = false;
                            //    }
                            //    else
                            //    {
                            //        block.UseConveyorSystem = true;
                            //    }
                            //}
                        }
                    }
                }
                else
                {
                    var targInv = react.Owner as IMyReactor;
                    targInv.UseConveyorSystem = true;
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
            powerPanel?.WriteText("<--------Power status--------->", true);
            powerPanel?.WriteText($"\nBatteryStatus:" +
                                   $"\nPower Load: {powerLoadPercentage} %" +
                                   $"\nTotal/Max stored:{Math.Round(currentStoredPower, 2)} / {maxStoredPower} MWt {Math.Round(currentStoredPower / maxStoredPower * 100, 1)} %"
                                 + $"\nInput/Output:{Math.Round(inputPower, 2)} / {Math.Round(outputPower, 2)} {(inputPower > outputPower ? "+" : "-")} MWt/h "
                                 + $"\nGens maxOut/Out: {Math.Round(generatorsMaxOutputPower, 2)} / {Math.Round(generatorsOutputPower, 2)} MWT", true);


        }


        /// <summary>
        /// Очистка сборщиков в случае если простой более нескольких циклов
        /// </summary>
        public void AssemblersClear()
        {
            var loadedAssemblers = assemblers.Where(ass => !ass.Closed || !ass.IsQueueEmpty || !ass.IsProducing).ToList();
            if (loadedAssemblers.Any())
                assemblersDeadlockTick++;

            if (assemblersDeadlockTick > 5)
            {
                foreach (var ass in loadedAssemblers)
                {
                    ass.ClearQueue();
                    assemblersDeadlockTick = 0;
                }
            }
        }

        /// <summary>
        /// Сислема заказа производства недостающих компонентов INOP
        /// </summary>
        public void PartsAutoBuild()
        {
            if (!useAutoBuildSystem)
                return;

            needReplaceParts = true;

            var desAss = specialAssemblers.Where(ass => ass.Mode == MyAssemblerMode.Disassembly);
            if (desAss.Any())
                return;

            var freeAssemblers = specialAssemblers.Where(ass => ass.Closed || !ass.IsQueueEmpty || ass.OutputInventory.ItemCount > 0).ToList();
            if (freeAssemblers.Any())
                return;

            Echo("------Auto build system-------");

            foreach(var key in partsDictionary)
            {
                if (key.Value.Current < key.Value.Requested)
                {
                    AddItemToProduct(key);
                }
            }

            foreach (var key in buildedIngnotsDictionary)
            {
                if (key.Value.Current < key.Value.Requested)
                {
                    AddItemToProduct(key);
                }
            }

            monitor.AddInstructions("");
        }//PartsAutoBuild()

        private void AddItemToProduct(KeyValuePair<string,ItemBalanser> key)
        {
            var needed = key.Value.Requested - key.Value.Current;

            var freeAssemblers = specialAssemblers;

            var bd = blueprintData.Where(k => k.Key.Contains(key.Key));

            if (!bd.Any())
            {
                Echo($"WARNING no blueprint: {key.Key}");
                nanobuildReady = false;
                return;
            }

            string name = "MyObjectBuilder_BlueprintDefinition/" + bd.FirstOrDefault().Value;

            MyDefinitionId blueprint;

            if (!MyDefinitionId.TryParse(name, out blueprint))
            {
                Echo($"WARNING cant parse: {name}");
                return;
            }

            var availAss = freeAssemblers.Where(ass => ass.CanUseBlueprint(blueprint)).ToList();
            if (!availAss.Any())
                return;

            var count = needed / availAss.Count;

            if (count < 1)
            {
                availAss[0].AddQueueItem(blueprint, (VRage.MyFixedPoint)1);
            }
            else
            {
                foreach (var ass in availAss)
                {
                    VRage.MyFixedPoint amount = (VRage.MyFixedPoint)count;
                    ass.AddQueueItem(blueprint, amount);
                }
            }


        }

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

            if (nanobotBuildQueue.Any())
                AddNanobotPartsToProduct();

            monitor.AddInstructions("");
        }

        public void AddNanobotPartsToProduct()
        {
           
            nanobuildReady = true;

            foreach (var ass in specialAssemblers)
            {
                if(!ass.IsProducing)
                {
                    ass.ClearQueue();
                }
            }

            var freeAssemblers = specialAssemblers.Where(ass => (!ass.Closed) && ass.IsQueueEmpty).ToList();
            if (!freeAssemblers.Any())
                return;

            foreach (var bps in nanobotBuildQueue)
            {
                var bd = blueprintData.Where(k => k.Key.Contains(bps.Key.SubtypeName));

                if (!bd.Any())
                {
                    Echo($"WARNING no blueprint: {bps.Key.SubtypeName}");
                    nanobuildReady = false;
                    continue;
                }

                string name = "MyObjectBuilder_BlueprintDefinition/" + bd.FirstOrDefault().Value;

                MyDefinitionId blueprint;

                if (!MyDefinitionId.TryParse(name, out blueprint))
                {
                    Echo($"WARNING cant parse: {name}");
                    continue;
                }

                var availAss = freeAssemblers.Where(ass => ass.CanUseBlueprint(blueprint)).ToList();
                if (!availAss.Any())
                    return;

                var count = bps.Value / availAss.Count;
                if (count < 1)
                    count = 1;

                foreach(var ass in availAss)
                {
                    VRage.MyFixedPoint amount = (VRage.MyFixedPoint)count;
                    ass.AddQueueItem(blueprint, amount);
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

            string sysState = nanobuildReady == true ? $"\nBlock:{nanobotBuildModule.CustomName}\nWork: {useNanobotAutoBuild}" : "\nNanobuild failed!!! check PC!!";

            nanobotDisplay?.WriteText("", false);
            nanobotDisplay?.WriteText("<<-----------Nanobot module----------->>", true);
            nanobotDisplay?.WriteText(sysState, true);

            foreach (var comp in nanobotBuildQueue.OrderBy(c => c.Key.ToString()))
            {
                nanobotDisplay?.WriteText($"\n{comp.Key.SubtypeName} X {comp.Value}", true);
            }

            monitor.AddInstructions("");
        }

        /// <summary>
        /// Начать/остановить поиск черчежей
        /// </summary>
        public void SwitchBlueprintGetter()
        {
            var ass = assemblers.Where(q => q.CustomName.Contains(assemblersBlueprintLeanerName)).First();

            if (ass == null)
            {
                assemblerBlueprintGetter = false;
                return;
            }

            assemblerBlueprintGetter = !assemblerBlueprintGetter;

            if (assemblerBlueprintGetter)
            {
                needReplaceParts = false;
                useAutoBuildSystem = false;
                SpecialAssemblerLastName = ass.CustomName;
                ass.CustomName = assemblersBlueprintLeanerName + "Assembler ready to copy bps";
                ass.ClearQueue();
               // ass.SetValueBool("OnOff", false);
                ass.Enabled = false;
            }
            else
            {
                ass.CustomName = SpecialAssemblerLastName;
                ass.ClearQueue();
               // ass.SetValueBool("OnOff", true);
                ass.Enabled = true;
            }
        }

        /// <summary>
        /// Сохранение названий черчежей компонентов, в ручном режиме
        /// </summary>
        public void SaveAllBlueprintsNames()
        {
            if (!assemblerBlueprintGetter)
                return;

            debugPanel?.WriteText("", false);
            debugPanel?.WriteText("<--------Production blocks--------->\n", true);

            var targetInventory = containers.Where(c => c.CustomName.Contains(componentsStorageName))
                                            .Select(i => i.GetInventory(0))
                                            .Where(i => !i.IsFull);

            var blueprints = new List<MyProductionItem>();
            var ass = assemblers.Where(q => q.CustomName.Contains(assemblersBlueprintLeanerName)).First();
            if (ass == null)
                return;

            var assInv = ass.GetInventory(1);

            ass.CustomName = assemblersBlueprintLeanerName + "Assembler ready to copy bps";
            ass.GetQueue(blueprints);

            if (blueprints.Count > 1)
            {
                ass.ClearQueue();
                return;
            }

            if (!ass.IsQueueEmpty)
            {
                lastDetectedBlueprintItem = blueprints[0];
                ass.CustomName = assemblersBlueprintLeanerName + $"bps:{lastDetectedBlueprintItem.BlueprintId.SubtypeName}";
                // ass.SetValueBool("OnOff", true);
                ass.Enabled = true;
                return;
            }

            if (assInv.ItemCount == 0)
                return;

            lastDetectedConstructItem = assInv.GetItemAt(assInv.ItemCount - 1);
            if (lastDetectedConstructItem == null)
                return;

            ass.CustomName = assemblersBlueprintLeanerName + $"item:{lastDetectedConstructItem.Value.Type.SubtypeId}";

            if (assInv.TransferItemTo(targetInventory.First(), 0, null, true))
                ass.Enabled = false;
               // ass.SetValueBool("OnOff", false);

            if (!blueprintData.ContainsKey(lastDetectedConstructItem.Value.Type.SubtypeId))
            {
                blueprintData.Add(lastDetectedConstructItem.Value.Type.SubtypeId, lastDetectedBlueprintItem.BlueprintId.SubtypeId.ToString());
            }
            else
            {
                blueprintData[lastDetectedConstructItem.Value.Type.SubtypeId] = lastDetectedBlueprintItem.BlueprintId.SubtypeId.ToString();
            }

            foreach (var key in blueprintData)
            {
                debugPanel?.WriteText($"ITEM:{key.Key} X BP: {key.Value} \n", true);
                dataSystem.Set("Blueprints", key.Key, key.Value);
            }

            ReloadData();
            monitor.AddInstructions("");
        }

        public class ItemBalanser
        {
            public int Current { set; get; } = 0;
            public int Requested { set; get; } = 0;
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

            public void Draw()
            {
                mainDisplay.WriteText("", false);
                mainDisplay.WriteText($"CUR ins: {TotalInstructions} / Max: {MaxInstructions}" +
                                      $"\nAV inst: {AverageInstructionsPerTick} / {MaxInstructionsPerTick}" +
                                      $"\nAV time:{AverageTimePerTick}", true);
            }

        }
        

        ///END OF SCRIPT///////////////
    }

}

//[Blueprints]
//AluminiumPlate = AluminiumPlate
//BulletproofGlass = BulletproofGlass
//CementMixItem = CementMixItem
//Computer = ComputerComponent
//Construction = ConstructionComponent
//CopperPlate = CopperPlate
//Detector = DetectorComponent
//Display = Display
//Explosives = ExplosivesComponent
//Girder = GirderComponent
//GravityGenerator = GravityGeneratorComponent
//InteriorPlate = InteriorPlate
//LargeTube = LargeTube
//Medical = MedicalComponent
//MediumCopperTube = MediumCopperTube
//MetalGrid = MetalGrid
//Motor = MotorComponent
//PowerCell = PowerCell
//RadioCommunication = RadioCommunicationComponent
//Reactor = ReactorComponent
//ShieldComponent = ShieldComponentBP
//SmallCopperTube = SmallCopperTube
//SmallTube = SmallTube
//SolarCell = SolarCell
//SteelBolt = SteelBolt
//SteelPlate = SteelPlate
//Superconductor = Superconductor
//Thrust = ThrustComponent
//AluminiumFanBlade = AluminiumFanBlade
//BallBearing = BallBearing
//CeramicPlate = CeramicPlate
//Chain = Chain
//CopperZincPlate = CopperZincPlate
//DenseSteelPlate = DenseSteelPlate
//LargeCopperTube = LargeCopperTube
//Neutronium = Neutronium
//PolyCarbonatePlate = PolyCarbonatePlate
//PulseCannonConstructionBoxS = PulseCannonConstructionComponentS
//Reinforced_Mesh = Reinforced_Mesh
//ReinforcedPlate = ReinforcedPlate
//LaserConstructionBoxS = SmallLaserConstructionComponentS
//SteelPulley = SteelPulley
//SteelSpring = SteelSpring
//StrongCopperPlate = StrongCopperPlate
//AdvancedReactorBundle = AdvancedReactorBundle
//Cutters = Cutters
//DiamondTooling = DiamondTooling
//Filter = Filter
//KC_Component = KC_Component
//LaserConstructionBoxL = LaserConstructionComponent
//Octocore = OctocoreComponent
//PulseCannonConstructionBoxL = PulseCannonConstructionComponent
//TitaniumAlloyPlate = TitaniumAlloyPlate
//TitaniumPlate = TitaniumPlate
//StrongTitanPlate = TitanPlate
//TungstenAlloyPlate = TungstenAlloyPlate
//dcZincPlate = ZincPlate
//AdvancedThrustModule = AdvancedThrustModule
//ArcReactorcomponent = ArcReactorcomponent
//CryoPump = CryoPump
//K-Crystal_Interface = K-Crystal_Interface
//largehydrogeninjector = largehydrogeninjector
//Nadium_Radioactive = Radioactive_Nadium_Ingot
//Trinium = Trinium
//Xenucore = XenucoreComponent
//Dynamite = Dynamite
//RTG_Plutonium238 = Plutonium238_RTG_BPDef
//BareBrassWire = BareBrassWire
//BrazingRod = BrazingRod
//CapacitorBank = CapacitorBankComponent
//CoolingHeatsink = CoolingHeatsinkComponent
//Diode = Diode
//EnergyCristal = EnergyCristal
//FocusPrysm = FocusPrysmComponent
//HeatSink-T2 = HeatSink-T2
//LeadAcidCell = LeadAcidCell
//LiIonCell = LiIonCell
//LithiumPowerCell = LithiumPowerCell
//PowerCoupler = PowerCouplerComponent
//PWMCircuit = PWMCircuitComponent
//Resistor = Resistor
//SafetyBypass = SafetyBypassComponent
//Sensor = Sensor
//ShieldFrequencyModule = ShieldFrequencyModuleComponent
//Transistor = Transistor
//WarpCell = WarpCell
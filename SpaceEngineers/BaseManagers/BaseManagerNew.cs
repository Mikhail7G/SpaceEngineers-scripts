using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript.BaseManager.BaseNew
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
        bool useRefinereyPriorty = false;
        bool reactorPayloadLimitter = false;

        int totalOreStorageVolume = 0;
        int freeOreStorageVolume = 0;

        int totalIngnotStorageVolume = 0;
        int freeIngnotStorageVolume = 0;

        int totalPartsStorageVolume = 0;
        int freePartsStorageVolume = 0;

        int currentTick = 0;

        int refinereyReloadPrecentage = 70;
        int maxVolumeContainerPercentage = 95;

        float fontSize = 0.8f;

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
        Dictionary<string, int> partsIngotAndOresDictionary;

        Dictionary<string, ItemBalanser> ingotsDict;
        Dictionary<string, ItemBalanser> partsDictionary;
        Dictionary<string, ItemBalanser> ammoDictionary;
        Dictionary<string, ItemBalanser> buildedIngnotsDictionary;

        Dictionary<string, OrePriority> oresDict;

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
        StringBuilder oreDisplayData;


        //Поиск чертежей
        MyProductionItem lastDetectedBlueprintItem;
        MyInventoryItem? lastDetectedConstructItem;

        Dictionary<MyDefinitionId, int> nanobotBuildQueue;

        // ^(?<Name>\w*\W?\w*)\s:\s\d*?\W*?\s?(?<Amount>\d+)$ общий имя + необходимое кол-во
        System.Text.RegularExpressions.Regex NameRegex = new System.Text.RegularExpressions.Regex(@"(?<Name>\w*\-?\w*)\s:");//(?<Name>\w*)\s: || (?<Name>^\w*\-*\s*\w*)\s: - c пробелами и подчеркиваниями
        System.Text.RegularExpressions.Regex AmountRegex = new System.Text.RegularExpressions.Regex(@"(?<Amount>\d+)$");

        //Имя и приоритет руды, ищет по всем строкам
        System.Text.RegularExpressions.Regex OrePriorRegex = new System.Text.RegularExpressions.Regex(@"^(?<Name>\w*\W?\w*)\s:\s\w*?\sP\s?(?<Prior>\d+)\s?$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled);
        //Имя и количество компонентов для автостройки
        System.Text.RegularExpressions.Regex ProdItemFullRegex = new System.Text.RegularExpressions.Regex(@"^(?<Name>\w*\W?\w*)\s:\s\d*?\W*?\s?(?<Amount>\d+)$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled);


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
            oreDisplayData = new StringBuilder();

            oresDict = new Dictionary<string, OrePriority>();
            ingotsDict = new Dictionary<string, ItemBalanser>();
            partsDictionary = new Dictionary<string, ItemBalanser>();
            partsIngotAndOresDictionary = new Dictionary<string, int>();
            nanobotBuildQueue = new Dictionary<MyDefinitionId, int>();
            ammoDictionary = new Dictionary<string, ItemBalanser>();
            buildedIngnotsDictionary = new Dictionary<string, ItemBalanser>();

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
            Load();

            if (autorun)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                Echo($"Script autostart in prog");
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

        public void Save()
        {
            MyIni oreData = new MyIni();
            Storage = null;

            oreData.AddSection("Ores");
            oreData.AddSection("Ingots");
            oreData.AddSection("Parts");
            oreData.AddSection("Ammo");
            oreData.AddSection("BuildIngots");

            foreach (var dict in oresDict)
            {
                oreData.Set("Ores", dict.Key, dict.Value.Priority);
            }

            //Ingot
            foreach (var dict in ingotsDict)
            {
                oreData.Set("Ingots", dict.Key, dict.Value.Current);
            }

            //Parts
            foreach (var dict in partsDictionary)
            {
                oreData.Set("Parts", dict.Key, dict.Value.Current);
            }

            //Ammo
            foreach (var dict in ammoDictionary)
            {
                oreData.Set("Ammo", dict.Key, dict.Value.Current);
            }

            //Builded ingots
            foreach (var dict in buildedIngnotsDictionary)
            {
                oreData.Set("BuildIngots", dict.Key, dict.Value.Current);
            }





            //Ores as Ingots
            //foreach (var dict in partsIngotAndOresDictionary)
            //{
            //    oreData.Set("Ingots", dict.Key, dict.Value);
            //}

            Storage = oreData.ToString();
        }

        public void Load()
        {
            MyIni oreData = new MyIni();

            if (oreData.TryParse(Storage))
            {
                Echo("Load internal data started");

                List<MyIniKey> keys = new List<MyIniKey>();
                oreData.GetKeys("Ores", keys);

                foreach (var key in keys)
                {
                    oresDict.Add(key.Name, new OrePriority() { Priority = oreData.Get(key).ToInt32() });
                }

                keys.Clear();
                oreData.GetKeys("Ingots", keys);

                foreach (var key in keys)
                {
                    ingotsDict.Add(key.Name, new ItemBalanser { Current = oreData.Get(key).ToInt32() });
                }

                keys.Clear();
                oreData.GetKeys("Parts", keys);

                foreach (var key in keys)
                {
                    partsDictionary.Add(key.Name, new ItemBalanser { Current = oreData.Get(key).ToInt32() });
                }

                keys.Clear();
                oreData.GetKeys("Ammo", keys);

                foreach (var key in keys)
                {
                    ammoDictionary.Add(key.Name, new ItemBalanser { Current = oreData.Get(key).ToInt32() });
                }

                keys.Clear();
                oreData.GetKeys("BuildIngots", keys);

                foreach (var key in keys)
                {
                    buildedIngnotsDictionary.Add(key.Name, new ItemBalanser { Current = oreData.Get(key).ToInt32() });
                }

                Echo("Load internal data OK");

            }

        }

        public void Commands(string str)
        {
            string argument = str.ToUpper();

            switch (argument)
            {
                case "SAVEBPS":
                    Echo("Try save bps names");
                    SwitchBlueprintGetter();
                    break;
            }
        }


        public void Update()
        {
         
            DrawEcho();

            SaveAllBlueprintsNames();

            switch (currentTick)
            {
                case 0:
                    FindLcds();
                    FindInventories();
                    break;
                case 1:
                    DisplayOres();
                    RefinereysPrintData();
                    break;
                case 2:
                    DisplayIngnots();
                    break;
                case 3:
                    DisplayParts();
                    break;

            }

            currentTick++;
            if (currentTick == 6)
                currentTick = 0;

            monitor.AddInstructions("");
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
                useRefinereyPriorty = dataSystem.Get("Operations", "UseRefinereyPriortty").ToBoolean();
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

                //INOP
                foreach (var key in keys)
                {
                    orePriority.Add(key.Name, dataSystem.Get(key).ToInt32());
                }

                keys.Clear();

                // Blueprints
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
                dataSystem.Set("Operations", "UseRefinereyPriortty", false);
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

        /// <summary>
        /// Перезапись настроек при изменении в поцессе работы скрипта
        /// </summary>
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
            dataSystem.Set("Operations", "UseRefinereyPriortty", useRefinereyPriorty);
            dataSystem.Set("Operations", "ReactorFuelLimitter", reactorPayloadLimitter);

            dataSystem.Set("DisplaysNames", "lcdInventoryOresName", lcdInventoryOresName);
            dataSystem.Set("DisplaysNames", "lcdInventoryIngnotsName", lcdInventoryIngnotsName);
            dataSystem.Set("DisplaysNames", "lcdPowerSystemName", lcdPowerSystemName);
            dataSystem.Set("DisplaysNames", "lcdPowerDetailedName", lcdPowerDetailedName);
            dataSystem.Set("DisplaysNames", "lcdPartsName", lcdPartsName);
            dataSystem.Set("DisplaysNames", "NanobotDisplayName", lcdNanobotName);
            dataSystem.Set("DisplaysNames", "lcdInventoryDebugName", lcdInventoryDebugName);
            dataSystem.Set("DisplaysNames", "lcdRefinereyName", lcdRefinereyName);

            dataSystem.Set("ContainerNames", "oreStorageName", oreStorageName);
            dataSystem.Set("ContainerNames", "ingnotStorageName", ingnotStorageName);
            dataSystem.Set("ContainerNames", "componentsStorageName", componentsStorageName);

            dataSystem.Set("TagsNames", "assemblersSpecialOperationsTagName", assemblersSpecialOperationsName);
            dataSystem.Set("TagsNames", "assemblersBlueprintLeanerName", assemblersBlueprintLeanerName);


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

            if ((ingnotPanel == null) || (ingnotPanel.Closed))
            {
                //Echo($"Try find:{lcdInventoryIngnotsName}");
                ingnotPanel = GridTerminalSystem.GetBlockWithName(lcdInventoryIngnotsName) as IMyTextPanel;
                if (ingnotPanel != null)
                {
                    ingnotPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    ingnotPanel.FontSize = fontSize;
                }
            }

            if ((powerPanel == null) || (powerPanel.Closed))
            {
                // Echo($"Try find:{lcdPowerSystemName}");
                powerPanel = GridTerminalSystem.GetBlockWithName(lcdPowerSystemName) as IMyTextPanel;
                if (powerPanel != null)
                {
                    powerPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    powerPanel.FontSize = fontSize;
                }
            }

            if ((detailedPowerPanel == null) || (detailedPowerPanel.Closed))
            {
                //Echo($"Try find:{lcdPowerDetailedName}");
                detailedPowerPanel = GridTerminalSystem.GetBlockWithName(lcdPowerDetailedName) as IMyTextPanel;
                if (detailedPowerPanel != null)
                {
                    detailedPowerPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    detailedPowerPanel.FontSize = fontSize;
                }
            }



            if ((partsPanel == null) || (partsPanel.Closed))
            {
                //Echo($"Try find:{lcdPartsName}");
                partsPanel = GridTerminalSystem.GetBlockWithName(lcdPartsName) as IMyTextPanel;
                if (partsPanel != null)
                {
                    partsPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    partsPanel.FontSize = fontSize;
                }
            }

            if ((nanobotDisplay == null) || (nanobotDisplay.Closed))
            {
                // Echo($"Try find:{lcdNanobotName}");
                nanobotDisplay = GridTerminalSystem.GetBlockWithName(lcdNanobotName) as IMyTextPanel;
                if (nanobotDisplay != null)
                {
                    nanobotDisplay.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    nanobotDisplay.FontSize = fontSize;
                }
            }

            if ((refinereysDisplay == null) || (refinereysDisplay.Closed))
            {
                // Echo($"Try find:{lcdRefinereyName}");
                refinereysDisplay = GridTerminalSystem.GetBlockWithName(lcdRefinereyName) as IMyTextPanel;
                if (refinereysDisplay != null)
                {
                    refinereysDisplay.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    refinereysDisplay.FontSize = fontSize;
                }
            }

            if ((oreDisplay == null) || (oreDisplay.Closed))
            {
                // Echo($"Try find:{lcdInventoryOresName}");
                oreDisplay = GridTerminalSystem.GetBlockWithName(lcdInventoryOresName) as IMyTextPanel;
                if (oreDisplay != null)
                {
                    oreDisplay.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    oreDisplay.FontSize = fontSize;
                }
            }

        }

        /// <summary>
        /// Печатаем текст в терминал блока
        /// </summary>
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

            refinereys = blocks.Where(b => b is IMyRefinery)
                               .Where(r => r.IsFunctional)
                               .Where(b => b.CubeGrid == Me.CubeGrid)
                               .Select(t => t as IMyRefinery).ToList();

            assemblers = blocks.Where(b => b is IMyAssembler)
                               .Where(a => a.IsFunctional)
                               .Where(b => b.CubeGrid == Me.CubeGrid)
                               .Select(t => t as IMyAssembler).ToList();

            containers = blocks.Where(b => b is IMyCargoContainer)
                               .Where(c => c.IsFunctional)
                               .Select(t => t as IMyCargoContainer).ToList();

            batteries = blocks.Where(b => b is IMyBatteryBlock)
                              .Where(b => b.CubeGrid == Me.CubeGrid)
                              .Where(b => b.IsFunctional)
                              .Select(t => t as IMyBatteryBlock).ToList();

            gasTanks = blocks.Where(b => b is IMyGasTank)
                             .Where(b => b.CubeGrid == Me.CubeGrid)
                             .Where(g => g.IsFunctional)
                             .Select(t => t as IMyGasTank).ToList();

            generators = blocks.Where(b => b is IMyPowerProducer)
                               .Where(b => b.CubeGrid == Me.CubeGrid)
                               .Where(r => r.IsFunctional)
                               .Select(t => t as IMyPowerProducer).ToList();

            nanobotBuildModule = blocks.Where(b => b.IsFunctional)
                                       .Where(g => g.BlockDefinition.SubtypeName.ToString() == "SELtdLargeNanobotBuildAndRepairSystem")
                                       .Where(n => n.CubeGrid == Me.CubeGrid).FirstOrDefault();

            specialAssemblers = assemblers.Where(a => a.CustomName.Contains(assemblersSpecialOperationsName)).ToList();

        }

        /// <summary>
        /// Отображение руды в контейнерах
        /// </summary>
        public void DisplayOres()
        {
            var oreInventory = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(oreStorageName)))
                                         .Select(i => i.GetInventory(0))
                                         .Where(i => i.ItemCount > 0).ToList();

            if (!oreInventory.Any())
            {
                oreDisplay?.WriteText("", false);
                oreDisplay?.WriteText($"<<-----------Ores----------->>" +
                                      $"\nUse prior:{useRefinereyPriorty} INOP" +
                                      $"\nContainers:{oreInventory.Count}" +
                                      $"\nVolume: {0} % {freeOreStorageVolume} / {totalOreStorageVolume} T", true);

                return;
            }

            Echo($"Ore conts: {oreInventory.Count}");

            foreach (var key in oresDict)
            {
                key.Value.Amount = 0;
            }

            freeOreStorageVolume = oreInventory.Sum(i => i.CurrentVolume.ToIntSafe());
            totalOreStorageVolume = oreInventory.Sum(i => i.MaxVolume.ToIntSafe());

            double precentageVolume = Math.Round(((double)freeOreStorageVolume / (double)totalOreStorageVolume) * 100, 1);

            foreach (var inv in oreInventory)
            {
                inv.GetItems(oreItems);

                foreach (var item in oreItems)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Ore")
                    {
                        if (oresDict.ContainsKey(item.Type.SubtypeId))
                        {
                            oresDict[item.Type.SubtypeId].Amount += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            oresDict.Add(item.Type.SubtypeId, new OrePriority
                            {
                                Amount = item.Amount.ToIntSafe(),
                                Priority = 0
                            });

                        }
                    }
                }
                oreItems.Clear();
            }

            if (oreDisplay == null)
                return;

            //Блок считывания приоритета с дисплея
            if (useRefinereyPriorty)
            {
                oreDisplayData.Clear();
                oreDisplay?.ReadText(oreDisplayData);

                System.Text.RegularExpressions.MatchCollection matches = OrePriorRegex.Matches(oreDisplayData.ToString());

                if (matches.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (oresDict.ContainsKey(match.Groups["Name"].Value))
                        {
                            int prior = 0;

                            if (int.TryParse(match.Groups["Prior"].Value, out prior))
                            {
                                oresDict[match.Groups["Name"].Value].Priority = prior;
                            }
                        }
                    }
                }
            }
            //Отрисовка на дисплей
            oreDisplay?.WriteText("", false);
            oreDisplay?.WriteText($"<<-----------Ores----------->>" +
                                  $"\nUse prior:{useRefinereyPriorty}" +
                                  $"\nContainers:{oreInventory.Count}" +
                                  $"\nVolume: {precentageVolume} % {freeOreStorageVolume} / {totalOreStorageVolume} T", true);

            foreach (var dict in oresDict.OrderBy(k => k.Key))
            {
                oreDisplay?.WriteText($"\n{dict.Key} : {dict.Value.Amount} P {dict.Value.Priority} ", true);
            }
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
                    refinereysDisplay?.WriteText($"\n{bp.BlueprintId.SubtypeName} X Ore:{bp.Amount}", true);
                }
                refinereysDisplay?.WriteText("\n----------", true);

            }
            refinereysItems.Clear();
        }

        /// <summary>
        /// Вывод информации о компонентах на дисплей
        /// </summary>
        public void DisplayParts()
        {
            if (partsPanel == null)
                return;

            totalPartsStorageVolume = 0;
            freePartsStorageVolume = 0;

            foreach (var dict in partsDictionary)
            {
                dict.Value.Current = 0;
            }
            foreach (var dict in ammoDictionary)
            {
                dict.Value.Current = 0;
            }
            foreach (var dict in buildedIngnotsDictionary)
            {
                dict.Value.Current = 0;
            }

            var partsInventorys = containers.Where(c => (!c.Closed) && c.CustomName.Contains(componentsStorageName))
                                          .Select(i => i.GetInventory(0));

            freePartsStorageVolume = partsInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalPartsStorageVolume = partsInventorys.Sum(i => i.MaxVolume.ToIntSafe());

            double precentageVolume = Math.Round(((double)freePartsStorageVolume / (double)totalPartsStorageVolume) * 100, 1);

            //Блок получения всех компонентов в контейнерах
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

                    else if (item.Type.TypeId == "MyObjectBuilder_AmmoMagazine")//Боеприпасы
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

                    else if ((item.Type.TypeId == "MyObjectBuilder_Ingot") || (item.Type.TypeId == "MyObjectBuilder_Ore"))//Построенные слитки 
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
            }//

            //Автосборка компонентов
            if (useAutoBuildSystem)
            {
                partsDisplayData.Clear();
                partsPanel?.ReadText(partsDisplayData);

                System.Text.RegularExpressions.MatchCollection matches = ProdItemFullRegex.Matches(partsDisplayData.ToString());

                if (matches.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        int amount = 0;
                        string name = match.Groups["Name"].Value;

                        if (int.TryParse(match.Groups["Amount"].Value, out amount))
                        {
                            if (partsDictionary.ContainsKey(name))
                            {
                                partsDictionary[name].Requested = amount;
                            }

                            if (buildedIngnotsDictionary.ContainsKey(name))
                            {
                                buildedIngnotsDictionary[name].Requested = amount;
                            }

                            if (ammoDictionary.ContainsKey(name))
                            {
                                ammoDictionary[name].Requested = amount;
                            }
                        }


                    }
                }
            }///

            //Блок вывода инфорации на дисплеи
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

        }

        /// <summary>
        /// Вывод информации о слитках
        /// </summary>
        public void DisplayIngnots()
        {
            if (ingnotPanel == null)
            {
                return;
            }

            foreach (var dict in ingotsDict.ToList())
            {
                dict.Value.Current = 0;
            }

            partsIngotAndOresDictionary.Clear();
            totalIngnotStorageVolume = 0;
            freeIngnotStorageVolume = 0;

            var ingnotInventorys = containers.Where(c => (!c.Closed) && c.CustomName.Contains(ingnotStorageName))
                                             .Select(i => i.GetInventory(0));

            freeIngnotStorageVolume = ingnotInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalIngnotStorageVolume = ingnotInventorys.Sum(i => i.MaxVolume.ToIntSafe());

            double precentageVolume = Math.Round(((double)freeIngnotStorageVolume / (double)totalIngnotStorageVolume) * 100, 1);

            //Проверка контейнеров
            foreach (var inventory in ingnotInventorys)
            {
                inventory.GetItems(ingnotItems);

                foreach (var item in ingnotItems)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Ingot")//слитки 
                    {
                        if (ingotsDict.ContainsKey(item.Type.SubtypeId))
                        {
                            ingotsDict[item.Type.SubtypeId].Current += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            ingotsDict.Add(item.Type.SubtypeId, new ItemBalanser { Current = item.Amount.ToIntSafe() });
                        }
                    }

                    if (item.Type.TypeId == "MyObjectBuilder_Ore")//построенная руда  
                    {
                        if (partsIngotAndOresDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            partsIngotAndOresDictionary[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            partsIngotAndOresDictionary.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                        }
                    }

                }
                ingnotItems.Clear();
            }//

            //Вывод на дисплей
            ingnotPanel?.WriteText("", false);
            ingnotPanel?.WriteText($"<<-----------Ingnots----------->>" +
                                   $"\nContainers:{ingnotInventorys.Count()}" +
                                   $"\nVolume: {precentageVolume} % {freeIngnotStorageVolume} / {totalIngnotStorageVolume} T", true);

            ingnotPanel?.WriteText("\n<<-----------Ingnots----------->>", true);

            foreach (var dict in ingotsDict.OrderBy(k => k.Key))
            {
                ingnotPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} ", true);
            }

            ingnotPanel?.WriteText("\n<<-----------Ores----------->>", true);

            foreach (var dict in partsIngotAndOresDictionary.OrderBy(k => k.Key))
            {
                ingnotPanel?.WriteText($"\n{dict.Key} : {dict.Value} ", true);
            }
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
                useNanobotAutoBuild = false;

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
            //  monitor.AddInstructions("");
        }

      

        public class ItemBalanser
        {
            public int Current { set; get; } = 0;
            public int Requested { set; get; } = 0;
        }

        public class OrePriority
        {
            public int Priority { set; get; } = 0;
            public int Amount { set; get; } = 0;

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

                AverageTimePerTick = avrTime / CallPerTick;
                avrTime = 0;

                CallPerTick = 0;

            }

            public void Draw()
            {
                mainDisplay.WriteText("", false);
                mainDisplay.WriteText($"CUR ins: {TotalInstructions} / Max: {MaxInstructions}" +
                                      $"\nAV inst: {AverageInstructionsPerTick} / {MaxInstructionsPerTick}" +
                                      $"\nAV time:{UpdateTime}", true);
            }

        }


        ///END OF SCRIPT///////////////
    }
}

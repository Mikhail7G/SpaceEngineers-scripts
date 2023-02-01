using Sandbox.Game;
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
using System.Text.RegularExpressions;
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
        string ingotStorageName = "Ingot";
        string componentsStorageName = "Part";
        string ammoStorageName = "Ammo";

        string lcdInventoryOresName = "LCD Ore";
        string lcdInventoryIngotsName = "LCD Inventory";
        string lcdPowerSystemName = "LCD Power";
        string lcdPartsName = "LCD Parts";
        string lcdInventoryDebugName = "LCD Debug";
        string lcdPowerDetailedName = "LCD Power full";
        string lcdNanobotName = "LCD Nano";
        string lcdRefinereyName = "LCD Refinerey";

        string assemblersSpecialOperationsName = "[sp]";
        string assemblersBlueprintLeanerName = "[bps]";


        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        string operationSection = "Operations";
        string namesSection = "DisplaysNames";
        string contSection = "ContainerNames";
        string tagSection = "TagsNames";
        string bpcSection = "Blueprints";

        string autorunDataName = "Autorun";
        string replaceIngotsName = "ReplaceIngots";
        string replacePartsName = "ReplaceParts";
        string powerManagmentName = "PowerManagmentSystem";
        string detailedPowerManagmentName = "DetailedPowerMonitoring";
        string autoBuildSystem = "AutoBuildSystem";
        string transferOresFromOtherName = "TransferOreFromTransports";
        string nanobotOrerationsName = "UseNanobotAutoBuild";
        string refinereyOperationsName = "UseRefinereyOperations";
        string refinereyPriorityName = "UseRefinereyPriortty";
        string reactorFuelLimitterName = "ReactorFuelLimitter";
        string deepContScanName = "DeepContainerScan";

        string oreLCDName = "LcdInventoryOresName";
        string ingotLCDName = "LcdInventoryIngotsName";
        string powerLCDName = "LcdPowerSystemName";
        string powerFullLCDName = "LcdPowerDetailedName";
        string partsLCDName = "LcdPartsName";
        string nanoLCDName = "NanobotDisplayName";
        string debugLCDName = "LcdInventoryDebugName";
        string refinereysLCDName = "LcdRefinereyName";

        string oreContName = "OreStorageName";
        string ingotContName = "IngotStorageName";
        string componentContName = "ComponentsStorageName";
        string ammoContName = "AmmoStorageName";

        string specAssTagName = "AssemblersSpecialOperationsTagName";
        string bpcLearnTagName = "AssemblersBlueprintLeanerName";




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
        IEnumerable<IMyInventory> oreInventories;
        IEnumerable<IMyInventory> partsInventories;
        IEnumerable<IMyInventory> ingnotInventorys;

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
        bool switchProdModulesAsScript = false;
        bool needReplaceIngots = false;
        bool needReplaceParts = false;
        bool usePowerManagmentSystem = false;
        bool useDetailedPowerMonitoring = false;
        bool useAutoBuildSystem = false;
        bool getOreFromTransports = false;
        bool useNanobotAutoBuild = false;
        bool useRefinereysOperations = false;
        bool useRefinereyPriorty = false;
        bool reactorPayloadLimitter = false;
        bool deepScan = false;

        int totalOreStorageVolume = 0;
        int freeOreStorageVolume = 0;

        int totalIngotStorageVolume = 0;
        int freeIngotStorageVolume = 0;

        int totalPartsStorageVolume = 0;
        int freePartsStorageVolume = 0;

        int currentTick = 0;

        int maxReactorPayload = 50;

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

        double precentageOreVolume = 0;
        double precentageIngotsVolume = 0;
        double precentagePartsVolume = 0;

        bool nanobuildReady = true;

        bool assemblerBlueprintGetter = false;
        string SpecialAssemblerLastName = "";

        //Словари компонентов по типам
        Dictionary<string, ItemBalanser> ingotsDict;
        Dictionary<string, ItemBalanser> partsDictionary;
        Dictionary<string, ItemBalanser> ammoDictionary;
        Dictionary<string, ItemBalanser> buildedIngotsDictionary;

        Dictionary<string, OrePriority> oreDictionary;

        Dictionary<string, string> blueprintData;

        // List<string> orePriority;
        Dictionary<string, int> orePriority;

        //Печки
        Dictionary<IMyRefinery, float> refinereyModules;
        Dictionary<string, float> refsUpgradeList;
        List<MyInventoryItem> oreItems;
        List<MyInventoryItem> ingotItems;
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

            oreDictionary = new Dictionary<string, OrePriority>();
            ingotsDict = new Dictionary<string, ItemBalanser>();
            partsDictionary = new Dictionary<string, ItemBalanser>();
            nanobotBuildQueue = new Dictionary<MyDefinitionId, int>();
            ammoDictionary = new Dictionary<string, ItemBalanser>();
            buildedIngotsDictionary = new Dictionary<string, ItemBalanser>();

            blueprintData = new Dictionary<string, string>();

            refsUpgradeList = new Dictionary<string, float>();
            refinereysItems = new List<MyProductionItem>();
            productionItems = new List<MyInventoryItem>();
            ingotItems = new List<MyInventoryItem>();
            refinereyModules = new Dictionary<IMyRefinery, float>();
            orePriority = new Dictionary<string, int>();

            dataSystem = new MyIni();
            monitor = new PerformanceMonitor(this, Me.GetSurface(1));

            GetIniData();
            Load();

            if (autorun)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
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
            MyIni SaveData = new MyIni();
            Storage = null;

            SaveData.AddSection("Ores");
            SaveData.AddSection("Ingots");
            SaveData.AddSection("Parts");
            SaveData.AddSection("Ammo");
            SaveData.AddSection("BuildIngots");

            foreach (var dict in oreDictionary)
            {
                SaveData.Set("Ores", dict.Key, dict.Value.Priority);
            }

            //Ingot
            foreach (var dict in ingotsDict)
            {
                SaveData.Set("Ingots", dict.Key, dict.Value.Current);
            }

            //Parts
            foreach (var dict in partsDictionary)
            {
                SaveData.Set("Parts", dict.Key, dict.Value.Current);
            }

            //Ammo
            foreach (var dict in ammoDictionary)
            {
                SaveData.Set("Ammo", dict.Key, dict.Value.Current);
            }

            //Builded ingots
            foreach (var dict in buildedIngotsDictionary)
            {
                SaveData.Set("BuildIngots", dict.Key, dict.Value.Current);
            }

            Storage = SaveData.ToString();
        }

        public void Load()
        {
            MyIni LoadData = new MyIni();

            if (LoadData.TryParse(Storage))
            {
                Echo("Load internal data started");

                List<MyIniKey> keys = new List<MyIniKey>();
                LoadData.GetKeys("Ores", keys);

                foreach (var key in keys)
                {
                    oreDictionary.Add(key.Name, new OrePriority() { Priority = LoadData.Get(key).ToInt32() });
                }

                keys.Clear();
                LoadData.GetKeys("Ingots", keys);

                foreach (var key in keys)
                {
                    ingotsDict.Add(key.Name, new ItemBalanser { Current = LoadData.Get(key).ToInt32() });
                }

                keys.Clear();
                LoadData.GetKeys("Parts", keys);

                foreach (var key in keys)
                {
                    partsDictionary.Add(key.Name, new ItemBalanser { Current = LoadData.Get(key).ToInt32() });
                }

                keys.Clear();
                LoadData.GetKeys("Ammo", keys);

                foreach (var key in keys)
                {
                    ammoDictionary.Add(key.Name, new ItemBalanser { Current = LoadData.Get(key).ToInt32() });
                }

                keys.Clear();
                LoadData.GetKeys("BuildIngots", keys);

                foreach (var key in keys)
                {
                    buildedIngotsDictionary.Add(key.Name, new ItemBalanser { Current = LoadData.Get(key).ToInt32() });
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
                case "INGOT":
                    SwitchIngotMode();
                    break;
                case "PART":
                    SwitchPartsMode();
                    break;
            }
        }


        public void Update()
        {
            DrawEcho();
            SaveAllBlueprints();

            switch (currentTick)
            {
                case 0:
                    FindLcds();
                    FindInventories();
                    break;
                case 1:
                    FindOres();
                    PrintOres();

                    RefinereysGetData();
                    break;
                case 2:
                    FindIngots();
                    PrintIngots();
                    break;
                case 3:
                    FindParts();
                    ReadPartsData();
                   // PrintParts();
                    break;
                case 4:
                    PowerMangment();
                    PowerSystemDetailed();
                    break;
                case 5:
                    LoadRefinereys();
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

                autorun = dataSystem.Get(operationSection, autorunDataName).ToBoolean();

                needReplaceIngots = dataSystem.Get(operationSection, replaceIngotsName).ToBoolean();
                needReplaceParts = dataSystem.Get(operationSection, replacePartsName).ToBoolean();
                usePowerManagmentSystem = dataSystem.Get(operationSection, powerManagmentName).ToBoolean();
                useDetailedPowerMonitoring = dataSystem.Get(operationSection, detailedPowerManagmentName).ToBoolean();
                useAutoBuildSystem = dataSystem.Get(operationSection, autoBuildSystem).ToBoolean();
                getOreFromTransports = dataSystem.Get(operationSection, transferOresFromOtherName).ToBoolean();
                useNanobotAutoBuild = dataSystem.Get(operationSection, nanobotOrerationsName).ToBoolean();
                useRefinereysOperations = dataSystem.Get(operationSection, refinereyOperationsName).ToBoolean();
                useRefinereyPriorty = dataSystem.Get(operationSection, refinereyPriorityName).ToBoolean();
                reactorPayloadLimitter = dataSystem.Get(operationSection, reactorFuelLimitterName).ToBoolean();
                deepScan = dataSystem.Get(operationSection, deepContScanName).ToBoolean();

                //Containers 
                oreStorageName = dataSystem.Get(contSection, oreContName).ToString();
                ingotStorageName = dataSystem.Get(contSection, ingotContName).ToString();
                componentsStorageName = dataSystem.Get(contSection, componentContName).ToString();
                ammoStorageName = dataSystem.Get(contSection, ammoContName).ToString();

                //Displays 
                lcdInventoryOresName = dataSystem.Get(namesSection, oreLCDName).ToString();
                lcdInventoryIngotsName = dataSystem.Get(namesSection, ingotLCDName).ToString();
                lcdPowerSystemName = dataSystem.Get(namesSection, powerLCDName).ToString();
                lcdPartsName = dataSystem.Get(namesSection, partsLCDName).ToString();
                lcdInventoryDebugName = dataSystem.Get(namesSection, debugLCDName).ToString();
                lcdPowerDetailedName = dataSystem.Get(namesSection, powerFullLCDName).ToString();
                lcdNanobotName = dataSystem.Get(namesSection, nanoLCDName).ToString();
                lcdRefinereyName = dataSystem.Get(namesSection, refinereysLCDName).ToString();

                //Tags
                assemblersSpecialOperationsName = dataSystem.Get(tagSection, specAssTagName).ToString();
                assemblersBlueprintLeanerName = dataSystem.Get(tagSection, bpcLearnTagName).ToString();


                List<MyIniKey> keys = new List<MyIniKey>();
                // Blueprints
                dataSystem.GetKeys(bpcSection, keys);

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

                dataSystem.AddSection(operationSection);
                dataSystem.Set(operationSection, autorunDataName, false);
                dataSystem.Set(operationSection, replaceIngotsName, false);
                dataSystem.Set(operationSection, replacePartsName, false);
                dataSystem.Set(operationSection, powerManagmentName, false);
                dataSystem.Set(operationSection, detailedPowerManagmentName, false);
                dataSystem.Set(operationSection, autoBuildSystem, false);
                dataSystem.Set(operationSection, transferOresFromOtherName, false);
                dataSystem.Set(operationSection, nanobotOrerationsName, false);
                dataSystem.Set(operationSection, refinereyOperationsName, false);
                dataSystem.Set(operationSection, refinereyPriorityName, false);
                dataSystem.Set(operationSection, reactorFuelLimitterName, false);
                dataSystem.Set(operationSection, deepContScanName, false);

                dataSystem.AddSection(namesSection);
                dataSystem.Set(namesSection, oreLCDName, lcdInventoryOresName);
                dataSystem.Set(namesSection, ingotLCDName, lcdInventoryIngotsName);
                dataSystem.Set(namesSection, powerLCDName, lcdPowerSystemName);
                dataSystem.Set(namesSection, powerFullLCDName, lcdPowerDetailedName);
                dataSystem.Set(namesSection, partsLCDName, lcdPartsName);
                dataSystem.Set(namesSection, nanoLCDName, lcdNanobotName);
                dataSystem.Set(namesSection, debugLCDName, lcdInventoryDebugName);
                dataSystem.Set(namesSection, refinereysLCDName, lcdRefinereyName);

                dataSystem.AddSection(contSection);
                dataSystem.Set(contSection, oreContName, oreStorageName);
                dataSystem.Set(contSection, ingotContName, ingotStorageName);
                dataSystem.Set(contSection, componentContName, componentsStorageName);
                dataSystem.Set(contSection, ammoContName, ammoStorageName);

                dataSystem.AddSection(tagSection);
                dataSystem.Set(tagSection, specAssTagName, assemblersSpecialOperationsName);
                dataSystem.Set(tagSection, bpcLearnTagName, assemblersBlueprintLeanerName);

              //  dataSystem.AddSection("OrePriority");

                dataSystem.AddSection(bpcSection);

                Me.CustomData = dataSystem.ToString();
            }

            Echo("Custom data ready");
        }

        /// <summary>
        /// Перезапись настроек при изменении в поцессе работы скрипта
        /// </summary>
        public void ReloadData()
        {
            dataSystem.Set(operationSection, autorunDataName, autorun);
            dataSystem.Set(operationSection, replaceIngotsName, needReplaceIngots);
            dataSystem.Set(operationSection, replacePartsName, needReplaceParts);
            dataSystem.Set(operationSection, powerManagmentName, usePowerManagmentSystem);
            dataSystem.Set(operationSection, detailedPowerManagmentName, useDetailedPowerMonitoring);
            dataSystem.Set(operationSection, autoBuildSystem, useAutoBuildSystem);
            dataSystem.Set(operationSection, transferOresFromOtherName, getOreFromTransports);
            dataSystem.Set(operationSection, nanobotOrerationsName, useNanobotAutoBuild);
            dataSystem.Set(operationSection, refinereyOperationsName, useRefinereysOperations);
            dataSystem.Set(operationSection, refinereyPriorityName, useRefinereyPriorty);
            dataSystem.Set(operationSection, reactorFuelLimitterName, reactorPayloadLimitter);
            dataSystem.Set(operationSection, deepContScanName, deepScan);

            dataSystem.Set(namesSection, oreLCDName, lcdInventoryOresName);
            dataSystem.Set(namesSection, ingotLCDName, lcdInventoryIngotsName);
            dataSystem.Set(namesSection, powerLCDName, lcdPowerSystemName);
            dataSystem.Set(namesSection, powerFullLCDName, lcdPowerDetailedName);
            dataSystem.Set(namesSection, partsLCDName, lcdPartsName);
            dataSystem.Set(namesSection, nanoLCDName, lcdNanobotName);
            dataSystem.Set(namesSection, debugLCDName, lcdInventoryDebugName);
            dataSystem.Set(namesSection, refinereysLCDName, lcdRefinereyName);

            dataSystem.Set(contSection, oreContName, oreStorageName);
            dataSystem.Set(contSection, ingotContName, ingotStorageName);
            dataSystem.Set(contSection, componentContName, componentsStorageName);
            dataSystem.Set(contSection, ammoContName, ammoStorageName);

            dataSystem.Set(tagSection, specAssTagName, assemblersSpecialOperationsName);
            dataSystem.Set(tagSection, bpcLearnTagName, assemblersBlueprintLeanerName);


            Me.CustomData = dataSystem.ToString();
        }


        public void SwitchIngotMode()
        {
            needReplaceIngots = !needReplaceIngots;
        }

        public void SwitchPartsMode()
        {
            needReplaceParts = !needReplaceParts;
        }

        /// <summary>
        /// Включение и выключение печек и сборщиков при выключении/включении скрипта
        /// </summary>
        public void SwitchProductionModules()
        {
            switchProdModulesAsScript = !switchProdModulesAsScript;
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
                ingnotPanel = GridTerminalSystem.GetBlockWithName(lcdInventoryIngotsName) as IMyTextPanel;
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

            string nanoFinded = nanobotBuildModule != null ? "OK" : "NO module";
            Echo($"Nanobot:{nanoFinded}:{nanobotBuildModule?.CustomName}");
         

            Echo(">>>-------------------------------<<<");

            Echo($"Auto build system: {useAutoBuildSystem}");
            Echo($"Nanobot system: {useNanobotAutoBuild}");
            Echo($"Ingot replace system: {needReplaceIngots}");
            Echo($"Parts replace system: {needReplaceParts}");
            Echo($"Power mng system: {usePowerManagmentSystem}");
            Echo($"Get ore frm outer: {getOreFromTransports}");
            Echo($"Refinerey ops: {useRefinereysOperations}");
            Echo($"Scan blueprints: {assemblerBlueprintGetter}");
            Echo($"Deep cont scan: {deepScan}");

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
        /// Включение/Выключение сборщиков и печей
        /// </summary>
        public void SwitchAssemblers(bool turnOnOff)
        {
            foreach(var ass in assemblers)
            {
                if (!ass.Closed)
                    ass.Enabled = turnOnOff;
            }

            foreach(var refs in refinereys)
            {
                if (!refs.Closed)
                    refs.Enabled = turnOnOff;
            }
        }


        /// <summary>
        /// Поиск руды в контейнерах
        /// </summary>
        public void FindOres()
        {
            Echo("Find ores in containers");

            string containerNames = deepScan == true ? "" : oreStorageName;

            oreInventories = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(oreStorageName) || c.CustomName.Contains(containerNames)))
                                     .Select(i => i.GetInventory(0));

            foreach (var key in oreDictionary)
            {
                key.Value.Amount = 0;
            }

            freeOreStorageVolume = oreInventories.Sum(i => i.CurrentVolume.ToIntSafe());
            totalOreStorageVolume = oreInventories.Sum(i => i.MaxVolume.ToIntSafe());

            precentageOreVolume = Math.Round(((double)freeOreStorageVolume / (double)totalOreStorageVolume) * 100, 1);

            foreach (var inv in oreInventories)
            {
                inv.GetItems(oreItems);

                foreach (var item in oreItems)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Ore")
                    {
                        if (oreDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            oreDictionary[item.Type.SubtypeId].Amount += item.Amount.ToIntSafe();
                            oreDictionary[item.Type.SubtypeId].Type = item.Type;
                        }
                        else
                        {
                            oreDictionary.Add(item.Type.SubtypeId, new OrePriority
                            {
                                Type = item.Type,
                                Amount = item.Amount.ToIntSafe(),
                                Priority = 0
                            });

                        }
                    }
                }
                oreItems.Clear();
            }
        }

        /// <summary>
        /// Вывод содержимого контейнеров с рудой на дисплей
        /// </summary>
        public void PrintOres()
        {
            if (oreDisplay == null)
                return;

            Echo("Update ores LCD");

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
                        if (oreDictionary.ContainsKey(match.Groups["Name"].Value))
                        {
                            int prior = 0;

                            if (int.TryParse(match.Groups["Prior"].Value, out prior))
                            {
                                oreDictionary[match.Groups["Name"].Value].Priority = prior;
                            }
                        }
                    }
                }
            }
            //Отрисовка на дисплей
            oreDisplay?.WriteText("", false);
            oreDisplay?.WriteText($"<<-----------Ores----------->>" +
                                  $"\nUse prior:{useRefinereyPriorty}" +
                                  $"\nContainers:{oreInventories.Count()}" +
                                  $"\nVolume: {precentageOreVolume} % {freeOreStorageVolume} / {totalOreStorageVolume} T", true);

            foreach (var dict in oreDictionary.OrderBy(k => k.Key))
            {
                oreDisplay?.WriteText($"\n{dict.Key} : {dict.Value.Amount} P {dict.Value.Priority} ", true);
            }

        }

        /// <summary>
        /// Вывод информации о заполнении печек и наличию модов
        /// </summary>
        public void RefinereysGetData()
        {

            Echo("Get refinereys data");

            foreach (var refs in refinereys.Where(refs => (!refs.Closed) && (refs is IMyUpgradableBlock)))
            {
                var upgradeBlock = refs as IMyUpgradableBlock;
                upgradeBlock?.GetUpgrades(out refsUpgradeList);

                if (refinereyModules.ContainsKey(refs))
                {
                    refinereyModules[refs] = refsUpgradeList["Effectiveness"];
                }
                else
                {
                    refinereyModules.Add(refs, refsUpgradeList["Effectiveness"]);
                }
            }

            if (refinereysDisplay == null)
                return;

            refinereysDisplay?.WriteText("", false);
            refinereysDisplay?.WriteText("<<---------------Refinereys-------------->>", true);

            foreach (var refs in refinereyModules)
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
        /// Загрузка руды в печи
        /// </summary>
        public void LoadRefinereys()
        {
            if (!useRefinereysOperations)
                return;

            string containerNames = deepScan == true ? "" : oreStorageName;

            var oreInventory = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(oreStorageName) || c.CustomName.Contains(containerNames)))
                                         .Select(i => i.GetInventory(0))
                                         .Where(i => i.ItemCount > 0);
            
            if (!oreInventory.Any())
                return;

            foreach (var refs in refinereys)
            {
                if (refs.Closed)
                    continue;

                refs.UseConveyorSystem = false;

                bool customData = false;
                KeyValuePair<string, OrePriority> ore = new KeyValuePair<string, OrePriority>();
                List<int> refinereyPriority = new List<int>();

                //Считывание даты для приоритетов конкретных руд
                if (refs.CustomData.Length > 1)
                {
                    var refPriors = refs.CustomData.Split(' ');

                    foreach(var str in refPriors)
                    {
                        int pr = 0;
                        if (int.TryParse(str, out pr))
                        {
                            refinereyPriority.Add(pr);
                            customData = true;
                        }
                        else
                        {
                            Echo($"Failed to parse CD on {refs?.CustomName}");
                        }
                    }
                }

                if (refs.InputInventory.ItemCount == 0)
                {
                    //Если конкретные приоритеты, то ищем только приоритетную руду
                    if (customData)
                    {
                        foreach (var prior in refinereyPriority)
                        {
                            var oreList = oreDictionary.Where(oreItem => oreItem.Value.Amount > 0 && oreItem.Value.Priority == prior);

                            if (!oreList.Any())
                                continue;

                            ore = oreList.FirstOrDefault();
                        }
                    }
                    else
                    {   //Сортировка по уменьшению приоритета
                        var oreList = oreDictionary.Where(oreItem => oreItem.Value.Amount > 0)
                                                   .OrderByDescending(k => k.Value.Priority);

                        if (!oreList.Any())
                            continue;

                        ore = oreList.FirstOrDefault();
                    }

                    if (ore.Equals(default(KeyValuePair<string,OrePriority>)))
                    {
                        continue;
                    }

                    //тут загрузка руды пустой печи
                    foreach (var inv in oreInventory)
                    {
                        var oreToLoad = inv.FindItem(ore.Value.Type);

                        if(oreToLoad.HasValue)
                        {
                            if (!inv.TransferItemTo(refs.InputInventory, oreToLoad.Value, null))
                            {
                                Echo($"Ore loaded: {oreToLoad.GetValueOrDefault()} to {refs?.CustomName}");
                                break;
                            }
                        }
                    }
                }
                else
                {   //Догрузка руды в печи
                    var load = (double)refs.InputInventory.CurrentVolume * 100 / (double)refs.InputInventory.MaxVolume;

                    if (load < refinereyReloadPrecentage)
                    {
                        var refsItem = refs.InputInventory.GetItemAt(0);

                        if (refsItem == null)
                            continue;

                        foreach (var inv in oreInventory)
                        {
                            var targItem = inv.FindItem(refsItem.Value.Type);

                            if (targItem.HasValue)
                            {
                                if (!inv.TransferItemTo(refs.InputInventory, targItem.Value, null))
                                {
                                    Echo($"Ore loaded: {targItem.GetValueOrDefault()} to {refs?.CustomName}");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Поиск компонентов
        /// </summary>
        public void FindParts()
        {
            Echo("Find parts in containers");

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
            foreach (var dict in buildedIngotsDictionary)
            {
                dict.Value.Current = 0;
            }

            string containerNames = deepScan == true ? "" : componentsStorageName;

            partsInventories = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(componentsStorageName) || c.CustomName.Contains(containerNames)))
                                        .Select(i => i.GetInventory(0));

            freePartsStorageVolume = partsInventories.Sum(i => i.CurrentVolume.ToIntSafe());
            totalPartsStorageVolume = partsInventories.Sum(i => i.MaxVolume.ToIntSafe());

            precentagePartsVolume = Math.Round(((double)freePartsStorageVolume / (double)totalPartsStorageVolume) * 100, 1);

            //Блок получения всех компонентов в контейнерах
            foreach (var inventory in partsInventories)
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
                        //Добавляем только те слитки, у которых есть возомжность построить
                        if (blueprintData.ContainsKey(item.Type.SubtypeId))
                        {
                            if (buildedIngotsDictionary.ContainsKey(item.Type.SubtypeId))
                            {
                                buildedIngotsDictionary[item.Type.SubtypeId].Current += item.Amount.ToIntSafe();
                            }
                            else
                            {
                                buildedIngotsDictionary.Add(item.Type.SubtypeId, new ItemBalanser { Current = item.Amount.ToIntSafe() });
                            }
                        }
                    }
                }
                productionItems.Clear();
            }//
        }

        public void ReadPartsData()
        {
            if (partsPanel == null)
                return;

            Echo("Read parts LCD");

            //Автосборка компонентов
            if (useAutoBuildSystem)
            {
                partsDisplayData.Clear();
                partsPanel?.ReadText(partsDisplayData);

                System.Text.RegularExpressions.MatchCollection matches = ProdItemFullRegex.Matches(partsDisplayData.ToString());

     
                if (matches.Count == 0)
                    return;
                
                foreach (System.Text.RegularExpressions.Match match in matches.Cast<System.Text.RegularExpressions.Match>())
                {
                    int amount = 0;
                    string name = match.Groups["Name"].Value;

                    if (int.TryParse(match.Groups["Amount"].Value, out amount))
                    {
                        if (partsDictionary.ContainsKey(name))
                        {
                            partsDictionary[name].Requested = amount;
                        }
                        else if (buildedIngotsDictionary.ContainsKey(name))
                        {
                            buildedIngotsDictionary[name].Requested = amount;
                        }
                        else if (ammoDictionary.ContainsKey(name))
                        {
                            ammoDictionary[name].Requested = amount;
                        }
                    }
                }
                
            }///

        }

        /// <summary>
        /// Отображение компонентов на дисплее
        /// </summary>
        public void PrintParts()
        {
           
            if (partsPanel == null)
                return;

            Echo("Update parts LCD");

            //Блок вывода инфорации на дисплеи
            string sysState = useAutoBuildSystem == true ? "Auto mode ON" : "Auto mode OFF";
            partsPanel?.WriteText("", false);
            partsPanel?.WriteText($"<<-------------Production------------->>" +
                                  $"\n{sysState}" +
                                  $"\nContainers:{partsInventories.Count()}" +
                                  $"\nVolume: {precentagePartsVolume} % {freePartsStorageVolume} / {totalPartsStorageVolume} T" +
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

            partsPanel?.WriteText("\n<<-----------Ingot----------->>", true);

            foreach (var dict in buildedIngotsDictionary.OrderBy(k => k.Key))
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
        /// Перенос из одного инвентаря в другой
        /// </summary>
        public void TransferItems(IEnumerable<IMyInventory> from, IEnumerable<IMyInventory> to)
        {
            foreach (var inventory in from)
            {
                var availConts = to.Where(i => i.IsConnectedTo(inventory));

                if (!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }

                var currentCargo = inventory.ItemCount;
                var targInv = availConts.First().Owner as IMyCargoContainer;

                for (int i = 0; i <= currentCargo; i++)
                {
                    var item = inventory.GetItemAt(0);

                    if (item == null)
                        continue;

                    if (inventory.TransferItemTo(availConts.First(), 0, null, true))
                    {
                        Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                    }
                    else
                    {
                        Echo($"Transer FAILED!: {item.GetValueOrDefault()} to {targInv?.CustomName}");
                    }
                }
            }
        }

        /// <summary>
        /// Поиск слитков
        /// </summary>
        public void FindIngots()
        {
            Echo("Find ingnots in containers");

            foreach (var dict in ingotsDict.ToList())
            {
                dict.Value.Current = 0;
            }

            totalIngotStorageVolume = 0;
            freeIngotStorageVolume = 0;

            string containerNames = deepScan == true ? "" : ingotStorageName;

            ingnotInventorys = containers.Where(c => (!c.Closed) && (c.CustomName.Contains(ingotStorageName) || c.CustomName.Contains(containerNames)))
                                         .Select(i => i.GetInventory(0));

            freeIngotStorageVolume = ingnotInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalIngotStorageVolume = ingnotInventorys.Sum(i => i.MaxVolume.ToIntSafe());

            precentageIngotsVolume = Math.Round(((double)freeIngotStorageVolume / (double)totalIngotStorageVolume) * 100, 1);

            //Проверка контейнеров
            foreach (var inventory in ingnotInventorys)
            {
                inventory.GetItems(ingotItems);

                foreach (var item in ingotItems)
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
                }
                ingotItems.Clear();
            }//
        }

        /// <summary>
        /// Отображение слитков на дисплее
        /// </summary>
        public void PrintIngots()
        {
            if (ingnotPanel == null)
                return;

            Echo("Update ingots LCD");

            //Вывод на дисплей
            ingnotPanel?.WriteText("", false);
            ingnotPanel?.WriteText($"<<-----------Ingots----------->>" +
                                   $"\nContainers:{ingnotInventorys.Count()}" +
                                   $"\nVolume: {precentageIngotsVolume} % {freeIngotStorageVolume} / {totalIngotStorageVolume} T", true);

            ingnotPanel?.WriteText("\n<<-----------Ingots----------->>", true);

            foreach (var dict in ingotsDict.OrderBy(k => k.Key))
            {
                ingnotPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} ", true);
            }
        }

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

            generatorsMaxOutputPower = generators.Where(g => !g.Closed && g.IsWorking).Sum(g => g.MaxOutput);
            generatorsOutputPower = generators.Where(g => !g.Closed && g.IsWorking).Sum(g => g.CurrentOutput);

            powerLoadPercentage = (float)Math.Round(generatorsOutputPower / generatorsMaxOutputPower * 100, 1);

            powerPanel?.WriteText("", false);
            powerPanel?.WriteText("<--------Power status--------->", true);
            powerPanel?.WriteText($"\nBatteryStatus:" +
                                   $"\nPower Load: {powerLoadPercentage} %" +
                                   $"\nTotal/Max stored:{Math.Round(currentStoredPower, 2)} / {maxStoredPower} MWt {Math.Round(currentStoredPower / maxStoredPower * 100, 1)} %"
                                 + $"\nInput/Output:{Math.Round(inputPower, 2)} / {Math.Round(outputPower, 2)} {(inputPower > outputPower ? "+" : "-")} MWt/h "
                                 + $"\nGens maxOut/Out: {Math.Round(generatorsMaxOutputPower, 2)} / {Math.Round(generatorsOutputPower, 2)} MWT", true);
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
                        detailedPowerPanel?.WriteText($"\nR:{item.Value.Type.SubtypeId} / {item.Value.Amount.ToIntSafe()} {block?.IsWorking}", true);

                        if (reactorPayloadLimitter)//ручная установка количества топлива
                        {
                           
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

            // monitor.AddInstructions("");
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
        public void SaveAllBlueprints()
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
                dataSystem.Set(bpcSection, key.Key, key.Value);
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
            public MyItemType Type { set; get; }
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

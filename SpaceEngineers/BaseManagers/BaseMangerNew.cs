using Microsoft.VisualBasic;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
using VRage.Game.VisualScripting.Utils;
using VRageMath;



namespace IngameScript.BaseManager.BaseNew
{
    public sealed class Program : MyGridProgram
    {
        #region mdk preserve
        //Названия тегов для игнорирования системой поиска и управления инвентарем
        string[] ignoreNames = new[] { "[Ignore]", "[None]", "[None]" };
        #endregion

        //string[] blueprintAddedNames = new[] { "BP", "Component", "" };

        //Названия контейнеров 
        string oreStorageName = "Ore";
        string ingotStorageName = "Ingot";
        string componentsStorageName = "Part";
        string ammoStorageName = "Ammo";
        string equipStorageName = "Item";

        string lcdInventoryOresName = "Ore";
        string lcdInventoryIngotsName = "Ingot";
        string lcdPowerSystemName = "Power";
        string lcdAutobuildName = "Autobuild";
        string lcdInventoryDebugName = "Debug";
        string lcdPowerDetailedName = "Generator";
        string lcdNanobotName = "Nano";
        string lcdRefinereyName = "Refinerey";

        string assemblersSpecialOperationsName = "[sp]";
        string assemblersBlueprintLeanerName = "[bps]";


        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        string operationSection = "Operations";
        string namesSection = "DisplaysNames";
        string contSection = "ContainerNames";
        string tagSection = "TagsNames";
        string bpcSection = "Blueprints";
        string oreBpcSection = "OreBlueprints";

        string autorunDataName = "Autorun";
        string replaceIngotsName = "ReplaceIngots";
        string replacePartsName = "ReplaceParts";
        string powerManagmentName = "PowerManagmentSystem";
        string detailedPowerManagmentName = "DetailedPowerMonitoring";
        string autoBuildSystem = "AutoBuildSystem";
        string nanobotOrerationsName = "UseNanobotAutoBuild";
        string refinereyOperationsName = "UseRefinereyOperations";
        //string refinereyPriorityName = "UseRefinereyPriortty";
        string reactorFuelLimitterName = "ReactorFuelLimitter";
        string containerSortingName = "SortingContainers";

        string oreLCDName = "LcdInventoryOresName";
        string ingotLCDName = "LcdInventoryIngotsName";
        string powerLCDName = "LcdPowerSystemName";
        string powerFullLCDName = "LcdPowerDetailedName";
        string autobuildLCDName = "LcdAutobuildName";
        string nanoLCDName = "NanobotDisplayName";
        string debugLCDName = "LcdInventoryDebugName";
        string refinereysLCDName = "LcdRefinereyName";

        string oreContName = "OreStorageName";
        string ingotContName = "IngotStorageName";
        string componentContName = "ComponentsStorageName";
        string ammoContName = "AmmoStorageName";
        string itemContName = "ItemStorageName";

        string specAssTagName = "AssemblersSpecialOperationsTagName";
        string bpcLearnTagName = "AssemblersBlueprintLeanerName";

        string emptyOreBlueprint = "none";

        string hddBlockTag = "[HDD]";

        PerformanceMonitor monitor;
        MyIni dataSystem;

        //Блок хранения информации, вместо внутреннего Storage
        IMyTerminalBlock hddDataBlock;

        //дисплеи
        IMyTextPanel debugPanel;
        IMyTextPanel ingotPanel;
        IMyTextPanel powerPanel;
        IMyTextPanel detailedPowerPanel;
        IMyTextPanel autoBuildPanel;
        IMyTextPanel nanobotDisplay;
        IMyTextPanel refinereysDisplay;
        IMyTextPanel oreDisplay;

        //все объекты, содержащие инвентарь
        IEnumerable<IMyInventory> nonContainerInventories;
        IEnumerable<IMyInventory> containerInventories;
        IEnumerable<IMyInventory> oreInventories;
        IEnumerable<IMyInventory> partsInventories;
        IEnumerable<IMyInventory> ingotInventorys;
        IEnumerable<IMyInventory> ammoInventorys;
        IEnumerable<IMyInventory> itemInventorys;

        List<IMyTerminalBlock> allBlocks;
        //сборщики, печки, контейнера
        List<IMyRefinery> refinereys;
        List<IMyAssembler> assemblers;
        List<IMyCargoContainer> containers;
        List<IMyBatteryBlock> batteries;
        List<IMyGasTank> gasTanks;
        List<IMyPowerProducer> generators;
        List<IMyShipConnector> connectors;

        List<IMyAssembler> specialAssemblers;

        IMyTerminalBlock nanobotBuildModule;

        IMyEventControllerBlock eventControllerBlock;

        bool autorun = false;
        bool switchProdModulesAsScript = false;
        bool needReplaceIngots = false;
        bool needReplaceParts = false;
        bool usePowerManagmentSystem = false;
        //bool useDetailedPowerMonitoring = false;
        bool useAutoBuildSystem = false;
        bool useNanobotAutoBuild = false;
        bool useRefinereysOperations = false;
       //bool useRefinereyPriorty = false;
        bool reactorPayloadLimitter = false;
        bool containerSorting = false;

        bool loadDataAfterInit = true;

        int totalOreStorageVolume = 0;
        int freeOreStorageVolume = 0;

        int totalIngotStorageVolume = 0;
        int freeIngotStorageVolume = 0;

        int totalPartsStorageVolume = 0;
        int freePartsStorageVolume = 0;

        int totalAmmoStorageVolume = 0;
        int freeAmmoStorageVolume = 0;

        int totalItemStorageVolume = 0;
        int freeItemStorageVolume = 0;

        int currentTick = 0;
        int globalTick = 0;
        int globalTickLimit = 4;

        int workingRefinereys = 0;

        //int maxReactorPayload = 50;

        //int refinereyReloadPrecentage = 70;
        int maxVolumeContainerPercentage = 95;

        int partsStateMachineCounter = 0;
        int maxPartsPerScan = 5;

        int maxContRenderSymbols = 20;

        float fontSize = 0.8f;

        float maxStoredPower = 0;
        float currentStoredPower = 0;
        float battsStoredPrecentage = 0;

        float inputPower = 0;
        float outputPower = 0;

        float generatorsMaxOutputPower = 0;
        float generatorsOutputPower = 0;
        float powerLoadPercentage = 0;

        double precentageOreVolume = 0;
        double precentageIngotsVolume = 0;
        double precentagePartsVolume = 0;
        double precentageAmmoVolume = 0;
        double precentageItemVolume = 0;

        bool nanobuildReady = true;

        bool assemblerBlueprintGetter = false;
        string SpecialAssemblerLastName = "";

        //Словари компонентов по типам
        Dictionary<string, ItemBalanser> ingotsDict;
        Dictionary<string, ItemBalanser> partsDictionary;
        Dictionary<string, ItemBalanser> ammoDictionary;
        Dictionary<string, ItemBalanser> buildedIngotsDictionary;
        Dictionary<string, ItemBalanser> equipmentDictionary;

        Dictionary<string, OreData> oreDictionary;

        Dictionary<string, string> blueprintData;

        Dictionary<string, List<IMyGasTank>> gasTanksDictoinary;

        // List<string> orePriority;
        Dictionary<string, int> orePriority;

        //Печки
        Dictionary<IMyRefinery, RefinereyData> refineryData;
        Dictionary<string, float> refsUpgradeList;

        List<MyInventoryItem> oreItems;
        List<MyInventoryItem> ingotItems;
        List<MyInventoryItem> refinereysItems;
        List<MyInventoryItem> productionItems;

        List<string> refinereyDataList;

        IMyAssembler learnerAss;

        StringBuilder partsDisplayData;
        StringBuilder oreDisplayData;

        TextAdapter refineryPrintData;

        //Поиск чертежей
        MyProductionItem lastDetectedBlueprintItem;
        MyInventoryItem? lastDetectedConstructItem;

        Dictionary<MyDefinitionId, int> nanobotBuildQueue;

        // ^(?<Name>\w*\W?\w*)\s:\s\d*?\W*?\s?(?<Amount>\d+)$ общий имя + необходимое кол-во
        //System.Text.RegularExpressions.Regex NameRegex = new System.Text.RegularExpressions.Regex(@"(?<Name>\w*\-?\w*)\s:");//(?<Name>\w*)\s: || (?<Name>^\w*\-*\s*\w*)\s: - c пробелами и подчеркиваниями
       // System.Text.RegularExpressions.Regex AmountRegex = new System.Text.RegularExpressions.Regex(@"(?<Amount>\d+)$");

        //Имя и приоритет руды, ищет по всем строкам
        //System.Text.RegularExpressions.Regex OrePriorRegex = new System.Text.RegularExpressions.Regex(@"^(?<Name>\w*\W?\w*)\s:\s\w*?\sP\s?(?<Prior>\d+)\s?$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled);
        //Имя и количество компонентов для автостройки
        System.Text.RegularExpressions.Regex ProdItemFullRegex = new System.Text.RegularExpressions.Regex(@"^(?<Name>\w*\W?\w*)\s:\s\d*?\W*?\s?(?<Amount>\d+)$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled);

        IEnumerator<bool> partsReadstateMachine;
        IEnumerator<bool> updateStateMachine;

        IEnumerator<bool> displayUpdateMachine;
        IEnumerator<bool> displayUpdateStateMachine;


        /// <summary>
        /// Инициализация компонентов 1 раз при создании объекта
        /// </summary>
        public Program()
        {

            Echo($"Script first init starting");
            Runtime.UpdateFrequency = UpdateFrequency.None;

            allBlocks = new List<IMyTerminalBlock>();
            containerInventories = new List<IMyInventory>();
            oreItems = new List<MyInventoryItem>();
            refinereys = new List<IMyRefinery>();
            assemblers = new List<IMyAssembler>();
            containers = new List<IMyCargoContainer>();
            batteries = new List<IMyBatteryBlock>();
            generators = new List<IMyPowerProducer>();
            gasTanks = new List<IMyGasTank>();
            connectors = new List<IMyShipConnector>();
            specialAssemblers = new List<IMyAssembler>();

            partsDisplayData = new StringBuilder();
            oreDisplayData = new StringBuilder();

            oreDictionary = new Dictionary<string, OreData>();
            ingotsDict = new Dictionary<string, ItemBalanser>();
            partsDictionary = new Dictionary<string, ItemBalanser>();
            nanobotBuildQueue = new Dictionary<MyDefinitionId, int>();
            ammoDictionary = new Dictionary<string, ItemBalanser>();
            buildedIngotsDictionary = new Dictionary<string, ItemBalanser>();
            equipmentDictionary = new Dictionary<string, ItemBalanser>();
            gasTanksDictoinary = new Dictionary<string, List<IMyGasTank>>();

            refsUpgradeList = new Dictionary<string, float>();

            refinereyDataList = new List<string>();

            blueprintData = new Dictionary<string, string>();

            refinereysItems = new List<MyInventoryItem>();
            productionItems = new List<MyInventoryItem>();
            ingotItems = new List<MyInventoryItem>();
            refineryData = new Dictionary<IMyRefinery, RefinereyData>();
            orePriority = new Dictionary<string, int>();

            dataSystem = new MyIni();
            monitor = new PerformanceMonitor(this, Me.GetSurface(1));

            refineryPrintData = new TextAdapter();

            Load();
            GetIniData();

            if (autorun)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                Echo($"Script autostart in prog");
            }

            partsReadstateMachine = ReadPartsData();
            updateStateMachine = Update();

            displayUpdateMachine = DisplayUpdateStateMachine();
            displayUpdateStateMachine = DisplaysUpdate();
        }


        public void Main(string args, UpdateType updateType)
        {

            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                Commands(args);

            monitor.AddRuntime();

            if (globalTick == globalTickLimit)
            {
                globalTick = 0;
                DrawEcho();

                //  PrintRefs();

                if (displayUpdateMachine != null)
                {
                    bool hasMoreSteps = displayUpdateMachine.MoveNext();

                    if (!hasMoreSteps)
                    {
                        displayUpdateMachine.Dispose();
                        displayUpdateMachine = DisplayUpdateStateMachine();
                    }
                }


                if (updateStateMachine != null)
                {
                    bool hasMoreSteps = updateStateMachine.MoveNext();

                    if (!hasMoreSteps)
                    {
                        updateStateMachine.Dispose();
                        updateStateMachine = Update();
                    }
                }
            }

            globalTick++;

            monitor.AddInstructions();
            monitor.EndOfFrameCalc();
            monitor.Draw();
        }

        public void Save()
        {
            if (hddDataBlock == null)
                return;

            MyIni SaveData = new MyIni();
           // Storage = null;

            SaveData.AddSection("Ores");
            SaveData.AddSection("Ingots");
            SaveData.AddSection("Parts");
            SaveData.AddSection("Ammo");
            SaveData.AddSection("BuildIngots");

            foreach (var dict in oreDictionary)
            {
                SaveData.Set("Ores", dict.Key, 0);//dict.Value.Priority
            }

            //Ingot
            foreach (var dict in ingotsDict)
            {
                SaveData.Set("Ingots", dict.Key, 0);// dict.Value.Current
            }

            //Parts
            foreach (var dict in partsDictionary)
            {
                SaveData.Set("Parts", dict.Key, 0);// dict.Value.Current
            }

            //Ammo
            foreach (var dict in ammoDictionary)
            {
                SaveData.Set("Ammo", dict.Key, 0);// dict.Value.Current
            }

            //Builded ingots
            foreach (var dict in buildedIngotsDictionary)
            {
                SaveData.Set("BuildIngots", dict.Key, 0);// dict.Value.Current
            }

            hddDataBlock.CustomData = SaveData.ToString();

           // Storage = SaveData.ToString();
        }

        public void Load()
        {
            allBlocks.Clear();

            GridTerminalSystem.GetBlocksOfType(allBlocks, (IMyTerminalBlock b) => b.CubeGrid == Me.CubeGrid);

            hddDataBlock = allBlocks.Where(b => b is IMyTerminalBlock)
                                    .Where(r => r.IsFunctional)
                                    .Where(n => n.CustomName.Contains(hddBlockTag)).FirstOrDefault();

            if (hddDataBlock == null)
                return;

            MyIni LoadData = new MyIni();

            if (LoadData.TryParse(hddDataBlock.CustomData))
            {
                Echo("Load internal data started");

                List<MyIniKey> keys = new List<MyIniKey>();
                LoadData.GetKeys("Ores", keys);

                foreach (var key in keys)
                {
                    oreDictionary.Add(key.Name, new OreData() { Priority = LoadData.Get(key).ToInt32() });
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
                    BlueprintLearnerSwitcher();
                    break;
                case "INGOT":
                    SwitchIngotMode();
                    break;
                case "PART":
                    SwitchPartsMode();
                    break;
                case "NANO":
                    SwitchNanobotMode();
                    break;
            }
        }

        public IEnumerator<bool> Update()
        {
           
            SaveAllBlueprints();

            switch (currentTick)
            {
                case 0:
                    FindInventories();
                    yield return true;
                    FindLcds();
                    yield return true;
                    GetContainers();
                    break;
                case 1:
                    FindOres();
                    yield return true;
                    PrintOres();
                    yield return true;
                    ContainersSortingOres();
                    yield return true;
                    RefinereysGetData();
                    yield return true;
                    RefinereysSaveOreData();
                    yield return true;
                    PrintRefinerysData();
                    break;
                case 2:
                    FindIngots();
                    yield return true;
                    PrintIngots();
                    yield return true;
                    UnloadRefinereys();
                    yield return true;
                    ContainersSortingIngots();
                    break;
                case 3:
                    ReplaceParts();
                    yield return true;
                    FindParts();

                    while (PartReadingStateMachine())
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.Update1;
                        yield return true;
                    }
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;

                    PrintAutobuildComponents();
                    yield return true;
                    ContainersSortingParts();
                    yield return true;
                    ContainersSortingAmmo();
                    yield return true;
                    ContainersSortingItems();
                    break;
                case 4:
                    GetPowerData();
                    PowerSystemDetailed();
                    PrintPowerData();
                    yield return true;
                    PartsAutoBuild();
                    break;
                case 5:
                    ClearAssemblers();
                    yield return true;
                    NanobotOperations();
                    PrintNanobotQueue();
                    yield return true;
                    ConnectorSorting();
                    BlueprintLearnerSwitcher();
                    break;

            }

            //AssemblersBalancing();

            currentTick++;
            if (currentTick == 6)
                currentTick = 0;

        }

        public IEnumerator<bool> DisplayUpdateStateMachine()
        {
            while (DisplayRenderMachine())
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
                yield return true;
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

        }

        public IEnumerator<bool> DisplaysUpdate()
        {
            PrintRefs();
            yield return true;

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
                //useDetailedPowerMonitoring = dataSystem.Get(operationSection, detailedPowerManagmentName).ToBoolean();
                useAutoBuildSystem = dataSystem.Get(operationSection, autoBuildSystem).ToBoolean();
                useNanobotAutoBuild = dataSystem.Get(operationSection, nanobotOrerationsName).ToBoolean();
                useRefinereysOperations = dataSystem.Get(operationSection, refinereyOperationsName).ToBoolean();
                //useRefinereyPriorty = dataSystem.Get(operationSection, refinereyPriorityName).ToBoolean();
                reactorPayloadLimitter = dataSystem.Get(operationSection, reactorFuelLimitterName).ToBoolean();
                containerSorting = dataSystem.Get(operationSection, containerSortingName).ToBoolean();

                //Containers 
                oreStorageName = dataSystem.Get(contSection, oreContName).ToString();
                ingotStorageName = dataSystem.Get(contSection, ingotContName).ToString();
                componentsStorageName = dataSystem.Get(contSection, componentContName).ToString();
                ammoStorageName = dataSystem.Get(contSection, ammoContName).ToString();
                equipStorageName = dataSystem.Get(contSection, itemContName).ToString();

                //Displays 
                lcdInventoryOresName = dataSystem.Get(namesSection, oreLCDName).ToString();
                lcdInventoryIngotsName = dataSystem.Get(namesSection, ingotLCDName).ToString();
                lcdPowerSystemName = dataSystem.Get(namesSection, powerLCDName).ToString();
                lcdAutobuildName = dataSystem.Get(namesSection, autobuildLCDName).ToString();
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

                //Ore Blueprints
                keys.Clear();
                dataSystem.GetKeys(oreBpcSection, keys);

                foreach (var key in keys)
                {
                    string[] splitedStrings = dataSystem.Get(key).ToString().Split('|');
                    string blueprintName = emptyOreBlueprint;

                    if (splitedStrings.Length > 0)
                        blueprintName = splitedStrings[0];

                    if (oreDictionary.ContainsKey(key.Name))
                    {
                        oreDictionary[key.Name].Ready = false;
                        oreDictionary[key.Name].Type = "MyObjectBuilder_Ore/" + key.Name;
                        oreDictionary[key.Name].Blueprint = blueprintName;

                        if (splitedStrings.Length > 1)
                        {
                            oreDictionary[key.Name].Ready = true;
                            for (int i = 1; i < splitedStrings.Length; i++)
                            {
                                oreDictionary[key.Name].IngotNames.Add(splitedStrings[i]);
                            }
                        }
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
                //dataSystem.Set(operationSection, detailedPowerManagmentName, false);
                dataSystem.Set(operationSection, autoBuildSystem, false);
                dataSystem.Set(operationSection, nanobotOrerationsName, false);
                dataSystem.Set(operationSection, refinereyOperationsName, false);
               //dataSystem.Set(operationSection, refinereyPriorityName, false);
                dataSystem.Set(operationSection, reactorFuelLimitterName, false);
                dataSystem.Set(operationSection, containerSortingName, false);

                dataSystem.AddSection(namesSection);
                dataSystem.Set(namesSection, oreLCDName, lcdInventoryOresName);
                dataSystem.Set(namesSection, ingotLCDName, lcdInventoryIngotsName);
                dataSystem.Set(namesSection, powerLCDName, lcdPowerSystemName);
                dataSystem.Set(namesSection, powerFullLCDName, lcdPowerDetailedName);
                dataSystem.Set(namesSection, autobuildLCDName, lcdAutobuildName);
                dataSystem.Set(namesSection, nanoLCDName, lcdNanobotName);
                dataSystem.Set(namesSection, debugLCDName, lcdInventoryDebugName);
                dataSystem.Set(namesSection, refinereysLCDName, lcdRefinereyName);

                dataSystem.AddSection(contSection);
                dataSystem.Set(contSection, oreContName, oreStorageName);
                dataSystem.Set(contSection, ingotContName, ingotStorageName);
                dataSystem.Set(contSection, componentContName, componentsStorageName);
                dataSystem.Set(contSection, ammoContName, ammoStorageName);
                dataSystem.Set(contSection, itemContName, equipStorageName);

                dataSystem.AddSection(tagSection);
                dataSystem.Set(tagSection, specAssTagName, assemblersSpecialOperationsName);
                dataSystem.Set(tagSection, bpcLearnTagName, assemblersBlueprintLeanerName);

                //  dataSystem.AddSection("OrePriority");

                dataSystem.AddSection(bpcSection);
                dataSystem.AddSection(oreBpcSection);

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
            //dataSystem.Set(operationSection, detailedPowerManagmentName, useDetailedPowerMonitoring);
            dataSystem.Set(operationSection, autoBuildSystem, useAutoBuildSystem);
            dataSystem.Set(operationSection, nanobotOrerationsName, useNanobotAutoBuild);
            dataSystem.Set(operationSection, refinereyOperationsName, useRefinereysOperations);
           //dataSystem.Set(operationSection, refinereyPriorityName, useRefinereyPriorty);
            dataSystem.Set(operationSection, reactorFuelLimitterName, reactorPayloadLimitter);
            dataSystem.Set(operationSection, containerSortingName, containerSorting);

            dataSystem.Set(namesSection, oreLCDName, lcdInventoryOresName);
            dataSystem.Set(namesSection, ingotLCDName, lcdInventoryIngotsName);
            dataSystem.Set(namesSection, powerLCDName, lcdPowerSystemName);
            dataSystem.Set(namesSection, powerFullLCDName, lcdPowerDetailedName);
            dataSystem.Set(namesSection, autobuildLCDName, lcdAutobuildName);
            dataSystem.Set(namesSection, nanoLCDName, lcdNanobotName);
            dataSystem.Set(namesSection, debugLCDName, lcdInventoryDebugName);
            dataSystem.Set(namesSection, refinereysLCDName, lcdRefinereyName);

            dataSystem.Set(contSection, oreContName, oreStorageName);
            dataSystem.Set(contSection, ingotContName, ingotStorageName);
            dataSystem.Set(contSection, componentContName, componentsStorageName);
            dataSystem.Set(contSection, ammoContName, ammoStorageName);
            dataSystem.Set(contSection, itemContName, equipStorageName);

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

        public void SwitchNanobotMode()
        {
            useNanobotAutoBuild = !useNanobotAutoBuild;
            if (useNanobotAutoBuild)
            {
                if (nanobotBuildModule != null)
                    nanobotBuildModule.SetValueBool("OnOff", true);
            }
        }

        /// <summary>
        /// Включение и выключение печек и сборщиков при выключении/включении скрипта
        /// </summary>
        public void SwitchProductionModules()
        {
            switchProdModulesAsScript = !switchProdModulesAsScript;
        }

        /// <summary>
        /// Управление корутиной по считыванию данных с диплея компонентов
        /// </summary>
        public bool PartReadingStateMachine()
        {
            if (partsReadstateMachine == null)
                return false;

            bool hasMoreSteps = partsReadstateMachine.MoveNext();

            if (hasMoreSteps)
            {
                return true;
            }
            else
            {
                partsReadstateMachine.Dispose();
                partsReadstateMachine = ReadPartsData();
                return false;
            }
        }


        public bool DisplayRenderMachine()
        {
            if (displayUpdateStateMachine == null)
                return false;

            bool hasMoreSteps = displayUpdateStateMachine.MoveNext();

            if (hasMoreSteps)
            {
                return true;
            }
            else
            {
                displayUpdateStateMachine.Dispose();
                displayUpdateStateMachine = DisplaysUpdate();
                return false;
            }
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

            if ((ingotPanel == null) || !ingotPanel.CustomName.Contains(lcdInventoryIngotsName) || (ingotPanel.Closed))
            {
                ingotPanel = null;

                ingotPanel = allBlocks.Where(b => b is IMyTextPanel)
                                      .Where(r => r.IsFunctional && r.CustomName.Contains(lcdInventoryIngotsName))
                                      .Select(t => t as IMyTextPanel).FirstOrDefault();

                if (ingotPanel != null)
                {
                    ingotPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    ingotPanel.FontSize = fontSize;
                }
            }

            if ((powerPanel == null) || !powerPanel.CustomName.Contains(lcdPowerSystemName) || (powerPanel.Closed))
            {
                powerPanel = null;

                powerPanel = allBlocks.Where(b => b is IMyTextPanel)
                                      .Where(r => r.IsFunctional && r.CustomName.Contains(lcdPowerSystemName))
                                      .Select(t => t as IMyTextPanel).FirstOrDefault();

                if (powerPanel != null)
                {
                    powerPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    powerPanel.FontSize = fontSize;
                }
            }

            if ((detailedPowerPanel == null) || !detailedPowerPanel.CustomName.Contains(lcdPowerDetailedName) || (detailedPowerPanel.Closed))
            {
                detailedPowerPanel = null;

                detailedPowerPanel = allBlocks.Where(b => b is IMyTextPanel)
                                              .Where(r => r.IsFunctional && r.CustomName.Contains(lcdPowerDetailedName))
                                              .Select(t => t as IMyTextPanel).FirstOrDefault();

                if (detailedPowerPanel != null)
                {
                    detailedPowerPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    detailedPowerPanel.FontSize = fontSize;
                }
            }

            if ((autoBuildPanel == null) || !autoBuildPanel.CustomName.Contains(lcdAutobuildName) || (autoBuildPanel.Closed))
            {
                autoBuildPanel = null;

                autoBuildPanel = allBlocks.Where(b => b is IMyTextPanel)
                                          .Where(r => r.IsFunctional && r.CustomName.Contains(lcdAutobuildName))
                                          .Select(t => t as IMyTextPanel).FirstOrDefault();

                if (autoBuildPanel != null)
                {
                    autoBuildPanel.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    autoBuildPanel.FontSize = fontSize;
                }
            }

            if ((nanobotDisplay == null) || !nanobotDisplay.CustomName.Contains(lcdNanobotName) || (nanobotDisplay.Closed))
            {
                nanobotDisplay = null;

                nanobotDisplay = allBlocks.Where(b => b is IMyTextPanel)
                                          .Where(r => r.IsFunctional && r.CustomName.Contains(lcdNanobotName))
                                          .Select(t => t as IMyTextPanel).FirstOrDefault();

                if (nanobotDisplay != null)
                {
                    nanobotDisplay.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    nanobotDisplay.FontSize = fontSize;
                }
            }

            if ((refinereysDisplay == null) || !refinereysDisplay.CustomName.Contains(lcdRefinereyName) || (refinereysDisplay.Closed))
            {
                refinereysDisplay = null;

                refinereysDisplay = allBlocks.Where(b => b is IMyTextPanel)
                                             .Where(r => r.IsFunctional && r.CustomName.Contains(lcdRefinereyName))
                                             .Select(t => t as IMyTextPanel).FirstOrDefault();
                 
                if (refinereysDisplay != null)
                {
                    refinereysDisplay.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    refinereysDisplay.FontSize = fontSize;
                }
            }

            if ((oreDisplay == null) || !oreDisplay.CustomName.Contains(lcdInventoryOresName) || (oreDisplay.Closed))
            {
                oreDisplay = null;

                oreDisplay = allBlocks.Where(b => b is IMyTextPanel)
                                      .Where(r => r.IsFunctional && r.CustomName.Contains(lcdInventoryOresName))
                                      .Select(t => t as IMyTextPanel).FirstOrDefault();

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

            string hddFinded = hddDataBlock != null ? "OK" : "NO module";
            Echo($"HDD block:{hddFinded}:{hddDataBlock?.CustomName}");

            string nanoFinded = nanobotBuildModule != null ? "OK" : "NO module";
            Echo($"Nanobot:{nanoFinded}:{nanobotBuildModule?.CustomName}");

            string eventFinded = eventControllerBlock != null ? "OK" : "NO module";
            Echo($"Event controller:{eventFinded}:{eventControllerBlock?.CustomName}");

            Echo(">>>-------------------------------<<<");

            Echo($"Auto build system: {useAutoBuildSystem}");
            Echo($"Nanobot system: {useNanobotAutoBuild}");
            Echo($"Ingot replace system: {needReplaceIngots}");
            Echo($"Parts replace system: {needReplaceParts}");
            Echo($"Conts sorting: {containerSorting}");
            Echo($"Power mng system: {usePowerManagmentSystem}");
            Echo($"Refinerey ops: {useRefinereysOperations}");
            Echo($"Scan blueprints: {assemblerBlueprintGetter}");

            Echo(">>>-------------------------------<<<");
        }

        /// <summary>
        /// Поиск всех обьектов, печек, сборщиков, ящиков
        /// </summary>
        public void FindInventories()
        {
            Echo("Find blocks");

            allBlocks.Clear();

            GridTerminalSystem.GetBlocksOfType(allBlocks, (IMyTerminalBlock b) => b.CubeGrid == Me.CubeGrid);

            eventControllerBlock = allBlocks.Where(b => b is IMyEventControllerBlock)
                                            .Where(r => r.IsFunctional)
                                            .Select(t => t as IMyEventControllerBlock).FirstOrDefault();

            refinereys = allBlocks.Where(b => b is IMyRefinery)
                                  .Where(r => r.IsFunctional)
                                  .Select(t => t as IMyRefinery).ToList();

            assemblers = allBlocks.Where(b => b is IMyAssembler)
                                  .Where(a => a.IsFunctional)
                                  .Select(t => t as IMyAssembler).ToList();

            containers = allBlocks.Where(b => b is IMyCargoContainer)
                                  .Where(c => c.IsFunctional)
                                  .Select(t => t as IMyCargoContainer).ToList();

            batteries = allBlocks.Where(b => b is IMyBatteryBlock)
                                 .Where(b => b.IsFunctional)
                                 .Select(t => t as IMyBatteryBlock).ToList();

            gasTanks = allBlocks.Where(b => b is IMyGasTank)
                                .Where(g => g.IsFunctional)
                                .Select(t => t as IMyGasTank).ToList();

            generators = allBlocks.Where(b => b is IMyPowerProducer)
                                  .Where(r => r.IsFunctional)
                                  .Select(t => t as IMyPowerProducer).ToList();

            connectors = allBlocks.Where(b => b is IMyShipConnector)
                                  .Where(r => r.IsFunctional)
                                  .Select(t => t as IMyShipConnector).ToList();

            nanobotBuildModule = allBlocks.Where(b => b.IsFunctional)
                                          .Where(g => g.BlockDefinition.SubtypeName.ToString() == "SELtdLargeNanobotBuildAndRepairSystem")
                                          .FirstOrDefault();

            specialAssemblers = assemblers.Where(a => a.CustomName.Contains(assemblersSpecialOperationsName)).ToList();

        }

        /// <summary>
        /// Поиск спец контейнеров и рассчет их обьема
        /// </summary>
        public void GetContainers()
        {
            Echo("Get containers data");

            containerInventories = containers.Where(b => !b.Closed && !ignoreNames.Any(txt => b.CustomName.Contains(txt)))
                                            .Select(b => b.GetInventory(0));

            oreInventories = containers.Where(c => (!c.Closed) && c.CustomName.Contains(oreStorageName))
                                       .Select(i => i.GetInventory(0));

            freeOreStorageVolume = oreInventories.Sum(i => i.CurrentVolume.ToIntSafe());
            totalOreStorageVolume = oreInventories.Sum(i => i.MaxVolume.ToIntSafe());
            precentageOreVolume = Math.Round((double)freeOreStorageVolume / Math.Max(1, (double)totalOreStorageVolume) * 100, 1);

            ingotInventorys = containers.Where(c => (!c.Closed) && c.CustomName.Contains(ingotStorageName))
                                        .Select(i => i.GetInventory(0));

            freeIngotStorageVolume = ingotInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalIngotStorageVolume = ingotInventorys.Sum(i => i.MaxVolume.ToIntSafe());
            precentageIngotsVolume = Math.Round((double)freeIngotStorageVolume / Math.Max(1, (double)totalIngotStorageVolume) * 100, 1);

            partsInventories = containers.Where(c => (!c.Closed) && c.CustomName.Contains(componentsStorageName))
                                         .Select(i => i.GetInventory(0));

            freePartsStorageVolume = partsInventories.Sum(i => i.CurrentVolume.ToIntSafe());
            totalPartsStorageVolume = partsInventories.Sum(i => i.MaxVolume.ToIntSafe());
            precentagePartsVolume = Math.Round((double)freePartsStorageVolume / Math.Max(1, (double)totalPartsStorageVolume) * 100, 1);

            ammoInventorys = containers.Where(c => (!c.Closed) && c.CustomName.Contains(ammoStorageName))
                                       .Select(i => i.GetInventory(0));

            freeAmmoStorageVolume = ammoInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalAmmoStorageVolume = ammoInventorys.Sum(i => i.MaxVolume.ToIntSafe());
            precentageAmmoVolume = Math.Round((double)freeAmmoStorageVolume / Math.Max(1, (double)totalAmmoStorageVolume) * 100, 1);

            itemInventorys = containers.Where(c => (!c.Closed) && c.CustomName.Contains(equipStorageName))
                                       .Select(i => i.GetInventory(0));

            freeItemStorageVolume = itemInventorys.Sum(i => i.CurrentVolume.ToIntSafe());
            totalItemStorageVolume = itemInventorys.Sum(i => i.MaxVolume.ToIntSafe());
            precentageItemVolume = Math.Round((double)freeItemStorageVolume / Math.Max(1, (double)totalItemStorageVolume) * 100, 1);
        }

        /// <summary>
        /// Включение/Выключение сборщиков и печей
        /// </summary>
        public void SwitchAssemblers(bool turnOnOff)
        {
            foreach (var ass in assemblers)
            {
                if (!ass.Closed)
                    ass.Enabled = turnOnOff;
            }

            foreach (var refs in refinereys)
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

            foreach (var key in oreDictionary)
            {
                key.Value.Amount = 0;
            }

            foreach (var inv in containerInventories)
            {
                inv.GetItems(oreItems);

                foreach (var item in oreItems)
                {
                    if (item.Type.TypeId == "MyObjectBuilder_Ore")
                    {
                        if (oreDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            oreDictionary[item.Type.SubtypeId].Amount += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            oreDictionary.Add(item.Type.SubtypeId, new OreData
                            {
                                Type = item.Type.ToString(),
                                Amount = item.Amount.ToIntSafe(),
                                Priority = 0,
                                Blueprint = emptyOreBlueprint
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
            //if (useRefinereysOperations)
            //{
            //    oreDisplayData.Clear();
            //    oreDisplay?.ReadText(oreDisplayData);

            //    System.Text.RegularExpressions.MatchCollection matches = OrePriorRegex.Matches(oreDisplayData.ToString());

            //    if (matches.Count > 0)
            //    {
            //        foreach (System.Text.RegularExpressions.Match match in matches)
            //        {
            //            if (oreDictionary.ContainsKey(match.Groups["Name"].Value))
            //            {
            //                int prior = 0;

            //                if (int.TryParse(match.Groups["Prior"].Value, out prior))
            //                {
            //                    oreDictionary[match.Groups["Name"].Value].Priority = prior;
            //                }
            //            }
            //        }
            //    }
            //}

            //Отрисовка на дисплей
            oreDisplay?.WriteText("", false);
            oreDisplay?.WriteText($"<<-----------Ores----------->>" +
                                  $"\nContainers:{oreInventories.Count()} " +
                                  $"{NumberToStringConverter(precentageOreVolume)}" +
                                  $"\nVolume: {precentageOreVolume} % {freeOreStorageVolume} / {totalOreStorageVolume} T" +
                                  $"\n<<-----------Ores----------->>", true);

            foreach (var dict in oreDictionary.OrderBy(k => k.Key))
            {
                double amount = dict.Value.Amount;

                oreDisplay?.WriteText($"\n{dict.Key} : {NumberConverter(amount)}", true);
            }
        }

        /// <summary>
        /// Вывод информации о заполнении печек и наличию модов
        /// </summary>
        public void RefinereysGetData()
        {
            Echo("Get refinereys data");

            RefinereyData.TotalOresInProcessing.Clear();

            foreach (var refs in refinereys)
            {
                if (refs.Closed)
                    continue;

                if (refineryData.ContainsKey(refs))
                {
                    if (refs is IMyUpgradableBlock)
                    {
                        refsUpgradeList.Clear();

                        var upgradeBlock = refs as IMyUpgradableBlock;
                        upgradeBlock?.GetUpgrades(out refsUpgradeList);

                        refineryData[refs].Effectiveness = (float)Math.Round(refsUpgradeList["Effectiveness"], 2);
                    }

                    refineryData[refs].InputInventory = (float)Math.Round((double)refs.InputInventory.CurrentVolume.ToIntSafe() / (double)refs.InputInventory.MaxVolume.ToIntSafe() * 100, 2);
                    refineryData[refs].OutputInventory = (float)Math.Round((double)refs.OutputInventory.CurrentVolume.ToIntSafe() / (double)refs.OutputInventory.MaxVolume.ToIntSafe() * 100, 2);

                    refs.InputInventory.GetItems(refinereysItems);

                    refineryData[refs].OreLoad.Clear();

                    foreach (var item in refinereysItems)
                    {
                        refineryData[refs].OreLoad.Add(item.Type.SubtypeId + " X " + item.Amount.ToIntSafe());

                        if (RefinereyData.TotalOresInProcessing.ContainsKey(item.Type.SubtypeId))
                        {
                            RefinereyData.TotalOresInProcessing[item.Type.SubtypeId] += item.Amount.ToIntSafe();

                        }
                        else
                        {
                            RefinereyData.TotalOresInProcessing.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                        }
                    }

                    refinereysItems.Clear();
                }
                else
                {
                    refineryData.Add(refs, new RefinereyData());
                }

            }

            workingRefinereys = refineryData.Where(inv => inv.Value.InputInventory > 5).Count();

        }

        public void PrintRefinerysData()
        {
            if (refinereysDisplay == null)
                return;

            Echo("Update refinereys LCD");

            refinereyDataList.Clear();

            List<string> stat = new List<string>
            {
                "<<------------Refinereys------------>>",
                $"Total/Working: {refineryData.Count} / {workingRefinereys} "
            };

            foreach(var key in RefinereyData.TotalOresInProcessing)
            {
                stat.Add($"{key.Key} X {NumberConverter(key.Value)}");
            }

            stat.Add("---------------------------");


            foreach (var refs in refineryData)
            {
                if (refs.Key.Closed)
                    continue;

                refinereyDataList.Add(refs.Key.CustomName);
                refinereyDataList.Add($"Eff: {refs.Value.Effectiveness}  Load: {refs.Value.InputInventory} / {refs.Value.OutputInventory} %");

                foreach(var load in refs.Value.OreLoad)
                {
                    refinereyDataList.Add($"{load}");
                }
                refinereyDataList.Add("----------------");
            }

            refinereyDataList.Add("");
            refineryPrintData.Init(refinereysDisplay, stat, refinereyDataList);
        }

        public void PrintRefs()
        {
          
            refineryPrintData.PrintDynamicText();
        }

        /// <summary>
        /// Привязка руды к готовой продукции и рецептам
        /// </summary>
        public void RefinereysSaveOreData()
        {
           
        }

        public bool TryUnloadRefinerey(IMyRefinery refinerey)
        {
            var targetOreInventory = oreInventories.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            if (!targetOreInventory.Any())
                return false;

            var currentCargo = refinerey.InputInventory.ItemCount;

            for (int i = currentCargo; i >= 0; i--)
            {
                var getItem = refinerey.InputInventory.GetItemAt(i);

                if (getItem == null)
                    continue;

                var item = getItem.Value;

                if (item.Type.TypeId == "MyObjectBuilder_Ore")
                {
                    TransferItem(item, refinerey.InputInventory, targetOreInventory, i);
                }
            }

            currentCargo = refinerey.InputInventory.ItemCount;

            if (currentCargo == 0)
                return true;

            return false;//разгрузка не удалась
        }
       
        public bool TryLoadRefinerey(IMyRefinery refinerey, MyItemType ore)
        {
            var targetOreInventory = oreInventories.Where(i => i.ItemCount > 0);

            if (!targetOreInventory.Any())
                return false;

            foreach (var inv in targetOreInventory)
            {
                var item = inv.FindItem(ore);

                if(item.HasValue)
                {
                    inv.TransferItemTo(refinerey.InputInventory, item.Value);

                    if (refinerey.InputInventory.FindItem(ore).HasValue)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Перекладываем слитки из печек по контейнерам
        /// </summary>
        public void UnloadRefinereys()
        {
            if (!needReplaceIngots)
                return;

            var targetIngotInventory = ingotInventorys.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            var refsInventory = refinereys.Where(r => !r.Closed)
                                          .Select(i => i.GetInventory(1))
                                          .Where(i => i.ItemCount > 0);

            Echo("Replace ingots starting");

            if  (!refsInventory.Any())
            {
                Echo("------No items to transfer-----");
                return;
            }

            TransferItems(refsInventory, targetIngotInventory);
        }

        /// <summary>
        /// Поиск компонентов
        /// </summary>
        public void FindParts()
        {
            Echo("Find parts in containers");

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
            foreach (var dict in equipmentDictionary)
            {
                dict.Value.Current = 0;
            }


            //Блок получения всех компонентов в контейнерах
            foreach (var inventory in containerInventories)
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
                        //Добавляем только те слитки, у которых есть возомжность построить и которых нет в списке компонентов
                        if (partsDictionary.ContainsKey(item.Type.SubtypeId))
                        {//удаляем компонент из списка руд, если у него есть чертеж
                            if (buildedIngotsDictionary.ContainsKey(item.Type.SubtypeId))
                            {
                                buildedIngotsDictionary.Remove(item.Type.SubtypeId);
                            }
                            continue;
                        }

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
                    else if ((item.Type.TypeId == "MyObjectBuilder_PhysicalGunObject") || (item.Type.TypeId == "MyObjectBuilder_ConsumableItem") || (item.Type.TypeId == "MyObjectBuilder_PhysicalObject") || (item.Type.TypeId == "MyObjectBuilder_OxygenContainerObject") || (item.Type.TypeId == "MyObjectBuilder_GasContainerObject"))//Испльзуемые вещи
                    {
                        if (equipmentDictionary.ContainsKey(item.Type.SubtypeId))
                        {
                            equipmentDictionary[item.Type.SubtypeId].Current += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            equipmentDictionary.Add(item.Type.SubtypeId, new ItemBalanser { Current = item.Amount.ToIntSafe() });
                        }
                    }
                }
                productionItems.Clear();
            }//
        }

        /// <summary>
        /// Разгрузка сборщиков
        /// </summary>
        public void ReplaceParts()
        {
            if (!needReplaceParts)
                return;

            var assInventory = assemblers.Where(a => !a.Closed && !a.CustomName.Contains(assemblersBlueprintLeanerName) && a.Mode == MyAssemblerMode.Assembly)
                                         .Select(i => i.GetInventory(1))
                                         .Where(i => i.ItemCount > 0);

            Echo("Replace parts starting");

            if (!assInventory.Any())
            {
                Echo("------No items to transfer-----");
                return;
            }

            foreach (var ass in assInventory)
            {
                var currentCargo = ass.ItemCount;

                for (int i = currentCargo; i >= 0; i--)
                {
                    var getItem = ass.GetItemAt(i);

                    if (getItem == null)
                        continue;

                    var item = getItem.Value;

                    SortingItem(item, ass, i);

                }
            }
        }

        /// <summary>
        /// Очистка входного инвентаря ассемблера
        /// </summary>
        public void ClearAssemblers()
        {
            Echo("------Clear assemblers------");

            var assInventory = assemblers.Where(a => !a.Closed && !a.CustomName.Contains(assemblersBlueprintLeanerName) && (a.IsQueueEmpty || a.Mode == MyAssemblerMode.Disassembly))
                                         .Select(i => i.GetInventory(0))
                                         .Where(i => i.ItemCount > 0);

            var targetInventory = ingotInventorys.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            var freeAssInv = specialAssemblers.Where(a => !a.Closed && a.IsQueueEmpty && !a.CustomName.Contains(assemblersBlueprintLeanerName) && a.Mode == MyAssemblerMode.Disassembly);

            foreach (var ass in freeAssInv)
            {
                ass.Mode = MyAssemblerMode.Assembly;
            }

            if (!assInventory.Any() || !targetInventory.Any())
                return;

            TransferItems(assInventory, targetInventory);
        }

        /// <summary>
        /// Считывает данные с дисплея, с корутиной для potato серверов
        /// </summary>
        public IEnumerator<bool> ReadPartsData()
        {
            if (autoBuildPanel == null)
                yield return false;

            Echo("Read parts LCD");

            //Автосборка компонентов
            if (useAutoBuildSystem)
            {
                partsDisplayData.Clear();
                autoBuildPanel?.ReadText(partsDisplayData);

                var strings = partsDisplayData.ToString().Split('\n');

                foreach (var str in strings)
                {
                    if (partsStateMachineCounter > maxPartsPerScan)
                    {
                        partsStateMachineCounter = 0;
                        yield return true;
                    }
                    partsStateMachineCounter++;

                    System.Text.RegularExpressions.Match ResultReg = ProdItemFullRegex.Match(str);

                    if (ResultReg.Success)
                    {
                        int amount = 0;
                        string name = ResultReg.Groups["Name"].Value;

                        if (int.TryParse(ResultReg.Groups["Amount"].Value, out amount))
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
                }
            }///
        }

        /// <summary>
        /// Отображение компонентов на дисплее
        /// </summary>
        public void PrintAutobuildComponents()
        {

            if (autoBuildPanel == null)
                return;

            Echo("Update parts LCD");

            //Блок вывода инфорации на дисплеи
            string sysState = useAutoBuildSystem == true ? "Auto mode ON" : "Auto mode OFF";
            autoBuildPanel?.WriteText("", false);
            autoBuildPanel?.WriteText($"<<-------------Production------------->>" +
                                     $"\n{sysState}", true);

            autoBuildPanel?.WriteText("\n<<-----------Parts----------->>", true);

            foreach (var dict in partsDictionary.OrderBy(k => k.Key))
            {
                if (blueprintData.ContainsKey(dict.Key))
                {
                    autoBuildPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current}. / {dict.Value.Requested}", true);
                }
                else
                {
                    autoBuildPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} / {dict.Value.Requested}", true);
                }
            }

            autoBuildPanel?.WriteText("\n<<-----------Ammo----------->>", true);

            foreach (var dict in ammoDictionary.OrderBy(k => k.Key))
            {
                if (blueprintData.ContainsKey(dict.Key))
                {
                    autoBuildPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current}. / {dict.Value.Requested}", true);
                }
                else
                {
                    autoBuildPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} / {dict.Value.Requested}", true);
                }
            }

            autoBuildPanel?.WriteText("\n<<-----------Ingot----------->>", true);

            foreach (var dict in buildedIngotsDictionary.OrderBy(k => k.Key))
            {
                if (blueprintData.ContainsKey(dict.Key))
                {
                    autoBuildPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current}. / {dict.Value.Requested}", true);
                }
                else
                {
                    autoBuildPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} / {dict.Value.Requested}", true);
                }
            }

            autoBuildPanel?.WriteText("\n<<-----------Equipment----------->>", true);

            foreach (var dict in equipmentDictionary.OrderBy(k => k.Key))
            {
                if (blueprintData.ContainsKey(dict.Key))
                {
                    autoBuildPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current}. / {dict.Value.Requested}", true);
                }
                else
                {
                    autoBuildPanel?.WriteText($"\n{dict.Key} : {dict.Value.Current} / {dict.Value.Requested}", true);
                }
            }
        }

        /// <summary>
        /// Перенос из одного инвентаря в другой, возможно не за 1 цикл
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

                for (int i = currentCargo; i >= 0; i--)
                {
                    var item = inventory.GetItemAt(i);

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
        /// Перенос из одного инвентаря в другой по одному компоненту
        /// </summary>
        public void TransferItem(MyInventoryItem item, IMyInventory from, IEnumerable<IMyInventory> to, int pos = 0)
        {
            var availConts = to.Where(i => i.IsConnectedTo(from));

            if (!availConts.Any())
            {
                Echo($"No reacheable containers, check connection!");
                return;
            }

            var targInv = availConts.First().Owner as IMyCargoContainer;

            if (from.TransferItemTo(availConts.First(), pos, null, true))
            {
                Echo($"Transer item: {item} to {targInv?.CustomName}");
            }
            else
            {
                Echo($"Transer FAILED!: {item} to {targInv?.CustomName}");
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

            //Проверка контейнеров
            foreach (var inventory in containerInventories)
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
            if (ingotPanel == null)
                return;

            Echo("Update ingots LCD");

            //Вывод на дисплей
            ingotPanel?.WriteText("", false);
            ingotPanel?.WriteText($"<<-----------Ingots----------->>" +
                                   $"\nContainers:{ingotInventorys.Count()} " +
                                   $"{NumberToStringConverter(precentageIngotsVolume)}" +
                                   $"\nVolume: {precentageIngotsVolume} % {freeIngotStorageVolume} / {totalIngotStorageVolume} T", true);

            ingotPanel?.WriteText("\n<<-----------Ingots----------->>", true);

            foreach (var dict in ingotsDict.OrderBy(k => k.Key))
            {
                double amount = dict.Value.Current;
               
                ingotPanel?.WriteText($"\n{dict.Key} : {NumberConverter(amount)} ", true);
            }
        }

        /// <summary>
        /// Система управления питанием базы
        /// </summary>
        public void GetPowerData()
        {
            if (!usePowerManagmentSystem)
                return;

            Echo("Get power data");
            Echo($"Batt:{batteries.Count}");
            Echo($"Gens:{generators.Count}");

            maxStoredPower = batteries.Any() ? batteries.Where(b => !b.Closed).Sum(b => b.MaxStoredPower) : 1;
            currentStoredPower = batteries.Any() ? batteries.Where(b => !b.Closed).Sum(b => b.CurrentStoredPower) : 0;

            inputPower = batteries.Where(b => !b.Closed).Sum(b => b.CurrentInput);
            outputPower = batteries.Where(b => !b.Closed).Sum(b => b.CurrentOutput);

            generatorsMaxOutputPower = generators.Any() ? generators.Where(g => !g.Closed && g.IsWorking).Sum(g => g.MaxOutput) : 1;
            generatorsOutputPower = generators.Any() ? generators.Where(g => !g.Closed && g.IsWorking).Sum(g => g.CurrentOutput) : 1;

            powerLoadPercentage = (float)Math.Round(generatorsOutputPower / generatorsMaxOutputPower * 100, 1);
            battsStoredPrecentage = (float)Math.Round(currentStoredPower / maxStoredPower * 100, 1);
           
        }

        /// <summary>
        /// Вывод инфорации о энергосистемы базы на дисплей
        /// </summary>
        public void PrintPowerData()
        {
            if (powerPanel == null)
                return;

            powerPanel?.WriteText("", false);
            powerPanel?.WriteText("<--------Power status--------->", true);
            powerPanel?.WriteText($"\nPower Load: {powerLoadPercentage} % {NumberToStringConverter(powerLoadPercentage)}"
                                 + $"\nBatteryStatus: {battsStoredPrecentage} % {NumberToStringConverter(battsStoredPrecentage)} "
                                 + $"\nTotal/Max stored:{Math.Round(currentStoredPower, 2)} / {maxStoredPower} MWh"
                                 + $"\nInput/Output:{Math.Round(inputPower, 2)} / {Math.Round(outputPower, 2)} {(inputPower > outputPower ? "+" : "-")} MWt/h "
                                 + $"\nGens maxOut/Out: {Math.Round(generatorsMaxOutputPower, 2)} / {Math.Round(generatorsOutputPower, 2)} MWT", true);

        }

        /// <summary>
        /// Детальная информация о электросистемах
        /// </summary>
        public void PowerSystemDetailed()
        {
            if (!usePowerManagmentSystem)
                return;

            Echo("Get power full");

            gasTanksDictoinary.Clear();

            foreach (var gen in gasTanks)
            {
                if(gasTanksDictoinary.ContainsKey(gen.BlockDefinition.ToString()))
                {
                    gasTanksDictoinary[gen.BlockDefinition.ToString()].Add(gen);
                }
                else
                {
                    gasTanksDictoinary.Add(gen.BlockDefinition.ToString(), new List<IMyGasTank> { gen });
                }
            }

            // Print

            detailedPowerPanel?.WriteText("", false);

            detailedPowerPanel?.WriteText($"GAS:", true);

            foreach (var tank in gasTanksDictoinary)
            {
                detailedPowerPanel?.WriteText($"\n----------------", true);

                var name = System.Text.RegularExpressions.Regex.Replace(tank.Value.First().CustomName, @"[\d-]", string.Empty);

                var cap = tank.Value.Any() ? tank.Value.Sum(t => t.Capacity) : 1;
                var totalStored = tank.Value.Any() ? tank.Value.Sum(t => t.Capacity * t.FilledRatio) : 0;

                var percentage = Math.Round(totalStored / cap * 100, 1);

                detailedPowerPanel?.WriteText($"\n{name} {NumberToStringConverter(percentage)}" +
                                              $"\n{NumberConverter(totalStored)} / {NumberConverter(cap)}", true);

            }


            //debugPanel?.WriteText("", false);
            //debugPanel?.WriteText($"{debugPanel.SurfaceSize}", true);

            //foreach(var gas in generators)
            //{
            //    debugPanel?.WriteText($"\n{gas.BlockDefinition}", true);
            //}


            //var reactorInventory = generators.Where(g => !g.Closed && g.HasInventory).Select(g => g.GetInventory(0)).ToList();
            //int reactorsCount = generators.Where(g => g is IMyReactor).Count();
            //int windCount = generators.Where(g => g.BlockDefinition.TypeId.ToString() == "MyObjectBuilder_WindTurbine").Count();
            //int gasCount = generators.Where(g => g.BlockDefinition.TypeId.ToString() == "MyObjectBuilder_HydrogenEngine").Count();

            //var hydrogenTanks = gasTanks.Where(g => g.BlockDefinition.ToString().Contains("HydrogenTank"));

            //maxHydrogenCap = hydrogenTanks.Any() ? hydrogenTanks.Sum(t => t.Capacity) : 1;
            //totalHydrogenStored = hydrogenTanks.Any() ? hydrogenTanks.Sum(t => t.Capacity * t.FilledRatio) : 0;

            //hydrogenPercentage = Math.Round(totalHydrogenStored / maxHydrogenCap * 100, 1);



            //Print data

            //detailedPowerPanel?.WriteText("", false);
            //detailedPowerPanel?.WriteText("<--------Gens status--------->", true);// $"\n{Math.Round(totalHydrogenStored / gasTanksDivider,1)} / {Math.Round(maxHydrogenCap / gasTanksDivider,1)} kL"
            //detailedPowerPanel?.WriteText($"\nWind: {windCount} React: {reactorsCount} GasGens: {gasCount} GasTanks: {gasTanks.Count} ", true);
            //detailedPowerPanel?.WriteText($"\nHydrogen: {hydrogenTanks.Count()} Filled: {hydrogenPercentage} % {NumberToStringConverter(hydrogenPercentage)} " +
            //                              $"\n{NumberConverter(totalHydrogenStored)} / {NumberConverter(maxHydrogenCap)}" +
            //                              $"\n-------------------", true);

            //foreach (var react in reactorInventory)
            //{
            //    if (react.ItemCount != 0)
            //    {
            //        var block = react.Owner as IMyReactor;

            //        var item = react.GetItemAt(0);

            //        if (item != null)
            //        {
            //            detailedPowerPanel?.WriteText($"\nR:{item.Value.Type.SubtypeId} / {item.Value.Amount.ToIntSafe()} {block?.IsWorking}", true);

            //            if (reactorPayloadLimitter)//ручная установка количества топлива
            //            {

            //            }
            //        }
            //    }
            //    else
            //    {
            //        var targInv = react.Owner as IMyReactor;
            //        targInv.UseConveyorSystem = true;
            //        detailedPowerPanel?.WriteText($"\nR:{targInv?.CustomName} EMPTY!!", true);
            //    }
            //}
        }

        /// <summary>
        /// Начать/остановить поиск черчежей
        /// </summary>
        public void BlueprintLearnerSwitcher()
        {
            if (learnerAss == null)
            {
                var ass = assemblers.Where(q => q.CustomName.Contains(assemblersBlueprintLeanerName)).FirstOrDefault();

                if (ass == null)
                {
                    return;
                }

                learnerAss = ass;
                SpecialAssemblerLastName = learnerAss.CustomName.Replace(assemblersBlueprintLeanerName,"");
                learnerAss.CustomName = assemblersBlueprintLeanerName + " Assembler ready to copy bps";
                learnerAss.ClearQueue();
                learnerAss.Enabled = false;
                assemblerBlueprintGetter = true;
            }
        }

        /// <summary>
        /// Сохранение названий черчежей компонентов, в ручном режиме
        /// </summary>
        public void SaveAllBlueprints()
        {
            if (!assemblerBlueprintGetter)
                return;

            var targetInventory = containers.Where(c => c.CustomName.Contains(componentsStorageName))
                                            .Select(i => i.GetInventory(0))
                                            .Where(i => !i.IsFull);

            var blueprints = new List<MyProductionItem>();
            var ass = learnerAss;
            if (ass == null)
                return;

            if (!targetInventory.Any())
                return;

            if (!learnerAss.CustomName.Contains(assemblersBlueprintLeanerName))
            {
                learnerAss.CustomName = SpecialAssemblerLastName;
                learnerAss.ClearQueue();
                learnerAss.Enabled = true;
                learnerAss = null;
                assemblerBlueprintGetter = false;
                return;
            }

            var assInv = ass.GetInventory(1);

            ass.CustomName = assemblersBlueprintLeanerName + " Assembler ready to copy bps";
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
                dataSystem.Set(bpcSection, key.Key, key.Value);
            }

            ReloadData();
        }

        /// <summary>
        /// Получение чертежа по названию компонента
        /// </summary>
        public bool TryGetBlueprint(string itemName, out MyDefinitionId blueprint)
        {
            blueprint = default(MyDefinitionId);

            string value = "";

            if(blueprintData.TryGetValue(itemName, out value))
            {
                string name = "MyObjectBuilder_BlueprintDefinition/" + value;

                if (!MyDefinitionId.TryParse(name, out blueprint))
                {
                    Echo($"WARNING cant parse: {name}");
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Автосборка компонентов
        /// </summary>
        void PartsAutoBuild()
        {
            if (!useAutoBuildSystem)
                return;

            Echo("Auto build system");

            var assemblers = specialAssemblers.Where(ass => !ass.Closed && !ass.CustomName.Contains(assemblersBlueprintLeanerName) && ass.Mode == MyAssemblerMode.Assembly).ToList();

            var reqItems = partsDictionary.Where(k => k.Value.Current < k.Value.Requested);
            var reqIngnotsItem = buildedIngotsDictionary.Where(k => k.Value.Current < k.Value.Requested);
            var reqAmmoItem = ammoDictionary.Where(k => k.Value.Current < k.Value.Requested);
            var reqEqItem = equipmentDictionary.Where(k => k.Value.Current < k.Value.Requested);

            //if (reqItems.Any() || reqIngnotsItem.Any())
            //{
            //    Echo($"Total to build:{reqItems.Count() + reqIngnotsItem.Count()}");
            //}

            foreach (var key in reqItems)
            {
                AddItemToAutoProduct(key, assemblers);
            }

            foreach (var key in reqIngnotsItem)
            {
                AddItemToAutoProduct(key, assemblers);
            }

            foreach (var key in reqAmmoItem)
            {
                AddItemToAutoProduct(key, assemblers);
            }

            foreach (var key in reqEqItem)
            {
                AddItemToAutoProduct(key, assemblers);
            }

        }

        /// <summary>
        /// Добавить  предмет на автосборку с учетом в очереди и предметов в сборщике
        /// </summary>
        private void AddItemToAutoProduct(KeyValuePair<string, ItemBalanser> key, List<IMyAssembler> availAssemblers)
        {
            var needed = key.Value.Requested - key.Value.Current;

            MyDefinitionId blueprint;

            if (!TryGetBlueprint(key.Key, out blueprint))
            {
                return;
            }

            var availAss = availAssemblers.Where(ass => !ass.Closed && ass.CanUseBlueprint(blueprint)).ToList();
            if (!availAss.Any())
                return;

            List<MyProductionItem> items = new List<MyProductionItem>();
            List<MyInventoryItem> invItems = new List<MyInventoryItem>();

            foreach (var ass in availAss)
            {
                items.Clear();
                invItems.Clear();

                ass.GetQueue(items);
                ass.OutputInventory.GetItems(invItems);

                var itemsInOutInv = invItems.Where(item => item.Type.SubtypeId.Contains(key.Key)).ToList();
                var neededItems = items.Where(i => i.BlueprintId.Equals(blueprint)).ToList();

                if (neededItems.Any())
                {
                    needed -= neededItems.Sum(i => i.Amount.ToIntSafe());
                    if (needed < 0)
                        return;
                }

                if (itemsInOutInv.Any())
                {
                    needed -= itemsInOutInv.Sum(i => i.Amount.ToIntSafe());
                    if (needed < 0)
                        return;
                }
            }

            int count = needed / availAss.Count;//целое число компонентов
            double div = needed % availAss.Count;//остаток от деления

            if (count > 0)
            {
                foreach (var ass in availAss)
                {
                    VRage.MyFixedPoint amount = (VRage.MyFixedPoint)count;
                    ass.AddQueueItem(blueprint, amount);

                    Echo($"Item added: {blueprint.SubtypeId} x {amount} to \n{ass.CustomName}");
                }
                if (div == 0)
                    return;

                availAss.First().AddQueueItem(blueprint, (MyFixedPoint)div);
            }
            else
            {
                if (div == 0)
                    return;

                availAss.First().AddQueueItem(blueprint, (MyFixedPoint)div);
            }
        }

        /// <summary>
        /// Балансировка компонентов по сборщикам
        /// </summary>
        //public void AssemblersBalancing()
        //{
        //    var assemblers = specialAssemblers.Where(ass => !ass.Closed && !ass.CustomName.Contains(assemblersBlueprintLeanerName) && ass.Mode == MyAssemblerMode.Assembly);
        //    var busyAssemblers = specialAssemblers.Where(a => !a.Closed && !a.IsQueueEmpty && !a.CustomName.Contains(assemblersBlueprintLeanerName) && a.Mode == MyAssemblerMode.Assembly);

        //    if (!busyAssemblers.Any())
        //        return;

        //    List<MyProductionItem> items = new List<MyProductionItem>();

        //    foreach (var assembler in busyAssemblers)
        //    {
        //        assembler.GetQueue(items);

        //        for (int i = items.Count-1; i >= 0; i--)
        //        {
        //            var item = items[i];
        //            var amount = item.Amount - 1;

        //            var availAssemblers = assemblers.Where(ass => ass.CanUseBlueprint(item.BlueprintId));

        //            if(availAssemblers.Any())
        //            {
                       
        //                assembler.ClearQueue();
        //                KeyValuePair<string, ItemBalanser> component = new KeyValuePair<string, ItemBalanser>("Construction", new ItemBalanser { Requested = amount.ToIntSafe() });

        //                AddItemToAutoProduct(component, availAssemblers.ToList());
        //            }

        //            //assembler.RemoveQueueItem(i, amount);
        //            //assembler.ClearQueue();

        //        }
        //        items.Clear();
        //    }
        //}

        /// <summary>
        /// Автосборка с помощью мода Nanobot
        /// </summary>
        public void NanobotOperations()
        {
            if (nanobotBuildModule == null)
                return;

            Echo("Nanobot system working");

            nanobotBuildQueue.Clear();

            nanobotBuildQueue = nanobotBuildModule.GetValue<Dictionary<MyDefinitionId, int>>("BuildAndRepair.MissingComponents");
            Echo($"Nanobot total components:{nanobotBuildQueue.Count}");

            if (useNanobotAutoBuild && nanobotBuildQueue.Any())
            {
                AddNanobotPartsToProduct();
            }
        }

        public void AddNanobotPartsToProduct()
        {

            var assemblers = specialAssemblers.Where(ass => !ass.Closed && !ass.CustomName.Contains(assemblersBlueprintLeanerName) && ass.Mode == MyAssemblerMode.Assembly).ToList();

            foreach (var bps in nanobotBuildQueue)
            {
                KeyValuePair<string, ItemBalanser> component = new KeyValuePair<string, ItemBalanser>(bps.Key.SubtypeName, new ItemBalanser { Requested = bps.Value });

                AddItemToAutoProduct(component, assemblers);

            }
        }

        public void PrintNanobotQueue()
        {
            if ((nanobotBuildModule == null) || (nanobotBuildModule.Closed) || (nanobotDisplay == null) || (nanobotDisplay.Closed))
            {
                nanobotDisplay?.WriteText("\n\n     -------->>>No Nanobot module<<<--------", false);
                return;
            }

            nanobotDisplay?.WriteText("", false);
            nanobotDisplay?.WriteText("<<-----------Nanobot module----------->>", true);
            nanobotDisplay?.WriteText($"\nName:{nanobotBuildModule.CustomName}\nAuto request: {useNanobotAutoBuild}"
                                      +"\n-------------------------", true);

            foreach (var comp in nanobotBuildQueue.OrderBy(c => c.Key.ToString()))
            {
                nanobotDisplay?.WriteText($"\n{comp.Key.SubtypeName} X {comp.Value}", true);
            }
        }

        /// <summary>
        /// Поиск руд в контейнерах не для руд и перенос куда надо
        /// </summary>
        public void ContainersSortingOres()
        {
            if (!containerSorting)
                return;

            Echo("Sorting ores to conts");

            var nonOreInventories = containers.Where(c => (!c.Closed) && !c.CustomName.Contains(oreStorageName))
                                              .Select(i => i.GetInventory(0));

            var targetOreInventory = oreInventories.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            if (!targetOreInventory.Any())
            {
                Echo("No target containers!");
                return;
            }

            Echo($"Conts:{nonOreInventories.Count()}");

            foreach (var inv in nonOreInventories)
            {
                var currentCargo = inv.ItemCount;

                for (int i = currentCargo; i >= 0; i--)
                {
                    var getItem = inv.GetItemAt(i);

                    if (getItem == null)
                        continue;

                    var item = getItem.Value;

                    if (item.Type.TypeId == "MyObjectBuilder_Ore")//Руда
                    {
                        TransferItem(item, inv, targetOreInventory, i);
                    }
                }
            }
        }

        /// <summary>
        /// Перенос слитков в нужный контейнер
        /// </summary>
        public void ContainersSortingIngots()
        {
            if (!containerSorting)
                return;

            Echo("Sorting ingots to conts");

            var nonIngotInventories = containers.Where(c => (!c.Closed) && !c.CustomName.Contains(ingotStorageName))
                                                .Select(i => i.GetInventory(0));

            var targetIngotInventory = ingotInventorys.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            if (!targetIngotInventory.Any())
            {
                Echo("No target containers!");
                return;
            }

            Echo($"Conts:{nonIngotInventories.Count()}");

            foreach (var inv in nonIngotInventories)
            {
                var currentCargo = inv.ItemCount;

                for (int i = currentCargo; i >= 0; i--)
                {
                    var getItem = inv.GetItemAt(i);

                    if (getItem == null)
                        continue;

                    var item = getItem.Value;

                    if (item.Type.TypeId == "MyObjectBuilder_Ingot")//Слитки 
                    {
                        TransferItem(item, inv, targetIngotInventory, i);
                    }
                }
            }
        }

        /// <summary>
        /// Перенос всех построенных компонентов в нужные контейнера
        /// </summary>
        public void ContainersSortingParts()
        {
            if (!containerSorting)
                return;

            Echo("Sorting parts to conts");

            var nonPartsInventories = containers.Where(c => (!c.Closed) && !c.CustomName.Contains(componentsStorageName))
                                                .Select(i => i.GetInventory(0));

            var targetItemInventory = partsInventories.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            if (!targetItemInventory.Any())
            {
                Echo("No target containers!");
                return;
            }

            Echo($"Conts:{nonPartsInventories.Count()}");

            foreach (var inv in nonPartsInventories)
            {
                var currentCargo = inv.ItemCount;

                for (int i = currentCargo; i >= 0; i--)
                {
                    var getItem = inv.GetItemAt(i);

                    if (getItem == null)
                        continue;

                    var item = getItem.Value;

                    if (item.Type.TypeId == "MyObjectBuilder_Component")//Части
                    {
                        TransferItem(item, inv, targetItemInventory, i);
                    }
                }

            }
        }

        /// <summary>
        /// Перенос всех боеприпасов в нужные контейнера
        /// </summary>
        public void ContainersSortingAmmo()
        {
            if (!containerSorting)
                return;

            Echo("Sorting ammo to conts");

            var nonAmmoInventories = containers.Where(c => (!c.Closed) && !c.CustomName.Contains(ammoStorageName))
                                               .Select(i => i.GetInventory(0));

            var targetAmmoInventory = ammoInventorys.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            if (!targetAmmoInventory.Any())
            {
                Echo("No target containers!");
                return;
            }

            Echo($"Conts:{nonAmmoInventories.Count()}");

            foreach (var inv in nonAmmoInventories)
            {
                var currentCargo = inv.ItemCount;

                for (int i = currentCargo; i >= 0; i--)
                {
                    var getItem = inv.GetItemAt(i);

                    if (getItem == null)
                        continue;

                    var item = getItem.Value;

                    if (item.Type.TypeId == "MyObjectBuilder_AmmoMagazine")//Боеприпасы
                    {
                        TransferItem(item, inv, targetAmmoInventory, i);
                    }
                }
            }

        }

        /// <summary>
        /// Перенос всех используемых игроком предметов в заданный контейнер
        /// </summary>
        public void ContainersSortingItems()
        {
            if (!containerSorting)
                return;

            Echo("Sorting items to conts");

            var nonEquipInventories = containers.Where(c => (!c.Closed) && !c.CustomName.Contains(equipStorageName))
                                                .Select(i => i.GetInventory(0));

            var targetEquipInventory = itemInventorys.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            if (!targetEquipInventory.Any())
            {
                Echo("No target containers!");
                return;
            }


            Echo($"Conts:{nonEquipInventories.Count()}");

            foreach (var inv in nonEquipInventories)
            {
                var currentCargo = inv.ItemCount;

                for (int i = currentCargo; i >= 0; i--)
                {
                    var getItem = inv.GetItemAt(i);

                    if (getItem == null)
                        continue;

                    var item = getItem.Value;

                    if ((item.Type.TypeId == "MyObjectBuilder_PhysicalGunObject") || (item.Type.TypeId == "MyObjectBuilder_ConsumableItem") || (item.Type.TypeId == "MyObjectBuilder_PhysicalObject") || (item.Type.TypeId == "MyObjectBuilder_OxygenContainerObject") || (item.Type.TypeId == "MyObjectBuilder_GasContainerObject"))//Испльзуемые вещи
                    {
                        TransferItem(item, inv, targetEquipInventory, i);
                    }
                }

            }

        }

        /// <summary>
        /// Выгрузка ресурсов из коннекторов
        /// </summary>
        public void ConnectorSorting()
        {
            if (!containerSorting)
                return;

            Echo("Connectors sorting");

            var connInv = connectors.Where(c => !c.Closed && c.HasInventory)
                                    .Select(i => i.GetInventory(0))
                                    .Where(c => c.ItemCount > 0);

            if (!connInv.Any())
                return;

            foreach(var inv in connInv)
            {
                var currentCargo = inv.ItemCount;

                for (int i = currentCargo; i >= 0; i--)
                {
                    var getItem = inv.GetItemAt(i);

                    if (getItem == null)
                        continue;

                    var item = getItem.Value;

                    SortingItem(item, inv, i);
                }
            }
        }

        /// <summary>
        /// Сортировка вещей по типу
        /// </summary>
        public void SortingItem(MyInventoryItem item, IMyInventory from, int pos = 0)
        {
            var targetOreInventory = oreInventories.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);
            var targetIngotInventory = ingotInventorys.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);
            var targetItemInventory = partsInventories.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);
            var targetAmmoInventory = ammoInventorys.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);
            var targetEquipInventory = itemInventorys.Where(i => ((double)i.CurrentVolume * 100 / (double)i.MaxVolume) < maxVolumeContainerPercentage);

            if (item.Type.TypeId == "MyObjectBuilder_Ore")//Руда
            {
                TransferItem(item, from, targetOreInventory, pos);
            }
            else if (item.Type.TypeId == "MyObjectBuilder_Ingot")//Слитки 
            {
                TransferItem(item, from, targetIngotInventory, pos);
            }
            else if (item.Type.TypeId == "MyObjectBuilder_Component")//Части
            {
                TransferItem(item, from, targetItemInventory, pos);
            }
            else if (item.Type.TypeId == "MyObjectBuilder_AmmoMagazine")//Боеприпасы
            {
                TransferItem(item, from, targetAmmoInventory, pos);
            }
            else if ((item.Type.TypeId == "MyObjectBuilder_PhysicalGunObject") || (item.Type.TypeId == "MyObjectBuilder_ConsumableItem") || (item.Type.TypeId == "MyObjectBuilder_PhysicalObject") || (item.Type.TypeId == "MyObjectBuilder_OxygenContainerObject") || (item.Type.TypeId == "MyObjectBuilder_GasContainerObject"))//Испльзуемые вещи
            {
                TransferItem(item, from, targetEquipInventory, pos);
            }
        }

        /// <summary>
        /// Преобразует число % в строковый тип
        /// </summary>
        public string NumberToStringConverter(double percent)
        {
           
            int loadedSymb = (int)(maxContRenderSymbols * (percent / 100));
            int freeSymb = (int)(maxContRenderSymbols * (1 - percent / 100));

            string state = "[";
            state += string.Concat(Enumerable.Repeat("|", loadedSymb));
            state += string.Concat(Enumerable.Repeat("-", freeSymb));
            state += "]";

            return state;
        }

        public string NumberConverter(double amount)
        {
            string result = amount.ToString();

            if (amount > 1000000)
                result = Math.Round(amount /= 1000000, 2).ToString() + " M";
            else if (amount > 100000)
                result = Math.Round(amount /= 1000, 2).ToString() + " K";
            return result;
        }



        public class TextAdapter
        {
            int upperTextPoz = 0;
            int downTextPoz = 0;
            int dir = 1;

            int wStringsCount = 0;

            float surfaceHeight = 0;
            float surfaceWight = 0;
            float fontSize = 0;
            float padding = 0;

            public List<string> ConstantsStrings;
            public List<string> DynamicStrings;

            public IMyTextPanel Panel;


            public TextAdapter()
            {
                ConstantsStrings = new List<string>();
                DynamicStrings = new List<string>();
            }

            public void Init(IMyTextPanel panel, List<string> constantText, List<string> dynamicText)
            {
                Panel = panel;
                surfaceHeight = panel.SurfaceSize.X;
                surfaceWight = panel.SurfaceSize.Y;
                fontSize = panel.FontSize;
                padding = panel.TextPadding;

                ConstantsStrings = constantText;
                DynamicStrings = dynamicText;
            }

            public void PrintDynamicText()
            {
                wStringsCount = (int)Math.Round((surfaceWight - surfaceWight * (padding / 100)) / 30 / fontSize);

                Panel?.WriteText("", false);

                int statStr = ConstantsStrings.Count;
                int dynStr = DynamicStrings.Count;

                if (statStr >= wStringsCount)
                {
                    Panel?.WriteText("Text is to complex! Cant print", true);
                    return;
                }

                for (int i = 0; i < statStr; i++)
                {
                    Panel?.WriteText($"{ConstantsStrings[i]}\n", true);
                  
                }

                 wStringsCount -= statStr;

                if (dynStr > wStringsCount)
                {
                    downTextPoz = upperTextPoz + wStringsCount;

                    if (downTextPoz >= dynStr)
                    {
                        dir *= -1;
                        downTextPoz = dynStr;
                    }

                    if (upperTextPoz < 0)
                    {
                        dir *= -1;
                        upperTextPoz = 0;
                    }
                }
                else
                {
                    downTextPoz = dynStr;
                    upperTextPoz = 0;
                }


                for (int i = upperTextPoz; i < downTextPoz; i++)
                {
                    Panel?.WriteText($"{DynamicStrings[i]}\n", true);
                }

                upperTextPoz += dir;
            }

        }

        public class ItemBalanser
        {
            public int Current { set; get; } = 0;
            public int Requested { set; get; } = 0;
        }

        public class OreData
        {
            public bool Ready { set; get; } = false;
            public string Type { set; get; }
            public string Blueprint { set; get; }
            public int Priority { set; get; } = 0;
            public int Amount { set; get; } = 0;

            public List<string> IngotNames = new List<string>();

        }

        public class RefinereyData
        {
            public float Effectiveness { set; get; } = 0;
            public float InputInventory { set; get; } = 0;
            public float OutputInventory { set; get; } = 0;
            public List<string> OreLoad { set; get; } = new List<string>();

            public static Dictionary<string, int> TotalOresInProcessing { set; get; } = new Dictionary<string, int>();

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

                if (mainDisplay != null)
                {
                    mainDisplay.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    mainDisplay.FontSize = 1;
                }

            }

            public void AddRuntime()
            {
                UpdateTime = mainProgram.Runtime.LastRunTimeMs;
                avrTime += UpdateTime;

            }

            public void AddInstructions()
            {
                TotalInstructions = mainProgram.Runtime.CurrentInstructionCount;
                MaxInstructions = mainProgram.Runtime.MaxInstructionCount;
                avrInst += TotalInstructions;

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
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
        string lcdPowerDetailedName = "LCD power full";


        /////////////DO NOT EDIT BELOW THE LINE//////////////////

        MyIni dataSystem;
        //дисплеи
        IMyTextPanel debugPanel;
        IMyTextPanel ingnotPanel;
        IMyTextPanel powerPanel;
        IMyTextPanel detailedPowerPanel;
        IMyTextPanel partsPanel;
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

        bool needReplaceIngnots = false;
        bool needReplaceParts = false;
        bool usePowerManagmentSystem = false;
        bool useDetailedPowerMonitoring = false;
        bool useAutoBuildSystem = false;
        bool getOreFromTransports = false;

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

        int totalInstructions = 0;
        int maxInstructions = 0;
        double updateTime = 0;

        int reactorMinFuel = 100;

        //словарь готовых компонентов и словарь запросов на автосборку компонентов
        Dictionary<string, int> partsDictionary;
        Dictionary<string, int> partsRequester;

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

            partsDictionary = new Dictionary<string, int>();
            partsRequester = new Dictionary<string, int>();

            dataSystem = new MyIni();
            GetIniData();

        }

        /// <summary>
        /// Функия выполняется каждые 100 тиков
        /// </summary>
        public void Main(string args, UpdateType updateType)
        {

            if (updateType == UpdateType.Terminal)
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
                    Echo("Try prunt pbs names");
                    PrintAllBluepritnsNames();
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
                    // PartsAutoBuild();
                    GetOreFromTransport();
                    AddInstructions();
                    break;
            }

            currentTick++;
            if (currentTick == 4)
                currentTick = 0;

        }

        private void AddInstructions()
        {
            totalInstructions = Runtime.CurrentInstructionCount;
            updateTime = Runtime.LastRunTimeMs;
            maxInstructions = Runtime.MaxInstructionCount;
            mainDisplay.WriteText($"Calls/Max: {totalInstructions} / {maxInstructions}" +
                                  $"\nTime: {updateTime}", false);
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

                oreStorageName = dataSystem.Get("Names", "oreStorageName").ToString();
                ingnotStorageName = dataSystem.Get("Names", "ingnotStorageName").ToString();
                componentsStorageName = dataSystem.Get("Names", "componentsStorageName").ToString();

                lcdInventoryIngnotsName = dataSystem.Get("Names", "lcdInventoryIngnotsName").ToString();
                lcdPowerSystemName = dataSystem.Get("Names", "lcdPowerSystemName").ToString();
                lcdPartsName = dataSystem.Get("Names", "lcdPartsName").ToString();
                lcdInventoryDebugName = dataSystem.Get("Names", "lcdInventoryDebugName").ToString();
                lcdPowerDetailedName = dataSystem.Get("Names", "lcdPowerDetailedName").ToString();
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

                dataSystem.AddSection("Names");
                dataSystem.Set("Names", "oreStorageName", "Ore");
                dataSystem.Set("Names", "ingnotStorageName", "Ingnot");
                dataSystem.Set("Names", "componentsStorageName", "Parts");
                dataSystem.Set("Names", "lcdInventoryIngnotsName", "LCD Inventory");
                dataSystem.Set("Names", "lcdPowerSystemName", "LCD Power");
                dataSystem.Set("Names", "lcdPowerDetailedName", "LCD power full");
                dataSystem.Set("Names", "lcdPartsName", "LCD Parts");
                dataSystem.Set("Names", "lcdInventoryDebugName", "LCD Debug");



                Me.CustomData = dataSystem.ToString();
            }

            Echo("Custom data ready");
        }

        public void PrintAllBluepritnsNames()
        {
            debugPanel?.WriteText("", false);

            var blueprints = new List<MyProductionItem>();
            var ass = assemblers.Where(q => !q.IsQueueEmpty).ToList();
            foreach (var a in ass)
            {
                a.GetQueue(blueprints);

                foreach (var bp in blueprints)
                {
                    debugPanel?.WriteText($"{bp.BlueprintId}\n", true);
                }
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
            else
            {
                Echo($"Debug LCDs found:{lcdInventoryDebugName}");
            }

            if ((ingnotPanel == null) || (ingnotPanel.Closed))
            {
                ingnotPanel = GridTerminalSystem.GetBlockWithName(lcdInventoryIngnotsName) as IMyTextPanel;
            }
            else
            {
                Echo($"Ingnot LCDs found:{lcdInventoryIngnotsName}");
            }

            if ((powerPanel == null) || (powerPanel.Closed))
            {
                powerPanel = GridTerminalSystem.GetBlockWithName(lcdPowerSystemName) as IMyTextPanel;
            }
            else
            {
                Echo($"Power LCDs found:{lcdPowerSystemName}");
            }

            if ((detailedPowerPanel == null) || (detailedPowerPanel.Closed))
            {
                detailedPowerPanel = GridTerminalSystem.GetBlockWithName(lcdPowerDetailedName) as IMyTextPanel;
            }
            else
            {
                Echo($"Full power LCDs found:{lcdPowerDetailedName}");
            }


            if ((partsPanel == null) || (partsPanel.Closed))
            {
                partsPanel = GridTerminalSystem.GetBlockWithName(lcdPartsName) as IMyTextPanel;
            }
            else
            {
                Echo($"Parts LCDs found:{lcdPartsName}");
            }

            AddInstructions();
        }

        /// <summary>
        /// Отладка
        /// </summary>
        public void WriteDebugText()
        {
            debugPanel.WriteText("", false);
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


            refinereys = blocks.Where(b => b is IMyRefinery).Where(r => r.IsFunctional).Select(t => t as IMyRefinery).ToList();
            assemblers = blocks.Where(b => b is IMyAssembler).Where(a => a.IsFunctional).Select(t => t as IMyAssembler).ToList();
            containers = blocks.Where(b => b is IMyCargoContainer).Where(c => c.IsFunctional).Select(t => t as IMyCargoContainer).ToList();
            batteries = blocks.Where(b => b is IMyBatteryBlock).Where(b => b.IsFunctional).Select(t => t as IMyBatteryBlock).ToList();
            gasTanks = blocks.Where(b => b is IMyGasTank).Where(g => g.IsFunctional).Select(t => t as IMyGasTank).ToList();

            generators = blocks.Where(b => b is IMyPowerProducer).Where(r => r.IsFunctional).Select(t => t as IMyPowerProducer).ToList();

            Echo(">>>-------------------------------<<<");
            Echo($"Refinereys found:{refinereys.Count}");
            Echo($"Assemblers found:{assemblers.Count}");
            Echo($"Containers found my/conn: {containers.Where(c => c.CubeGrid == Me.CubeGrid).Count()}/" +
                                   $"{containers.Where(c => c.CubeGrid != Me.CubeGrid).Count()}");

            Echo($"Battery found my/conn: {batteries.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
                                        $"{batteries.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");

            Echo($"Generators found my/conn: {generators.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
                                         $"{generators.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");

            Echo(">>>-------------------------------<<<");
            AddInstructions();
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

                var availConts = targetInventory.Where(inv => inv.CanTransferItemTo(refs, MyItemType.MakeIngot("MyObjectBuilder_Ingot")));
                if (!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }
                var item = refs.GetItemAt(0);
                var targInv = availConts.First().Owner as IMyCargoContainer;

                Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName} ");
                refs.TransferItemTo(availConts.First(), 0, null, true);

            }
            AddInstructions();
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

            var ingnotInventorys = containers.Where(c => c.CustomName.Contains(ingnotStorageName))
                                             .Select(i => i.GetInventory(0));

            Dictionary<string, int> CargoDict = new Dictionary<string, int>();

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
                        if (CargoDict.ContainsKey(item.Type.SubtypeId))
                        {
                            CargoDict[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                        }
                        else
                        {
                            CargoDict.Add(item.Type.SubtypeId, item.Amount.ToIntSafe());
                        }
                    }
                }
            }

            ingnotPanel?.WriteText("", true);
            ingnotPanel?.WriteText($"Total/max ingnot cont volume: {freeIngnotStorageVolume} / {totalIngnotStorageVolume} T", false);

            foreach (var dict in CargoDict.OrderBy(k => k.Key))
            {
                ingnotPanel?.WriteText($"\n{dict.Key} : {dict.Value} ", true);
            }
            AddInstructions();

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
                var availConts = targetInventory.Where(inv => inv.CanTransferItemTo(cargo, MyItemType.MakeOre("MyObjectBuilder_Ore")));

                if (!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }
                var item = cargo.GetItemAt(0);
                var targInv = availConts.First().Owner as IMyCargoContainer;

                Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName} ");
                cargo.TransferItemTo(availConts.First(), 0, null, true);

            }
            AddInstructions();
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
                var availConts = targetInventory.Where(inv => inv.CanTransferItemTo(ass, MyItemType.MakeComponent("MyObjectBuilder_Component")));
                if (!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }
                var item = ass.GetItemAt(0);
                var targInv = availConts.First().Owner as IMyCargoContainer;

                Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName} ");
                ass.TransferItemTo(availConts.First(), 0, null, true);

            }
            AddInstructions();
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
            AddInstructions();

        }//DisplayParts()

        /// <summary>
        /// Система управления питанием базы
        /// </summary>
        public void PowerMangment()
        {
            if (!usePowerManagmentSystem)
                return;


            Echo("------Power managment system-------");

            maxStoredPower = batteries.Sum(b => b.MaxStoredPower);
            currentStoredPower = batteries.Sum(b => b.CurrentStoredPower);

            inputPower = batteries.Sum(b => b.CurrentInput);
            outputPower = batteries.Sum(b => b.CurrentOutput);

            generatorsMaxOutputPower = generators.Sum(g => g.MaxOutput);
            generatorsOutputPower = generators.Sum(g => g.CurrentOutput);

            PowerSystemDetailed();
            AddInstructions();
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

                string lowCount = items[0].Amount < reactorMinFuel ? "TO LOW" : "";
                detailedPowerPanel?.WriteText($"\nR: {items[0].Type.SubtypeId} / {items[0].Amount} {lowCount}", true);

            }
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
        /// Сислема заказа производства недостающих компонентов
        /// </summary>
        public void PartsAutoBuild()
        {
            if (!useAutoBuildSystem)
                return;

            Echo("------Auto build system-------");
            partsRequester.Clear();

            string[] lines = Me.CustomData.Split('\n');

            foreach (var line in lines)
            {
                string[] itemName = line.Split('=');

                if (!partsRequester.ContainsKey(itemName[0]))
                {
                    int count = 0;

                    if (int.TryParse(itemName[1], out count))
                    {
                        partsRequester.Add(itemName[0], count);
                    }
                }

            }
            Echo($"Detected auto build componetns: {partsRequester.Count}");

            var workingAssemblers = assemblers.Where(ass => !ass.IsQueueEmpty).ToList();

            if (workingAssemblers.Count == 0)
            {
                foreach (var req in partsRequester)
                {
                    if (partsDictionary.ContainsKey(req.Key))
                    {
                        int needComponents = req.Value - partsDictionary[req.Key];
                        if (needComponents > 0)
                        {
                            string name = "MyObjectBuilder_BlueprintDefinition/" + req.Key;//Название компонента для строительсвта
                            var parser = blueprintDataBase.Where(n => n.Contains(req.Key)).FirstOrDefault();//Если компонент стандартный ищем его в готовом списке

                            if (parser != null)
                                name = parser;

                            Echo($"Start build: {req.Key} X {needComponents}");
                            Echo($"D_name: {name}");
                            Echo("\n");

                            MyDefinitionId blueprint;
                            if (!MyDefinitionId.TryParse(name, out blueprint))
                                Echo($"WARNING cant parse: {name}");

                            var assemblersCanBuildThis = assemblers.Where(a => a.CanUseBlueprint(blueprint)).ToList();
                            var count = needComponents / assemblersCanBuildThis.Count;
                            if (count < 1)
                                count = 1;

                            foreach (var asembler in assemblersCanBuildThis)
                            {
                                VRage.MyFixedPoint amount = (VRage.MyFixedPoint)count;
                                asembler.AddQueueItem(blueprint, amount);
                                Echo($"Assemblers starts: {req.Key}");
                            }
                        }
                    }
                }
            }
            AddInstructions();
        }//PartsAutoBuild()

        ///END OF SCRIPT///////////////
    }

}
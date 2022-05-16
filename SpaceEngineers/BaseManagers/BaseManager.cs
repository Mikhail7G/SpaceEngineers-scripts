using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;

namespace SpaceEngineers.BaseManagers.Base
{
    public sealed class Program : MyGridProgram
    {

        ////////////SCRIPT START HERE/////////////////////////

        //названия всех необходимых компонентов, названия дислеев точное, название контейнеров дожно содержать строку названия
        //пример Storage 1 менять только строковые названия!

        string oreStorageName = "Ore";
        string ingnotStorageName = "Storage";
        string componentsStorageName = "Parts";
        string lcdInventoryIngnotsName = "LCD Inventory";
        string lcdPowerSystemName = "LCD Power";
        string lcdPartsName = "LCD Parts";
        string lcdInventoryDebugName = "LCD Debug";
        string emerHydrogenGeneratorsGroupName = "EmerHydGens";


       /////////////DO NOT EDIT BELOW THE LINE//////////////////

        //дисплеи
        IMyTextPanel debugPanel;
        IMyTextPanel ingnotPanel;
        IMyTextPanel powerPanel;
        IMyTextPanel partsPanel;

        //все объекты, содержащие инвентарь
        IEnumerable<IMyInventory> inventories;

        //сборщики, печки, контейнера
        List<IMyRefinery> refinereys;
        List<IMyAssembler> assemblers;
        List<IMyCargoContainer> containers;
        List<IMyBatteryBlock> batteries;
        List<IMyGasTank> gasTanks;
        IMyBlockGroup emerHydGens;

        int totalIngnotStorageVolume = 0;
        int freeIngnotStorageVolume = 0;

        int totalPartsStorageVolume = 0;
        int freePartsStorageVolume = 0;

        int currentTick = 0;

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
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            inventories = new List<IMyInventory>();
            refinereys = new List<IMyRefinery>();
            assemblers = new List<IMyAssembler>();
            containers = new List<IMyCargoContainer>();
            batteries = new List<IMyBatteryBlock>();
            gasTanks = new List<IMyGasTank>();

            partsDictionary = new Dictionary<string, int>();
            partsRequester = new Dictionary<string, int>();
        }

        /// <summary>
        /// Функия выполняется каждые 100 тиков
        /// </summary>
        public void Main(string args)
        {
            Commands(args);

            Update();
        }

        public void Commands(string str)
        {
            string argument = str.ToUpper();

            switch(argument)
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
                    PrintAllBluepritnsNames();
                    break;
            }
        }


        public void Update()
        {
            FindLcds();
            FindInventories();

            switch(currentTick)
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
                    PartsAutoBuild();
                    GetOreFromTransport();
                    break;
            }

            currentTick++;
            if (currentTick == 3)
                currentTick = 0;

        }

        public void PrintAllBluepritnsNames()
        {
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

            if ((partsPanel == null) || (partsPanel.Closed))
            {
                partsPanel = GridTerminalSystem.GetBlockWithName(lcdPartsName) as IMyTextPanel;
            }
            else
            {
                Echo($"Parts LCDs found:{lcdPartsName}");
            }


        }

        /// <summary>
        /// Отладка
        /// </summary>
        public void WriteDebugText()
        {
           
            debugPanel.WriteText("", false);

            //}

            //var inv = containers.Where(c => c.CustomName.Contains(componentsStorageName)).FirstOrDefault().GetInventory(0);
            //List<MyInventoryItem> items = new List<MyInventoryItem>();
            //inv.GetItems(items);
            //{
            //    foreach(var item in items)
            //    {
            //        MyDefinitionId blueprint;
            //        MyDefinitionId.TryParse("MyObjectBuilder_BlueprintDefinition/" + item.Type.SubtypeId,out blueprint);
            //        debugPanel.WriteText(blueprint.SubtypeName + "\n", true);
            //    }
            //}
        }

        /// <summary>
        /// Поиск всех обьектов, печек, сборщиков, ящиков
        /// </summary>
        public void FindInventories()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);
            inventories = blocks.Where(b => b.HasInventory)
                                .Select(b=>b.GetInventory(b.InventoryCount-1));//берем из инвентаря готовой продукции


            refinereys = blocks.Where(b => b is IMyRefinery).Where(r => r.IsFunctional).Select(t => t as IMyRefinery).ToList();
            assemblers = blocks.Where(b => b is IMyAssembler).Where(a => a.IsFunctional).Select(t => t as IMyAssembler).ToList();
            containers = blocks.Where(b => b is IMyCargoContainer).Where(c => c.IsFunctional).Select(t => t as IMyCargoContainer).ToList();
            batteries = blocks.Where(b => b is IMyBatteryBlock).Where(b => b.IsFunctional).Select(t => t as IMyBatteryBlock).ToList();
            gasTanks = blocks.Where(b => b is IMyGasTank).Where(g => g.IsFunctional).Select(t => t as IMyGasTank).ToList();

            emerHydGens = GridTerminalSystem.GetBlockGroupWithName(emerHydrogenGeneratorsGroupName);

            Echo(">>>-------------------------------<<<");
            Echo($"Refinereys found:{refinereys.Count}");
            Echo($"Assemblers found:{assemblers.Count}");
            Echo($"Containers found my/conn: {containers.Where(c => c.CubeGrid == Me.CubeGrid).Count()}/" +
                                   $"{containers.Where(c => c.CubeGrid != Me.CubeGrid).Count()}");

            Echo($"Battery found my/conn: {batteries.Where(b => b.CubeGrid == Me.CubeGrid).Count()}/" +
                                        $"{batteries.Where(b => b.CubeGrid != Me.CubeGrid).Count()}");
            Echo(">>>-------------------------------<<<");
        }

        /// <summary>
        /// Перекладываем слитки из печек по контейнерам
        /// </summary>
        public void ReplaceIgnots()
        {
            var targetInventory = containers.Where(c => c.CustomName.Contains(ingnotStorageName))
                                            .Select(i => i.GetInventory(0))
                                            .Where(i => !i.IsFull);

            var refsInventory = refinereys.Select(i => i.GetInventory(1))
                                           .Where(i=>i.ItemCount>0);

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
                if(!availConts.Any())
                {
                    Echo($"No reacheable containers, check connection!");
                    continue;
                }
                var item = refs.GetItemAt(0);
                var targInv = availConts.First().Owner as IMyCargoContainer;

                Echo($"Transer item: {item.GetValueOrDefault()} to {targInv?.CustomName} ");
                refs.TransferItemTo(availConts.First(), 0, null, true);

            }
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

            foreach(var dict in CargoDict.OrderBy(k=>k.Key))
            {
                ingnotPanel?.WriteText($"\n{dict.Key} : {dict.Value} ", true);
            }
           
        }//DisplayIngnots()

        /// <summary>
        /// Выгрузка руды из подключенных к базе кораблей
        /// </summary>
        public void GetOreFromTransport()
        {
            Echo("------Replase Ore from transport------");
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);

            var externalContainers = blocks.Where(b => b is IMyCargoContainer)
                                           .Where(c => c.IsFunctional)
                                           .Where(c=>c.CubeGrid!=Me.CubeGrid)
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
        }

        /// <summary>
        /// Перекладка запчастей из сбощиков в контейнеры
        /// </summary>
        public void ReplaceParts()
        {
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
            
        }//DisplayParts()

        /// <summary>
        /// Система управления питанием базы
        /// </summary>
        public void PowerMangment()
        {
            Echo("------Power managment system-------");

            float maxStoredPower = 0;
            float currentStoredPower = 0;

            float inputPower = 0;
            float outputPower = 0;

            maxStoredPower = batteries.Sum(b => b.MaxStoredPower);
            currentStoredPower = batteries.Sum(b => b.CurrentStoredPower);

            inputPower = batteries.Sum(b => b.CurrentInput);
            outputPower = batteries.Sum(b => b.CurrentOutput);

            List<IMyTerminalBlock> emerGens = new List<IMyTerminalBlock>();
            emerHydGens.GetBlocks(emerGens);

            //Всего рабочих генераторов
            var aliveGens = emerGens.Where(g => !g.Closed)
                                    .Where(g => g.IsFunctional);
            //Генераторы включены
            var workingGens = aliveGens.Where(g => g.IsWorking);
            //Генераторы выключены
            var sleepingGens = aliveGens.Where(g => !g.IsWorking);

            float currentStoredPowerProcentage = currentStoredPower / maxStoredPower * 100;

            if (currentStoredPowerProcentage < 55)
            {
                if (outputPower > inputPower) 
                {
                    sleepingGens.ToList()[0].SetValueBool("OnOff", true);
                }
            }
            else
            {
                if (currentStoredPowerProcentage > 90)
                {

                    foreach (var gen in workingGens)
                    {
                        gen.SetValueBool("OnOff", false);
                    }
                }
            }

            Echo($"Emer hydrogen gens run/avail: {workingGens.Count()} / {aliveGens.Count()}");

            powerPanel?.WriteText("", false);
            powerPanel?.WriteText($"BatteryStatus:\nTotal/Max power:{Math.Round(currentStoredPower, 2)} / {maxStoredPower} MWt {Math.Round(currentStoredPowerProcentage,1)} %"
                                 + $"\nInput/Output:{Math.Round(inputPower,2)} / {Math.Round(outputPower,2)} {(inputPower > outputPower ? "+":"-")} MWt/h "
                                 + $"\nHudrogen gens run/avail: {workingGens.Count()}/{aliveGens.Count()}", true);
        }

        /// <summary>
        /// Сислема заказа производства недостающих компонентов
        /// </summary>
        public void PartsAutoBuild()
        {
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
        }//PartsAutoBuild()


        /////////////////////END OF SCRIPT///////////////////////////////////
    }

}

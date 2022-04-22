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
using SpaceEngineers.Game.ModAPI.Ingame;

namespace Inventory
{
    public sealed class Program : MyGridProgram
    {

        IMyCargoContainer storage;
        IMyCargoContainer parts;
        IMyInventory inventory;
        List<MyInventoryItem> items;

        IMyTextPanel lcdInventory;

        List<IMyRefinery> refinereys;
        List<IMyAssembler> assemblers;


        public Program()
        {
            items = new List<MyInventoryItem>();
            refinereys = new List<IMyRefinery>();
            assemblers = new List<IMyAssembler>();

            storage = GridTerminalSystem.GetBlockWithName("Storage") as IMyCargoContainer;
            parts = GridTerminalSystem.GetBlockWithName("Parts") as IMyCargoContainer;
            lcdInventory = GridTerminalSystem.GetBlockWithName("LCD Inventory") as IMyTextPanel;

            inventory = storage.GetInventory(0);

            GridTerminalSystem.GetBlocksOfType(refinereys);
            GridTerminalSystem.GetBlocksOfType(assemblers);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;

        }

        public void Main(string args)
        {

            lcdInventory.WriteText("", false);

            items.Clear();
            inventory.GetItems(items);
            items.Sort();

            var selItem = items.OrderBy(p => p.Type.SubtypeId);

            foreach (var item in selItem) 
            {
                if (item.Type.TypeId == "MyObjectBuilder_Ingot")//слитки
                {
                    lcdInventory.WriteText(item.Type.SubtypeId + ": " + item.Amount + "\n", true);
                }
            }

            ReplaceIgnots();
            ReplaseParts();

        }

        public void Save()
        {

        }

        /// <summary>
        /// Перекладка слитков из печек 
        /// </summary>

        public void ReplaceIgnots()
        {
            foreach (IMyRefinery refs in refinereys)
            {
                IMyInventory inventory = refs.GetInventory(1);
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);

                IMyInventory targetInventory = storage.GetInventory(0);

                if (inventory.CanTransferItemTo(targetInventory, MyItemType.MakeIngot("MyObjectBuilder_Ingot")))
                {
                    inventory.TransferItemTo(targetInventory, 0, null, true);
                }
            }
        }

        public void ReplaseParts()
        {
            foreach(IMyAssembler ass in assemblers)
            {
                IMyInventory inventory = ass.GetInventory(1);
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);

                IMyInventory targetInventory = parts.GetInventory(0);
                if (inventory.CanTransferItemTo(targetInventory, MyItemType.MakeComponent("MyObjectBuilder_Component")))
                {
                    inventory.TransferItemTo(targetInventory, 0, null, true);
                }

            }

        }
        ///////////////


    }
}
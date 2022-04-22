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

public sealed class Program : MyGridProgram
{
    IMyEntity entity;
    IMyTextPanel lcd;
    IMyTextPanel missileMonitor;

    IMyRadioAntenna antenna;
    IMyProgrammableBlock missileControlBlock;

    List<IMyProgrammableBlock> missilePCs;//список всех блоков ракет
    List<IMyBroadcastListener> listeners;
    IMyBroadcastListener listener;

    IMyProjector projector;

    string missileTagSender = "ch1R";
    string missileTagFinder;

    string missileMonitorName = "!MissileMonitor";
    string antennaName = "!AntGround";
    string controlPcName = "!ControlCop";

    int currentMissile = 0;

    public Program()
    {
        missilePCs = new List<IMyProgrammableBlock>();
        missileMonitor = GridTerminalSystem.GetBlockWithName(missileMonitorName) as IMyTextPanel;
        antenna = GridTerminalSystem.GetBlockWithName(antennaName) as IMyRadioAntenna;
        missileControlBlock = GridTerminalSystem.GetBlockWithName(controlPcName) as IMyProgrammableBlock;

        projector = GridTerminalSystem.GetBlockWithName("Projector") as IMyProjector;

        listener = IGC.RegisterBroadcastListener("ch1");
        listener.SetMessageCallback("ch1");

        missileTagFinder = missileControlBlock.CustomData;

        FindMissiles();

        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }

    public void Main(string args)
    {
        while(listener.HasPendingMessage)
        {
            MyIGCMessage mess = listener.AcceptMessage();
            if(mess.Tag == "ch1")
            {
                //lcd.WriteText("", false);
                //lcd.WriteText(mess.Data.ToString(), true);
            }
        }

        switch(args)
        {
            case "FIND":
                FindMissiles();
                break;
            case "DISARMALL":
                DisarmAll();
                break;
            case "PREARM":
                ArmAll();
                break;
            case "PREARMONE":
                ArmOne();
                break;

            case "FIRE":
                Fire();
                break;
        }

        missileMonitor.WriteText("", false);
        missileMonitor.WriteText(projector.TotalBlocks.ToString() +"/" + projector.RemainingBlocks, true);
    }

    public void Save()
    {

    }

    public void Fire()
    {
        if (projector.RemainingBlocks == 0)
        {
            FindMissiles();
            if (missilePCs.Count > 0)
            {
                if (missilePCs[0] != null)
                {
                    missilePCs[0].TryRun("PREARM");
                    missilePCs[0].TryRun("FIRE");
                }
            }
        }
    }

    public void FindMissiles()
    {
        currentMissile = 0;
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        GridTerminalSystem.SearchBlocksOfName(missileTagFinder, blocks);
        missilePCs.Clear();

        foreach (var bl in blocks)
        {
            if(bl is IMyProgrammableBlock)
            {
                if(bl!=null)
                {
                    missilePCs.Add(bl as IMyProgrammableBlock);
                    string args = bl.CustomData;
                    (bl as IMyProgrammableBlock).TryRun("BUILD");
                }
            }
        }

        Echo("Missile TAG: " + missileControlBlock.CustomData + "\n");
        Echo("Finded missiles: " + missilePCs.Count.ToString() + "\n");

        missileMonitor.WriteText("", false);
        missileMonitor.WriteText(missilePCs.Count.ToString(), true);
    }

    public void DisarmAll()
    {
        FindMissiles();
        foreach (var pc in missilePCs)
        {
            if (pc != null)
                pc.TryRun("DISARM");
        }
    }

    public void ArmAll()
    {
        FindMissiles();
        foreach (var pc in missilePCs)
        {
            if (pc != null) 
                pc.TryRun("PREARM");
        }
    }

    public void ArmOne()
    {
        FindMissiles();
        if (missilePCs[0] != null)
            missilePCs[0].TryRun("PREARM");
    }
}
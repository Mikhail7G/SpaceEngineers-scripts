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

namespace SpaceEngineers.Turret
{
    public sealed class Program : MyGridProgram
    {
        IMyInteriorLight _panelLight;
        IMyTextPanel _textPanel;
        IEnumerator<bool> _stateMachine;

        public Program()
        {
            Echo("");

            _panelLight = GridTerminalSystem.GetBlockWithName("Interior Light") as IMyInteriorLight;
            _textPanel = GridTerminalSystem.GetBlockWithName("LCD Panel") as IMyTextPanel;


            _stateMachine = RunStuffOverTime();


            Runtime.UpdateFrequency |= UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateType)
        {

            // if ((updateType & UpdateType.Once) == UpdateType.Once)
            {
                RunStateMachine();
            }
        }

        public void RunStateMachine()
        {

            if (_stateMachine != null)
            {

                bool hasMoreSteps = _stateMachine.MoveNext();

                if (!hasMoreSteps)
                {
                    Echo($"{_stateMachine}");
                    _stateMachine.Dispose();

                    _stateMachine = null;

                    if (_stateMachine == null)
                        Echo("Relased");
                }
            }
        }


        public IEnumerator<bool> RunStuffOverTime()
        {

            _panelLight.Enabled = true;

            yield return true;

            bool cont = true;
            int i = 0;

            while (cont)
            {
                _textPanel.WriteText(i.ToString());
                i++;
                if (i < 500)
                {
                    yield return true;
                }
                else
                {
                    cont = false;
                }
            }
        }

    }
}

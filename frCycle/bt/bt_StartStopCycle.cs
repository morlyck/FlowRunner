using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.Engine.Cycle;

//StartStopCycle

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {
        StartStopCycle? IFlowRunnerCycle.StartStopCycle { get => StartStopCycle; }
        StartStopCycle? StartStopCycle = null;

        [FlowRunnerCycleInitialization]
        void Initialization_StartStopCycle() {
            StartStopCycle = new StartStopCycle(this);

            //サービスリストに追加
            flowRunnerCycles.Add(StartStopCycle);
        }
    }
}

namespace FlowRunner.Engine.Cycle
{
    public partial interface IFlowRunnerCycle
    {
        StartStopCycle? StartStopCycle { get; }
    }
}

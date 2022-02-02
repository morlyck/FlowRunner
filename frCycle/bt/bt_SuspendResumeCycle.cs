using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.Engine.Cycle;

//SuspendResumeCycle

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {
        SuspendResumeCycle? IFlowRunnerCycle.SuspendResumeCycle { get => suspendResumeCycle; }
        SuspendResumeCycle? suspendResumeCycle = null;

        [FlowRunnerCycleInitialization]
        void Initialization_SuspendResumeCycle() {
            suspendResumeCycle = new SuspendResumeCycle(this);

            //サービスリストに追加
            flowRunnerCycles.Add(suspendResumeCycle);
        }
    }
}

namespace FlowRunner.Engine.Cycle
{
    public partial interface IFlowRunnerCycle
    {
        SuspendResumeCycle? SuspendResumeCycle { get; }
    }
}

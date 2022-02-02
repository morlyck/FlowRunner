using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner.Engine;

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {

    }
}

namespace FlowRunner.Engine.Cycle
{
    public class SuspendResumeCycle : FlowRunnerCycle
    {
        public SuspendResumeCycle(FlowRunnerEngine engine) : base(engine) { }

        //プロパティ
        //FlowRunnerEngine? engine { get; }
        //IFlowRunnerService? service { get; }
        //protected FlowRunner.Engine.Infra.IFlowRunnerInfra? infra { get; }


    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner.Engine;
using FlowRunner.Utl;

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {

    }
}

namespace FlowRunner.Engine.Cycle
{

    public class StartStopCycle : FlowRunnerCycle
    {
        public StartStopCycle(FlowRunnerEngine engine) : base(engine) { }

        //プロパティ
        //FlowRunnerEngine? engine { get; }
        //IFlowRunnerService? service { get; }
        //protected FlowRunner.Engine.Infra.IFlowRunnerInfra? infra { get; }

        public class StartCycleTimeAttribute : Attribute { }
        public class StopCycleTimeAttribute : Attribute { }

        public void StartCycle() {
            engine.Node.StartCycleTimeAll();
        }

    }


}

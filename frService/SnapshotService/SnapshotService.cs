using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {

    }
}

namespace FlowRunner.Engine.Service
{
    public class SnapshotService : FlowRunnerService
    {
        public SnapshotService(FlowRunnerEngine engine) : base(engine) { }

        //プロパティ
        //FlowRunnerEngine? engine { get; }
        //IFlowRunnerService? service { get; }
        //protected FlowRunner.Engine.Infra.IFlowRunnerInfra? infra { get; }


    }
}

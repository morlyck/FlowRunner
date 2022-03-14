using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner.Engine;
using CommonElement.Utl;

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


        public void SuspendCycle(out string snapshotText) {
            SnapshotFrame snapshot = new SnapshotFrame();
            snapshot.InvokeSuspendCycleTimeMethod(engine);

            snapshotText = infra.GeneralSd.Serialize(snapshot);
        }
        public void ResumeCycle(string snapshotText) {
            SnapshotFrame snapshot = infra.GeneralSd.Deserialize<SnapshotFrame>(snapshotText);
            snapshot.InvokeResumeCycleTimeMethod(engine);
        }

    }

    public partial class SnapshotFrame
    {
        public class SuspendCycleTimeAttribute : Attribute { }
        public class ResumeCycleTimeAttribute : Attribute { }

        public void InvokeSuspendCycleTimeMethod(FlowRunnerEngine engine) {
            this.InvokeActionAll<SuspendCycleTimeAttribute>(engine);
        }
        public void InvokeResumeCycleTimeMethod(FlowRunnerEngine engine) {
            this.InvokeActionAll<ResumeCycleTimeAttribute>(engine);
        }
    }

}

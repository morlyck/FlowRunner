using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.Utl;

namespace FlowRunner
{
    public partial class FlowRunnerEngine : Engine.Cycle.IFlowRunnerCycle
    {
        public Engine.Cycle.IFlowRunnerCycle Cycle { get => this; }

        List<Engine.Cycle.FlowRunnerCycle> flowRunnerCycles = new List<Engine.Cycle.FlowRunnerCycle>();

        //サイクルの初期化
        void Initialization_FlowRunnerCycle() {
            this.InvokeActionAll<Engine.Cycle.FlowRunnerCycleInitializationAttribute>();
        }
    }

}
namespace FlowRunner.Engine.Cycle
{
    public partial interface IFlowRunnerCycle
    { }

    public class FlowRunnerCycleInitializationAttribute : Attribute { }

    public abstract class FlowRunnerCycle
    {
        protected FlowRunnerEngine? engine { get; private set; } = null;
        protected Service.IFlowRunnerService? service { get; private set; } = null;
        protected Infra.IFlowRunnerInfra? infra { get; private set; } = null;
        public FlowRunnerCycle(FlowRunnerEngine engine) {
            this.engine = engine;
            this.service = engine.Service;
            this.infra = engine.Infra;

            //初期化
            this.Initialization();
        }

        protected virtual void Initialization() { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommonElement.Utl;


namespace FlowRunner
{
    public partial class FlowRunnerEngine : Engine.Service.IFlowRunnerService
    {
        public Engine.Service.IFlowRunnerService Service { get => this; }

        List<Engine.Service.FlowRunnerService> flowRunnerServices = new List<Engine.Service.FlowRunnerService>();

        //サービスの初期化
        void Initialization_FlowRunnerService() {
            this.InvokeActionAll<Engine.Service.FlowRunnerServiceInitializationAttribute>();
        }
    }

}
namespace FlowRunner.Engine.Service
{
    public partial interface IFlowRunnerService
    { }

    public class FlowRunnerServiceInitializationAttribute : Attribute { }

    public abstract class FlowRunnerService
    {
        protected FlowRunnerEngine? engine { get; private set; } = null;
        protected IFlowRunnerService? service { get; private set; } = null;
        protected Infra.IFlowRunnerInfra? infra { get; private set; } = null;
        protected Cycle.IFlowRunnerCycle? cycle { get; private set; } = null;
        public FlowRunnerService(FlowRunnerEngine engine) {
            this.engine = engine;
            this.service = engine.Service;
            this.infra = engine.Infra;
            this.cycle = engine.Cycle;

            //初期化
            this.Initialization();
        }

        protected virtual void Initialization() { }
    }
}

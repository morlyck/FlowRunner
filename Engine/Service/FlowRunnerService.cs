using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.Utl;

namespace FlowRunner
{
    public partial class FlowRunnerEngine : Engine.Service.IFlowRunnerService
    {
        public Engine.Service.IFlowRunnerService Service { get => this; }

        //サービスの初期化
        void Initialization_FlowRunnerService() {
            this.InvokeActionAll<Engine.Service.FlowRunnerServiceInitializationAttribute>();
        }

        List<Engine.Service.FlowRunnerService> flowRunnerServices = new List<Engine.Service.FlowRunnerService>();
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
        protected FlowRunner.Engine.Infra.IFlowRunnerInfra? infra { get; private set; } = null;
        public FlowRunnerService(FlowRunnerEngine engine) {
            this.engine = engine;
            this.service = engine.Service;
            this.infra = engine.Infra;

            //初期化
            this.Initialization();
        }

        protected virtual void Initialization() { }
    }
}

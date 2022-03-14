using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommonElement.Utl;

namespace FlowRunner
{
    public partial class FlowRunnerEngine : Engine.Infra.IFlowRunnerInfra
    {
        public Engine.Infra.IFlowRunnerInfra Infra { get => this; }
    }

}
namespace FlowRunner.Engine.Infra
{
    public partial interface IFlowRunnerInfra { }
}

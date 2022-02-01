using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.Engine.Infra;

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {
        Engine.IRepositoryInfra? IFlowRunnerInfra.Repository { get; set; }
    }
}

namespace FlowRunner.Engine.Infra
{
    public partial interface IFlowRunnerInfra
    {
        IRepositoryInfra? Repository { get; set; }
    }
}

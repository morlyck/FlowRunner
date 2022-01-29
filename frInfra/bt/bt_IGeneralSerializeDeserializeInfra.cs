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
        Engine.IGeneralSerializeDeserializeInfra? IFlowRunnerInfra.GeneralSd { get; set; } = null; //= new Engine.Infra.GeneralSerializeDeserializeInfra();
    }
}

namespace FlowRunner.Engine.Infra
{
    public partial interface IFlowRunnerInfra
    {
        IGeneralSerializeDeserializeInfra? GeneralSd { get; set; }
    }
}

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
        ITextFileIOInfra? IFlowRunnerInfra.TextFileIO { get; set; } = new Engine.Infra.TextFileIOInfra();
    }
}

namespace FlowRunner.Engine.Infra
{
    public partial interface IFlowRunnerInfra
    {
        FlowRunner.ITextFileIOInfra? TextFileIO { get; set; }
    }
}

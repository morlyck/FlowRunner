using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.Engine.Infra;

namespace FlowRunner.Engine.Operation
{
    public static class RunningContextSdOperation
    {
        public static string Serialize(this RunningContext context, IFlowRunnerInfra infra) {
            return infra.GeneralSd.Serialize(context.GetSdReady());
        }

        public static RunningContext Deserialize(this string text, IFlowRunnerInfra infra) {
            return infra.GeneralSd.Deserialize<RunningContext>(text);
        }
    }
}

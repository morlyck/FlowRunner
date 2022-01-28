using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.Engine.Service;

//SnapshotService

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {
        SnapshotService? IFlowRunnerService.SnapshotService { get => snapshotService; }
        SnapshotService? snapshotService = null;

        [FlowRunnerServiceInitialization]
        void Initialization_SnapshotService() {
            snapshotService = new SnapshotService(this);

            //サービスリストに追加
            flowRunnerServices.Add(snapshotService);
        }
    }
}

namespace FlowRunner.Engine.Service
{
    public partial interface IFlowRunnerService
    {
        SnapshotService? SnapshotService { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {
        public async Task QuickSuspendAsync() {
            await SuspendAsync("QuickSuspend");
        }

        public async Task SuspendAsync(string snapshotCode) {
            string text = await Service.SnapshotService.SuspendAsync();
            Infra.Repository.SetSnapshot(snapshotCode, text);
        }
        public async Task QuickResumeAsync() {
            await ResumeAsync("QuickSuspend");
        }

        public async Task ResumeAsync(string snapshotCode) {
            string text = Infra.Repository.GetSnapshot(snapshotCode);
            await Service.SnapshotService.ResumeAsync(text);
        }




    }
}

namespace FlowRunner.Engine.Service
{
    public class SnapshotService : FlowRunnerService
    {
        public SnapshotService(FlowRunnerEngine engine) : base(engine) { }

        //プロパティ
        //FlowRunnerEngine? engine { get; }
        //IFlowRunnerService? service { get; }
        //protected FlowRunner.Engine.Infra.IFlowRunnerInfra? infra { get; }

        public async Task<string> SuspendAsync() {
            throw new NotImplementedException();
        }
        public async Task ResumeAsync(string text) {
            throw new NotImplementedException();
        }
        public string Suspend() {
            throw new NotImplementedException();
        }
        public string Resume(string text) {
            throw new NotImplementedException();
        }
    }
}

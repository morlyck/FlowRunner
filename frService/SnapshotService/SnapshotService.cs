using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner.Engine;
using FlowRunner.Utl;

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
            if (text == null || text == "") return;

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
            return Suspend();
        }
        public async Task ResumeAsync(string text) {
            Resume(text);
        }

        public string Suspend() {
            cycle.SuspendResumeCycle.SuspendCycle(out string text);
            return text;
        }
        public void Resume(string text) {
            cycle.SuspendResumeCycle.ResumeCycle(text);
        }

    }


}

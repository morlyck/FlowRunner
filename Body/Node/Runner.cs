using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utl_Drifts;

using FlowRunner;
using FlowRunner.Engine;
using FlowRunner.Another;

namespace FlowRunner.Engine
{

    public interface IRunner
    {
        bool Active { get; set; }
        Runner Run(string packCode);

        Runner Run(string packCode, string entryLabel);

        bool FrameSleep { get; set; }
        bool Deleted { get; set; }

        bool RunHalted { get; set; }
        void HaltRun();

        RunningContext Context { get; set; }
        public ChainEnvironment ChainEnvironment { get; set; }

    }

    public class RunnerSdReady
    {
        public bool Active { get; set; } = false;
        public bool FrameSleep { get; set; } = false;
        public bool Deleted { get; set; } = false;

        public string ChainEnvironmentText = "";
    }

    public class Runner : RunnerSdReady, IRunner
    {
        //シリアライズ対応
        public string Serialize(FlowRunnerEngine engine) {
            RunnerSdReady sdReady = new RunnerSdReady();

            sdReady.Active = Active;
            sdReady.FrameSleep = FrameSleep;
            sdReady.Deleted = Deleted;
            //
            sdReady.ChainEnvironmentText = ChainEnvironment.Serialize(engine);

            return engine.Infra.GeneralSd.Serialize(sdReady);
        }

        //デシリアライズ対応
        public void Deserialize(FlowRunnerEngine engine, string text) {
            RunnerSdReady sdReady = engine.Infra.GeneralSd.Deserialize<RunnerSdReady>(text);

            Active = sdReady.Active;
            FrameSleep = sdReady.FrameSleep;
            Deleted = sdReady.Deleted;
            //
            ChainEnvironment = new ChainEnvironment();
            ChainEnvironment.Deserialize(engine, sdReady.ChainEnvironmentText);
        }
        //---

        public bool RunHalted { get=>Context.IsHalting; set=> Context.IsHalting = value; }
        public void HaltRun() {
            Context.IsHalting = true;
        }
        public ChainEnvironment ChainEnvironment { get; set; } = new ChainEnvironment();


        //---
        public RunningContext Context { get; set; }
        public LabelRun LabelRun = new LabelRun();

        ILabelRunOrdertaker LabelRunOrdertaker {
            get => LabelRun.LabelRunOrdertaker;
        }
        public void RunnerSetup(LabelRun labelRun, ILabelRunOrdertaker labelRunOrdertaker, RunningContext context) {
            LabelRun = labelRun;
            LabelRun.LabelRunOrdertaker = labelRunOrdertaker;
            Context = context;
            Context.SetRunner(this);
        }
        public void Setup(string packCode, string entryLabel) {
            Context.CurrentPackCode = packCode;

            Pack pack = LabelRunOrdertaker.GetPack(Context, packCode);
            Context.SetLabelsAndStatements(pack);
            //
            Context.ProgramCounter = LabelRun.GetStatementIndex_LabelResolution(Context, packCode, entryLabel);
            Context.IsHalting = false;
        }

        //---
        public Runner Run(string packCode) {
            Run(packCode, NodeDefaultValue.EntryLabel);
            return this;
        }

        public Runner Run(string packCode, string entryLabel) {
            Setup(packCode, entryLabel);
            Active = true;
            return this;
        }
    }

}

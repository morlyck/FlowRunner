using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utl_Drifts;

using CommonElement;

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
        ChainEnvironment Environment { get; }
        VariableEventSubject VariableEventSubject { get; }

        InterruptController InterruptController { get; }

    }
    public interface IRunnerEngineInside
    {
        void StartCycleTime();
    }

    public class RunnerSdReady
    {
        public bool Active { get; set; } = false;
        public bool FrameSleep { get; set; } = false;
        public bool Deleted { get; set; } = false;

        public string EnvironmentText = "";
        public string InterruptControllerText = "";
    }

    public class Runner : IRunner, IRunnerEngineInside
    {
        //シリアライズ対応
        public string Serialize(FlowRunnerEngine engine) {
            RunnerSdReady sdReady = new RunnerSdReady();

            sdReady.Active = Active;
            sdReady.FrameSleep = FrameSleep;
            sdReady.Deleted = Deleted;
            //
            sdReady.EnvironmentText = Environment.Serialize(engine.Infra.GeneralSd);
            sdReady.InterruptControllerText = InterruptController.Serialize(engine);

            return engine.Infra.GeneralSd.Serialize(sdReady);
        }

        //デシリアライズ対応
        public void Deserialize(FlowRunnerEngine engine, string text) {
            RunnerSdReady sdReady = engine.Infra.GeneralSd.Deserialize<RunnerSdReady>(text);

            Active = sdReady.Active;
            FrameSleep = sdReady.FrameSleep;
            Deleted = sdReady.Deleted;
            //
            Environment = new ChainEnvironment();
            Environment.Deserialize(engine.Infra.GeneralSd, sdReady.EnvironmentText);

            InterruptController = new InterruptController();
            InterruptController.Deserialize(engine, sdReady.InterruptControllerText);
        }
        //---

        public bool Active { get; set; } = false;
        public bool FrameSleep { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public bool RunHalted { get=>Context.IsHalting; set=> Context.IsHalting = value; }
        public void HaltRun() {
            Context.IsHalting = true;
        }
        public ChainEnvironment Environment { get; private set; } = new ChainEnvironment();

        public VariableEventSubject VariableEventSubject { get; private set; } = new VariableEventSubject();

        public InterruptController InterruptController { get; private set; } = new InterruptController();

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


        //---

        void IRunnerEngineInside.StartCycleTime() {
            Environment.Ordertaker = VariableEventSubject;
            
            //ランニングコンテキストのキャッシュの更新
            if (Context.CurrentPackCode != "") {
                Context.SetLabelsAndStatements(LabelRunOrdertaker.GetPack(Context, Context.CurrentPackCode));
            }

            //割り込みコントローラの初期化
            InterruptController.RunningContext = Context;

            //スタート時実行機能の呼び出し用
            VariableEventSubject.StartCycleTime(Environment);
        }









    }

}

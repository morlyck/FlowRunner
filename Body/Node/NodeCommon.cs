using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Utl_Drifts;

using CommonElement;

using FlowRunner;
using FlowRunner.Engine;
using FlowRunner.Another;

namespace FlowRunner.Engine
{
    public partial interface IRunningContext : IRunningContext_atRunner { }

    public partial class RunningContext
    {
        Runner runner;
        public Runner Runner { get=> runner; }
        public void SetRunner(Runner runner) {
            this.runner = runner;
        }
    }
}

namespace FlowRunner.Engine
{
    public interface IRunningContext_atRunner
    {
        Runner Runner { get; }
        void SetRunner(Runner runner);
    }

    public static class NodeDefaultValue
    {
        public static string EntryLabel { get=> "main"; }
    }

    //---
    public class NodeSdReadyCommon
    {
        //環境
        public string EnvironmentText = "";
        //
        public List<string> RunnerCodes = new List<string>();
        public List<string> RunnerTexts = new List<string>();
        public List<string> Contexts = new List<string>();

        public List<string> AnonymousRunnerTexts = new List<string>();
        public List<string> AnonymousContexts = new List<string>();
    }

    public abstract class NodeCommon
    {
        protected abstract ILabelRunOrdertaker LabelRunOrdertaker { get; }

        Dictionary<string, Runner> runners = new Dictionary<string, Runner>();
        List<Runner> anonymousRunners = new List<Runner>();

        //シリアライズ
        protected void NodeSerializerDelta(FlowRunnerEngine engine,ref NodeSdReadyCommon sdReady) {
            //環境
            sdReady.EnvironmentText = Environment.Serialize(engine.Infra.GeneralSd);

            //ランナー
            foreach (KeyValuePair<string, Runner> runner in runners) {
                sdReady.RunnerCodes.Add(runner.Key);
                sdReady.RunnerTexts.Add(runner.Value.Serialize(engine));
                sdReady.Contexts.Add(Operation.RunningContextSdOperation.Serialize(runner.Value.Context, engine.Infra));
            }
            foreach (Runner runner in anonymousRunners) {
                sdReady.AnonymousRunnerTexts.Add(runner.Serialize(engine));
                sdReady.AnonymousContexts.Add(Operation.RunningContextSdOperation.Serialize(runner.Context, engine.Infra));
            }

        }

        //デシリアライズ
        protected void NodeDeserializeDelta(FlowRunnerEngine engine, ref NodeSdReadyCommon sdReady) {
            //環境
            Environment.Deserialize(engine.Infra.GeneralSd, sdReady.EnvironmentText);

            //ランナー
            for (int index = 0; index < sdReady.RunnerCodes.Count; index++) {
                Runner runner = new Runner(); 
                runner.Deserialize(engine, sdReady.RunnerTexts[index]);
                runner.RunnerSetup(new LabelRun(), LabelRunOrdertaker, Operation.RunningContextSdOperation.Deserialize(sdReady.Contexts[index], engine.Infra));

                runners.Add(sdReady.RunnerCodes[index], runner);
            }
            for (int index = 0; index < sdReady.AnonymousRunnerTexts.Count; index++) {
                Runner runner = new Runner();
                runner.Deserialize(engine, sdReady.AnonymousRunnerTexts[index]);
                runner.RunnerSetup(new LabelRun(), LabelRunOrdertaker, Operation.RunningContextSdOperation.Deserialize(sdReady.AnonymousContexts[index], engine.Infra));

                anonymousRunners.Add(runner);
            }
        }
        //
        //環境
        public ChainEnvironment Environment { get; private set; } = new ChainEnvironment();

        //
        public IRunner CreateAndSetRunner() {
            return CreateAndSetRunner(null, "");
        }
        public IRunner CreateAndSetRunner(string runnerCode) {
            return CreateAndSetRunner(runnerCode, "");
        }
        //エントリーラベルを省略した場合は既定値のラベルを使用する
        public IRunner CreateAndSetRunner(string runnerCode, string packCode) {
            return CreateAndSetRunner(runnerCode, packCode, NodeDefaultValue.EntryLabel);
        }
        //すでに同Codeのランナーがあった場合は上書きする
        public IRunner CreateAndSetRunner(string runnerCode, string packCode, string entryLabel) {
            Runner runner = new Runner();
            runner.RunnerSetup(new LabelRun(), LabelRunOrdertaker, new RunningContext());

            //PackCodeが指定されていないときはランナーを非アクティブで作成する
            if (packCode == "") {
                runner.Active = false;
            } else {
                runner.Active = true;
                runner.Setup(packCode, entryLabel);
            }

            //runnerCodeがnullの場合は無名ランナーとして登録する
            if(runnerCode == null) {
                anonymousRunners.Add(runner);
                return runner;
            }

            //追加
            if (!runners.ContainsKey(runnerCode)) {
                runners.Add(runnerCode, runner);
            } else {
                runners[runnerCode] = runner;
            }
            return runner;
        }

        public IRunner GetRunner(string runnerCode) {
            if (!runners.ContainsKey(runnerCode)) return null;
            return runners[runnerCode];
        }

        void RunnerAll(Action<Runner> action) {
            foreach (KeyValuePair<string, Runner> runner in runners) {
                action(runner.Value);
            }
            foreach (Runner runner in anonymousRunners) {
                action(runner);
            }
        }

        public void Update() {
            RemoveDeletedRunner();
            RunnerAll(FrameRun);
        }

        List<string> removeCodes = new List<string>();
        List<Runner> removes = new List<Runner>();
        public void RemoveDeletedRunner() {
            //Code
            removeCodes.Clear();
            foreach (KeyValuePair<string, Runner> runner in runners) {
                if(runner.Value.Deleted) removeCodes.Add(runner.Key);
            }
            foreach(string code in removeCodes) {
                runners.Remove(code);
            }

            //無名
            removes.Clear();
            foreach (Runner runner in anonymousRunners) {
                if (runner.Deleted) removes.Add(runner);
            }
            foreach (Runner runner in removes) {
                anonymousRunners.Remove(runner);
            }
        }

        public void FrameRun(Runner runner) {
            runner.FrameSleep = false;

            for (int count = 0; count < 500; count++) {
                if (!runner.Active || runner.Context.IsHalting || runner.FrameSleep || runner.Deleted) return;
                runner.LabelRun.ShotRun(runner.Context);
            }
        }

        public void StartCycleTime() {
            RunnerAll(runner => {
                (runner as IRunnerEngineInside).StartCycleTime();
            });
        }


    }


}
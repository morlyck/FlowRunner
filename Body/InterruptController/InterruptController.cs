using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner;
using FlowRunner.Engine;

namespace FlowRunner.Engine
{
    public class InterruptControllerSdReady
    {
        public Dictionary<string, InterruptConfig> interruptConfigs = null;
        public Dictionary<string, HaltedStateLiftConfig> haltedStateLiftConfigs = null;
    }
    public class InterruptController
    {
        //シリアライズ対応
        public string Serialize(FlowRunnerEngine engine) {
            InterruptControllerSdReady sdReady = new InterruptControllerSdReady();
            sdReady.interruptConfigs = interruptConfigs;
            sdReady.haltedStateLiftConfigs = haltedStateLiftConfigs;

            return engine.Infra.GeneralSd.Serialize(sdReady);
        }

        //デシリアライズ対応
        public void Deserialize(FlowRunnerEngine engine, string text) {
            InterruptControllerSdReady sdReady = engine.Infra.GeneralSd.Deserialize<InterruptControllerSdReady>(text);

            interruptConfigs = sdReady.interruptConfigs;
            haltedStateLiftConfigs = sdReady.haltedStateLiftConfigs;
        }

        //---
        public RunningContext RunningContext { get; set; }

        Dictionary<string, InterruptConfig> interruptConfigs = new Dictionary<string, InterruptConfig>();
        Dictionary<string, HaltedStateLiftConfig> haltedStateLiftConfigs = new Dictionary<string, HaltedStateLiftConfig>();

        public void AddInterruptConfig(string interruptConfigCode, string packCode, string label) {
            if (!interruptConfigs.ContainsKey(interruptConfigCode)) {
                interruptConfigs.Add(interruptConfigCode, new InterruptConfig());
            }
            var config = interruptConfigs[interruptConfigCode];

            config.PackCode = packCode;
            config.Label = label;
        }
        public void RemoveInterruptConfig(string interruptConfigCode) {
            if (!interruptConfigs.ContainsKey(interruptConfigCode)) return;
            interruptConfigs.Remove(interruptConfigCode);
        }
        public bool Interrupt(string interruptConfigCode) {
            if (!interruptConfigs.ContainsKey(interruptConfigCode))return false;

            var config = interruptConfigs[interruptConfigCode];

            InterruptInfo info = new InterruptInfo();
            info.PackCode = config.PackCode;
            info.Label = config.Label;
            RunningContext.InterruptInfos.Add(info);
            return true;
        }

        //

        public void AddHaltedStateLiftConfig(string haltedStateLiftConfigCode) {
            if (!haltedStateLiftConfigs.ContainsKey(haltedStateLiftConfigCode)) {
                haltedStateLiftConfigs.Add(haltedStateLiftConfigCode, new HaltedStateLiftConfig());
            }
        }
        public void RemoveHaltedStateLiftConfig(string haltedStateLiftConfigCode) {
            if (!haltedStateLiftConfigs.ContainsKey(haltedStateLiftConfigCode)) return;
            haltedStateLiftConfigs.Remove(haltedStateLiftConfigCode);
        }
        //
        public void TurnActive_HaltedStateLift(string haltedStateLiftConfigCode) {

            if (!haltedStateLiftConfigs.ContainsKey(haltedStateLiftConfigCode)) return;
            haltedStateLiftConfigs[haltedStateLiftConfigCode].Active = true;
        }


        //停止状態の解除
        public bool LiftTheHaltedState(string haltedStateLiftConfigCode) {
            if (!haltedStateLiftConfigs.ContainsKey(haltedStateLiftConfigCode)) return false;

            var config = haltedStateLiftConfigs[haltedStateLiftConfigCode];
            if (!config.Active) return false;

            //非アクティブに変更
            config.Active = false;

            //停止状態の解除
            RunningContext.IsHalting = false;

            return true;
        }
    }

    public class InterruptConfig
    {
        public string PackCode = "";
        public string Label = "";

    }

    public class HaltedStateLiftConfig
    {
        public bool Active = false;
    }


}

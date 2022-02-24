using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner;
using FlowRunner.Engine;
using FlowRunner.Another;

namespace FlowRunner
{
    public static class ICreateOrGetEx
    {
        public static CustomNode CreateOrGetNode(this FlowRunnerEngine engine, out bool createSwitch, string path) {
            createSwitch = !engine.ExistsNode(path);

            if (createSwitch) {
                return engine.CreateAndSetNode<CustomNode>(path);
            } else {
                return engine.GetNode(path) as CustomNode;
            }
        }
        public static typ CreateOrGetNode<typ>(this FlowRunnerEngine engine, out bool createSwitch, string path)
            where typ : CustomNode {
            createSwitch = !engine.ExistsNode(path);

            if (createSwitch) {
                return engine.CreateAndSetNode<typ>(path);
            } else {
                return engine.GetNode(path) as typ;
            }
        }
        public static IRunner? CreateOrGetRunner(this INode node, bool createSwitch) {
            if (createSwitch) {
                return node.CreateAndSetRunner();
            } else {
                return null;
            }
        }
        public static IRunner CreateOrGetRunner(this INode node, bool createSwitch, string runnerCode) {
            if (createSwitch) {
                return node.CreateAndSetRunner(runnerCode);
            } else {
                return node.GetRunner(runnerCode);
            }
        }
        public static IRunner CreateOrGetRunner(this INode node, bool createSwitch, string runnerCode, string packCode) {
            if (createSwitch) {
                return node.CreateAndSetRunner(runnerCode, packCode);
            } else {
                return node.GetRunner(runnerCode);
            }
        }
        public static IRunner CreateOrGetRunner(this INode node, bool createSwitch, string runnerCode, string packCode, string entryLabel) {
            if (createSwitch) {
                return node.CreateAndSetRunner(runnerCode, packCode, entryLabel);
            } else {
                return node.GetRunner(runnerCode);
            }
        }
    }
}

namespace FlowRunner.Engine
{
    public partial interface IFlowRunnerEngineInside
    {
        void ReplaceNode(FlowRunnerEngineNode node);
    }
}
namespace FlowRunner
{
    public partial class FlowRunnerEngine : IFlowRunnerEngineInside {
        public Engine.FlowRunnerEngineNode Node { get; private set; }
        void Engine.IFlowRunnerEngineInside.ReplaceNode(Engine.FlowRunnerEngineNode node) {
            Node = node;
        }

        public void Update() {
            Node.UpdateAll();
        }

        public typ CreateAndSetNode<typ>(string path) 
            where typ : CustomNode
            {
            typ node = (typ)Activator.CreateInstance(typeof(typ), new object[] { Node, path });

            Node.SetNode(node);

            return node;
        }

        public INode GetNode(string path) {
            return Node.GetNode(path);
        }
        public void SetNode(string path, INode node) {
            Node.SetNode(path, node);
        }

        public bool ExistsNode(string path) {
            return Node.GetNode(path) != null;
        }

    }

}

namespace FlowRunner.Engine
{
    public interface INodeCnpn_fromEngine
    {
        void Update();
        void StartCycleTime();
    }

    public class FlowRunnerEngineNode : NodeCommon, INode
    {
        protected override ILabelRunOrdertaker LabelRunOrdertaker { get => this; }
        public FlowRunnerEngine Engine { get => engine; }

        FlowRunnerEngine engine;
        public FlowRunnerEngineNode(FlowRunnerEngine engine) { 
            this.engine = engine;
        }


        Dictionary<string, INode> nodes = new Dictionary<string, INode>();

        #region(INode)
        public string Path { get => "/"; }
        public string EngineManagedPath { get => "/"; }

        public string GetEngineManagedPath(INode node) {
            if (node == this) return "/";

            foreach(KeyValuePair<string,INode> keyValuePair in nodes) {
                if(keyValuePair.Value == node) {
                    return keyValuePair.Key;
                }
            }

            return null;
        }

        public INode GetNode(string path) {
            if (path.Trim() == "/") return this;
            //
            if (!nodes.ContainsKey(path)) return null;
            return nodes[path];
        }
        public void SetNode(string path, INode node) {
            if (!nodes.ContainsKey(path)) {
                nodes.Add(path, node);
                return;
            }
            nodes[path] = node;
        }
        public void SetNode(INode node) {
            if (!nodes.ContainsKey(node.Path)) {
                nodes.Add(node.Path, node);
                return;
            }
            nodes[node.Path] = node;
        }
        public INode NodeOperationRelay(string path) {

            INode node = null;
            do {
                if (path.Trim() == "/") return this;
                path = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                node = GetNode(path);

            } while (node == null);

            return node;
        }

        #endregion

        //---
        #region(ILabelRunOrdertaker)
        public Pack GetPack(IRunningContext runningContext, string packCode) {
            throw new NotImplementedException();
        }
        public CommandExecutionContext Evaluation_ArgumentExpansion(IRunningContext runningContext, string commandSymbol, string packCode, string label, string expansionArgumentText) {
            throw new NotImplementedException();
        }

        public bool ExecutionExpansionCommand(IRunningContext runningContext, string commandSymbol, CommandExecutionContext commandExecutionContext) {
            return false;
        }


        public bool CatchException_InvalidCommand(IRunningContext runningContext, InvalidCommandException e) {
            return false;
        }

        public bool CatchException_ProgramCounterOutOfRange(IRunningContext runningContext, ProgramCounterOutOfRangeException e) {
            return false;
        }

        public bool CatchException_LabelResolutionMiss(IRunningContext runningContext, LabelResolutionMissException e) {
            return false;
        }
        public bool CatchException_CallStackEmptyPop(IRunningContext runningContext, CallStackEmptyPopException e) {
            return false;
        }
        #endregion

        public void NodeAll(Action<string, INode> action) {
            foreach (KeyValuePair<string, INode> keyValuePair in nodes) {
                action(keyValuePair.Key, keyValuePair.Value);
            }
            action("/", this);
        }

        public void UpdateAll() {
            foreach (KeyValuePair<string, INode> keyValuePair in nodes) {
                keyValuePair.Value.Update();
            }
            this.Update();
        }

        //起動
        public void StartCycleTimeAll() {
            foreach (KeyValuePair<string, INode> keyValuePair in nodes) {
                keyValuePair.Value.StartCycleTime();
            }
            this.StartCycleTime();
        }

        //--
        
        public string Serialize() {

            NodeSdReadyCommon sdReady = new CustomNodeSdReady();

            //差分シリアライザを呼び出す
            NodeSerializerDelta(engine, ref sdReady);

            return Engine.Infra.GeneralSd.Serialize((sdReady as CustomNodeSdReady));

        }

        public void Deserialize(FlowRunnerEngine engine, string text) {
            NodeSdReadyCommon sdReady = engine.Infra.GeneralSd.Deserialize<CustomNodeSdReady>(text);


            //差分シリアライザを呼び出す
            NodeDeserializeDelta(engine, ref sdReady);
        }



    }
}

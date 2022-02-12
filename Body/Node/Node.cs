using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner;
using FlowRunner.Engine;
using FlowRunner.Another;


namespace FlowRunner.Engine
{

    public interface INode : ILabelRunOrdertaker, INodeCnpn_fromEngine
    {
        string Path { get; }

        string EngineManagedPath { get; }
        INode GetNode(string path);
        void SetNode(string path, INode node);
        INode NodeOperationRelay(string path);

        //
        IRunner CreateAndSetRunner();
        IRunner CreateAndSetRunner(string runnerCode);
        //エントリーラベルを省略した場合は既定値のラベルを使用する
        IRunner CreateAndSetRunner(string runnerCode, string packCode);
        IRunner CreateAndSetRunner(string runnerCode, string packCode, string entryLabel);

        IRunner GetRunner(string runnerCode);

        //Serialize/Deserialize
        FlowRunnerEngine Engine { get; }
        string Serialize();
        void Deserialize(FlowRunnerEngine engine, string text);
    }

    public class CustomNode : NodeCommon, INode
    {
        public CustomNode(INode root, string path) {
            Root = root;
            Path = path;
        }
        protected override ILabelRunOrdertaker LabelRunOrdertaker { get => this; }
        public FlowRunnerEngine Engine { get => Root.Engine; }

        #region(INode)
        public INode Root { get; protected set; }
        public string Path { get; protected set; }

        public string EngineManagedPath {
            get => Engine.Node.GetEngineManagedPath(this);
        }

        public virtual INode GetNode(string path) {
            return Root.GetNode(path);
        }
        public virtual void SetNode(INode node) {
            Root.SetNode(node.Path, node);
        }
        public virtual void SetNode(string path, INode node) {
            Root.SetNode(path, node);
        }
        public virtual INode NodeOperationRelay(string path) {
            return Root.NodeOperationRelay(path);
        }
        #endregion

        //
        #region(ILabelRunOrdertaker)
        //
        public Pack GetPack(IRunningContext runningContext, string packCode) {
            var returnValue = Localize_GetPack(runningContext.Runner, packCode);
            if (returnValue.Item1) return returnValue.Item2;

            return NodeOperationRelay(Path).GetPack(runningContext, packCode);
        }
        protected virtual (bool, Pack) Localize_GetPack(IRunner runner, string packCode) {
            return (false, null);
        }

        //
        public CommandExecutionContext Evaluation_ArgumentExpansion(IRunningContext runningContext, string commandSymbol, string packCode, string label, string expansionArgumentText) {
            var returnValue = Localize_Evaluation_ArgumentExpansion(runningContext.Runner, commandSymbol, packCode, label, expansionArgumentText);
            if (returnValue.Item1) return returnValue.Item2;

            return NodeOperationRelay(Path).Evaluation_ArgumentExpansion(runningContext, commandSymbol, packCode, label, expansionArgumentText);
        }
            return (false, null);
        protected virtual (bool, CommandExecutionContext) Localize_Evaluation_ArgumentExpansion(IRunner runner, string commandSymbol, string packCode, string label, string expansionArgumentText) {
        }

        //
        Dictionary<string, Action<IRunningContext, CommandExecutionContext>> commands = new Dictionary<string, Action<IRunningContext, CommandExecutionContext>>();
        public void AddCommand(string commandSymbol, Action<IRunningContext, CommandExecutionContext> action) {
            if (!commands.ContainsKey(commandSymbol)) {
                commands.Add(commandSymbol, action);
            } else {
                commands[commandSymbol] = action;
            }
        }
        public bool ExecutionExpansionCommand(IRunningContext runningContext, string commandSymbol, CommandExecutionContext commandExecutionContext) {
            //コマンドリストに登録されているなら実行する
            if (commands.ContainsKey(commandSymbol)) {
                commands[commandSymbol].Invoke(runningContext, commandExecutionContext);
                return true;
            }

            //ノードオペレーションリレーを使用してコマンドの実行を試みる
            var returnValue = Localize_ExecutionExpansionCommand(runningContext.Runner, commandSymbol, commandExecutionContext);
            if (returnValue) return true;

            return NodeOperationRelay(Path).ExecutionExpansionCommand(runningContext, commandSymbol, commandExecutionContext);
        }

        protected virtual bool Localize_ExecutionExpansionCommand(IRunner runner, string commandSymbol, CommandExecutionContext commandExecutionContext) {
            return false;
        }

        //
        public bool CatchException_InvalidCommand(IRunningContext runningContext, InvalidCommandException e) {
            var returnValue = Localize_CatchException_InvalidCommand(runningContext.Runner, e);
            if (returnValue) return true;

            return NodeOperationRelay(Path).CatchException_InvalidCommand(runningContext, e);
        }

        protected virtual bool Localize_CatchException_InvalidCommand(IRunner runner, InvalidCommandException e) {
            return false;
        }

        //
        public bool CatchException_LabelResolutionMiss(IRunningContext runningContext, LabelResolutionMissException e) {
            var returnValue = Localize_CatchException_LabelResolutionMiss(runningContext.Runner, e);
            if (returnValue) return true;

            return NodeOperationRelay(Path).CatchException_LabelResolutionMiss(runningContext, e);
        }
        protected virtual bool Localize_CatchException_LabelResolutionMiss(IRunner runner, LabelResolutionMissException e) {
            return false;
        }

        //
        public bool CatchException_ProgramCounterOutOfRange(IRunningContext runningContext, ProgramCounterOutOfRangeException e) {
            var returnValue = Localize_CatchException_ProgramCounterOutOfRange(runningContext.Runner, e);
            if (returnValue) return true;

            return NodeOperationRelay(Path).CatchException_ProgramCounterOutOfRange(runningContext, e);
        }
        protected virtual bool Localize_CatchException_ProgramCounterOutOfRange(IRunner runner, ProgramCounterOutOfRangeException e) {
            return false;
        }

        #endregion


        public string Serialize() {
            var returnValue = Localize_Serialize(true, null);

            bool engineRoot = NodeOperationRelay(Path).Path == "/";
            //sdReady
            NodeSdReadyCommon sdReady = null;
            if (returnValue.Item1) {
                sdReady = returnValue.Item2;
            } else {
                sdReady = new CustomNodeSdReady();
            }

            //差分シリアライザを呼び出す
            NodeSerializerDelta(Engine, ref sdReady);


            //パスの保存
            (sdReady as CustomNodeSdReady).Path = Path;

            //ルートノードパスを保存しておく
            (sdReady as CustomNodeSdReady).RootNodeEngineManagementPath = Root.EngineManagedPath;


            //ローカルのシリアライズアフター
            if (returnValue.Item1) {
                return Engine.Infra.GeneralSd.Serialize(Localize_Serialize(false, (sdReady as CustomNodeSdReady)).Item2);
            } else {
                return Engine.Infra.GeneralSd.Serialize((sdReady as CustomNodeSdReady));
            }

        }
        protected virtual (bool, CustomNodeSdReady) Localize_Serialize(bool beforeSwitch, CustomNodeSdReady sdReady) {
            return (false, null);
        }

        public void Deserialize(FlowRunnerEngine engine, string text) {
            var returnValue = Localize_Deserialize(true, null, engine, text);
            NodeSdReadyCommon sdReady;
            if (returnValue.Item1) {
                sdReady = returnValue.Item2;
            } else {
                sdReady = engine.Infra.GeneralSd.Deserialize<CustomNodeSdReady>(text);

            }

            //Rootの再設定
            Root = engine.GetNode((sdReady as CustomNodeSdReady).RootNodeEngineManagementPath);

            //Path
            Path = (sdReady as CustomNodeSdReady).Path;

            //差分シリアライザを呼び出す
            NodeDeserializeDelta(engine, ref sdReady);



            //アフター
            if (returnValue.Item1) Localize_Deserialize(false, (sdReady as CustomNodeSdReady), engine, text);
        }

        protected virtual (bool, CustomNodeSdReady) Localize_Deserialize(bool beforeSwitch, CustomNodeSdReady sdReady, FlowRunnerEngine engine, string text) {
            return (false, null);
        }

    }


    public class CustomNodeSdReady: NodeSdReadyCommon
    {
        public string Path = "";
        public string RootNodeEngineManagementPath = "/";
    }


}

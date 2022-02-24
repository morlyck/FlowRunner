using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner;
using FlowRunner.Engine;
using FlowRunner.Another;

using FlowRunner.Utl;

namespace FlowRunner.Engine
{

    public interface INode : ILabelRunOrdertaker
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

            this.InvokeActionAll<CustomNode, ConstructorAttribute>();
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
        [Constructor]
        void Constructor_GetPack() {
            if (Local_GetPack == null) Local_GetPack = (runner, packCode) => Localize_GetPack(runner, packCode);
        }
        public Pack GetPack(IRunningContext runningContext, string packCode) {
            var returnValue = Local_GetPack(runningContext.Runner, packCode);
            if (returnValue.Item1) return returnValue.Item2;

            return NodeOperationRelay(Path).GetPack(runningContext, packCode);
        }
        public Func<IRunner, string, (bool, Pack)>? Local_GetPack = null;
        protected virtual (bool, Pack) Localize_GetPack(IRunner runner, string packCode) {
            return (false, null);
        }

        //
        [Constructor]
        void Constructor_Evaluation_ArgumentExpansion() {
            if (Local_Evaluation_ArgumentExpansion == null) Local_Evaluation_ArgumentExpansion = Localize_Evaluation_ArgumentExpansion;
        }
        public CommandExecutionContext Evaluation_ArgumentExpansion(IRunningContext runningContext, string commandSymbol, string packCode, string label, string expansionArgumentText) {
            var returnValue = Local_Evaluation_ArgumentExpansion(runningContext.Runner, commandSymbol, packCode, label, expansionArgumentText);
            if (returnValue.Item1) return returnValue.Item2;

            return NodeOperationRelay(Path).Evaluation_ArgumentExpansion(runningContext, commandSymbol, packCode, label, expansionArgumentText);
        }
        public Func<IRunner, string, string, string, string, (bool, CommandExecutionContext)> Local_Evaluation_ArgumentExpansion = null;
        protected virtual (bool, CommandExecutionContext) Localize_Evaluation_ArgumentExpansion(IRunner runner, string commandSymbol, string packCode, string label, string expansionArgumentText) {
            return (false, null);
        }

        //
        Dictionary<string, Action<IRunner, CommandExecutionContext>> commands = new Dictionary<string, Action<IRunner, CommandExecutionContext>>();
        public void AddCommand(string commandSymbol, Action<IRunner, CommandExecutionContext> action) {
            if (!commands.ContainsKey(commandSymbol)) {
                commands.Add(commandSymbol, action);
            } else {
                commands[commandSymbol] = action;
            }
        }
        [Constructor]
        void Constructor_ExecutionExpansionCommand() {
            if (Local_ExecutionExpansionCommand == null) Local_ExecutionExpansionCommand = Localize_ExecutionExpansionCommand;
        }
        public bool ExecutionExpansionCommand(IRunningContext runningContext, string commandSymbol, CommandExecutionContext commandExecutionContext) {
            //コマンドリストに登録されているなら実行する
            if (commands.ContainsKey(commandSymbol)) {
                commands[commandSymbol].Invoke(runningContext.Runner, commandExecutionContext);
                return true;
            }

            //ノードオペレーションリレーを使用してコマンドの実行を試みる
            var returnValue = Local_ExecutionExpansionCommand(runningContext.Runner, commandSymbol, commandExecutionContext);
            if (returnValue) return true;

            return NodeOperationRelay(Path).ExecutionExpansionCommand(runningContext, commandSymbol, commandExecutionContext);
        }
        public Func<IRunner, string, CommandExecutionContext, bool> Local_ExecutionExpansionCommand = null;
        protected virtual bool Localize_ExecutionExpansionCommand(IRunner runner, string commandSymbol, CommandExecutionContext commandExecutionContext) {
            return false;
        }

        //
        [Constructor]
        void Constructor_CatchException_InvalidCommand() {
            if (Local_CatchException_InvalidCommand == null) Local_CatchException_InvalidCommand = Localize_CatchException_InvalidCommand;
        }
        public bool CatchException_InvalidCommand(IRunningContext runningContext, InvalidCommandException e) {
            var returnValue = Local_CatchException_InvalidCommand(runningContext.Runner, e);
            if (returnValue) return true;

            return NodeOperationRelay(Path).CatchException_InvalidCommand(runningContext, e);
        }
        public Func<IRunner, InvalidCommandException, bool> Local_CatchException_InvalidCommand = null;
        protected virtual bool Localize_CatchException_InvalidCommand(IRunner runner, InvalidCommandException e) {
            return false;
        }

        //
        [Constructor]
        void Constructor_CatchException_LabelResolutionMiss() {
            if (Local_CatchException_LabelResolutionMiss == null) Local_CatchException_LabelResolutionMiss = Localize_CatchException_LabelResolutionMiss;
        }
        public bool CatchException_LabelResolutionMiss(IRunningContext runningContext, LabelResolutionMissException e) {
            var returnValue = Local_CatchException_LabelResolutionMiss(runningContext.Runner, e);
            if (returnValue) return true;

            return NodeOperationRelay(Path).CatchException_LabelResolutionMiss(runningContext, e);
        }
        public Func<IRunner, LabelResolutionMissException, bool> Local_CatchException_LabelResolutionMiss = null;
        protected virtual bool Localize_CatchException_LabelResolutionMiss(IRunner runner, LabelResolutionMissException e) {
            return false;
        }

        //
        [Constructor]
        void Constructor_CatchException_CallStackEmptyPop() {
            if (Local_CatchException_CallStackEmptyPop == null) Local_CatchException_CallStackEmptyPop = Localize_CatchException_CallStackEmptyPop;
        }
        public bool CatchException_CallStackEmptyPop(IRunningContext runningContext, CallStackEmptyPopException e) {
            var returnValue = Local_CatchException_CallStackEmptyPop(runningContext.Runner, e);
            if (returnValue) return true;

            return NodeOperationRelay(Path).CatchException_CallStackEmptyPop(runningContext, e);
        }
        public Func<IRunner, CallStackEmptyPopException, bool> Local_CatchException_CallStackEmptyPop = null;
        protected virtual bool Localize_CatchException_CallStackEmptyPop(IRunner runner, CallStackEmptyPopException e) {
            return false;
        }

        //
        [Constructor]
        void Constructor_CatchException_ProgramCounterOutOfRange() {
            if (Local_CatchException_ProgramCounterOutOfRange == null) Local_CatchException_ProgramCounterOutOfRange = Localize_CatchException_ProgramCounterOutOfRange;
        }
        public bool CatchException_ProgramCounterOutOfRange(IRunningContext runningContext, ProgramCounterOutOfRangeException e) {
            var returnValue = Local_CatchException_ProgramCounterOutOfRange(runningContext.Runner, e);
            if (returnValue) return true;

            return NodeOperationRelay(Path).CatchException_ProgramCounterOutOfRange(runningContext, e);
        }
        public Func<IRunner, ProgramCounterOutOfRangeException, bool> Local_CatchException_ProgramCounterOutOfRange = null;
        protected virtual bool Localize_CatchException_ProgramCounterOutOfRange(IRunner runner, ProgramCounterOutOfRangeException e) {
            return false;
        }

        #endregion

        //シリアライズ
        [Constructor]
        void Constructor_Serialize() {
            if (Local_Serialize == null) Local_Serialize = Localize_Serialize;
            if(Local_SerializeAfter == null) Local_SerializeAfter = Localize_SerializeAfter;
        }
        public string Serialize() {
            var returnValue = Local_Serialize();

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

            //アフターの実行
            Localize_SerializeAfter((sdReady as CustomNodeSdReady));

            //シリアライズの実行
            return Engine.Infra.GeneralSd.Serialize((sdReady as CustomNodeSdReady));

        }
        public Func<(bool, CustomNodeSdReady)> Local_Serialize = null;
        protected virtual (bool, CustomNodeSdReady) Localize_Serialize() {
            return (false, null);
        }
        public Action<CustomNodeSdReady> Local_SerializeAfter = null;
        protected virtual void Localize_SerializeAfter(CustomNodeSdReady sdReady) { }

        //デシリアライズ
        [Constructor]
        void Constructor_Deserialize() {
            if (Local_Deserialize == null) Local_Deserialize = Localize_Deserialize;
        }
        public void Deserialize(FlowRunnerEngine engine, string text) {
            var returnValue = Local_Deserialize(engine, text);
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
        }
        public Func<FlowRunnerEngine, string, (bool, CustomNodeSdReady)> Local_Deserialize = null;
        protected virtual (bool, CustomNodeSdReady) Localize_Deserialize(FlowRunnerEngine engine, string text) {
            return (false, null);
        }

    }


    public class CustomNodeSdReady: NodeSdReadyCommon
    {
        public string Path = "";
        public string RootNodeEngineManagementPath = "/";
    }


}

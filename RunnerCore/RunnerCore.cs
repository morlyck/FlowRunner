using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowRunner.Engine
{
    public interface IRunnerCoreOrdertaker
    {
        IPack GetPack(IRunningContext runningContext, string packCode);
        ICommandExecutionContext Evaluation_ArgumentExpansion(IRunningContext runningContext, string commandSymbol, string packCode, string label, string expansionArgumentText);
        //コマンドの実行を行わなかった場合の戻り値 : false
        bool ExecutionExpansionCommand(IRunningContext runningContext, string commandSymbol, ICommandExecutionContext commandExecutionContext);
    }

    //仕様
    //・ビルドインのコマンドよりも拡張コマンドの方を優先して実行する((例)拡張コマンドとして"return"がある場合はビルドインコマンドは実行されない)
    //
    public static class RunnerCore
    {
        public static IRunnerCoreOrdertaker RunnerCoreOrdertaker;

        //ラベルランナーの実行としての最小単位を実行します。
        //この関数が実行中はスナップショットによる復元性を保証しません。
        //実行中にスナップショットを作成するとデータが破損する場合があります。
        public static void ShotRun(IRunningContext runningContext) {
            //停止状態になっている場合は処理を切り上げる
            if (runningContext.IsHalting) return;

            //ステートメントの取得
            IStatement statement = runningContext.Statements[runningContext.ProgramCounter];

            //引数の評価
            ICommandExecutionContext commandExecutionContext = null;
            if (!statement.ArgumentEvaluationExpansionMode) {
                //引数評価を非拡張モードで行います。
                commandExecutionContext = runningContext.BuildinCommandExecutionContext;
                //ステートメントの値をセット
                commandExecutionContext.JumpPackCode = statement.PackCode;
                commandExecutionContext.JumpLabel = statement.Label;
                commandExecutionContext.ArgumentText = statement.ArgumentText;

            } else {
                //引数評価を拡張モードで行います。
                commandExecutionContext = RunnerCoreOrdertaker.Evaluation_ArgumentExpansion(runningContext, statement.CommandSymbol, statement.PackCode, statement.Label, statement.ArgumentText);
            }

            //コンテキストの初期化
            commandExecutionContext.ReturnFlag = false;
            commandExecutionContext.PushFlag = false;
            commandExecutionContext.JumpFlag = false;

            //コマンドの実行
            ExecutionCommand(runningContext, statement.CommandSymbol, commandExecutionContext);
        }

        //ラベルが指すStatementIndex を取得します。
        static int GetStatementIndex_LabelResolution(IRunningContext runningContext, string packCode, string label) {
            if (label == "") return -1;

            //PackCodeが現在ロードしているパックを指していなかった場合はパックを取得する。
            Dictionary<string, int> targetLabels = (runningContext.CurrentPackCode == packCode) ?
                runningContext.Labels :
                RunnerCoreOrdertaker.GetPack(runningContext, label).Labels;

            if (!targetLabels.ContainsKey(label)) {
                runningContext.IsHalting = true;
                throw new Exception("LabelRunner ラベルの解決に失敗しました");
                return -1;
            }
            return targetLabels[label];
        }

        //コマンドの実行を行う関数です
        static void ExecutionCommand(IRunningContext runningContext, string commandSymbol, ICommandExecutionContext commandExecutionContext) {

            //コマンドシンボルごとの処理を行います

            //拡張コマンドの実行
            //コマンドの実行を行わなかった場合の戻り値 : false
            bool ran = RunnerCoreOrdertaker.ExecutionExpansionCommand(runningContext, commandSymbol, commandExecutionContext);

            //拡張コマンドの実行がなかった場合はビルドインコマンドを実行します
            if (!ran) {
                switch (commandSymbol) {
                    default:
                        runningContext.IsHalting = true;
                        throw new Exception("無効なCommandを実行しようとした");
                        break;

                    //以下ビルドインコマンド
                    case "nop":
                        break;
                    case "halt":
                        runningContext.IsHalting = true;
                        break;
                    case "jump":
                        commandExecutionContext.JumpFlag = true;
                        break;
                    case "call":
                        commandExecutionContext.PushFlag = true;
                        commandExecutionContext.JumpFlag = true;
                        break;
                    case "return":
                        commandExecutionContext.ReturnFlag = true;
                        break;
                }
            }


            //PackCodeとPCの更新を行います

            //現在のPackCodeとPCをコールスタックに積みます
            if (commandExecutionContext.PushFlag) {
                StackFrame stackFrame = new StackFrame();
                stackFrame.PackCode = runningContext.CurrentPackCode;
                stackFrame.ProgramCounter = runningContext.ProgramCounter;

                runningContext.CallStack.Push(stackFrame);
            }

            //コールスタックからPackCodeとPCを復元します
            if (commandExecutionContext.ReturnFlag) {
                StackFrame stackFrame = runningContext.CallStack.Pop();
                runningContext.CurrentPackCode = stackFrame.PackCode;
                runningContext.ProgramCounter = stackFrame.ProgramCounter;
            }

            //移動先のPCを決定します
            string nextPackCode = "";
            if (commandExecutionContext.JumpFlag) {
                //ジャンプする場合
                nextPackCode = commandExecutionContext.JumpPackCode;
                runningContext.ProgramCounter = GetStatementIndex_LabelResolution(runningContext, commandExecutionContext.JumpPackCode, commandExecutionContext.JumpLabel);
            } else {
                //次へ進む場合
                runningContext.ProgramCounter++;
            }

            //必要であればパックをロードします
            if (nextPackCode != "" && nextPackCode != runningContext.CurrentPackCode) {
                IPack pack = RunnerCoreOrdertaker.GetPack(runningContext, nextPackCode);
                runningContext.Labels = pack.Labels;
                runningContext.Statements = pack.Statements;

                runningContext.CurrentPackCode = nextPackCode;
            }

            //移動先が有効か確認します
            if (!(runningContext.ProgramCounter <= runningContext.Statements.Length)) {
                runningContext.IsHalting = true;
                throw new Exception($"プログラムカウンターが有効な範囲にありません。 PC : {runningContext.ProgramCounter} Statements.Length : {runningContext.Statements.Length} PackCode : {runningContext.CurrentPackCode}");
            }

            //コマンドの実行はこれにて完了です
            return;
        }




    }

    public interface IRunningContext
    {
        bool IsHalting { get; set; }
        Dictionary<string, int> Labels { get; set; }
        IStatement[] Statements { get; set; }

        int ProgramCounter { get; set; }
        string CurrentPackCode { get; set; }

        Stack<StackFrame> CallStack { get; set; }

        //
        BuildinCommandExecutionContext BuildinCommandExecutionContext { get; }
    }

    public class RunningContext : IRunningContext
    {
        public bool IsHalting { get; set; } = true;
        public Dictionary<string, int> Labels { get; set; } = new Dictionary<string, int>();
        public IStatement[] Statements { get; set; } = new IStatement[0];
        public int ProgramCounter { get; set; } = 0;
        public string CurrentPackCode { get; set; } = "";
        public Stack<StackFrame> CallStack { get; set; } = new Stack<StackFrame>();

        //
        public BuildinCommandExecutionContext BuildinCommandExecutionContext { get; } = new BuildinCommandExecutionContext();
    }

    public class StackFrame
    {
        public string PackCode;
        public int ProgramCounter;
    }

    public interface IPack
    {
        Dictionary<string, int> Labels { get; }
        IStatement[] Statements { get; }
    }
    public class Pack : IPack
    {
        public Dictionary<string, int> Labels { get; set; } = new Dictionary<string, int>();
        public IStatement[] Statements { get; set; } = new IStatement[0];
    }

    public interface IStatement
    {
        string CommandSymbol { get; }
        string PackCode { get; }
        string Label { get; }
        bool ArgumentEvaluationExpansionMode { get; }
        string ArgumentText { get; }
    }
    public class Statement : IStatement
    {
        public string CommandSymbol { get; set; } = "";
        public string PackCode { get; set; } = "";
        public string Label { get; set; } = "";
        public bool ArgumentEvaluationExpansionMode { get; set; } = false;
        public string ArgumentText { get; set; } = "";
    }
    public interface ICommandExecutionContext
    {
        string JumpPackCode { get; set; }
        string JumpLabel { get; set; }
        string ArgumentText { get; set; }

        bool ReturnFlag { get; set; }
        bool PushFlag { get; set; }
        bool JumpFlag { get; set; }
    }
    public class BuildinCommandExecutionContext : ICommandExecutionContext
    {

        public string JumpPackCode { get; set; }
        public string JumpLabel { get; set; }
        public string ArgumentText { get; set; } = "";
        public bool ReturnFlag { get; set; }
        public bool PushFlag { get; set; }
        public bool JumpFlag { get; set; }
    }
}

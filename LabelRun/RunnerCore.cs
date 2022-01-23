using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
using FlowRunner.Engine;

namespace FlowRunner.Engine
{
    public partial interface IRunningContext : FlowRunner.LabelRun.IRunningContext_atLabelRun
    { }
}

namespace FlowRunner.LabelRun
{
    public interface IRunningContext_atLabelRun
    {
        bool IsHalting { get; set; }
        Dictionary<string, int> Labels { get; set; }
        Statement[] Statements { get; set; }

        int ProgramCounter { get; set; }
        string CurrentPackCode { get; set; }

        Stack<StackFrame> CallStack { get; set; }

        //
        BuildinCommandExecutionContext BuildinCommandExecutionContext { get; }
    }
    public interface ILabelRunOrdertaker
    {
        Pack GetPack(IRunningContext runningContext, string packCode);
        CommandExecutionContext Evaluation_ArgumentExpansion(IRunningContext runningContext, string commandSymbol, string packCode, string label, string expansionArgumentText);
        //コマンドの実行を行わなかった場合の戻り値 : false
        bool ExecutionExpansionCommand(IRunningContext runningContext, string commandSymbol, CommandExecutionContext commandExecutionContext);
    }

    public abstract class Pack
    {
        public Dictionary<string, int> Labels = new Dictionary<string, int>();
        public Statement[] Statements = new Statement[0];
    }

    public abstract class Statement
    {
        public string CommandSymbol = "";
        public string PackCode = "";
        public string Label = "";
        public bool ArgumentEvaluationExpansionMode = false;
        public string ArgumentText = "";
    }
    public abstract class CommandExecutionContext
    {
        public string JumpPackCode = "";
        public string JumpLabel = "";
        public string ArgumentText = "";

        public bool ReturnFlag = false;
        public bool PushFlag = false;
        public bool JumpFlag = false;
    }

    //仕様
    //・ビルドインのコマンドよりも拡張コマンドの方を優先して実行する((例)拡張コマンドとして"return"がある場合はビルドインコマンドは実行されない)
    //
    public static class RunnerCore
    {
        public static ILabelRunOrdertaker LabelRunOrdertaker;

        //ラベルランナーの実行としての最小単位を実行します。
        //この関数が実行中はスナップショットによる復元性を保証しません。
        //実行中にスナップショットを作成するとデータが破損する場合があります。
        public static void ShotRun(IRunningContext runningContext) {
            //停止状態になっている場合は処理を切り上げる
            if (runningContext.IsHalting) return;

            //ステートメントの取得
            Statement statement = runningContext.Statements[runningContext.ProgramCounter];

            //引数の評価
            CommandExecutionContext commandExecutionContext = null;
            if (!statement.ArgumentEvaluationExpansionMode) {
                //引数評価を非拡張モードで行います。
                commandExecutionContext = runningContext.BuildinCommandExecutionContext;
                //ステートメントの値をセット
                commandExecutionContext.JumpPackCode = statement.PackCode;
                commandExecutionContext.JumpLabel = statement.Label;
                commandExecutionContext.ArgumentText = statement.ArgumentText;

            } else {
                //引数評価を拡張モードで行います。
                commandExecutionContext = LabelRunOrdertaker.Evaluation_ArgumentExpansion(runningContext, statement.CommandSymbol, statement.PackCode, statement.Label, statement.ArgumentText);
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
                LabelRunOrdertaker.GetPack(runningContext, label).Labels;

            if (!targetLabels.ContainsKey(label)) {
                runningContext.IsHalting = true;
                throw new Exception("LabelRunner ラベルの解決に失敗しました");
                return -1;
            }
            return targetLabels[label];
        }

        //コマンドの実行を行う関数です
        static void ExecutionCommand(IRunningContext runningContext, string commandSymbol, CommandExecutionContext commandExecutionContext) {

            //コマンドシンボルごとの処理を行います

            //拡張コマンドの実行
            //コマンドの実行を行わなかった場合の戻り値 : false
            bool ran = LabelRunOrdertaker.ExecutionExpansionCommand(runningContext, commandSymbol, commandExecutionContext);

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
                Pack pack = LabelRunOrdertaker.GetPack(runningContext, nextPackCode);
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

    public class StackFrame
    {
        public string PackCode = "";
        public int ProgramCounter = -1;
    }

    public class BuildinCommandExecutionContext : CommandExecutionContext
    { }
}

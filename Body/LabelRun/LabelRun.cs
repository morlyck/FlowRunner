﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
using FlowRunner.Engine;

namespace FlowRunner.Engine
{
    public partial interface IRunningContext : Engine.IRunningContext_atLabelRun
    { }
}

namespace FlowRunner.Engine
{
    public class Pack
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

        public Action? AfterAction = null;
    }
}

namespace FlowRunner.Engine
{
    public interface IRunningContext_atLabelRun
    {
        bool IsHalting { get; set; }
        Dictionary<string, int> Labels { get; set; }
        Statement[] Statements { get; set; }

        int ProgramCounter { get; set; }
        string CurrentPackCode { get; set; }

        Stack<StackFrame> CallStack { get; set; }

        //割り込み情報
        List<InterruptInfo> InterruptInfos { get; set; }

        //
        BuildinCommandExecutionContext BuildinCommandExecutionContext { get; }

        void SetLabelsAndStatements(Pack pack);

    }
    public interface ILabelRunOrdertaker
    {
        Pack GetPack(IRunningContext runningContext, string packCode);
        CommandExecutionContext Evaluation_ArgumentExpansion(IRunningContext runningContext, string commandSymbol, string packCode, string label, string expansionArgumentText);
        //コマンドの実行を行わなかった場合の戻り値 : false
        bool ExecutionExpansionCommand(IRunningContext runningContext, string commandSymbol, CommandExecutionContext commandExecutionContext);

        //例外処理
        //リスローする場合は戻り値 : false
        //無効なコマンドを実行しようとしたとき
        bool CatchException_InvalidCommand(IRunningContext runningContext, InvalidCommandException e);
        //PCが有効範囲外であるとき
        bool CatchException_ProgramCounterOutOfRange(IRunningContext runningContext, ProgramCounterOutOfRangeException e);
        //ラベルの解決に失敗したとき
        bool CatchException_LabelResolutionMiss(IRunningContext runningContext, LabelResolutionMissException e);
    }

    //仕様
    //・ビルドインのコマンドよりも拡張コマンドの方を優先して実行する((例)拡張コマンドとして"return"がある場合はビルドインコマンドは実行されない)
    //
    public class LabelRun
    {
        public ILabelRunOrdertaker LabelRunOrdertaker = null;

        //ラベルランナーの実行としての最小単位を実行します。
        //この関数が実行中はスナップショットによる復元性を保証しません。
        //実行中にスナップショットを作成するとデータが破損する場合があります。
        public void ShotRun(IRunningContext runningContext) {
            //停止状態になっている場合は処理を切り上げる
            if (runningContext.IsHalting) return;

            try {
                //オーダーテイカーがセットされていない場合は例外を投げる
                if (LabelRunOrdertaker == null) throw new Null_LabelRunOrdertakerException("オーダーテイカーがセットされていません");

                //ステートメントの取得
                Statement statement = runningContext.Statements[runningContext.ProgramCounter];

                //割り込み対応
                if(runningContext.InterruptInfos.Count != 0) {
                    InterruptInfo info = runningContext.InterruptInfos[0];

                    //割り込みからの復帰にリターンを使うためPCを一つ減算しておく
                    runningContext.ProgramCounter--;

                    CommandExecutionContext interrup_CommandExecutionContext = runningContext.BuildinCommandExecutionContext;
                    interrup_CommandExecutionContext.JumpPackCode = info.PackCode;
                    interrup_CommandExecutionContext.JumpLabel = info.Label;

                    //フラグの設定
                    interrup_CommandExecutionContext.ReturnFlag = false;
                    interrup_CommandExecutionContext.PushFlag = true;//減算した状態でスタックに積む
                    interrup_CommandExecutionContext.JumpFlag = true;//割り込みで指定されたところにジャンプする
                    UpdateContext(runningContext, interrup_CommandExecutionContext);

                    runningContext.InterruptInfos.RemoveAt(0);

                    ShotRun(runningContext);
                    return;
                }

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
                commandExecutionContext.AfterAction = null;

                //コマンドの実行
                ExecutionCommand(runningContext, statement.CommandSymbol, commandExecutionContext);

                //アフターアクション
                commandExecutionContext.AfterAction?.Invoke();

            } catch (Null_LabelRunOrdertakerException e) {
                //オーダーテイカーがセットされていない場合は無条件でリスロー
                throw;
            } catch (InvalidCommandException e) {
                //無効なコマンドを実行しようとしたとき
                if (!LabelRunOrdertaker.CatchException_InvalidCommand(runningContext, e)) throw;
            } catch (ProgramCounterOutOfRangeException e) {
                //PCが有効範囲外であるとき
                if (!LabelRunOrdertaker.CatchException_ProgramCounterOutOfRange(runningContext, e)) throw;
            } catch (LabelResolutionMissException e) {
                //ラベルの解決に失敗したとき
                if (!LabelRunOrdertaker.CatchException_LabelResolutionMiss(runningContext, e)) throw;
            } catch (Exception_atLabelRun e) {
                //その他のLabelRunの例外
                throw;//リスロー
            }
        }

        //ラベルが指すStatementIndex を取得します。
        public int GetStatementIndex_LabelResolution(IRunningContext runningContext, string packCode, string label) {
            if (label == "") return -1;

            //PackCodeが指しているパックが現在ロードされていない場合はパックを取得する。
            Dictionary<string, int> targetLabels = (runningContext.CurrentPackCode == packCode || packCode == "") ?
                runningContext.Labels :
                LabelRunOrdertaker.GetPack(runningContext, packCode).Labels;

            if (targetLabels == null) throw new LabelResolutionMissException($"パックの取得に失敗しました runningContext:{runningContext} packCode:{packCode} label:{label}");
            if (!targetLabels.ContainsKey(label)) {
                runningContext.IsHalting = true;
                throw new LabelResolutionMissException($"ラベルの解決に失敗しました runningContext:{runningContext}packCode:{packCode} label:{label}");
                return -1;
            }
            return targetLabels[label];
        }

        //コマンドの実行を行う関数です
        void ExecutionCommand(IRunningContext runningContext, string commandSymbol, CommandExecutionContext commandExecutionContext) {

            //コマンドシンボルごとの処理を行います

            //拡張コマンドの実行
            //コマンドの実行を行わなかった場合の戻り値 : false
            bool ran = LabelRunOrdertaker.ExecutionExpansionCommand(runningContext, commandSymbol, commandExecutionContext);

            //拡張コマンドの実行がなかった場合はビルドインコマンドを実行します
            if (!ran) {
                switch (commandSymbol) {
                    default:
                        runningContext.IsHalting = true;
                        throw new InvalidCommandException($"無効なCommandを実行しようとした commandSymbol : {commandSymbol}");
                        break;

                    //以下ビルドインコマンド
                    //ノーオペレーション命令
                    case "nop":
                        break;
                    //停止命令
                    case "halt":
                        runningContext.IsHalting = true;
                        break;
                    //ジャンプ命令
                    case "jump":
                        commandExecutionContext.JumpFlag = true;
                        break;
                    //呼び出し命令
                    case "call":
                        commandExecutionContext.PushFlag = true;
                        commandExecutionContext.JumpFlag = true;
                        break;
                    //サブルーチンからの復帰命令
                    case "return":
                        commandExecutionContext.ReturnFlag = true;
                        break;
                    //割り込み命令
                    case "int":
                        InterruptInfo interruptInfo = new InterruptInfo();
                        interruptInfo.PackCode = commandExecutionContext.JumpPackCode;
                        interruptInfo.Label = commandExecutionContext.JumpLabel;
                        runningContext.InterruptInfos.Add(interruptInfo);
                        break;
                }
            }

            UpdateContext(runningContext, commandExecutionContext);

        }
        
        void UpdateContext(IRunningContext runningContext, CommandExecutionContext commandExecutionContext) {
            //PackCodeとPCの更新を行います

            //現在のPackCodeとPCをコールスタックに積みます
            if (commandExecutionContext.PushFlag) {
                StackFrame stackFrame = new StackFrame();
                stackFrame.PackCode = runningContext.CurrentPackCode;
                stackFrame.ProgramCounter = runningContext.ProgramCounter;

                runningContext.CallStack.Push(stackFrame);
            }

            //コールスタックからPackCodeとPCを復元します
            bool packReloadFlag = false;
            if (commandExecutionContext.ReturnFlag) {
                StackFrame stackFrame = runningContext.CallStack.Pop();

                if (stackFrame.PackCode != runningContext.CurrentPackCode) packReloadFlag = true;

                runningContext.CurrentPackCode = stackFrame.PackCode;
                runningContext.ProgramCounter = stackFrame.ProgramCounter;
            }

            //移動先のPCを決定します
            if (commandExecutionContext.JumpFlag) {
                //ジャンプする場合
                if (commandExecutionContext.JumpPackCode != "") packReloadFlag = true;
                //ジャンプ先が省略されているときのcommandExecutionContext.JumpPackCodeの値は""
                //runningContext.CurrentPackCode の値と引数のPackCodeの値が異なる場合はPackを取得してくれる
                runningContext.ProgramCounter = GetStatementIndex_LabelResolution(runningContext, commandExecutionContext.JumpPackCode, commandExecutionContext.JumpLabel);
                //GetStatementIndex_LabelResolutionを呼ぶ前にrunningContext.CurrentPackCodeの値を更新するとパックの取得をしてくれない
                if (commandExecutionContext.JumpPackCode != "") runningContext.CurrentPackCode = commandExecutionContext.JumpPackCode;
            } else {
                //次へ進む場合
                runningContext.ProgramCounter++;
            }

            //必要であればパックをロードします
            if (packReloadFlag) {
                //ランニングコンテキストのパックキャッシュを更新する
                runningContext.SetLabelsAndStatements(LabelRunOrdertaker.GetPack(runningContext, runningContext.CurrentPackCode));
            }

            //移動先が有効か確認します
            if (runningContext.ProgramCounter < 0 || !(runningContext.ProgramCounter <= runningContext.Statements.Length)) {
                runningContext.IsHalting = true;
                throw new ProgramCounterOutOfRangeException($"プログラムカウンターが有効な範囲にありません。 PC : {runningContext.ProgramCounter} Statements.Length : {runningContext.Statements.Length} PackCode : {runningContext.CurrentPackCode}");
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

    //割り込み情報
    public class InterruptInfo
    {
        public string PackCode = "";
        public string Label = "";
    }


    //例外
    public class Exception_atLabelRun: Exception {
        public Exception_atLabelRun(string? message) : base(message) { }
    }
    //オーダーテイカーが未セット
    public class Null_LabelRunOrdertakerException : Exception_atLabelRun
    {
        public Null_LabelRunOrdertakerException(string? message) : base(message) { }
    }
    //無効なコマンドを実行しようとした
    public class InvalidCommandException : Exception_atLabelRun
    {
        public InvalidCommandException(string? message) : base(message) { }
    }
    //PCが有効範囲外を指している
    public class ProgramCounterOutOfRangeException : Exception_atLabelRun
    {
        public ProgramCounterOutOfRangeException(string? message) : base(message) { }
    }
    //ラベルの解決に失敗した
    public class LabelResolutionMissException : Exception_atLabelRun
    {
        public LabelResolutionMissException(string? message) : base(message) { }
    }



}

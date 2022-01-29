using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.LabelRun;

namespace FlowRunner.Engine
{



    public class RunningContextSdReady
    {
        public bool IsHalting { get; set; } = true;
        public int ProgramCounter { get; set; } = 0;
        public string CurrentPackCode { get; set; } = "";
        public Stack<StackFrame> CallStack { get; set; } = new Stack<StackFrame>();
    }
    public class RunningContext : RunningContextSdReady,
   IRunningContext
    {
        public RunningContextSdReady GetSdReady() {
            RunningContextSdReady sdReady = new RunningContextSdReady();
            sdReady.IsHalting = IsHalting;
            sdReady.ProgramCounter = ProgramCounter;
            sdReady.CurrentPackCode = CurrentPackCode;
            sdReady.CallStack = CallStack;

            return sdReady;
        }

        public Dictionary<string, int> Labels { get; set; } = new Dictionary<string, int>();
        public Statement[] Statements { get; set; } = new Statement[0];
        //
        public BuildinCommandExecutionContext BuildinCommandExecutionContext { get; } = new BuildinCommandExecutionContext();

        public void SetLabelsAndStatements(Pack pack) {
            Labels = pack.Labels;
            Statements = pack.Statements;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowRunner.LabelRun;

namespace FlowRunner.Engine
{
    public class RunningContext : IRunningContext
    {
        public bool IsHalting { get; set; } = true;
        public Dictionary<string, int> Labels { get; set; } = new Dictionary<string, int>();
        public Statement[] Statements { get; set; } = new Statement[0];
        public int ProgramCounter { get; set; } = 0;
        public string CurrentPackCode { get; set; } = "";
        public Stack<StackFrame> CallStack { get; set; } = new Stack<StackFrame>();

        //
        public BuildinCommandExecutionContext BuildinCommandExecutionContext { get; } = new BuildinCommandExecutionContext();
    }
}

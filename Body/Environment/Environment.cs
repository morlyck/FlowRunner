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
    public class Environment
    {
        Dictionary<string, string> variables = new Dictionary<string, string>();
        public string GetValue(string variableName) {
            if (!variables.ContainsKey(variableName)) return null;

            return variables[variableName];
        }
        public string SetValue(string variableName, string value) {
            if (!variables.ContainsKey(variableName)) {
                variables.Add(variableName, value);
            } else {
                variables[variableName] = value;
            }

            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowRunner.Engine
{

    public class VariableEventSubject : IChainEnvironmentOrdertaker
    {

        public void IgnitionGetValueEvent(string variableName, string value) {
            if (!variableEventObservers.ContainsKey(variableName)) return;
            variableEventObservers[variableName].NotifyGetEvent?.Invoke(value);
        }

        //string lastMainText = null;
        public void IgnitionSetValueEvent(string variableName, string value) {
            if (!variableEventObservers.ContainsKey(variableName)) return;
            variableEventObservers[variableName].NotifySetEvent?.Invoke(value);
        }

        Dictionary<string, VariableEventObserver> variableEventObservers = new Dictionary<string, VariableEventObserver>();
        public void AddVariableSetValueEvent(VariableEventObserver observer) {
            variableEventObservers.Add(observer.VariableName, observer);
        }

        public void StartCycleTime(ChainEnvironment chainEnvironment) {
            foreach (KeyValuePair<string, VariableEventObserver> keyValuePair in variableEventObservers) {
                if (!keyValuePair.Value.ResumeIgnition || !chainEnvironment.ExistsValue(keyValuePair.Key)) continue;

                keyValuePair.Value.NotifySetEvent?.Invoke(chainEnvironment.GetValue(keyValuePair.Key));
            }
        }

    }
    public class VariableEventObserver
    {
        public string ObserverCode = "";
        public string VariableName = "";
        public Action<string>? NotifyGetEvent = null;
        public Action<string>? NotifySetEvent = null;
        //
        public bool ResumeIgnition = false;
    }

}

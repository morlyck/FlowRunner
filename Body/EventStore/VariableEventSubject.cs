using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowRunner.Engine
{

    public class VariableEventSubject : IChainEnvironmentOrdertaker
    {

        public void IgnitionGetValueEvent(Type dataType, string variableName, object value) {
            string typeName = dataType.AssemblyQualifiedName;
            if (!variableEventObservers.ContainsKey(typeName)) return;
            if (!variableEventObservers[typeName].ContainsKey(variableName)) return;

            variableEventObservers[dataType.AssemblyQualifiedName][variableName].IgnitionNotifyGetEvent(value);
        }

        //string lastMainText = null;
        public void IgnitionSetValueEvent(Type dataType, string variableName, object value) {
            string typeName = dataType.AssemblyQualifiedName;
            if (!variableEventObservers.ContainsKey(typeName)) return;
            if (!variableEventObservers[typeName].ContainsKey(variableName)) return;

            variableEventObservers[dataType.AssemblyQualifiedName][variableName].IgnitionNotifySetEvent(value);
        }

        Dictionary<string, Dictionary<string, IVariableEventObserver>> variableEventObservers = new Dictionary<string, Dictionary<string, IVariableEventObserver>>();
        public void AddVariableSetValueEvent(IVariableEventObserver observer) {
            Dictionary<string, IVariableEventObserver> observers;
            string typeName = observer.GetDataType().AssemblyQualifiedName;
            if (!variableEventObservers.ContainsKey(typeName)) {
                observers = new Dictionary<string, IVariableEventObserver>();
                observers.Add(observer.VariableName, observer);
                variableEventObservers.Add(typeName, observers);
                return;
            }

            observers = variableEventObservers[typeName];
            if (!observers.ContainsKey(observer.VariableName)) {
                observers.Add(observer.VariableName, observer);
                return;
            }

            observers[observer.VariableName] = observer;
        }

        public void StartCycleTime(ChainEnvironment chainEnvironment) {
            foreach (KeyValuePair<string, Dictionary<string, IVariableEventObserver>> observersData in variableEventObservers) {
                foreach (KeyValuePair<string, IVariableEventObserver> observerData in observersData.Value) {
                    if (!observerData.Value.ResumeIgnition || !chainEnvironment.Exists(observerData.Key)) continue;

                    observerData.Value.IgnitionNotifySetEvent(chainEnvironment.GetValue(observerData.Key));
                }
            }
        }

    }
    public interface IVariableEventObserver {
        Type GetDataType();

        string VariableName { get; set; }
        void IgnitionNotifyGetEvent(object value);
        void IgnitionNotifySetEvent(object value);

        //再開時にイベントを発行するか否か
        bool ResumeIgnition { get; set; }
    }
    public class VariableEventObserver<DataType>: IVariableEventObserver
    {
        public Type GetDataType() {
            return typeof(DataType);
        }
        //
        public string ObserverCode = "";
        public string VariableName { get; set; }
        public Action<DataType>? NotifyGetEvent { get; set; }
        public Action<DataType>? NotifySetEvent { get; set; }
        public void IgnitionNotifyGetEvent(object value) {
            NotifyGetEvent?.Invoke((DataType)value);
        }
        public void IgnitionNotifySetEvent(object value) {
            NotifySetEvent?.Invoke((DataType)value);
        }
        //
        public bool ResumeIgnition { get; set; } = false;
    }

}

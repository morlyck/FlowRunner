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
            if (!variableEventObservers.ContainsKey(variableName)) return;

            string key = getTypeNameAndVariableName(dataType, variableName);
            variableEventObservers[key].IgnitionNotifyGetEvent(value);
        }

        //string lastMainText = null;
        public void IgnitionSetValueEvent(Type dataType, string variableName, object value) {
            if (!variableEventObservers.ContainsKey(variableName)) return;

            string key = getTypeNameAndVariableName(dataType, variableName);
            variableEventObservers[key].IgnitionNotifySetEvent(value);
        }

        Dictionary<string, IVariableEventObserver> variableEventObservers = new Dictionary<string, IVariableEventObserver>();
        public void AddVariableSetValueEvent(IVariableEventObserver observer) {
            string key = getTypeNameAndVariableName(observer.GetDataType(), observer.VariableName);
            variableEventObservers.Add(key, observer);
        }

        string getTypeNameAndVariableName(Type type, string ariableName) {
            return $"{type.AssemblyQualifiedName}|{ariableName}";
        }

        public void StartCycleTime(ChainEnvironment chainEnvironment) {
            foreach (KeyValuePair<string, IVariableEventObserver> keyValuePair in variableEventObservers) {
                if (!keyValuePair.Value.ResumeIgnition || !chainEnvironment.ExistsValue(keyValuePair.Key)) continue;

                keyValuePair.Value.IgnitionNotifySetEvent(chainEnvironment.GetValue(keyValuePair.Key));
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

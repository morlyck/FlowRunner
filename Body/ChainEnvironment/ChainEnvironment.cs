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
    public interface IChainEnvironmentOrdertaker
    {
        //イベントの発火
        void IgnitionGetValueEvent(Type dataType, string variableName, object value);
        void IgnitionSetValueEvent(Type dataType, string variableName, object value);
    }
    public class ChainEnvironmentSdReady : NodeSdReadyCommon
    {
        public List<string> TypeNames = new List<string>();
        public List<string> SerializeText = new List<string>();

    }
    public class ChainEnvironment
    {
        IChainEnvironmentOrdertaker? ordertaker = null;
        public IChainEnvironmentOrdertaker? Ordertaker {
            get => ordertaker;
            set {
                ordertaker = value;
                DataHolderAll((typeName, dataHolder) => {
                    dataHolder.Ordertaker = ordertaker;
                });
            }
        }
        //シリアライズ対応
        public string Serialize(FlowRunnerEngine engine) {
            ChainEnvironmentSdReady sdReady = new ChainEnvironmentSdReady();
            foreach (KeyValuePair<string, IChainEnvironmentDataHolder> dataHolderData in dataHolders) {
                sdReady.TypeNames.Add(dataHolderData.Key);
                sdReady.SerializeText.Add(dataHolderData.Value.Serialize(engine));
            }

            return engine.Infra.GeneralSd.Serialize(sdReady);
        }

        //デシリアライズ対応
        public void Deserialize(FlowRunnerEngine engine, string text) {
            ChainEnvironmentSdReady sdReady = engine.Infra.GeneralSd.Deserialize<ChainEnvironmentSdReady>(text);
            for(int count = 0; count < sdReady.TypeNames.Count; count++) {
                string typeName = sdReady.TypeNames[count];
                string serializeText = sdReady.SerializeText[count];

                var dataHolderType = typeof(ChainEnvironmentDataHolder<>).MakeGenericType(Type.GetType(typeName));
                IChainEnvironmentDataHolder dataHolder = Activator.CreateInstance(dataHolderType) as IChainEnvironmentDataHolder;

                dataHolder.Deserialize(engine, serializeText);

                dataHolders.Add(typeName, dataHolder);
            }
        }

        //---
        ChainEnvironment? upstairEnvironment = null;
        int connectionFloorNo = -1;
        bool looseConnection = false;
        public void SetUpstairEnvironment_LooseConnection(ChainEnvironment upstairEnvironment) {
            this.upstairEnvironment = upstairEnvironment;
            looseConnection = true;
            DataHolderAll((typeName, dataHolder) => {
                dataHolder.SetUpstairEnvironment(upstairEnvironment.GetDataHolder(typeName), looseConnection, connectionFloorNo);
            });
        }
        public void SetUpstairEnvironment(ChainEnvironment upstairEnvironment, int connectionFloorNo) {
            this.upstairEnvironment = upstairEnvironment;
            this.connectionFloorNo = connectionFloorNo;
            looseConnection = false;
            DataHolderAll((typeName, dataHolder) => {
                dataHolder.SetUpstairEnvironment(upstairEnvironment.GetDataHolder(typeName), looseConnection, connectionFloorNo);
            });
        }
        public void SetUpstairEnvironment_ConnectionToCurrentFloorNo(ChainEnvironment upstairEnvironment) {
            this.upstairEnvironment = upstairEnvironment;
            looseConnection = false;
            DataHolderAll((typeName, dataHolder) => {
                dataHolder.SetUpstairEnvironment(upstairEnvironment.GetDataHolder(typeName), looseConnection, connectionFloorNo);
            });
        }
        public void ClearUpstairEnvironmentSetting() {
            this.upstairEnvironment = null;
            this.connectionFloorNo = -1;
            DataHolderAll((typeName, dataHolder) => {
                dataHolder.ClearUpstairEnvironmentSetting();
            });
        }

        //
        Dictionary<string, IChainEnvironmentDataHolder> dataHolders = new Dictionary<string, IChainEnvironmentDataHolder>();

        public ChainEnvironment() {
            //dataHolders.Add(typeof(string).AssemblyQualifiedName, new ChainEnvironmentDataHolder<string>());
        }

        void DataHolderAll(Action<string,IChainEnvironmentDataHolder> action) {
            foreach (KeyValuePair<string, IChainEnvironmentDataHolder> dataHolderData in dataHolders) {
                action(dataHolderData.Key, dataHolderData.Value);
            }
        }
        public IChainEnvironmentDataHolder GetDataHolder(object obj) {
            return GetDataHolder(obj.GetType().AssemblyQualifiedName);
        }
        public IChainEnvironmentDataHolder GetDataHolder<type>(object obj) {
            return GetDataHolder(typeof(type).AssemblyQualifiedName);
        }
        public IChainEnvironmentDataHolder GetDataHolder(string typeName) {
            if (!dataHolders.ContainsKey(typeName)) {
                var dataHolderType = typeof(ChainEnvironmentDataHolder<>).MakeGenericType(Type.GetType(typeName));
                IChainEnvironmentDataHolder dataHolder = Activator.CreateInstance(dataHolderType) as IChainEnvironmentDataHolder;
                dataHolder.Ordertaker = Ordertaker;
                if (upstairEnvironment != null) dataHolder.SetUpstairEnvironment(upstairEnvironment.GetDataHolder(typeName), looseConnection, connectionFloorNo);
                dataHolders.Add(typeName, dataHolder);
                return dataHolder;
            }

            return dataHolders[typeName];
        }
        void DataHolderAction(List<string> typeNames, Action<string,IChainEnvironmentDataHolder> action, Action<string,IChainEnvironmentDataHolder> anotherAction) {
            foreach (KeyValuePair<string, IChainEnvironmentDataHolder> dataHolderData in dataHolders) {
                if (typeNames.IndexOf(dataHolderData.Key) == -1) {
                    action(dataHolderData.Key, dataHolderData.Value);
                } else {
                    anotherAction(dataHolderData.Key, dataHolderData.Value);
                }
            }
        }
        #region(string)
        public string GetValue(string variableName) {
            return GetDataHolder(typeof(string).AssemblyQualifiedName).GetValue(variableName) as string;
        }
        public string SetValue(string variableName, string value) {
            return GetDataHolder(typeof(string).AssemblyQualifiedName).SetValue(variableName, value) as string;
        }
        public string CreateOrSetValue_Local(string variableName, string value) {
            return GetDataHolder(typeof(string).AssemblyQualifiedName).CreateOrSetValue_Local(variableName, value) as string;
        }
        public bool Exists(string variableName) {
            return GetDataHolder(typeof(string).AssemblyQualifiedName).Exists(variableName);
        }
        #endregion

        #region(object)
        public object GetValue(Type type, string variableName) {
            return GetDataHolder(type.AssemblyQualifiedName).GetValue(variableName);
        }
        public object SetValue(Type type, string variableName, object value) {
            return GetDataHolder(type.AssemblyQualifiedName).SetValue(variableName, value);
        }
        public object CreateOrSetValue_Local(Type type, string variableName, object value) {
            return GetDataHolder(type.AssemblyQualifiedName).CreateOrSetValue_Local(variableName, value);
        }
        public bool Exists(Type type, string variableName) {
            return GetDataHolder(type.AssemblyQualifiedName).Exists(variableName);
        }
        #endregion

        public DataType GetValue<DataType>(string variableName) {
            return (DataType)GetDataHolder(typeof(DataType).AssemblyQualifiedName).GetValue(variableName);
        }
        public DataType SetValue<DataType>(string variableName, object value) {
            return (DataType)GetDataHolder(typeof(DataType).AssemblyQualifiedName).SetValue(variableName, value);
        }
        public DataType CreateOrSetValue_Local<DataType>(string variableName, object value) {
            return (DataType)GetDataHolder(typeof(DataType).AssemblyQualifiedName).CreateOrSetValue_Local(variableName, value);
        }
        public bool Exists<DataType>(string variableName) {
            return GetDataHolder(typeof(DataType).AssemblyQualifiedName).Exists(variableName);
        }

        #region(string)
        public void Down(List<string> returnValues = null, List<string> arguments = null) {
            GetDataHolder(typeof(string).AssemblyQualifiedName).Down(returnValues, arguments);
        }

        public void PullArguments(List<string> variables = null) {
            GetDataHolder(typeof(string).AssemblyQualifiedName).PullArguments(variables);
        }
        public void Up(List<string> returnValues = null) {
            GetDataHolder(typeof(string).AssemblyQualifiedName).Up(returnValues);
        }
        #endregion
        public void Down(List<(Type, string)> returnValues = null, List<(Type, string)> arguments = null) {
            Dictionary<string,List<string>> organizeReturnValues = new Dictionary<string, List<string>>();
            Dictionary<string,List<string>> organizeArguments = new Dictionary<string, List<string>>();

            //returnValues
            foreach ((Type, string) returnValueData in returnValues) {
                string key =returnValueData.Item1.AssemblyQualifiedName;
                if (!organizeReturnValues.ContainsKey(key)) {
                    List<string> list = new List<string>();
                    list.Add(returnValueData.Item2);
                    organizeReturnValues.Add(key, list);
                    continue;
                }
                organizeReturnValues[key].Add(returnValueData.Item2);
            }
            //arguments
            foreach ((Type, string) argumentData in arguments) {
                string key = argumentData.Item1.AssemblyQualifiedName;
                if (!organizeArguments.ContainsKey(key)) {
                    List<string> list = new List<string>();
                    list.Add(argumentData.Item2);
                    organizeArguments.Add(key, list);
                    continue;
                }
                organizeArguments[key].Add(argumentData.Item2);
            }

            //DataHolderの呼び出し
            /*
            foreach(KeyValuePair<string, List<string>> _returnValuesData in organizeReturnValues) {
                string key = _returnValuesData.Key;
                IChainEnvironmentDataHolder dataHolder = GetDataHolder(key);
                List<string> _arguments = (!organizeArguments.ContainsKey(key)) ? null : organizeArguments[key];

                dataHolder.Down(_returnValuesData.Value, _arguments);
            }*/
            DataHolderAction(new List<string>(organizeReturnValues.Keys), 
                (typeName, dataHolder) => {
                    List<string> _returnValues = organizeReturnValues[typeName];
                    List<string> _arguments = (!organizeArguments.ContainsKey(typeName)) ? null : organizeArguments[typeName];

                    dataHolder.Down(_returnValues, _arguments);
            }, (typeName, dataHolder) => {
                dataHolder.Down(null,null);
            });
        }

        public void PullArguments(List<(Type, string)> variables = null) {
            Dictionary<string, List<string>> organizeVariables = new Dictionary<string, List<string>>();

            //variables
            foreach ((Type, string) returnValueData in variables) {
                string key = returnValueData.Item1.AssemblyQualifiedName;
                if (!organizeVariables.ContainsKey(key)) {
                    List<string> list = new List<string>();
                    list.Add(returnValueData.Item2);
                    organizeVariables.Add(key, list);
                    continue;
                }
                organizeVariables[key].Add(returnValueData.Item2);
            }

            //DataHolderの呼び出し
            /*
            foreach (KeyValuePair<string, List<string>> _variablesData in organizeVariables) {
                string key = _variablesData.Key;
                IChainEnvironmentDataHolder dataHolder = GetDataHolder(key);

                dataHolder.Up(_variablesData.Value);
            }*/
            DataHolderAction(new List<string>(organizeVariables.Keys),
                (typeName, dataHolder) => {
                    dataHolder.PullArguments(organizeVariables[typeName]);
                }, (typeName, dataHolder) => {
                    dataHolder.PullArguments(null);
                });
        }
        public void Up(List<(Type, string)> returnValues = null) {
            Dictionary<string, List<string>> organizeReturnValues = new Dictionary<string, List<string>>();

            //returnValues
            foreach ((Type, string) returnValueData in returnValues) {
                string key = returnValueData.Item1.AssemblyQualifiedName;
                if (!organizeReturnValues.ContainsKey(key)) {
                    List<string> list = new List<string>();
                    list.Add(returnValueData.Item2);
                    organizeReturnValues.Add(key, list);
                    continue;
                }
                organizeReturnValues[key].Add(returnValueData.Item2);
            }

            //DataHolderの呼び出し
            /*
            foreach (KeyValuePair<string, List<string>> _returnValuesData in organizeReturnValues) {
                string key = _returnValuesData.Key;
                IChainEnvironmentDataHolder dataHolder = GetDataHolder(key);

                dataHolder.Up(_returnValuesData.Value);
            }*/
            DataHolderAction(new List<string>(organizeReturnValues.Keys),
                (typeName, dataHolder) => {
                    dataHolder.PullArguments(organizeReturnValues[typeName]);
                }, (typeName, dataHolder) => {
                    dataHolder.Up(null);
                });
        }


        //---


        //未定義の変数へアクセスしようとした場合
        public class UndefinedVariableException : Exception
        {
            public UndefinedVariableException(string text) : base(text) { }
        }
        //大域環境で引数引き込みを行おうとした場合
        public class PullArgumentsOnGlobalEnvironmentException : Exception
        {
            public PullArgumentsOnGlobalEnvironmentException(string text) : base(text) { }
        }
        //無効な引数引き込みを行おうとした場合
        public class InvalidPullArgumentsException : Exception
        {
            public InvalidPullArgumentsException(string text) : base(text) { }
        }
        //大域環境でアップ処理を行おうとした場合
        public class UpOnGlobalEnvironmentException : Exception
        {
            public UpOnGlobalEnvironmentException(string text) : base(text) { }
        }
        //指定されたコネクションフロアナンバーに該当する階層がない場合
        public class MisalignedConnectionFloorNoException : Exception
        {
            public MisalignedConnectionFloorNoException(string text) : base(text) { }
        }
    }

    public interface IChainEnvironmentDataHolder {
        string Serialize(FlowRunnerEngine engine);
        void Deserialize(FlowRunnerEngine engine, string text);
        //---
        IChainEnvironmentOrdertaker? Ordertaker { get; set; }
        void SetUpstairEnvironment(IChainEnvironmentDataHolder upstairEnvironment, bool looseConnection, int connectionFloorNo);
        void ClearUpstairEnvironmentSetting();
        //
        object GetValue(string variableName);
        object SetValue(string variableName, object value);
        object CreateOrSetValue_Local(string variableName, object value);
        bool Exists(string variableName);
        //
        void Down(List<string> returnValues, List<string> arguments);
        void PullArguments(List<string> variables);
        void Up(List<string> returnValues);
        //
        int CurrentFloorNo { get; set; }

    }
    public class ChainEnvironmentDataHolderSdReady<DataType> : NodeSdReadyCommon
    {
        public List<FloorDataFrame<DataType>> floorDataFrames = null;

        public int currentFloorNo = 0;
    }
    public class FloorDataFrame<DataType>
    {
        public Dictionary<string, DataType> Variables = new Dictionary<string, DataType>();

        public List<string> Arguments = new List<string>();
        public List<string> ReturnValues = new List<string>();

        //public Dictionary<string, List<string>> Lists = new Dictionary<string, List<string>>();
    }
    public class ChainEnvironmentDataHolder<DataType>: IChainEnvironmentDataHolder
    {
        public IChainEnvironmentOrdertaker? Ordertaker { get; set; } = null;
        //シリアライズ対応
        public string Serialize(FlowRunnerEngine engine) {
            ChainEnvironmentDataHolderSdReady<DataType> sdReady = new ChainEnvironmentDataHolderSdReady<DataType>();
            sdReady.floorDataFrames = floorDataFrames;
            sdReady.currentFloorNo = CurrentFloorNo;

            return engine.Infra.GeneralSd.Serialize(sdReady);
        }

        //デシリアライズ対応
        public void Deserialize(FlowRunnerEngine engine, string text) {
            ChainEnvironmentDataHolderSdReady<DataType> sdReady = engine.Infra.GeneralSd.Deserialize<ChainEnvironmentDataHolderSdReady<DataType>>(text);

            floorDataFrames = sdReady.floorDataFrames;
            CurrentFloorNo = sdReady.currentFloorNo;

            currentFloor = floorDataFrames[CurrentFloorNo];
        }

        //---
        //上位環境
        ChainEnvironmentDataHolder<DataType>? upstairEnvironment = null;
        int connectionFloorNo = 0;
        bool looseConnection = false;
        public void SetUpstairEnvironment(IChainEnvironmentDataHolder upstairEnvironment, bool looseConnection, int connectionFloorNo) {
            this.upstairEnvironment = upstairEnvironment as ChainEnvironmentDataHolder<DataType>;
            if (connectionFloorNo == -1) {
                this.connectionFloorNo = upstairEnvironment.CurrentFloorNo;
            } else {
                this.connectionFloorNo = connectionFloorNo;
            }
            this.looseConnection = looseConnection;
        }
        public void ClearUpstairEnvironmentSetting() {
            upstairEnvironment = null;
        }

        List<FloorDataFrame<DataType>> floorDataFrames = null;

        public int CurrentFloorNo { get; set; } = 0;
        FloorDataFrame<DataType> currentFloor = null;

        public ChainEnvironmentDataHolder() {
            CurrentFloorNo = 0;
            floorDataFrames = new List<FloorDataFrame<DataType>> { new FloorDataFrame<DataType>() };
            currentFloor = floorDataFrames[CurrentFloorNo];
        }

        public object GetValue(string variableName) {
            var returnValue = _GetValue(variableName);
            //イベント発火
            Ordertaker?.IgnitionGetValueEvent(typeof(DataType), variableName, returnValue);
            return returnValue;
        }
        DataType _GetValue(string variableName, bool lowerboundAccess = false, int _connectionFloorNo = 0, bool _looseConnection = false) {
            //フロアナンバーの決定
            int floorNo;
            if (!lowerboundAccess || _looseConnection) {
                floorNo = CurrentFloorNo;
            } else {
                if (CurrentFloorNo < _connectionFloorNo) throw new ChainEnvironment.MisalignedConnectionFloorNoException("指定されたコネクションフロアナンバーに該当する階層がない");
                floorNo = _connectionFloorNo;
            }

            return getValue(variableName, floorNo + 1);
        }

        DataType getValue(string variableName, int floorNo) {
            int nowFloorNo = floorNo - 1;

            if (nowFloorNo < 0) {
                //未定義の変数にアクセスしようとした
                if (upstairEnvironment == null) throw new ChainEnvironment.UndefinedVariableException("未定義の変数へアクセスしようとした");

                //上位環境での取得を試みる
                return upstairEnvironment._GetValue(variableName, true, connectionFloorNo, looseConnection);
            }
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) return floorDataFrames[nowFloorNo].Variables[variableName];

            return getValue(variableName, nowFloorNo);
        }
        public object SetValue(string variableName, object value) {
            _SetValue(variableName, (DataType)value);
            //イベント発火
            Ordertaker?.IgnitionSetValueEvent(typeof(DataType), variableName, value);

            return value;
        }
        bool _SetValue(string variableName, DataType value, bool lowerboundAccess = false, int _connectionFloorNo = 0, bool _looseConnection = false) {
            //フロアナンバーの決定
            int floorNo;
            if (!lowerboundAccess || _looseConnection) {
                floorNo = CurrentFloorNo;
            } else {
                if (CurrentFloorNo < _connectionFloorNo) throw new ChainEnvironment.MisalignedConnectionFloorNoException("指定されたコネクションフロアナンバーに該当する階層がない");
                floorNo = _connectionFloorNo;
            }

            //既存の変数に登録されたら処理を抜ける
            bool higher = setValue(variableName, value, floorNo + 1);
            if (higher) return true;

            //下階からのアクセスの場合は上位階層で登録されていなくても処理を抜ける
            if (lowerboundAccess && !higher) return false;

            //上位階層に対応する変数がない場合は今の階層に変数を新規追加する
            currentFloor.Variables.Add(variableName, value);

            return true;
        }
        bool setValue(string variableName, DataType value, int floorNo) {
            int nowFloorNo = floorNo - 1;

            //大域環境までに該当の変数がなかった場合
            if (nowFloorNo < 0) {
                //上位環境が設定されていない場合はfalseを返す
                if (upstairEnvironment == null) return false;

                //上位環境でのセットを試みる
                return upstairEnvironment._SetValue(variableName, value, true, connectionFloorNo, looseConnection);
            }

            //該当する変数がある場合は値を更新する
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) {
                floorDataFrames[nowFloorNo].Variables[variableName] = value;
                return true;
            }

            //該当する変数がない場合は上位階層に投げる
            return setValue(variableName, value, nowFloorNo);
        }
        public object CreateOrSetValue_Local(string variableName, object value) {
            _CreateOrSetValue_Local(variableName, (DataType)value);
            //イベント発火
            Ordertaker?.IgnitionSetValueEvent(typeof(DataType), variableName, value);

            return value;
        }
        DataType _CreateOrSetValue_Local(string variableName, DataType value) {
            //今の階層に変数を追加または値の更新をする
            if (!currentFloor.Variables.ContainsKey(variableName)) {
                currentFloor.Variables.Add(variableName, value);
            } else {
                currentFloor.Variables[variableName] = value;
            }

            return value;
        }
        public bool Exists(string variableName) {
            if (currentFloor.Variables.ContainsKey(variableName)) return true;

            return exists(variableName, CurrentFloorNo);
        }

        bool exists(string variableName, int floorNo) {
            int nowFloorNo = floorNo - 1;

            if (nowFloorNo < 0) return false;
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) return true;

            return exists(variableName, nowFloorNo);
        }

        public void Down(List<string> returnValues = null, List<string> arguments = null) {
            FloorDataFrame<DataType> underFloorDataFrame = new FloorDataFrame<DataType>();
            floorDataFrames.Add(underFloorDataFrame);

            underFloorDataFrame.Arguments = arguments;
            underFloorDataFrame.ReturnValues = returnValues;

            CurrentFloorNo++;
            currentFloor = underFloorDataFrame;
        }

        public void PullArguments(List<string> variables = null) {
            if (CurrentFloorNo == 0) throw new Exception("大域環境で引数引き込みを行おうとした");

            if (variables == null || variables.Count == 0) return;

            //呼び元が引数を省略したときは例外を投げる
            //引数の省略時に規定値を入れる処理を入れずに例外とします。
            if (currentFloor.Arguments == null) throw new ChainEnvironment.InvalidPullArgumentsException("無効な引数引き込みを行おうとした");


            for (int count = 0; count < variables.Count; count++) {
                SetValue(variables[count], getValue(currentFloor.Arguments[count], CurrentFloorNo));
            }
        }
        public void Up(List<string> returnValues = null) {
            if (CurrentFloorNo == 0) throw new ChainEnvironment.UpOnGlobalEnvironmentException("大域環境でアップ処理を行おうとした");

            int underFloorDataFrameNo = CurrentFloorNo;
            FloorDataFrame<DataType> underFloorDataFrame = floorDataFrames[CurrentFloorNo];
            CurrentFloorNo--;
            currentFloor = floorDataFrames[CurrentFloorNo];

            if (returnValues != null && underFloorDataFrame.ReturnValues != null) {
                for (int count = 0; count < underFloorDataFrame.ReturnValues.Count; count++) {
                    SetValue(underFloorDataFrame.ReturnValues[count], getValue(returnValues[count], underFloorDataFrameNo + 1));
                }
            }

            floorDataFrames.Remove(underFloorDataFrame);
        }


    }
}

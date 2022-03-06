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
    public interface IUpstairEnvironment
    {
        bool MultiBand { get; }
        IChainEnvironmentDataHolder GetDataHolder(string typeName);
        IChainEnvironmentDataHolder TryGetDataHolder(string typeName);
        void MultiBandDataHolderAll_Break(string typeName, Func<IChainEnvironmentDataHolder, bool> func);

    }
    public class MultiBandUpstairEnvironment: IUpstairEnvironment
    {
        ChainEnvironment targetEnvironment = null;
        ChainEnvironment TargetEnvironment {
            get => targetEnvironment;
            set {
                //一度に割り当てれるターゲット環境は一つまで
                targetEnvironment?.ClearUpstairEnvironmentSetting();

                targetEnvironment = value;
                targetEnvironment.SetUpstairEnvironment_LooseConnection(this);
            }
        }
        public MultiBandUpstairEnvironment() { }
        public MultiBandUpstairEnvironment(ChainEnvironment environment) {
            TargetEnvironment = environment;
        }
        //---
        public List<IUpstairEnvironment> UpstairEnvironments = new List<IUpstairEnvironment>();

        public bool MultiBand => true;

        public IChainEnvironmentDataHolder GetDataHolder(string typeName) {
            throw new NotImplementedException();
        }
        public IChainEnvironmentDataHolder TryGetDataHolder(string typeName) {
            return null;
        }

        public void MultiBandDataHolderAll_Break(string typeName, Func<IChainEnvironmentDataHolder, bool> func) {
            foreach (IUpstairEnvironment upstairEnvironment in UpstairEnvironments) {
                targetEnvironment.SetUpstairEnvironment_LooseConnection(upstairEnvironment);
                if (!func(targetEnvironment.GetDataHolder(typeName))) break;
            }
            //処理が終わったら自身をターゲット環境の上位環境に再セットしておく
            targetEnvironment.SetUpstairEnvironment_LooseConnection(this);
        }
    }
    public class ChainEnvironment: IUpstairEnvironment
    {
        public bool MultiBand { get => false; 
        }
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
        IUpstairEnvironment? upstairEnvironment = null;
        int connectionFloorNo = -1;
        bool looseConnection = false;
        public void SetUpstairEnvironment_LooseConnection(IUpstairEnvironment upstairEnvironment) {
            this.upstairEnvironment = upstairEnvironment;
            looseConnection = true;
            if (upstairEnvironment.MultiBand) return;

            DataHolderAll((typeName, dataHolder) => {
                dataHolder.SetUpstairEnvironment(upstairEnvironment.GetDataHolder(typeName), looseConnection, connectionFloorNo);
            });
        }
        public void SetUpstairEnvironment(IUpstairEnvironment upstairEnvironment, int connectionFloorNo) {
            this.upstairEnvironment = upstairEnvironment;
            this.connectionFloorNo = connectionFloorNo;
            looseConnection = false;
            if (upstairEnvironment.MultiBand) return;

            DataHolderAll((typeName, dataHolder) => {
                dataHolder.SetUpstairEnvironment(upstairEnvironment.GetDataHolder(typeName), looseConnection, connectionFloorNo);
            });
        }
        public void SetUpstairEnvironment_ConnectionToCurrentFloorNo(IUpstairEnvironment upstairEnvironment) {
            this.upstairEnvironment = upstairEnvironment;
            looseConnection = false;
            if (upstairEnvironment.MultiBand) return;

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

        void DataHolderAll(Action<string,IChainEnvironmentDataHolder> action) {
            foreach (KeyValuePair<string, IChainEnvironmentDataHolder> dataHolderData in dataHolders) {
                action(dataHolderData.Key, dataHolderData.Value);
            }
        }
        public IChainEnvironmentDataHolder GetDataHolder(string typeName) {
            if (!dataHolders.ContainsKey(typeName)) {
                var dataHolderType = typeof(ChainEnvironmentDataHolder<>).MakeGenericType(Type.GetType(typeName));
                IChainEnvironmentDataHolder dataHolder = Activator.CreateInstance(dataHolderType) as IChainEnvironmentDataHolder;
                dataHolder.Ordertaker = Ordertaker;
                if (upstairEnvironment != null && !upstairEnvironment.MultiBand) {
                    var upstairEnvironmentDataHolder = upstairEnvironment.TryGetDataHolder(typeName);
                    if(upstairEnvironmentDataHolder != null) dataHolder.SetUpstairEnvironment(upstairEnvironmentDataHolder, looseConnection, connectionFloorNo);
                }
                dataHolders.Add(typeName, dataHolder);
                return dataHolder;
            }

            return dataHolders[typeName];
        }
        public IChainEnvironmentDataHolder TryGetDataHolder(string typeName) {
            if (!dataHolders.ContainsKey(typeName)) return null;

            return dataHolders[typeName];
        }
        public void MultiBandDataHolderAll_Break(string typeName, Func<IChainEnvironmentDataHolder,bool> func) {
            func(GetDataHolder(typeName));
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
           return GetValue(typeof(string), variableName) as string;
        }
        public string SetValue(string variableName, string value) {
            return SetValue(typeof(string), variableName, value) as string;
        }
        public string CreateOrSetValue_Local(string variableName, string value) {
            return CreateOrSetValue_Local(typeof(string), variableName, value) as string;
        }
        public bool Exists(string variableName) {
            return Exists(typeof(string), variableName);
        }
        #endregion

        #region(object)
        public object GetValue(Type type, string variableName) {
            if (upstairEnvironment == null || !upstairEnvironment.MultiBand) return GetDataHolder(type.AssemblyQualifiedName).GetValue(false, variableName).Item1;

            object returnValue = null;
            bool get = false;
            upstairEnvironment.MultiBandDataHolderAll_Break(type.AssemblyQualifiedName ,(dataHolder) => {
                var result = dataHolder.GetValue(true, variableName);
                //値が取得できると戻り値をセットしてループを切り上げる
                if (result.Item2) {
                    returnValue = result.Item1;
                    get = true;
                    return false;
                }
                return true;
            });

            //どの上位環境でも値を取得できなかった場合
            if (!get) throw new ChainEnvironment.UndefinedVariableException("未定義の変数へアクセスしようとした"); ;

            return returnValue;
        }
        public object SetValue(Type type, string variableName, object value) {
            if (upstairEnvironment == null || !upstairEnvironment.MultiBand) return GetDataHolder(type.AssemblyQualifiedName).SetValue(false, variableName, value);

            bool set = false;
            upstairEnvironment.MultiBandDataHolderAll_Break(type.AssemblyQualifiedName, (dataHolder) => {
                //すべての上位環境の値を更新する
                if (dataHolder.SetValue(true, variableName, value)) {
                    set = true;
                }

                return true;
            });

            //もし上位環境全てに該当の変数がなかった場合はこの環境に新規追加する
            if(!set) GetDataHolder(type.AssemblyQualifiedName).CreateOrSetValue_Local(variableName, value);

            return value;
        }
        public object CreateOrSetValue_Local(Type type, string variableName, object value) {
            GetDataHolder(type.AssemblyQualifiedName).CreateOrSetValue_Local(variableName, value);
            return value;
        }
        public bool Exists(Type type, string variableName) {
            if (upstairEnvironment == null || !upstairEnvironment.MultiBand) return GetDataHolder(type.AssemblyQualifiedName).Exists(variableName);
            
            bool returnValue = false;
            upstairEnvironment.MultiBandDataHolderAll_Break(type.AssemblyQualifiedName, (dataHolder) => {
                var result = dataHolder.Exists(variableName);
                //一つでもtrueがあるとループを切り上げる
                if (result) {
                    returnValue = true;
                    return false;
                }
                return true;
            });

            return returnValue;
        }
        #endregion

        public DataType? GetValue<DataType>(string variableName) {
            return (DataType)GetValue(typeof(DataType), variableName);
        }
        public DataType SetValue<DataType>(string variableName, object value) {
            return (DataType)SetValue(typeof(DataType), variableName, value);
        }
        public DataType CreateOrSetValue_Local<DataType>(string variableName, object value) {
            return (DataType)CreateOrSetValue_Local(typeof(DataType), variableName, value);
        }
        public bool Exists<DataType>(string variableName) {
            return Exists(typeof(DataType), variableName);
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
        List<(Type, string)> dummyReturnValues = new List<(Type, string)>();
        List<(Type, string)> dummyArguments = new List<(Type, string)>();
        public void Down() {
            dummyReturnValues.Clear();
            dummyArguments.Clear();
            Down(dummyReturnValues, dummyArguments);
        }
        public void Down(List<(Type, string)> returnValues , List<(Type, string)> arguments = null) {
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
        (object, bool) GetValue(bool multiBandAccess, string variableName);
        bool SetValue(bool multiBandAccess, string variableName, object value);
        void CreateOrSetValue_Local(string variableName, object value);
        bool Exists(string variableName);
        //
        void Down(List<string> returnValues, List<string> arguments);
        void PullArguments(List<string> variables);
        void Up(List<string> returnValues);
        //
        int CurrentFloorNo { get; }

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
            currentFloorNo = sdReady.currentFloorNo;

            currentFloor = floorDataFrames[currentFloorNo];
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

        public int CurrentFloorNo { get=> currentFloorNo; }
        int currentFloorNo = 0;
        FloorDataFrame<DataType> currentFloor = null;

        public ChainEnvironmentDataHolder() {
            currentFloorNo = 0;
            floorDataFrames = new List<FloorDataFrame<DataType>> { new FloorDataFrame<DataType>() };
            currentFloor = floorDataFrames[currentFloorNo];
        }

        public (object,bool) GetValue(bool multiBandAccess, string variableName) {
            var returnValue = _GetValue(multiBandAccess, variableName);
            //イベント発火
            Ordertaker?.IgnitionGetValueEvent(typeof(DataType), variableName, returnValue.Item1);
            return returnValue;
        }
        (DataType?,bool) _GetValue(bool multiBandAccess, string variableName, bool lowerboundAccess = false, int _connectionFloorNo = 0, bool _looseConnection = false) {
            //フロアナンバーの決定
            int floorNo;
            if (!lowerboundAccess || _looseConnection) {
                floorNo = currentFloorNo;
            } else {
                if (currentFloorNo < _connectionFloorNo) throw new ChainEnvironment.MisalignedConnectionFloorNoException("指定されたコネクションフロアナンバーに該当する階層がない");
                floorNo = _connectionFloorNo;
            }

            return getValue(multiBandAccess, variableName, floorNo + 1);
        }

        (DataType?, bool) getValue(bool multiBandAccess, string variableName, int floorNo) {
            int nowFloorNo = floorNo - 1;

            if (nowFloorNo < 0) {
                //未定義の変数にアクセスしようとした
                if (upstairEnvironment == null) {
                    if (multiBandAccess) return (default(DataType), false);
                    throw new ChainEnvironment.UndefinedVariableException("未定義の変数へアクセスしようとした");
                } 

                //上位環境での取得を試みる
                return upstairEnvironment._GetValue(multiBandAccess, variableName, true, connectionFloorNo, looseConnection);
            }
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) return (floorDataFrames[nowFloorNo].Variables[variableName], true);

            return getValue(multiBandAccess, variableName, nowFloorNo);
        }
        public bool SetValue(bool multiBandAccess, string variableName, object value) {
            bool result = _SetValue(multiBandAccess, variableName, (DataType)value);
            //イベント発火
            Ordertaker?.IgnitionSetValueEvent(typeof(DataType), variableName, value);

            return result;
        }
        bool _SetValue(bool multiBandAccess, string variableName, DataType value, bool lowerboundAccess = false, int _connectionFloorNo = 0, bool _looseConnection = false) {
            //フロアナンバーの決定
            int floorNo;
            if (!lowerboundAccess || _looseConnection) {
                floorNo = currentFloorNo;
            } else {
                if (currentFloorNo < _connectionFloorNo) throw new ChainEnvironment.MisalignedConnectionFloorNoException("指定されたコネクションフロアナンバーに該当する階層がない");
                floorNo = _connectionFloorNo;
            }

            //既存の変数に登録されたら処理を抜ける
            bool higher = setValue(multiBandAccess, variableName, value, floorNo + 1);
            if (higher) return true;

            //下階からのアクセスの場合は上位階層で登録されていなくても処理を抜ける
            if (lowerboundAccess && !higher) return false;

            //マルチバンドアクセスの場合は上位階層で登録されていなくても処理を抜ける
            if (multiBandAccess) return false;

            //上位階層に対応する変数がない場合は今の階層に変数を新規追加する
            currentFloor.Variables.Add(variableName, value);

            return true;
        }
        bool setValue(bool multiBandAccess, string variableName, DataType value, int floorNo) {
            int nowFloorNo = floorNo - 1;

            //大域環境までに該当の変数がなかった場合
            if (nowFloorNo < 0) {
                //上位環境が設定されていない場合はfalseを返す
                if (upstairEnvironment == null) return false;

                //上位環境でのセットを試みる
                return upstairEnvironment._SetValue(multiBandAccess, variableName, value, true, connectionFloorNo, looseConnection);
            }

            //該当する変数がある場合は値を更新する
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) {
                floorDataFrames[nowFloorNo].Variables[variableName] = value;
                return true;
            }

            //該当する変数がない場合は上位階層に投げる
            return setValue(multiBandAccess, variableName, value, nowFloorNo);
        }
        public void CreateOrSetValue_Local(string variableName, object value) {
            _CreateOrSetValue_Local(variableName, (DataType)value);
            //イベント発火
            Ordertaker?.IgnitionSetValueEvent(typeof(DataType), variableName, value);
        }
        void _CreateOrSetValue_Local(string variableName, DataType value) {
            //今の階層に変数を追加または値の更新をする
            if (!currentFloor.Variables.ContainsKey(variableName)) {
                currentFloor.Variables.Add(variableName, value);
            } else {
                currentFloor.Variables[variableName] = value;
            }
        }
        public bool Exists(string variableName) {
            if (currentFloor.Variables.ContainsKey(variableName)) return true;

            return exists(variableName, currentFloorNo);
        }
        bool _Exists(string variableName, bool lowerboundAccess = false, int _connectionFloorNo = 0, bool _looseConnection = false) {
            //フロアナンバーの決定
            int floorNo;
            if (!lowerboundAccess || _looseConnection) {
                floorNo = currentFloorNo;
            } else {
                if (currentFloorNo < _connectionFloorNo) throw new ChainEnvironment.MisalignedConnectionFloorNoException("指定されたコネクションフロアナンバーに該当する階層がない");
                floorNo = _connectionFloorNo;
            }

            return exists(variableName, floorNo + 1);
        }
        bool exists(string variableName, int floorNo) {
            int nowFloorNo = floorNo - 1;

            //if (nowFloorNo < 0) //return false;
            if (nowFloorNo < 0) {
                //上位環境が設定されていない場合はfalseを返す
                if (upstairEnvironment == null) return false;

                //上位環境での確認を続行する
                return upstairEnvironment._Exists(variableName, true, connectionFloorNo, looseConnection);
            }
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) return true;

            return exists(variableName, nowFloorNo);
        }

        public void Down(List<string> returnValues = null, List<string> arguments = null) {
            FloorDataFrame<DataType> underFloorDataFrame = new FloorDataFrame<DataType>();
            floorDataFrames.Add(underFloorDataFrame);

            underFloorDataFrame.Arguments = arguments;
            underFloorDataFrame.ReturnValues = returnValues;

            currentFloorNo++;
            currentFloor = underFloorDataFrame;
        }

        public void PullArguments(List<string> variables = null) {
            if (currentFloorNo == 0) throw new Exception("大域環境で引数引き込みを行おうとした");

            if (variables == null || variables.Count == 0) return;

            //呼び元が引数を省略したときは例外を投げる
            //引数の省略時に規定値を入れる処理を入れずに例外とします。
            if (currentFloor.Arguments == null) throw new ChainEnvironment.InvalidPullArgumentsException("無効な引数引き込みを行おうとした");


            for (int count = 0; count < variables.Count; count++) {
                SetValue(false, variables[count], getValue(false, currentFloor.Arguments[count], currentFloorNo).Item1);
            }
        }
        public void Up(List<string> returnValues = null) {
            if (currentFloorNo == 0) throw new ChainEnvironment.UpOnGlobalEnvironmentException("大域環境でアップ処理を行おうとした");

            int underFloorDataFrameNo = currentFloorNo;
            FloorDataFrame<DataType> underFloorDataFrame = floorDataFrames[currentFloorNo];
            currentFloorNo--;
            currentFloor = floorDataFrames[currentFloorNo];

            if (returnValues != null && underFloorDataFrame.ReturnValues != null) {
                for (int count = 0; count < underFloorDataFrame.ReturnValues.Count; count++) {
                    SetValue(false, underFloorDataFrame.ReturnValues[count], getValue(false, returnValues[count], underFloorDataFrameNo + 1).Item1);
                }
            }

            floorDataFrames.Remove(underFloorDataFrame);
        }


    }
}

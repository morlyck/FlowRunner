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
        void IgnitionGetValueEvent(string variableName, string value);
        void IgnitionSetValueEvent(string variableName, string value);
    }
    public class ChainEnvironmentSdReady : NodeSdReadyCommon
    {
        public List<ChainEnvironment.FloorDataFrame> floorDataFrames = null;

        public int currentFloorNo = 0;
    }
    public class ChainEnvironment
    {
        public IChainEnvironmentOrdertaker? Ordertaker = null;
        //シリアライズ対応
        public string Serialize(FlowRunnerEngine engine) {
            ChainEnvironmentSdReady sdReady = new ChainEnvironmentSdReady();
            sdReady.floorDataFrames = floorDataFrames;
            sdReady.currentFloorNo = currentFloorNo;

            return engine.Infra.GeneralSd.Serialize(sdReady);
        }

        //デシリアライズ対応
        public void Deserialize(FlowRunnerEngine engine, string text) {
            ChainEnvironmentSdReady sdReady = engine.Infra.GeneralSd.Deserialize<ChainEnvironmentSdReady>(text);

            floorDataFrames = sdReady.floorDataFrames;
            currentFloorNo = sdReady.currentFloorNo;

            currentFloor = floorDataFrames[currentFloorNo];
        }

        //---
        //上位環境
        ChainEnvironment? upstairEnvironment = null;
        int connectionFloorNo = 0;
        bool looseConnection = false;
        public void SetUpstairEnvironment_LooseConnection(ChainEnvironment upstairEnvironment) {
            this.upstairEnvironment = upstairEnvironment;
            looseConnection = true;
        }
        public void SetUpstairEnvironment(ChainEnvironment upstairEnvironment, int connectionFloorNo) {
            this.upstairEnvironment = upstairEnvironment;
            this.connectionFloorNo = connectionFloorNo;
            looseConnection = false;
        }
        public void SetUpstairEnvironment_ConnectionToCurrentFloorNo(ChainEnvironment upstairEnvironment) {
            this.upstairEnvironment = upstairEnvironment;
            this.connectionFloorNo = upstairEnvironment.currentFloorNo;
            looseConnection = false;
        }
        public void ClearUpstairEnvironmentSetting() {
            upstairEnvironment = null;
        }

        List<FloorDataFrame> floorDataFrames = null;

        int currentFloorNo = 0;
        FloorDataFrame currentFloor = null;

        public ChainEnvironment() {
            currentFloorNo = 0;
            floorDataFrames = new List<FloorDataFrame> { new FloorDataFrame() };
            currentFloor = floorDataFrames[currentFloorNo];
        }

        public string GetValue(string variableName) {
            var returnValue = _GetValue(variableName);
            //イベント発火
            Ordertaker?.IgnitionGetValueEvent(variableName, returnValue);
            return returnValue;
        }
        string _GetValue(string variableName, bool lowerboundAccess = false, int _connectionFloorNo = 0, bool _looseConnection = false) {
            //フロアナンバーの決定
            int floorNo;
            if (!lowerboundAccess || _looseConnection) {
                floorNo = currentFloorNo;
            } else {
                if (currentFloorNo < _connectionFloorNo) throw new MisalignedConnectionFloorNoException("指定されたコネクションフロアナンバーに該当する階層がない");
                floorNo = _connectionFloorNo;
            }

            return getValue(variableName, floorNo + 1);
        }

        string getValue(string variableName, int floorNo) {
            int nowFloorNo = floorNo - 1;

            if(nowFloorNo < 0) {
                //未定義の変数にアクセスしようとした
                if(upstairEnvironment == null) throw new UndefinedVariableException("未定義の変数へアクセスしようとした");

                //上位環境での取得を試みる
                return upstairEnvironment._GetValue(variableName, true, connectionFloorNo, looseConnection);
            }
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) return floorDataFrames[nowFloorNo].Variables[variableName];

            return getValue(variableName, nowFloorNo);
        }
        public string SetValue(string variableName, string value) {
            _SetValue(variableName,value);
            //イベント発火
            Ordertaker?.IgnitionSetValueEvent(variableName, value);

            return value;
        }
        bool _SetValue(string variableName, string value, bool lowerboundAccess = false, int _connectionFloorNo = 0, bool _looseConnection = false) {
            //フロアナンバーの決定
            int floorNo;
            if (!lowerboundAccess || _looseConnection) {
                floorNo = currentFloorNo;
            } else {
                if (currentFloorNo < _connectionFloorNo) throw new MisalignedConnectionFloorNoException("指定されたコネクションフロアナンバーに該当する階層がない");
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
        bool setValue(string variableName, string value, int floorNo) {
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
        public string CreateOrSetValue_Local(string variableName, string value) {
            _CreateOrSetValue_Local(variableName, value);
            //イベント発火
            Ordertaker?.IgnitionSetValueEvent(variableName, value);

            return value;
        }
        string _CreateOrSetValue_Local(string variableName, string value) {
            //今の階層に変数を追加または値の更新をする
            if (!currentFloor.Variables.ContainsKey(variableName)) {
                currentFloor.Variables.Add(variableName, value);
            } else {
                currentFloor.Variables[variableName] = value;
            }

            return value;
        }
        public bool ExistsValue(string variableName) {
            if (currentFloor.Variables.ContainsKey(variableName)) return true;

            return existsValue(variableName, currentFloorNo);
        }

        bool existsValue(string variableName, int floorNo) {
            int nowFloorNo = floorNo - 1;

            if (nowFloorNo < 0) return false;
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) return true;

            return existsValue(variableName, nowFloorNo);
        }

        public void Down(List<string> returnValues = null, List<string> arguments = null) {
            FloorDataFrame underFloorDataFrame = new FloorDataFrame();
            floorDataFrames.Add(underFloorDataFrame);

            underFloorDataFrame.Arguments = arguments;
            underFloorDataFrame.ReturnValues = returnValues;

            currentFloorNo++;
            currentFloor = underFloorDataFrame;
        }

        public void PullArguments(List<string> variables = null) {
            if (currentFloorNo == 0) throw new Exception("大域環境で引数引き込みを行おうとした");

            if (variables == null|| variables.Count == 0) return;

            //呼び元が引数を省略したときは例外を投げる
            //引数の省略時に規定値を入れる処理を入れずに例外とします。
            if (currentFloor.Arguments == null) throw new InvalidPullArgumentsException("無効な引数引き込みを行おうとした");


            for (int count = 0; count < variables.Count; count++) {
                SetValue(variables[count], getValue(currentFloor.Arguments[count], currentFloorNo));
            }
        }
        public void Up(List<string> returnValues = null) {
            if (currentFloorNo == 0) throw new UpOnGlobalEnvironmentException("大域環境でアップ処理を行おうとした");

            int underFloorDataFrameNo = currentFloorNo;
            FloorDataFrame underFloorDataFrame = floorDataFrames[currentFloorNo];
            currentFloorNo--;
            currentFloor = floorDataFrames[currentFloorNo];

            if(returnValues != null && underFloorDataFrame.ReturnValues != null) {
                for (int count = 0; count < underFloorDataFrame.ReturnValues.Count; count++) {
                    SetValue(underFloorDataFrame.ReturnValues[count], getValue(returnValues[count], underFloorDataFrameNo + 1));
                }
            }

            floorDataFrames.Remove(underFloorDataFrame);
        }


        //---

        public class FloorDataFrame
        {
            public Dictionary<string, string> Variables = new Dictionary<string, string>();

            public List<string> Arguments = new List<string>();
            public List<string> ReturnValues = new List<string>();

            //public Dictionary<string, List<string>> Lists = new Dictionary<string, List<string>>();
        }

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
        public class UpOnGlobalEnvironmentException : Exception { 
            public UpOnGlobalEnvironmentException(string text) :base(text){ }
        }
        //指定されたコネクションフロアナンバーに該当する階層がない場合
        public class MisalignedConnectionFloorNoException : Exception { 
            public MisalignedConnectionFloorNoException(string text) :base(text){ }
        }

    }
}

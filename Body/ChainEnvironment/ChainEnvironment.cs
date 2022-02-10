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
    public class ChainEnvironmentSdReady : NodeSdReadyCommon
    {
        public List<ChainEnvironment.FloorDataFrame> floorDataFrames = null;

        public int currentFloorNo = 0;
    }
    public class ChainEnvironment
    {
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
        List<FloorDataFrame> floorDataFrames = null;

        int currentFloorNo = 0;
        FloorDataFrame currentFloor = null;

        public ChainEnvironment() {
            currentFloorNo = 0;
            floorDataFrames = new List<FloorDataFrame> { new FloorDataFrame() };
            currentFloor = floorDataFrames[currentFloorNo];
        }

        public string GetValue(string variableName) {
            if (currentFloor.Variables.ContainsKey(variableName)) return currentFloor.Variables[variableName];

            return getValue(variableName, currentFloorNo);
        }

        string getValue(string variableName, int floorNo) {
            int nowFloorNo = floorNo - 1;

            if(nowFloorNo < 0) throw new UndefinedVariableException("未定義の変数へアクセスしようとした");
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) return floorDataFrames[nowFloorNo].Variables[variableName];

            return getValue(variableName, nowFloorNo);
        }
        public string SetValue(string variableName, string value) {
            //今の階層にすでに変数がある場合は値を更新する
            if (currentFloor.Variables.ContainsKey(variableName)) {
                currentFloor.Variables[variableName] = value;
                return value;
            }

            //上位階層で登録されたら処理を抜ける
            if(setValue(variableName, value, currentFloorNo)) return value;

            //上位階層に対応する変数がない場合は今の階層に変数を新規追加する
            currentFloor.Variables.Add(variableName, value);

            return value;
        }
        bool setValue(string variableName, string value, int floorNo) {
            int nowFloorNo = floorNo - 1;

            //大域環境までに該当の変数がなかったらfalseを返す
            if (nowFloorNo < 0) return false;

            //該当する変数がある場合は値を更新する
            if (floorDataFrames[nowFloorNo].Variables.ContainsKey(variableName)) {
                floorDataFrames[nowFloorNo].Variables[variableName] = value;
                return true;
            }

            //該当する変数がない場合は上位階層に投げる
            return setValue(variableName, value, nowFloorNo);
        }
        public string CreateOrSetValue_Local(string variableName, string value) {
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

        public void Down(List<string> returnValues, List<string> arguments) {
            FloorDataFrame underFloorDataFrame = new FloorDataFrame();
            floorDataFrames.Add(underFloorDataFrame);

            underFloorDataFrame.Arguments = arguments;
            underFloorDataFrame.ReturnValues = returnValues;

            currentFloorNo++;
            currentFloor = underFloorDataFrame;
        }

        public void PullArguments(List<string> variables) {
            if (currentFloorNo == 0) throw new Exception("大域環境で引数引き込みを行おうとした");

            for (int count = 0; count < variables.Count; count++) {
                SetValue(variables[count], getValue(currentFloor.Arguments[count], currentFloorNo));
            }
        }
        public void Up(List<string> returnValues) {
            if (currentFloorNo == 0) throw new UpOnGlobalEnvironmentException("大域環境でアップ処理を行おうとした");

            int underFloorDataFrameNo = currentFloorNo;
            FloorDataFrame underFloorDataFrame = floorDataFrames[currentFloorNo];
            currentFloorNo--;
            currentFloor = floorDataFrames[currentFloorNo];

            for(int count = 0; count < underFloorDataFrame.ReturnValues.Count; count++) {
                SetValue(underFloorDataFrame.ReturnValues[count], getValue(returnValues[count], underFloorDataFrameNo + 1));
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
        public class PullArgumentsOnGlobalEnvironmentException : Exception { 
            public PullArgumentsOnGlobalEnvironmentException(string text) :base(text){ }
        }
        //大域環境でアップ処理を行おうとした場合
        public class UpOnGlobalEnvironmentException : Exception { 
            public UpOnGlobalEnvironmentException(string text) :base(text){ }
        }

    }
}

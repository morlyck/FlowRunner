﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace FlowRunner.Engine.Cycle
{
    public partial class SnapshotFrame
    {

        //
        public List<string> NodeTypeNames = null;
        public List<string> NodePaths = null;
        public List<string> NodeSerializeTexts = null;

        //
        public string EngineNodeSerializeText = null;
        public List<string> UpdateEvacuationNode_TypeNames = null;
        public List<string> UpdateEvacuationNode_Paths = null;
        public List<string> UpdateEvacuationNode_SerializeTexts = null;

        //---

        [SuspendCycleTime]
        void SuspendCycleTime_byNode(FlowRunnerEngine engine) {
            NodePaths = new List<string>();
            NodeSerializeTexts = new List<string>();
            NodeTypeNames = new List<string>();
            engine.Node.NodeAll((path, node) => {

                //エンジンノード
                if(path == "/") {
                    EngineNodeSerializeText = node.Serialize();

                    //アップデートタイム時に退避していたノードのシリアライズ化
                    UpdateEvacuationNode_TypeNames = new List<string>();
                    UpdateEvacuationNode_Paths = new List<string>();
                    UpdateEvacuationNode_SerializeTexts = new List<string>();
                    foreach (KeyValuePair<string,INode> keyValuePair in (node as IFlowRunnerEngineNode).UpdateEvacuationNodes) {
                        UpdateEvacuationNode_TypeNames.Add(keyValuePair.Value.GetType().AssemblyQualifiedName);
                        UpdateEvacuationNode_Paths.Add(keyValuePair.Key);
                        UpdateEvacuationNode_SerializeTexts.Add(keyValuePair.Value.Serialize());
                    }
                    return;
                }

                //Listに格納
                NodeTypeNames.Add(node.GetType().AssemblyQualifiedName);
                NodePaths.Add(path);
                NodeSerializeTexts.Add(node.Serialize());
            });
        }

        [ResumeCycleTime]
        public void ResumeCycleTime_byNode(FlowRunnerEngine engine) {
            if (NodePaths == null) return;

            //エンジンノードのインスタンスを作成して登録する
            FlowRunnerEngineNode engineNode = new FlowRunnerEngineNode(engine);
            engineNode.Deserialize(engine, EngineNodeSerializeText);
            (engine as IFlowRunnerEngineInside).ReplaceNode(engineNode);

            //各ノードインスタンスを生成してエンジンノードに登録する
            for(int index = 0; index < NodePaths.Count; index++) {
                CreateInstanceAndSet_byNode(engine, NodeTypeNames[index], NodePaths[index]);
                /*
                string path = NodePaths[index];

                //Typeを指定してインスタンス作成
                INode node = (INode)Activator.CreateInstance(Type.GetType(NodeTypeNames[index]), new object[] { null , "" });

                engine.SetNode(path, node);
                */
            }

            //アップデートタイム時に退避していたノードもこのタイミングで登録する
            for(int index  = 0; index < UpdateEvacuationNode_Paths.Count; index++) {
                CreateInstanceAndSet_byNode(engine, UpdateEvacuationNode_TypeNames[index], UpdateEvacuationNode_Paths[index]);
            }

            //各ノードのルートノードを再登録する
            for (int index = 0; index < NodePaths.Count; index++) {
                Deserialize_byNode(engine, NodePaths[index], NodeSerializeTexts[index]);
                /*
                string path = NodePaths[index];

                INode node = engine.GetNode(path);

                node.Deserialize(engine, NodeSerializeTexts[index]);
                */
            }
            //アップデートタイム時に退避していたノード
            for (int index = 0; index < UpdateEvacuationNode_Paths.Count; index++) {
                Deserialize_byNode(engine, UpdateEvacuationNode_Paths[index], UpdateEvacuationNode_SerializeTexts[index]);
            }

        }

        void CreateInstanceAndSet_byNode(FlowRunnerEngine engine, string typeName, string path) {
            //Typeを指定してインスタンス作成
            INode node = (INode)Activator.CreateInstance(Type.GetType(typeName), new object[] { null, "" });

            engine.SetNode(path, node);
        }
        void Deserialize_byNode(FlowRunnerEngine engine, string path, string serializeText) {
            INode node = engine.GetNode(path);

            node.Deserialize(engine, serializeText);
        }

    }


}

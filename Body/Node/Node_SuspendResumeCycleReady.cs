using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace FlowRunner.Engine.Cycle
{
    public partial class SnapshotFrame
    {
        [SuspendCycleTime]
        void SuspendCycleTime_byNode(FlowRunnerEngine engine) {
            NodePaths = new List<string>();
            NodeSerializeTexts = new List<string>();
            NodeTypeNames = new List<string>();
            engine.Node.NodeAll((path, node) => {

                //エンジンノード
                if(path == "/") {
                    EngineNodeSerializeText = node.Serialize();
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
                string path = NodePaths[index];

                //Typeを指定してインスタンス作成
                INode node = (INode)Activator.CreateInstance(Type.GetType(NodeTypeNames[index]), new object[] { null , "" });

                engine.SetNode(path, node);
            }

            //各ノードのルートノードを再登録する
            for (int index = 0; index < NodePaths.Count; index++) {
                string path = NodePaths[index];

                INode node = engine.GetNode(path);

                node.Deserialize(engine, NodeSerializeTexts[index]);

            }
        }

        //
        public List<string> NodeTypeNames = null;
        public List<string> NodePaths = null;
        public List<string> NodeSerializeTexts = null;

        //
        public string EngineNodeSerializeText = null;
    }
}

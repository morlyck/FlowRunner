using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {
        public FlowRunnerEngine() {
            Initialization_Engine();
        }
        
        void Initialization_Engine() {
            //エンジンノードの追加
            Node = new Engine.FlowRunnerEngineNode(this);

            //サイクルの初期化
            Initialization_FlowRunnerCycle();

            //サービスの初期化
            Initialization_FlowRunnerService();



        }

    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowRunner
{
    public partial class FlowRunnerEngine
    {
        /*
        //予め決められた回数実行する


        //予め決められた時間だけ実行する


        //停止するまで回し続ける
        public void Update() {

        }
        */

        public FlowRunnerEngine() {
            Initialization_Engine();
        }
        
        void Initialization_Engine() {
            //サイクルの初期化
            Initialization_FlowRunnerCycle();

            //サービスの初期化
            Initialization_FlowRunnerService();



        }

    }
}
namespace FlowRunner.Engine
{





    public class LabelRunnerBody
    {
        public string SnapShot() {
            return null;
        }

        public void Restore(string snapShotText) {

        }
    }



}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using FlowRunner;
using FlowRunner.Engine;
using FlowRunner.Another;


namespace FlowRunner.Engine
{
    public class DirectoryNode : CustomNode
    {
        public DirectoryNode(INode root, string path) : base(root, path) { }

        public override INode GetNode(string path) {
            return Root.GetNode(GetAbsolutePath(path));
        }
        public override void SetNode(INode node) {
            Root.SetNode(GetAbsolutePath(node.Path), node);
        }
        public override void SetNode(string path, INode node) {
            Root.SetNode(GetAbsolutePath(node.Path), node);
        }
        public override INode NodeOperationRelay(string path) {
            if (path == Path) path = $"{EntryPath}/@";

            return Root.NodeOperationRelay(GetAbsolutePath(path));
        }

        public string EntryPath = "";
        public string GetAbsolutePath(string relativePath) {
            if (relativePath == "/") return Path;

            return $"{Path}@{relativePath}";
        }

        //
        protected override (bool, CustomNodeSdReady) Localize_Serialize() {
            SdReady sdReady = new SdReady();

            sdReady.EntryPath = EntryPath;
            return (true, sdReady);
        }

        protected override (bool, CustomNodeSdReady) Localize_Deserialize(FlowRunnerEngine engine, string text) {
            SdReady sdReady = engine.Infra.GeneralSd.Deserialize<SdReady>(text);

            EntryPath = sdReady.EntryPath;
            return (true, sdReady);
        }
        //
        public class SdReady : CustomNodeSdReady
        {
            public string EntryPath = "";
        }
    }
}

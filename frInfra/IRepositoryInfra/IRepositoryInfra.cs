using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FlowRunner.Engine
{
    public interface IRepositoryInfra
    {
        void SetSnapshot(string snapshotCode, string text);
        string GetSnapshot(string snapshotCode);
    }
}

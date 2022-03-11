using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowRunner.Engine
{
    public interface IGeneralSerializeDeserializeInfra : CommonElement.SerializeDeserialize.ISerializerAndDeserializer
    {
        string Serialize(object obj);
        typ Deserialize<typ>(string jsonText);
        object Deserialize(Type typ, string jsonText);
    }
}

namespace FlowRunner.Engine.Infra
{
    /*
    public class GeneralSerializeDeserializeInfra : IGeneralSerializeDeserializeInfra
    {
        public string Serialize(object obj) {
            throw new NotImplementedException();
        }
        public typ Deserialize<typ>(string jsonText) {
            throw new NotImplementedException();
        }
    }
    */
}

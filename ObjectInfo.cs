using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SharpQuant.ObjectStore
{
    public interface IObjectInfo
    {
        string Catalogue { get; }
        string ID { get; }
        DateTime Version { get; }
        string Type { get; }
        IDictionary<string, string> Tags { get; }

        byte[] Data { get; }
    }

    [DataContract, Serializable]
    public class ObjectInfo : IObjectInfo
    {

        [DataMember(Order = 1, Name = "Catalogue")]
        public string Catalogue { get; set; }
        [DataMember(Order = 2, Name = "ID")]
        public string ID { get; set; }
        [DataMember(Order = 3, Name = "Version")]
        public DateTime Version { get; set; }
        [DataMember(Order = 4, Name = "Type")]
        public string Type { get; set; }
        [DataMember(Order = 5, Name = "Tags")]
        public IDictionary<string, string> Tags { get; set; }
        [DataMember(Order = 6, Name = "Data")]
        public byte[] Data { get; set; }


        public static ObjectInfo Create(IObjectInfo info)
        {
            return new ObjectInfo()
            {
                Catalogue = info.Catalogue,
                ID = info.ID,
                Version = info.Version,
                Type = info.Type,
                Tags = new Dictionary<string, string>(info.Tags),
                Data = info.Data,
            };
        }

    }


}

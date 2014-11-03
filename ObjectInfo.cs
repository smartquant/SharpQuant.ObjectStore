using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpQuant.ObjectStore
{
    public interface IObjectInfo<T>
    {
        string Entity { get; }
        string ID { get; }
        DateTime Version { get; }
        IDictionary<string, string> Tags { get; }

        T Instance { get; }
    }

    public class ObjectInfo<T> : IObjectInfo<T>
    {
        public string Entity { get; set; }
        public string ID { get; set; }
        public DateTime Version { get; set; }
        public IDictionary<string, string> Tags { get; set; }

        public T Instance { get; set; }
    }
}

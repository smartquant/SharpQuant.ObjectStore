using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpQuant.ObjectStore
{
    public interface IObjectSerializer
    {
        T Deserialize<T>(byte[] data);
        byte[] Serialize<T>(T obj);
    }
}

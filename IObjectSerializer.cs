using System;
using System.IO;


namespace SharpQuant.ObjectStore
{
    public interface IObjectSerializer
    {
        T Deserialize<T>(Stream stream);
        void Serialize<T>(Stream stream, T obj);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

//Simple exammple serializer: commented out so no reference to external serializers in the base assembly required

//using ProtoBuf;

//namespace SharpQuant.ObjectStore.Serializers
//{
//    public class ProtoObjectSerializer : IObjectSerializer
//    {
//        public T Deserialize<T>(byte[] data)
//        {
//            using (var stream = new MemoryStream(data))
//            {
//                return Serializer.Deserialize<T>(stream);
//            }           
//        }

//        public byte[] Serialize<T>(T obj)
//        {
//            using (var stream = new MemoryStream())
//            {
//                Serializer.Serialize<T>(stream, obj);
//                return stream.ToArray();
//            }
//        }
//    }

//}

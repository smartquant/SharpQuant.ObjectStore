using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpQuant.ObjectStore
{

    public static class ObjectInfoEx
    {
        /// <summary>
        /// Convert DTO to type T ObjectInfo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static IObjectInfo<T> ToObjectInfoT<T>(this IObjectInfo info, IObjectSerializer serializer)
        {
            var obj = new ObjectInfo<T>()
            {
                Catalogue = info.Catalogue,
                ID = info.ID,
                Type = info.Type,
                Tags = info.Tags,
                Version = info.Version,
            };
            using (var stream = new MemoryStream(info.Data, 0, info.DataSize))
                obj.Instance = serializer.Deserialize<T>(stream);
            return obj;
        }
        /// <summary>
        /// Convert type T ObjectInfo to DTO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static IObjectInfo ToObjectInfo<T>(this IObjectInfo<T> info, IObjectSerializer serializer)
        {
            var obj = new ObjectInfo()
            {
                Catalogue = info.Catalogue,
                ID = info.ID,
                Type = info.Type,
                Tags = info.Tags,
                Version = info.Version,
            };
            using (var stream = new MemoryStream())
            {
                serializer.Serialize<T>(stream, info.Instance);
                obj.Data = stream.ToArray();
                obj.DataSize = obj.Data.Length;
            }
            return obj;
        }
    }
}

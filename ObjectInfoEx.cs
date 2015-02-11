using System;
using System.Collections.Generic;
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
            return new ObjectInfo<T>()
            {
                Catalogue = info.Catalogue,
                ID = info.ID,
                Type = info.Type,
                Tags = info.Tags,
                Version = info.Version,
                Instance = serializer.Deserialize<T>(info.Data),
            };
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
            return new ObjectInfo()
            {
                Catalogue = info.Catalogue,
                ID = info.ID,
                Type = info.Type,
                Tags = info.Tags,
                Version = info.Version,
                Data = serializer.Serialize<T>(info.Instance),
            };
        }
    }
}

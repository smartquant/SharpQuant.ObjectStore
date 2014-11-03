using System;
using System.Collections.Generic;
using System.Data;

namespace SharpQuant.ObjectStore
{
    interface IObjectStore
    {

        // Creates a new entity (think table in a DB)
        bool CreateEntity(string entity, bool dropifexists = false);

        // Deletes a specific versions of an object! Returns how many records where deleted.
        int DeleteObject(string entity, string ID);
        // Deletes a specific versions of an object! Returns how many records where deleted.
        int DeleteObject(string entity, string ID, DateTime version);

        //check whether a particular version exists
        bool Exist(string entity, string DBID, DateTime version);
        //last version of an ID
        DateTime LastVersion(string entity, string ID);
        //last version before 'version'
        DateTime LastVersion(string entity, string ID, DateTime version);

        //Create or update a serialized object
        //tags will be overwritten if updateinfo = true
        bool CreateOrUpdate<T>(IObjectInfo<T> info, bool updateInfo = true);
        //get last version
        IObjectInfo<T> GetObject<T>(string entity, string ID);
        //get last version before 'version'
        IObjectInfo<T> GetObject<T>(string entity, string ID, DateTime version);

        //Gets all infos without deserializing the objects of the entire entity
        IList<IObjectInfo<object>> GetAllInfos(string entity);
        //Gets all infos without deserializing the objects for an ID
        IList<IObjectInfo<object>> GetAllInfos(string entity, string ID);

        //Transaction support
        IDbTransaction BeginTransaction(IsolationLevel il = IsolationLevel.Unspecified);
    }
}

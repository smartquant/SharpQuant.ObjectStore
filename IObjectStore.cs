using System;
using System.Collections.Generic;
using System.Data;

namespace SharpQuant.ObjectStore
{
    public interface IObjectStore
    {
        //Access to the serializer
        IObjectSerializer Serializer { get; }

		// Gets a single catalogue
        ICatalogue GetCatalogue(string code);

        // Creates a new catalogue (think table in a DB)
        bool CreateCatalogue(ICatalogue catalogue, bool dropifexists = false);
        // Careful! Deletes entire catalogue! 
        bool DeleteCatalogue(string code);
        // Updates catalogue information
        bool UpdateCatalogue(ICatalogue catalogue);
        //gets all the catalogues
        IList<ICatalogue> GetCatalogues();

		
        // Deletes all versions of an object! Returns how many records where deleted.
        int DeleteObject(string catalogue, string ID);
        // Deletes a specific version of an object! Returns how many records where deleted.
        int DeleteObject(string catalogue, string ID, DateTime version);

        //check whether a particular version exists
        bool Exist(string catalogue, string DBID, DateTime version);
        //last version of an ID
        DateTime LastVersion(string catalogue, string ID);
        //last version before 'version'
        DateTime LastVersion(string catalogue, string ID, DateTime version);

        //Create or update a serialized object
        //tags will be overwritten if updateinfo = true
        bool CreateOrUpdate<T>(IObjectInfo<T> info, bool updateInfo = true);
        bool CreateOrUpdate(IObjectInfo info, bool updateInfo = true);
        //get last version
        IObjectInfo<T> GetObject<T>(string catalogue, string ID);
        //get last version before 'version'
        IObjectInfo<T> GetObject<T>(string catalogue, string ID, DateTime version);

        //get last version
        IObjectInfo GetObjectInfo(string catalogue, string ID);
        //get last version before 'version'
        IObjectInfo GetObjectInfo(string catalogue, string ID, DateTime version);

        //Gets all infos without deserializing the objects of the entire catalogue
        IList<IObjectInfo> GetAllInfos(string catalogue, string type = "%", bool read_data = false);
        //Gets all infos without deserializing the objects for an ID
        IList<IObjectInfo> GetAllInfosForID(string catalogue, string ID, bool read_data = false);

        //Transaction support
        IDbTransaction BeginTransaction(IsolationLevel il = IsolationLevel.Unspecified);
    }
}

using System;
using System.Collections.Generic;
using System.Data;

namespace SharpQuant.ObjectStore
{
    interface IObjectStore
    {
		// Gets a single catalogue
        ICatalogue GetCatalogue(string code);

        // Creates a new catalogue (think table in a DB)
        bool CreateCatalogue(Catalogue catalogue, bool dropifexists = false);
        // Careful! Deletes entire catalogue! 
        bool DeleteCatalogue(string code);
        // Updates catalogue information
        bool UpdateCatalogue(Catalogue catalogue);
        //gets all the catalogues
        IList<ICatalogue> GetCatalogues();

		
        // Deletes a specific versions of an object! Returns how many records where deleted.
        int DeleteObject(string catalogue, string ID);
        // Deletes a specific versions of an object! Returns how many records where deleted.
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
        //get last version
        IObjectInfo<T> GetObject<T>(string catalogue, string ID);
        //get last version before 'version'
        IObjectInfo<T> GetObject<T>(string catalogue, string ID, DateTime version);

        //Gets all infos without deserializing the objects of the entire catalogue
        IList<IObjectInfo<object>> GetAllInfos(string catalogue);
        //Gets all infos without deserializing the objects for an ID
        IList<IObjectInfo<object>> GetAllInfos(string catalogue, string ID);

        //Transaction support
        IDbTransaction BeginTransaction(IsolationLevel il = IsolationLevel.Unspecified);
    }
}

using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace SharpQuant.ObjectStore
{
    /// <summary>
    /// Objectstore with versioning based on a ADO DB
    /// This is an example implementation based on SQLite
    /// Remark: For better performance one would typically use stored procedures in DBs which support that
    /// </summary>
    public class DbObjectStore : IObjectStore, IDisposable
    {
        protected IDbConnection _conn;
        protected IObjectSerializer _serializer;
        protected ITagsSerializer _tagsSerializer;

        #region SQL

        //SQLite SQL
        //since table is passed as a string this might lead to SQL injection 
        //so table name should be checked on special characters

        protected static string DDL_CATALOGUE =
            @"DROP TABLE IF EXISTS [tblCatalogue];
                CREATE TABLE [tblCatalogue] (
                ID          INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CODE        TEXT NOT NULL,
                Name        TEXT,
                Description TEXT,
                Info        TEXT
            );
            CREATE UNIQUE INDEX [idxCatalogue] on [tblCatalogue] (CODE ASC);
            ";

        protected static string READ_CATALOGUES = "SELECT CODE,Name,Description,Info FROM tblCatalogue";
        protected static string READ_CATALOGUE = "SELECT CODE,Name,Description,Info FROM tblCatalogue WHERE CODE=?";
        protected static string INSERT_CATALOGUE = "INSERT INTO tblCatalogue(CODE,Name,Description,Info) VALUES(?,?,?,?)";
        protected static string UPDATE_CATALOGUE = "UPDATE tblCatalogue SET(Name=?,Description=?,Info=?) WHERE CODE=?";
        protected static string DELETE_CATALOGUE = "DELETE FROM tblCatalogue WHERE CODE=?";
        protected static string DROP_CATALOGUE = "DROP TABLE IF EXISTS [{0}]";


        protected static string DDL =
            @"DROP TABLE IF EXISTS [{0}];
            CREATE TABLE [{0}] (
                ID          INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Version     DateTime NOT NULL,
                CODE        TEXT NOT NULL,
                Info        TEXT,
                Data        BLOB
            );
            CREATE UNIQUE INDEX [idx{0}] on [{0}] (CODE ASC,Version ASC);
            ";
        protected static string READ_VERSION = "SELECT version,code,info,data FROM {0} WHERE (CODE=? AND version=?) ORDER BY version";
        protected static string READ_ALL_INFO_CODES = "SELECT version,code,info FROM {0} WHERE (CODE=?) ORDER BY version";
        protected static string READ_ALL_INFO_ENTITY = "SELECT version,code,info FROM {0} ORDER BY version";

        protected static string EXIST_VERSION = "SELECT max(version) FROM {0} WHERE (CODE=? AND version=?)";
        protected static string EXIST_VERSION2 = "SELECT max(version) FROM {0} WHERE (CODE=? AND version<=?)";
        protected static string EXIST_LAST = "SELECT max(version) FROM {0} WHERE CODE=?";

        protected static string DELETE_ALL_VERSIONS = "DELETE FROM {0} WHERE CODE=?";
        protected static string DELETE_VERSION = "DELETE FROM {0} WHERE (CODE=? AND version=?)";

        protected static string UPDATE_VERSION = "UPDATE {0} SET info=?,data=?  WHERE (CODE=? AND version=?)";
        protected static string UPDATE_VERSION_NOINFO = "UPDATE {0} SET data=?  WHERE (CODE=? AND version=?)";

        protected static string INSERT = "INSERT INTO {0}(version,code,info,data) VALUES(?,?,?,?)";

        #endregion


        #region constructor

        public DbObjectStore(Func<IDbConnection> connect, IObjectSerializer objectSerializer, ITagsSerializer infoSerializer)
        {
            _conn = connect();
            _serializer = objectSerializer;
            _tagsSerializer = infoSerializer;

        }

        #endregion


        public virtual IDbTransaction BeginTransaction(IsolationLevel il=IsolationLevel.Unspecified)
        {
            return _conn.BeginTransaction(il);
        }


        /// <summary>
        /// Quickly check whether a particular version exists
        /// </summary>
        /// <param name="catalogue"></param>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public virtual bool Exist(string catalogue, string ID, DateTime version)
        {

            //returns last version date of the object or min_date
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(EXIST_VERSION, catalogue);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;
                command.Parameters[1].Value = version;

                object retval = command.ExecuteScalar();

                //Note: SQLite Max() does not return a DateTime data type, but a string
                return (retval.GetType() != typeof(System.DBNull));
            }

        }

        /// <summary>
        /// Just query the date of the lates verions
        /// </summary>
        /// <param name="catalogue"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public virtual DateTime LastVersion(string catalogue, string ID)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(EXIST_LAST, catalogue);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;

                object retval = command.ExecuteScalar();
                return (retval.GetType() == typeof(System.DBNull)) ? DateTime.MinValue : DateTime.Parse(retval.ToString());
            }
        }

        /// <summary>
        /// Get the time stamp of the version before or equal 'version'
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="catalogue"></param>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public virtual DateTime LastVersion(string catalogue, string ID, DateTime version)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(EXIST_VERSION2, catalogue);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;
                command.Parameters[1].Value = version;
                object retval = command.ExecuteScalar();
                return (retval.GetType() == typeof(System.DBNull)) ? DateTime.MinValue : DateTime.Parse(retval.ToString());
            }
        }

        /// <summary>
        /// Get all infos from an catalogue
        /// Object instancies will not be deserialized
        /// </summary>
        /// <param name="catalogue"></param>
        /// <returns></returns>
        public virtual IList<IObjectInfo<object>> GetAllInfos(string catalogue)
        {
            var list = new List<IObjectInfo<object>>();

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(READ_ALL_INFO_ENTITY, catalogue);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var info = new ObjectInfo<object>()
                        {
                            ID = (string)reader[1],
                            Version = (DateTime)reader[0],
                            Tags = _tagsSerializer.Deserialize((string)reader[2])
                        };
                        list.Add(info);
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// Get all infos(versions) for an ID
        /// Object instancies will not be deserialized
        /// </summary>
        /// <param name="catalogue"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public virtual IList<IObjectInfo<object>> GetAllInfos(string catalogue, string ID)
        {
            var list = new List<IObjectInfo<object>>();

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(READ_ALL_INFO_CODES, catalogue);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var info = new ObjectInfo<object>()
                        {
                            ID = (string)reader[1],
                            Version = (DateTime)reader[0],
                            Tags = _tagsSerializer.Deserialize((string)reader[2])
                        };
                        list.Add(info);
                    }
                }
                return list;
            }
        }

        #region CRUD catalogues

        /// <summary>
        /// Check whether an catalogue is present
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public virtual ICatalogue GetCatalogue(string code)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = READ_CATALOGUE;
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = code;
     
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Catalogue()
                        {
                            CODE = (string)reader[0],
                            Name = (string)reader[1],
                            Description = (string)reader[2],
                            Tags = _tagsSerializer.Deserialize((string)reader[3]),
                        };
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Creates a table in the DB
        /// CAUTION: drops existing table!
        /// </summary>
        /// <param name="catalogue"></param>
        /// <returns></returns>
        public virtual bool CreateCatalogue(Catalogue catalogue, bool dropifexists = false)
        {

            bool exists = GetCatalogue(catalogue.CODE) != null;
            if (!dropifexists && exists) return true;
            if (dropifexists && exists) DeleteCatalogue(catalogue.CODE);

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = INSERT_CATALOGUE;
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = catalogue.CODE;
                command.Parameters[1].Value = catalogue.Name;
                command.Parameters[2].Value = catalogue.Description;
                command.Parameters[3].Value = _tagsSerializer.Serialize(catalogue.Tags);
                command.ExecuteNonQuery();
                //Remark: we ignore the autoincrement value
                // SELECT last_insert_rowid()
                //would retrieve the autoincrement ID for sqlite; if needed just re-read the catalogue
            }
            using (var command = _conn.CreateCommand())
            {
                command.CommandText = string.Format(DDL, catalogue.CODE);
                command.ExecuteNonQuery();
            }
            return true;
        }

        public virtual bool DeleteCatalogue(string code)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = DELETE_CATALOGUE;
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = code;
                command.ExecuteNonQuery();
            }
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(DROP_CATALOGUE,code);
                command.ExecuteNonQuery();
            }
            return true;
        }

        public virtual bool UpdateCatalogue(Catalogue catalogue)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = UPDATE_CATALOGUE;
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = catalogue.Name;
                command.Parameters[1].Value = catalogue.Description;
                command.Parameters[2].Value = _tagsSerializer.Serialize(catalogue.Tags);
                command.Parameters[3].Value = catalogue.CODE;
                return command.ExecuteNonQuery() > 0;
            }
        }

        public virtual IList<ICatalogue> GetCatalogues()
        {
            var list = new List<ICatalogue>();

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = READ_CATALOGUES;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var cat = new Catalogue()
                        {
                            CODE = (string)reader[0],
                            Name = (string)reader[1],
                            Description = (string)reader[2],
                            Tags = _tagsSerializer.Deserialize((string)reader[3]),
                        };
                        list.Add(cat);
                    }
                }
                return list;
            }
        }

        #endregion

        #region CRUD

        /// <summary>
        /// Creates an instance if no instance is found with the time stamp given in info.Version
        /// Updates an instance if info.Version matches what is in the DB
        /// Updates the last instance if info.Version is not initialized (DateTime.MinValue)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <param name="updateTags">overwrite the info or leave what is currently in the field when updating</param>
        /// <returns></returns>
        public virtual bool CreateOrUpdate<T>(IObjectInfo<T> info, bool updateTags = true)
        {

            DateTime version = Exist(info.Entity, info.ID, info.Version) ? info.Version : DateTime.MinValue;
            bool update = false;

            if (info.Version == DateTime.MinValue && version > DateTime.MinValue) update = true;
            if (info.Version != DateTime.MinValue && version == info.Version) update = true;

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                if (update)
                {
                    int i = 0;
                    if (updateTags)
                    {
                        command.CommandText = string.Format(UPDATE_VERSION, info.Entity);
                        command.Parameters.Add(command.CreateParameter());
                        command.Parameters[0].Value = _tagsSerializer.Serialize(info.Tags);
                        i = 1;
                    }
                    else
                    {
                        command.CommandText = string.Format(UPDATE_VERSION_NOINFO, info.Entity);
                    }

                    command.Parameters.Add(command.CreateParameter());
                    command.Parameters.Add(command.CreateParameter());
                    command.Parameters.Add(command.CreateParameter());
                    command.Parameters[i++].Value = _serializer.Serialize<T>(info.Instance);
                    command.Parameters[i++].Value = info.ID;
                    command.Parameters[i++].Value = version;

                }
                else
                {
                    version = (info.Version > DateTime.MinValue) ?
                        info.Version.Date + new TimeSpan(info.Version.Hour, info.Version.Minute, info.Version.Second) :
                        info.Version;
                    command.CommandText = string.Format(INSERT, info.Entity);

                    for (int i = 0; i < 4; i++)
                        command.Parameters.Add(command.CreateParameter());

                    command.Parameters[0].Value = version;
                    command.Parameters[1].Value = info.ID;
                    command.Parameters[2].Value = _tagsSerializer.Serialize(info.Tags);
                    command.Parameters[3].Value = _serializer.Serialize<T>(info.Instance);
                }

                int res = command.ExecuteNonQuery();

                return res > 0;
            }

        }

        /// <summary>
        /// Gets a particular version of the object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="catalogue"></param>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public virtual IObjectInfo<T> GetObject<T>(string catalogue, string ID, DateTime version)
        {
            var info = new ObjectInfo<T>();
            info.Entity = catalogue;

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(READ_VERSION, catalogue);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;
                command.Parameters[1].Value = version;

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info.Version = (DateTime)reader[0];
                        info.ID = (string)reader[1];
                        info.Tags = _tagsSerializer.Deserialize((string)reader[2]);
                        info.Instance = _serializer.Deserialize<T>((byte[])reader[3]);
                    }
                }
                return info;
            }
        }

        /// <summary>
        /// Gets the latest version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ID"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public virtual IObjectInfo<T> GetObject<T>(string catalogue, string ID)
        {
            DateTime latest = LastVersion(catalogue, ID);
            if (latest > DateTime.MinValue)
                return GetObject<T>(catalogue, ID, latest);

            return null;
        }

        /// <summary>
        /// Delete all the versions of a particular object
        /// </summary>
        /// <param name="catalogue"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public int DeleteObject(string catalogue, string ID)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(DELETE_ALL_VERSIONS, catalogue);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;

                object retval = command.ExecuteNonQuery();
                return (retval.GetType() == typeof(System.DBNull)) ? 0 : (int)retval;
            }
        }

        /// <summary>
        /// Delete a specific version of the object
        /// </summary>
        /// <param name="catalogue"></param>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public virtual int DeleteObject(string catalogue, string ID, DateTime version)
        {
            using (var command = _conn.CreateCommand()as DbCommand)
            {
                command.CommandText = string.Format(DELETE_VERSION, catalogue);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;
                command.Parameters[1].Value = version;

                object retval = command.ExecuteNonQuery();
                return (retval.GetType() == typeof(System.DBNull)) ? 0 : (int)retval;
            }
        }

        #endregion

        #region finalizing
        
        ~DbObjectStore()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Code to dispose the managed resources of the class
                if (_conn != null) _conn.Dispose();
            }
            // Code to dispose the un-managed resources of the class

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


    }
}

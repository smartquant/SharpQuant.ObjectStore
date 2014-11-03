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
    /// </summary>
    public class DbObjectStore : IObjectStore, IDisposable
    {
        protected IDbConnection _conn;
        protected IObjectSerializer _serializer;
        protected IInfoSerializer _infoSerializer;

        #region SQL

        //SQLite SQL
        //since table is passed as a string this might lead to SQL injection 
        //so table name should be checked on special characters

        protected static string EXIST_TABLE = "SELECT name FROM sqlite_master WHERE tbl_name=? AND type='table'";
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

        public DbObjectStore(Func<IDbConnection> connect, IObjectSerializer objectSerializer, IInfoSerializer infoSerializer)
        {
            _conn = connect();
            _serializer = objectSerializer;
            _infoSerializer = infoSerializer;

        }

        #endregion


        public virtual IDbTransaction BeginTransaction(IsolationLevel il=IsolationLevel.Unspecified)
        {
            return _conn.BeginTransaction(il);
        }

        /// <summary>
        /// Creates a table in the DB
        /// CAUTION: drops existing table!
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual bool CreateEntity(string entity, bool dropifexists = false)
        {
            using (var command = _conn.CreateCommand())
            {
                if (!dropifexists && ExistEntity(entity)) return true;
                command.CommandText = string.Format(DDL, entity);
                return command.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Check whether an entity is present
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual bool ExistEntity(string entity)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = EXIST_TABLE;
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = entity;

                var result = command.ExecuteScalar();
                if (result==null) return false;
                return result.ToString().ToUpper()==entity.ToUpper();
            }
        }

        /// <summary>
        /// Quickly check whether a particular version exists
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public virtual bool Exist(string entity, string ID, DateTime version)
        {

            //returns last version date of the object or min_date
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(EXIST_VERSION, entity);
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
        /// <param name="entity"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public virtual DateTime LastVersion(string entity, string ID)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(EXIST_LAST, entity);
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
        /// <param name="entity"></param>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public virtual DateTime LastVersion(string entity, string ID, DateTime version)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(EXIST_VERSION2, entity);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;
                command.Parameters[1].Value = version;
                object retval = command.ExecuteScalar();
                return (retval.GetType() == typeof(System.DBNull)) ? DateTime.MinValue : DateTime.Parse(retval.ToString());
            }
        }

        /// <summary>
        /// Get all infos from an entity
        /// Object instancies will not be deserialized
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual IList<IObjectInfo<object>> GetAllInfos(string entity)
        {
            var list = new List<IObjectInfo<object>>();

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(READ_ALL_INFO_ENTITY, entity);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var info = new ObjectInfo<object>()
                        {
                            ID = (string)reader[1],
                            Version = (DateTime)reader[0],
                            Tags = _infoSerializer.Deserialize((string)reader[2])
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
        /// <param name="entity"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public virtual IList<IObjectInfo<object>> GetAllInfos(string entity, string ID)
        {
            var list = new List<IObjectInfo<object>>();

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(READ_ALL_INFO_CODES, entity);
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
                            Tags = _infoSerializer.Deserialize((string)reader[2])
                        };
                        list.Add(info);
                    }
                }
                return list;
            }
        }

        #region CRUD

        /// <summary>
        /// Creates an instance if no instance is found with the time stamp given in info.Version
        /// Updates an instance if info.Version matches what is in the DB
        /// Updates the last instance if info.Version is not initialized (DateTime.MinValue)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info"></param>
        /// <param name="updateInfo">overwrite the info or leave what is currently in the field when updating</param>
        /// <returns></returns>
        public virtual bool CreateOrUpdate<T>(IObjectInfo<T> info, bool updateInfo = true)
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
                    if (updateInfo)
                    {
                        command.CommandText = string.Format(UPDATE_VERSION, info.Entity);
                        command.Parameters.Add(command.CreateParameter());
                        command.Parameters[0].Value = _infoSerializer.Serialize(info.Tags);
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
                    command.Parameters[2].Value = _infoSerializer.Serialize(info.Tags);
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
        /// <param name="entity"></param>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public virtual IObjectInfo<T> GetObject<T>(string entity, string ID, DateTime version)
        {
            var info = new ObjectInfo<T>();
            info.Entity = entity;

            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(READ_VERSION, entity);
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
                        info.Tags = _infoSerializer.Deserialize((string)reader[2]);
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
        public virtual IObjectInfo<T> GetObject<T>(string entity, string ID)
        {
            DateTime latest = LastVersion(entity, ID);
            if (latest > DateTime.MinValue)
                return GetObject<T>(entity, ID, latest);

            return null;
        }

        /// <summary>
        /// Delete all the versions of a particular object
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public int DeleteObject(string entity, string ID)
        {
            using (var command = _conn.CreateCommand() as DbCommand)
            {
                command.CommandText = string.Format(DELETE_ALL_VERSIONS, entity);
                command.Parameters.Add(command.CreateParameter());
                command.Parameters[0].Value = ID;

                object retval = command.ExecuteNonQuery();
                return (retval.GetType() == typeof(System.DBNull)) ? 0 : (int)retval;
            }
        }

        /// <summary>
        /// Delete a specific version of the object
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public virtual int DeleteObject(string entity, string ID, DateTime version)
        {
            using (var command = _conn.CreateCommand()as DbCommand)
            {
                command.CommandText = string.Format(DELETE_VERSION, entity);
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

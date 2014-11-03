SharpQuant.ObjectStore
==========================

Simplistic implementation of a general purpose object serialization API that supports versioning.

Design principles:

- Objects can be stored in different entities (think tables in a DB).
- An object is uniquely identified with the tuple entity, ID and version.
- The object is serialized into a BLOB field (byte[] array).
- Additionally a Dictionary<string,string> holds freely definable tags
- An example ObjectStore is implemented for ADO.Net databases ([SQLite] SQL, but overridable)


```csharp
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
```


The object can be retrieved or stored through the following structure.

```csharp
public interface IObjectInfo<T>
{
	string Entity { get; }
	string ID { get; }
	DateTime Version { get; }
	IDictionary<string, string> Info { get; }

	T Instance { get; }
}
```


The following interface defines how to serialize the tagging dictionary. Two simplistic serializers (JSON like and .INI like), but these work only for simple key-value pairs.

```csharp
public interface IInfoSerializer
{
	IDictionary<string, string> Deserialize(string raw);
	string Serialize(IDictionary<string, string> info);
}
```

And this is the interface for the object serializer itself.

```csharp
public interface IObjectSerializer
{
	T Deserialize<T>(byte[] data);
	byte[] Serialize<T>(T obj);
}
```


Basically any binary serializer would do the trick, but depending on the application one would want to use a legacy serializer or add compression or encryption functionality to the serializer. Using [protobuf] the serializtion code could be as simple as the following snippet:

```csharp
using ProtoBuf;

namespace SharpQuant.ObjectStore.Serializers
{
    public class ProtoObjectSerializer : IObjectSerializer
    {
        public unsafe T Deserialize<T>(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(stream);
            }           
        }

        public unsafe byte[] Serialize<T>(T obj)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize<T>(stream, obj);
                return stream.ToArray();
            }
        }
    }

}
```

No object serializers or database frameworks are included in order to not have any external dependancies in the base assembly. However a simple implementation for [SQLite] could look like the following few lines of code.

```csharp
namespace SharpQuant.ObjectStore.SQLite
{
    public class SQLiteObjectStore : DbObjectStore
    {

        static Func<IDbConnection> ConnectionFactory(string fileName)
        {
            Func<IDbConnection> Func = () => 
            {
                IDbConnection conn = new SQLiteConnection();
                conn.ConnectionString = "Data Source=" + fileName + ";";
                conn.Open();
                return conn;
            };
            return Func;
        }

        public SQLiteObjectStore(string fileName)
            : base(ConnectionFactory(fileName), new MyObjectSerializer(), new INIInfoSerializer())
        {
        }
	}
}
```


[protobuf]:http://code.google.com/p/protobuf-net/
[SQLite]:http://www.sqlite.org/
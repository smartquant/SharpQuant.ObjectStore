SharpQuant.ObjectStore
==========================

Simplistic implementation of a general purpose object serialization API that supports versioning.

Design principles:

- Objects can be stored in different catalogues (think tables in a DB).
- An object is uniquely identified with the tuple catalogue, ID and version.
- The object is serialized into a BLOB field (byte[] array).
- Additionally a Dictionary<string,string> holds freely definable tags
- An example ObjectStore is implemented for ADO.Net databases ([SQLite] SQL, but overridable)


```csharp
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
```


The object can be retrieved or stored through the following structure.

```csharp
public interface IObjectInfo<T>
{
	string Catalogue { get; }
	string ID { get; }
	DateTime Version { get; }
	string Type { get; }
	IDictionary<string, string> Tags { get; }

	T Instance { get; }
}
```

But for process interaction it can be beneficial to use the DTO instead.

```csharp
public interface IObjectInfo
{
	string Catalogue { get; }
	string ID { get; }
	DateTime Version { get; }
	string Type { get; }
	IDictionary<string, string> Tags { get; }

	byte[] Data { get; }
	int DataSize { get; }
}
```

The following interface defines how to serialize the tagging dictionary. Two simplistic serializers (JSON like and .INI like), but these work only for simple key-value pairs.

```csharp
public interface ITagsSerializer
{
	IDictionary<string, string> Deserialize(string raw);
	string Serialize(IDictionary<string, string> info);
}
```

And this is the interface for the object serializer itself.

```csharp
public interface IObjectSerializer
{
	T Deserialize<T>(Stream stream);
	Stream Serialize<T>(T obj);
}
```


Basically any binary serializer would do the trick, but depending on the application one would want to use a legacy serializer or add compression or encryption functionality to the serializer. Using [protobuf] the serializtion code could be as simple as the following snippet:

```csharp
using ProtoBuf;

namespace SharpQuant.ObjectStore.Serializers
{
    public class ProtoObjectSerializer : IObjectSerializer
    {
        public T Deserialize<T>(Stream stream)
        {
            return Serializer.Deserialize<T>(stream);        
        }

        public Stream Serialize<T>(T obj)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize<T>(stream, obj);
                return stream;
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
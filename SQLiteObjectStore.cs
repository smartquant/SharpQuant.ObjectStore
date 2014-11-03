
//example SQLite DbObjectstore
//commented out in order to not have dependancies on external libraries

//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Data.SQLite;

//namespace SharpQuant.ObjectStore.SQLite
//{
//    public class SQLiteObjectStore : DbObjectStore
//    {

//        static Func<IDbConnection> ConnectionFactory(string fileName)
//        {
//            Func<IDbConnection> Func = () => 
//            {
//                IDbConnection conn = new SQLiteConnection();
//                conn.ConnectionString = "Data Source=" + fileName + ";";
//                conn.Open();
//                return conn;
//            };
//            return Func;
//        }

//        public SQLiteObjectStore(string fileName)
//            : base(ConnectionFactory(fileName), new MyObjectSerializer(), new INIInfoSerializer())
//        {
//        }
//    }
//}

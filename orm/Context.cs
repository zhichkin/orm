using System;
using System.Dynamic;
using System.Reflection;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        public sealed class Context : IDataMapper
        {
            # region " static members "

            private static object sync_root = new object();

            private static Context singelton = null;

            public static Context Current
            {
                get
                {
                    if (singelton != null) return singelton;

                    lock (sync_root)
                    {
                        if (singelton == null)
                        {
                            singelton = new Context();
                        }
                    }

                    return singelton;
                }
            }

            # endregion
            
            private readonly string   data_source;
            private readonly string   assembly_path;
            private readonly Assembly domain_model_assembly;
            private readonly UserTypeFactory factory;

            private Context() // thread safe
            {
                data_source = ConfigurationManager.ConnectionStrings["DataSource"].ConnectionString;

                assembly_path = ConfigurationManager.AppSettings["DomainModel"];

                domain_model_assembly = Assembly.Load(assembly_path);

                factory = new UserTypeFactory(domain_model_assembly);
            }

            public string DataSource { get { return data_source; } }

            public Type GetUserType(int discriminator)
            {
                return factory.GetUserType(discriminator);
            }

            public string DomainName { get { return factory.DomainName; } }

            public void ClearEntitiesCash() { factory.ClearEntitiesCash(); }

            public object New(Type type)                                     { return factory.New(type);             }
            public object New(Type type, object key)                         { return factory.New(type, key);        }
            public object New(Type type,             PersistenceState state) { return factory.New(type,      state); }
            public object New(Type type, object key, PersistenceState state) { return factory.New(type, key, state); }

            public object New(int type)                                     { return factory.New(type);             }
            public object New(int type, object key)                         { return factory.New(type, key);        }
            public object New(int type,             PersistenceState state) { return factory.New(type,      state); }
            public object New(int type, object key, PersistenceState state) { return factory.New(type, key, state); }

            public T New<T>()                                   { return factory.New<T>();           }
            public T New<T>(object key)                         { return factory.New<T>(key);        }
            public T New<T>(            PersistenceState state) { return factory.New<T>(     state); }
            public T New<T>(object key, PersistenceState state) { return factory.New<T>(key, state); }

            private Dictionary<Type, IDataMapper> mappers = new Dictionary<Type, IDataMapper>();

            public void Insert(ISerializable entity) { this.GetDataMapper(entity.GetType()).Insert(entity); }
            public void Select(ISerializable entity) { this.GetDataMapper(entity.GetType()).Select(entity); }
            public void Update(ISerializable entity) { this.GetDataMapper(entity.GetType()).Update(entity); }
            public void Delete(ISerializable entity) { this.GetDataMapper(entity.GetType()).Delete(entity); }

            private IDataMapper GetDataMapper(Type type)
            {
                IDataMapper mapper = null;

                if (!mappers.TryGetValue(type, out mapper))
                {
                    string mapper_name = type.FullName + "+DataMapper";

                    Type t = domain_model_assembly.GetType(mapper_name);

                    if (t == null) throw new UnknownTypeException(mapper_name);

                    ConstructorInfo constructor = t.GetConstructor(new Type[] { typeof(string) });

                    mapper = (IDataMapper)constructor.Invoke(new object[] { data_source });

                    mappers.Add(type, mapper);
                }

                return mapper;
            }
            
            public int ExecuteNonQuery(string sql)
            {
                int result = 0;
                using (SqlConnection connection = new SqlConnection(data_source))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();

                        result = command.ExecuteNonQuery();
                    }
                }
                return result;
            }

            public object ExecuteScalar(string sql)
            {
                object result = null;
                using (SqlConnection connection = new SqlConnection(data_source))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();

                        result = command.ExecuteScalar();
                    }
                }
                return result;
            }

            public List<dynamic> Execute(string sql)
            {
                List<dynamic> result = new List<dynamic>();
                using (SqlConnection connection = new SqlConnection(data_source))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dynamic item = new ExpandoObject();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string name = reader.GetName(i);
                                    object value = reader.GetValue(i);
                                    ((IDictionary<string, object>)item).Add(name, value);                                    
                                }
                                result.Add(item);
                            }
                        }
                    }
                }
                return result;
            }
        }
    }
}

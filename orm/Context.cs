using System;
using System.Reflection;
using System.Configuration;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        public interface IDataMapper
        {
            void Insert(ISerializable entity);
            void Select(ISerializable entity);
            void Update(ISerializable entity);
            void Delete(ISerializable entity);
        }

        public interface IContext : IDataMapper
        {
            string DataSource { get; }
            Factory Factory { get; }
        }

        public sealed class Context : IContext
        {
            # region " static members "

            private static object sync_root = new object();

            private static Context singelton = null;

            public static IContext Current
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

            private Factory factory = null;

            private string data_source = string.Empty;

            private string assembly_name = string.Empty;

            private Assembly domain_model_assembly = null;

            private Context() // thread safe
            {
                data_source = ConfigurationManager.ConnectionStrings["DataSource"].ConnectionString;

                assembly_name = ConfigurationManager.AppSettings["DomainModel"];

                domain_model_assembly = Assembly.Load(assembly_name);

                factory = new Factory();

                factory.Initialize(domain_model_assembly);
            }

            public string DataSource { get { return data_source; } }

            public Factory Factory { get { return factory; } }

            private Dictionary<Type, IDataMapper> mappers = new Dictionary<Type, IDataMapper>();

            void IDataMapper.Insert(ISerializable entity) { this.GetDataMapper(entity.GetType()).Insert(entity); }
            void IDataMapper.Select(ISerializable entity) { this.GetDataMapper(entity.GetType()).Select(entity); }
            void IDataMapper.Update(ISerializable entity) { this.GetDataMapper(entity.GetType()).Update(entity); }
            void IDataMapper.Delete(ISerializable entity) { this.GetDataMapper(entity.GetType()).Delete(entity); }

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
        }
    }
}

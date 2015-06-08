using System;
using System.IO;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        internal sealed class UserType
        {
            # region " Fields "

            private Type type = null;

            private int discriminator = 0;

            private Func<object> default_ctor = null; // persistent object constructor without parameters

            private Func<ISerializable, object> virtual_ctor = null; // persistent object constructor with TKey parameter

            private Func<ISerializable> key_ctor = null; // key object default constructor

            private Func<object, ISerializable> key_formatter = null; // key object ISerializable interface

            # endregion

            public UserType(Type type, int discriminator) { this.type = type; this.discriminator = discriminator; }

            # region " Properties "

            public Type Type { get { return type; } }

            public int Discriminator { get { return discriminator; } }

            internal Func<object> DefaultConstructor { get { return default_ctor; } }

            internal Func<ISerializable, object> VirtualConstructor { get { return virtual_ctor; } }

            public Func<ISerializable> KeyConstructor { get { return key_ctor; } }

            public Func<object, ISerializable> KeyFormatter { get { return key_formatter; } }

            # endregion

            internal sealed class Registry /* currently works only with IPersistent classes (see ctor info) */
            {
                private Dictionary<Type, int> map = new Dictionary<Type, int>();

                private Dictionary<int, UserType> registry = new Dictionary<int, UserType>();

                private string domain_name = null;

                public Registry(string domain_name) { this.domain_name = domain_name; }

                public string DomainName { get { return domain_name; } }

                public void Add(Type type, int discriminator)
                {
                    if (!registry.ContainsKey(discriminator))
                    {
                        UserType entry = new UserType(type, discriminator);

                        entry.default_ctor = CreateDefaultConstructor(type);

                        if (entry.default_ctor != null)
                        {
                            this.SetupKeyInfo(type, entry);

                            if (entry.key_ctor != null & entry.key_formatter != null)
                            {
                                entry.virtual_ctor = CreateVirtualConstructor(type);
                            }

                            map.Add(type, discriminator);

                            registry.Add(discriminator, entry);
                        }
                    }
                }

                # region " Getting UserType items "

                public UserType GetTypeInfo(Type type)
                {
                    int discriminator = (int)SystemType.Unknown;

                    if (!map.TryGetValue(type, out discriminator))
                    {
                        throw new UnknownTypeException(type.FullName);
                    }

                    return GetTypeInfo(discriminator);
                }

                public UserType GetTypeInfo(object entity)
                {
                    if (entity == null) throw new ArgumentNullException("entity");

                    Type type = entity.GetType();

                    return GetTypeInfo(type);
                }

                public UserType GetTypeInfo(int discriminator)
                {
                    UserType entry = null;

                    if (!registry.TryGetValue(discriminator, out entry))
                    {
                        throw new UnknownTypeException("discriminator(" + discriminator.ToString() + ")");
                    }

                    return entry;
                }

                # endregion

                # region " Building Constructors "

                private Func<ISerializable> CreateConstructor(Type type) /* constructor without parameters */
                {
                    Func<ISerializable> ctor = null;

                    ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);

                    if (constructor != null)
                    {
                        var call = Expression.New(constructor);

                        var lambda = Expression.Lambda(call);

                        ctor = (Func<ISerializable>)lambda.Compile();
                    }

                    return ctor;
                }

                private Func<object> CreateDefaultConstructor(Type type) /* state = New */
                {
                    Func<object> ctor = null;

                    ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);

                    if (constructor != null)
                    {
                        var call = Expression.New(constructor);

                        var lambda = Expression.Lambda(call);

                        ctor = (Func<object>)lambda.Compile();
                    }

                    return ctor;
                }

                private Func<ISerializable, object> CreateVirtualConstructor(Type type) /* ISerializable = key, state = Virtual supporting lazy load */
                {
                    Func<ISerializable, object> ctor = null;

                    ConstructorInfo constructor = type.GetConstructor(new[] { typeof(ISerializable) });

                    if (constructor != null)
                    {
                        var key = Expression.Parameter(typeof(ISerializable), "key");

                        var call = Expression.New(constructor, key);

                        var lambda = Expression.Lambda(call, key);

                        ctor = (Func<ISerializable, object>)lambda.Compile();
                    }

                    return ctor;
                }

                # endregion

                # region " Building formatters "

                private void SetupKeyInfo(Type type, UserType info)
                {
                    Type i = type.GetInterface(Factory.IPersistent);

                    if (i != null)
                    {
                        Type key = i.GetGenericArguments()[0];

                        if (key.GetInterface(Factory.ISerializable) != null)
                        {
                            info.key_ctor = this.CreateConstructor(key);

                            MethodInfo[] list = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static);

                            MethodInfo method = null;

                            if (list != null && list.Length > 0)
                            {
                                foreach (MethodInfo test in list)
                                {
                                    if (test.Name == "GetFormatter" && test.ReturnType == typeof(ISerializable) && test.IsGenericMethod && test.GetGenericArguments().Length == 1)
                                    {
                                        ParameterInfo[] p = test.GetParameters();

                                        if (p != null && p.Length == 1 && p[0].ParameterType == typeof(IPersistent<>))
                                        {
                                            method = test.MakeGenericMethod(key);

                                            var parameter = Expression.Parameter(typeof(IPersistent<>), "entity");
                                            var call = Expression.Call(method, parameter);
                                            var lambda = Expression.Lambda(call, parameter);

                                            info.key_formatter = (Func<object, ISerializable>)lambda.Compile();

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                # endregion

                # region " Static help methods "

                public static ISerializable GetFormatter<TKey>(IPersistent<TKey> entity)
                    where TKey : ISerializable, new()
                {
                    return (ISerializable)entity.Key;
                }

                # endregion
            }
        }
    }
}

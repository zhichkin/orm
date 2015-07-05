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

            private readonly Type type;
            private readonly Type tkey;
            private readonly int  dtor;

            private Func<object> ctor_0;
            private Func<object, object> ctor_1;
            private Func<object, PersistenceState, object> ctor_2;
            
            # endregion

            public UserType(Type type, Type tkey, int discriminator)
            {
                this.type = type;
                this.tkey = tkey;
                this.dtor = discriminator;
            }

            # region " Properties "

            public Type Type { get { return type; } }
            public Type TKey { get { return tkey; } }
            public int Discriminator { get { return dtor; } }

            internal Func<object> DefaultConstructor { get { return ctor_0; } }
            internal Func<object, object> KeyConstructor { get { return ctor_1; } }
            internal Func<object, PersistenceState, object> KeyStateConstructor { get { return ctor_2; } }

            # endregion

            internal sealed class Registry
            {
                # region " string constants "
                internal const string ISerializable = "zhichkin.orm.ISerializable";
                internal const string IPersistent_1 = "zhichkin.orm.IPersistent`1";
                # endregion

                private Dictionary<Type, int> map = new Dictionary<Type, int>();

                private Dictionary<int, UserType> registry = new Dictionary<int, UserType>();

                public Registry(Assembly domainModel)
                {
                    object[] attributes;
                    foreach (Type type in domainModel.GetTypes())
                    {
                        attributes = type.GetCustomAttributes(typeof(DiscriminatorAttribute), false);
                        if (attributes == null)
                        {
                            continue;
                        }
                        DiscriminatorAttribute attribute = (DiscriminatorAttribute)attributes[0];

                        Type IPersistent_1 = type.GetInterface(Registry.IPersistent_1);
                        if (IPersistent_1 == null)
                        {
                            throw new ArgumentNullException("IPersistent'1 is missing!");
                        }
                        Type tkey = IPersistent_1.GenericTypeArguments[0];

                        this.Add(type, tkey, attribute.Discriminator);
                    }
                }

                private void Add(Type type, Type tkey, int discriminator)
                {
                    UserType entry = new UserType(type, tkey, discriminator);

                    entry.ctor_0 = this.CreateDefaultConstructor(entry);
                    entry.ctor_1 = this.CreateKeyConstructor(entry);
                    entry.ctor_2 = this.CreateKeyStateConstructor(entry);

                    map.Add(type, discriminator);
                    registry.Add(discriminator, entry);
                }

                # region " Getting UserType items "

                public UserType GetUserType(Type type)
                {
                    int discriminator;
                    if (!map.TryGetValue(type, out discriminator))
                    {
                        throw new UnknownTypeException(type.FullName);
                    }
                    return GetUserType(discriminator);
                }

                public UserType GetUserType(object entity)
                {
                    if (entity == null) throw new ArgumentNullException("entity");
                    return GetUserType(entity.GetType());
                }

                public UserType GetUserType(int discriminator)
                {
                    UserType entry;
                    if (!registry.TryGetValue(discriminator, out entry))
                    {
                        throw new UnknownTypeException("discriminator(" + discriminator.ToString() + ")");
                    }
                    return entry;
                }

                # endregion

                # region " Building Constructors "

                private Func<object> CreateDefaultConstructor(UserType ut)
                {
                    Func<object> ctor = null;

                    ConstructorInfo constructor = ut.Type.GetConstructor(Type.EmptyTypes);

                    if (constructor != null)
                    {
                        var call = Expression.New(constructor);

                        var lambda = Expression.Lambda(call);

                        ctor = (Func<object>)lambda.Compile();
                    }

                    return ctor;
                }

                private Func<object, object> CreateKeyConstructor(UserType ut)
                {
                    ConstructorInfo constructor = ut.Type.GetConstructor(new[] { ut.TKey });
                    if (constructor == null)
                    {
                        throw new ArgumentNullException("Constructor is missing!");
                    }
                    var key = Expression.Parameter(ut.TKey, "key");
                    var call = Expression.New(constructor, key);
                    var lambda = Expression.Lambda(call, key);

                    return (Func<object, object>)lambda.Compile();
                }

                private Func<object, PersistenceState, object> CreateKeyStateConstructor(UserType ut)
                {
                    ConstructorInfo constructor = ut.Type.GetConstructor(new[] { ut.TKey });
                    if (constructor == null)
                    {
                        throw new ArgumentNullException("Constructor is missing!");
                    }
                    var key = Expression.Parameter(ut.TKey, "key");
                    var state = Expression.Parameter(typeof(PersistenceState), "state");
                    var call = Expression.New(constructor, key, state);
                    var lambda = Expression.Lambda(call, key, state);

                    return (Func<object, PersistenceState, object>)lambda.Compile();
                }

                # endregion
            }
        }
    }
}

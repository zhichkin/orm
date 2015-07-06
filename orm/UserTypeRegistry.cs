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
        public sealed class UserType
        {
            # region " Fields and Properties"

            private readonly Type type;
            private readonly Type tkey;
            private readonly int  dtor;
            private IUserTypeFactory factory;

            public Type Type { get { return type; } }
            public Type TKey { get { return tkey; } }
            public int Discriminator { get { return dtor; } }
            internal IUserTypeFactory Factory { get { return factory; } }

            # endregion

            private UserType(Type type, Type tkey, int discriminator)
            {
                this.type = type;
                this.tkey = tkey;
                this.dtor = discriminator;
            }

            public sealed class Registry
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
                    entry.factory = this.GetUserTypeFactory(entry);
                    map.Add(type, discriminator);
                    registry.Add(discriminator, entry);
                }

                private IUserTypeFactory GetUserTypeFactory(UserType ut)
                {
                    IUserTypeFactory factory = null;

                    // TODO: look at the GetDataMapper method implementation of the Context class

                    return factory;
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
            }
        }
    }
}

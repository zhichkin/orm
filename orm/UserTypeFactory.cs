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
        public sealed class UserTypeFactory
        {
            private readonly IdentityMap identity_map = new IdentityMap();
            
            private readonly UserType.Registry registry;

            private readonly string domain_name;
            public string DomainName { get { return domain_name; } }

            public UserTypeFactory(Assembly domainModel)
            {
                object[] attributes = domainModel.GetCustomAttributes(typeof(DomainModelAttribute), false);

                if (attributes == null)
                {
                    throw new ArgumentNullException("DomainModelAttribute is missing!");
                }
                DomainModelAttribute dma = (DomainModelAttribute)attributes[0];

                if (string.IsNullOrWhiteSpace(dma.Name))
                {
                    throw new ArgumentNullException("Domain model name is not defined!");
                }
                domain_name = dma.Name;

                registry = new UserType.Registry(domainModel);
            }

            internal Type GetUserType(int discriminator)
            {
                return registry.GetUserType(discriminator).Type;
            }

            internal void ClearEntitiesCash()
            {
                identity_map.Clear();
            }

            # region " Factory methods "

            public object New(Type type)
            {
                if (type == null) throw new ArgumentNullException("type");
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException(type.FullName);

                object item = info.Factory.New();

                Entity entity = item as Entity;
                if (entity != null)
                {
                    identity_map.Add(entity);
                    return entity;
                }

                return item;
            }

            public object New(Type type, object key)
            {
                if (key == null) throw new ArgumentNullException("key");
                if (type == null) throw new ArgumentNullException("type");
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException(type.FullName);

                object item = info.Factory.New(key);

                Entity entity = item as Entity;
                if (entity != null)
                {
                    Entity cashed = null;
                    bool exists = identity_map.Find(type, (Guid)key, ref cashed);
                    if (!exists)
                    {
                        identity_map.Add(entity);
                    }
                    return (exists) ? cashed : entity;
                }

                return item;
            }

            public object New(Type type, PersistenceState state)
            {
                if (type == null) throw new ArgumentNullException("type");
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException(type.FullName);

                object item = info.Factory.New(state);

                return item;
            }

            public object New(Type type, object key, PersistenceState state)
            {
                if (key == null) throw new ArgumentNullException("key");
                if (type == null) throw new ArgumentNullException("type");
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException(type.FullName);

                object item = info.Factory.New(key, state);

                Entity entity = item as Entity;
                if (entity != null)
                {
                    Entity cashed = null;
                    bool exists = identity_map.Find(type, (Guid)key, ref cashed);
                    if (!exists)
                    {
                        identity_map.Add(entity);
                    }
                    return (exists) ? cashed : entity;
                }

                return item;
            }

            public object New(int type)
            {
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException("discriminator(" + type.ToString() + ")");

                object item = info.Factory.New();

                Entity entity = item as Entity;
                if (entity != null)
                {
                    identity_map.Add(entity);
                    return entity;
                }

                return item;
            }

            public object New(int type, object key)
            {
                if (key == null) throw new ArgumentNullException("key");
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException("discriminator(" + type.ToString() + ")");

                object item = info.Factory.New(key);

                Entity entity = item as Entity;
                if (entity != null)
                {
                    Entity cashed = null;
                    bool exists = identity_map.Find(info.Type, (Guid)key, ref cashed);
                    if (!exists)
                    {
                        identity_map.Add(entity);
                    }
                    return (exists) ? cashed : entity;
                }

                return item;
            }

            public object New(int type, PersistenceState state)
            {
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException("discriminator(" + type.ToString() + ")");

                object item = info.Factory.New(state);

                return item;
            }

            public object New(int type, object key, PersistenceState state)
            {
                if (key == null) throw new ArgumentNullException("key");
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException("discriminator(" + type.ToString() + ")");

                object item = info.Factory.New(key, state);

                Entity entity = item as Entity;
                if (entity != null)
                {
                    Entity cashed = null;
                    bool exists = identity_map.Find(info.Type, (Guid)key, ref cashed);
                    if (!exists)
                    {
                        identity_map.Add(entity);
                    }
                    return (exists) ? cashed : entity;
                }

                return item;
            }

            # endregion

            # region " Generic factory methods "

            public T New<T>()
            {
                return (T)New(typeof(T));
            }

            public T New<T>(object key)
            {
                return (T)New(typeof(T), key);
            }

            public T New<T>(PersistenceState state)
            {
                return (T)New(typeof(T), state);
            }

            public T New<T>(object key, PersistenceState state)
            {
                return (T)New(typeof(T), key, state);
            }

            # endregion
        }
    }
}
// tip: if (type.IsPublic && type.IsAbstract && type.IsSealed) /* that means static class */
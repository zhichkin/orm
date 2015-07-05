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
        public sealed class Factory
        {
            private readonly IdentityMap identity_map = new IdentityMap();
            
            private readonly UserType.Registry registry;

            private readonly string domain_name;
            public string DomainName { get { return domain_name; } }

            public Factory(Assembly domainModel)
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
            
            # region " Factory methods "

            public object New(Type type)
            {
                if (type == null) throw new ArgumentNullException("type");
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException(type.FullName);

                object item = info.DefaultConstructor();

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

                object item = info.KeyConstructor(key);

                Entity entity = item as Entity;
                if (entity != null)
                {
                    bool exists = identity_map.Find(type, (Guid)key, ref entity);
                    if (!exists)
                    {
                        identity_map.Add(entity);
                    }
                    return entity;
                }

                return item;
            }

            public object New(Type type, object key, PersistenceState state)
            {
                if (key == null) throw new ArgumentNullException("key");
                if (type == null) throw new ArgumentNullException("type");
                UserType info = registry.GetUserType(type);
                if (info == null) throw new UnknownTypeException(type.FullName);

                object item = info.KeyStateConstructor(key, state);

                Entity entity = item as Entity;
                if (entity != null)
                {
                    bool exists = identity_map.Find(type, (Guid)key, ref entity);
                    if (!exists)
                    {
                        identity_map.Add(entity);
                    }
                    return entity;
                }

                return item;
            }

            # endregion
        }
    }
}
// tip: if (type.IsPublic && type.IsAbstract && type.IsSealed) /* that means static class */
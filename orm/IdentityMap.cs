using System;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        internal sealed class IdentityMap
        {
            public IdentityMap() { }

            private Dictionary<Guid, Entity> cash = new Dictionary<Guid, Entity>();

            public void Add(Entity item)
            {
                if (item == null) throw new ArgumentNullException("item");

                if (item.State == PersistenceState.New)
                {
                    item.StateChanged += NewEntity_StateChanged;
                }
                else if (item.State == PersistenceState.Virtual)
                {
                    item.StateChanged += Entity_StateChanged;
                }
            }

            public bool Find(Type type, ISerializable key, ref Entity item)
            {
                Guid guid = new Guid(key.ToString());

                bool ok = cash.TryGetValue(guid, out item);

                return ok;
            }

            private void NewEntity_StateChanged<TKey>(IPersistent<TKey> sender, StateEventArgs args)
                where TKey : ISerializable, new()
            {
                if (args.OldState == PersistenceState.New && args.NewState == PersistenceState.Original)
                {
                    Entity entity = (Entity)sender;

                    Guid key = new Guid(entity.Key.ToString());

                    cash.Add(key, (Entity)sender);

                    sender.StateChanged -= NewEntity_StateChanged;

                    sender.StateChanged += Entity_StateChanged;
                }
            }

            private void Entity_StateChanged<TKey>(IPersistent<TKey> sender, StateEventArgs args)
                where TKey : ISerializable, new()
            {
                if (args.NewState == PersistenceState.Deleted)
                {
                    Entity entity = (Entity)sender;

                    Guid key = new Guid(entity.Key.ToString());

                    cash.Remove(key);

                    sender.StateChanged -= Entity_StateChanged;
                }
            }
        }
    }
}
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

            public bool Find(Type type, Guid key, ref Entity item)
            {
                return cash.TryGetValue(key, out item);
            }

            private void NewEntity_StateChanged(object sender, StateEventArgs args)
            {
                if (args.OldState == PersistenceState.New && args.NewState == PersistenceState.Original)
                {
                    Entity entity = (Entity)sender;

                    cash.Add(entity.Key, (Entity)sender);

                    entity.StateChanged -= NewEntity_StateChanged;

                    entity.StateChanged += Entity_StateChanged;
                }
            }

            private void Entity_StateChanged(object sender, StateEventArgs args)
            {
                if (args.NewState == PersistenceState.Deleted)
                {
                    Entity entity = (Entity)sender;

                    cash.Remove(entity.Key);

                    entity.StateChanged -= Entity_StateChanged;
                }
            }
        }
    }
}
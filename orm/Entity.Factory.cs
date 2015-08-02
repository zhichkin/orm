using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        public partial class Entity
        {
            public abstract new class Factory<TEntity> : IUserTypeFactory where TEntity : Entity, new()
            {
                public Factory() { }

                private void CheckState(PersistenceState state)
                {
                    if (state == PersistenceState.Deleted ||
                        state == PersistenceState.Changed ||
                        state == PersistenceState.Loading ||
                        state == PersistenceState.Original) throw new ArgumentOutOfRangeException("state");
                }

                public object New()
                {
                    TEntity entity = new TEntity();
                    entity.context = Context.Current;
                    entity.key     = Guid.NewGuid();
                    return entity;
                }

                public object New(object key)
                {
                    TEntity entity = new TEntity();
                    entity.context = Context.Current;
                    entity.key     = (Guid)key;
                    return entity;
                }

                public object New(PersistenceState state)
                {
                    throw new NotSupportedException();
                }

                public object New(object key, PersistenceState state)
                {
                    CheckState(state);
                    TEntity entity = new TEntity();
                    entity.context = Context.Current;
                    entity.key     = (Guid)key;
                    entity.state   = (state == PersistenceState.New) ? state : PersistenceState.Virtual;
                    return entity;
                }
            }
        }
    }
}
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
        public partial class Persistent<TKey>
        {
            public abstract class Factory<TPersistent> : IUserTypeFactory where TPersistent : Persistent<TKey>, new()
            {
                public Factory() { }

                private void CheckState(PersistenceState state)
                {
                    if (state == PersistenceState.Deleted ||
                        state == PersistenceState.Changed ||
                        state == PersistenceState.Virtual ||
                        state == PersistenceState.Original) throw new ArgumentOutOfRangeException("state");
                }

                public object New()
                {
                    return new TPersistent()
                    {
                        context = Context.Current
                    };
                }

                public object New(object key)
                {
                    throw new NotSupportedException();
                }

                public object New(PersistenceState state)
                {
                    CheckState(state);
                    return new TPersistent()
                    {
                        context = Context.Current,
                        state   = state
                    };
                }

                public object New(object key, PersistenceState state)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
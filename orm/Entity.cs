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
        public abstract class Entity : Persistent<Guid>
        {
            protected byte[] version = new byte[8];

            public override void Serialize(BinaryWriter stream)
            {
                base.Serialize(stream);
                stream.Write(key.ToByteArray());
                stream.Write(version);
            }

            public override void Deserialize(BinaryReader stream)
            {
                base.Deserialize(stream);
                key = new Guid(stream.ReadBytes(16));
                version = stream.ReadBytes(8);
            }

            # region " Переопределение методов сравнения "

            public override Int32 GetHashCode() { return key.GetHashCode(); }

            public override Boolean Equals(Object obj)
            {
                if (obj == null) { return false; }

                Entity test = obj as Entity;

                if (test == null) { return false; }

                return key == test.key;
            }

            public static Boolean operator ==(Entity left, Entity right)
            {
                if (Object.ReferenceEquals(left, right)) { return true; }

                if (((Object)left == null) || ((Object)right == null)) { return false; }

                return left.Equals(right);
            }

            public static Boolean operator !=(Entity left, Entity right)
            {
                return !(left == right);
            }

            #endregion

            public abstract new class Factory<TEntity> : IUserTypeFactory where TEntity : Entity, new()
            {
                public Factory() { }

                private void CheckState(PersistenceState state)
                {
                    if (state == PersistenceState.Deleted ||
                        state == PersistenceState.Changed ||
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
                    entity.state   = (state == PersistenceState.New || state == PersistenceState.Loading ) ? state : PersistenceState.Virtual;
                    return entity;
                }
            }
        }
    }
}
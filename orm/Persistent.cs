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
        public abstract class Persistent<TKey> : IPersistent<TKey>
            where TKey : ISerializable, new()
        {
            protected IContext context;

            protected TKey key = default(TKey);
            protected int discriminator = -1;
            protected PersistenceState state = PersistenceState.New;

            // Constructors

            protected Persistent()
            {
                this.context = Context.Current;
            }

            protected Persistent(ISerializable key) : base()
            {
                if (key == null) throw new ArgumentNullException("key");

                this.key = (TKey)key;
            }

            public TKey Key { get { return key; } }
            public PersistenceState State { get { return state; } }
            public virtual int Discriminator { get { return discriminator; } }

            protected void Set<TValue>(TValue value, ref TValue storage)
            {
                if (state == PersistenceState.Loading) { storage = value; return; }

                if (state == PersistenceState.Deleted) return; // throw exception ?

                LazyLoad(); // this code is executed for Virtual state of persistent objects

                // The code below is executed for New, Changed and Original states of persistent objects

                bool changed = false;

                if (storage != null)
                    changed = !storage.Equals(value);
                else
                    changed = (value != null);

                if (changed)
                {
                    StateEventArgs args = new StateEventArgs(PersistenceState.Original, PersistenceState.Changed);

                    if (state == PersistenceState.Original)
                    {
                        OnStateChanging(args);
                    }

                    storage = value;

                    if (state == PersistenceState.Original)
                    {
                        state = PersistenceState.Changed;

                        OnStateChanged(args);
                    }
                }
            }

            protected TValue Get<TValue>(ref TValue storage)
            {
                LazyLoad(); return storage;
            }

            # region " state events handling "

            public event StateChangedEventHandler<TKey> StateChanged;
            public event StateChangingEventHandler<TKey> StateChanging;

            protected void OnStateChanging(StateEventArgs args)
            {
                if (StateChanging != null) StateChanging(this, args);
            }

            protected void OnStateChanged(StateEventArgs args)
            {
                if (StateChanged != null) StateChanged(this, args);
            }

            # endregion

            private void LazyLoad() { if (state == PersistenceState.Virtual) Load(); }

            # region " ActiveRecord "

            public virtual void Save()
            {
                if (state == PersistenceState.New || state == PersistenceState.Changed)
                {
                    StateEventArgs args = new StateEventArgs(state, PersistenceState.Original);

                    OnStateChanging(args);

                    if (state == PersistenceState.New)
                    {
                        context.Insert(this);
                    }
                    else
                    {
                        context.Update(this);
                    }

                    state = PersistenceState.Original;

                    OnStateChanged(args);
                }
            }

            public virtual void Kill()
            {
                if (state == PersistenceState.Original || state == PersistenceState.Changed || state == PersistenceState.Virtual)
                {
                    StateEventArgs args = new StateEventArgs(state, PersistenceState.Deleted);

                    OnStateChanging(args);

                    context.Delete(this);

                    state = PersistenceState.Deleted;

                    OnStateChanged(args);
                }
            }

            public void Load()
            {
                if (state == PersistenceState.Changed || state == PersistenceState.Original || state == PersistenceState.Virtual)
                {
                    PersistenceState old = state;

                    state = PersistenceState.Loading;

                    StateEventArgs args = new StateEventArgs(state, PersistenceState.Original);

                    try
                    {
                        OnStateChanging(args);

                        context.Select(this);

                        state = PersistenceState.Original;

                        OnStateChanged(args);
                    }
                    catch
                    {
                        if (state == PersistenceState.Loading) state = old; throw;
                    }
                }
            }

            # endregion

            # region " ISerializable "

            public virtual void Serialize(BinaryWriter stream)
            {
                context.Factory.Serialize(stream, discriminator);
                key.Serialize(stream);
                context.Factory.Serialize(stream, (byte)state);
            }

            public virtual void Deserialize(BinaryReader stream)
            {
                int test = 0; context.Factory.Deserialize(stream, ref test);
                if (test != discriminator) throw new ArgumentException("discriminator");
                key.Deserialize(stream);
                byte _state = 0; context.Factory.Deserialize(stream, ref _state); state = (PersistenceState)_state;
            }

            # endregion
        }

        //

        public sealed class Identity : ISerializable
        {
            private Factory formatter = Context.Current.Factory;

            private Guid value = Guid.NewGuid();

            public Identity() { }

            public Identity(Guid value) { this.value = value; }

            public Guid Value { get { return value; } }

            public void Serialize(BinaryWriter stream) { formatter.Serialize(stream, value); }
            public void Deserialize(BinaryReader stream) { formatter.Deserialize(stream, ref value); }

            public override string ToString() { return value.ToString(); }

            # region " Переопределение методов сравнения "

            public override Int32 GetHashCode() { return value.GetHashCode(); }

            public override Boolean Equals(Object obj)
            {
                if (obj == null) { return false; }

                Identity test = obj as Identity;

                if (test == null) { return false; }

                return this.value == test.value;
            }

            public static Boolean operator ==(Identity left, Identity right)
            {
                if (Object.ReferenceEquals(left, right)) { return true; }

                if (((Object)left == null) || ((Object)right == null)) { return false; }

                return left.Equals(right);
            }

            public static Boolean operator !=(Identity left, Identity right)
            {
                return !(left == right);
            }

            #endregion
        }

        //

        public abstract class Entity : Persistent<Identity>
        {
            protected Entity() : base() { key = new Identity(); }

            protected Entity(Identity key) : base(key) { }

            protected byte[] version = new byte[8];

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
        }
    }
}
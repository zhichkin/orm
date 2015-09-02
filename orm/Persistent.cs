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
        public abstract partial class Persistent<TKey> : IPersistent<TKey>
        {
            protected Context context;
            protected TKey key = default(TKey);
            protected PersistenceState state = PersistenceState.New;

            public TKey Key { get { return key; } }
            public PersistenceState State
            {
                get { return state; }
                set
                {
                    if (state == PersistenceState.Loading  && value == PersistenceState.Original)
                    {
                        state = value;
                        UpdateKeyValues();
                    }
                    else
                    {
                        throw new NotSupportedException("The transition from the current state to the new one is not supported!");
                    }
                }
            }
            public virtual int Discriminator { get { return 0; } }

            protected virtual void UpdateKeyValues()
            {
                // Compound keys can have fields changeable by user code.
                // When changed key is stored to the database, object's key values in memory must be synchronized.
            }

            protected void Set<TValue>(TValue value, ref TValue storage)
            {
                if (state == PersistenceState.Deleted) return;

                if (state == PersistenceState.New || state == PersistenceState.Loading || state == PersistenceState.Changed)
                {
                    storage = value; return;
                }

                LazyLoad(); // this code is executed for Virtual state of persistent objects

                // The code below is executed for Original state only

                if (state != PersistenceState.Original) return;

                bool changed = false;

                if (storage != null)
                {
                    changed = !storage.Equals(value);
                }
                else
                {
                    changed = (value != null);
                }

                if (changed)
                {
                    StateEventArgs args = new StateEventArgs(PersistenceState.Original, PersistenceState.Changed);

                    OnStateChanging(args);
                    
                    storage = value;

                    state = PersistenceState.Changed;

                    OnStateChanged(args);
                }
            }

            protected TValue Get<TValue>(ref TValue storage)
            {
                LazyLoad(); return storage;
            }

            # region " state events handling "

            public event StateChangedEventHandler StateChanged;
            public event StateChangingEventHandler StateChanging;
            protected void OnStateChanging(StateEventArgs args)
            {
                if (StateChanging != null) StateChanging(this, args);
            }
            protected void OnStateChanged(StateEventArgs args)
            {
                if (args.NewState == PersistenceState.Original)
                {
                    UpdateKeyValues();
                }
                if (StateChanged != null) StateChanged(this, args);
            }
            
            # endregion
            
            private void LazyLoad() { if (state == PersistenceState.Virtual) Load(); }

            # region " ActiveRecord "

            public event SaveEventHandler OnSave;
            public event KillEventHandler OnKill;
            public event LoadEventHandler OnLoad;

            private void FireSaveEvent()
            {
                if (OnSave != null) OnSave(this);
            }
            private void FireKillEvent()
            {
                if (OnKill == null) return;

                Delegate[] list = OnKill.GetInvocationList();
                int count = list.Length;
                while (count > 0)
                {
                    count--;
                    ((KillEventHandler)list[count])(this);
                }

            }
            private void FireLoadEvent()
            {
                if (OnLoad != null) OnLoad(this);
            }

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
                FireSaveEvent();
            }
            public virtual void Kill()
            {
                if (state == PersistenceState.Original || state == PersistenceState.Changed || state == PersistenceState.Virtual)
                {
                    FireKillEvent();

                    StateEventArgs args = new StateEventArgs(state, PersistenceState.Deleted);

                    OnStateChanging(args);

                    context.Delete(this);

                    state = PersistenceState.Deleted;

                    OnStateChanged(args);
                }
            }
            public virtual void Load()
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

                        FireLoadEvent();
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
                stream.Write(this.Discriminator);
                stream.Write((byte)state);                
            }

            public virtual void Deserialize(BinaryReader stream)
            {
                int test = stream.ReadInt32();
                if (test != this.Discriminator) throw new ArgumentException("discriminator");
                state = (PersistenceState)stream.ReadByte();
            }

            # endregion
        }
    }
}
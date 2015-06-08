using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        public enum PersistenceState : byte
        {
            New,      // объект только что создан в памяти, ещё не существует в источнике данных
            Virtual,  // объект существует в источнике данных, но ещё не загружены его свойства
            Loading,  // объект в данный момент загружается из источника данных
            Original, // объект загружен из источника данных и ещё ни разу с тех пор не изменялся
            Changed,  // объект загружен из источника данных и с тех пор был уже изменен
            Deleted   // объект удалён из источника данных, но пока ещё существует в памяти
        }

        public class StateEventArgs : EventArgs
        {
            private PersistenceState old_state;
            private PersistenceState new_state;
            
            private StateEventArgs() { }

            public StateEventArgs(PersistenceState old_state, PersistenceState new_state)
            {
                this.old_state = old_state;
                this.new_state = new_state;
            }

            public PersistenceState OldState { get { return old_state; } }
            public PersistenceState NewState { get { return new_state; } }
        }

        public delegate void StateChangingEventHandler<TKey>(IPersistent<TKey> sender, StateEventArgs args) where TKey : ISerializable, new();

        public delegate void StateChangedEventHandler<TKey>(IPersistent<TKey> sender, StateEventArgs args) where TKey : ISerializable, new();

        public interface ISerializable
        {
            void Serialize(BinaryWriter stream);
            void Deserialize(BinaryReader stream);
        }

        public interface IActiveRecord
        {
            void Save();
            void Kill();
            void Load();
        }

        public interface IPersistent<TKey> :  ISerializable, IActiveRecord
            where TKey : ISerializable, new()
        {
            TKey Key { get; }
            PersistenceState State { get; }
            event StateChangedEventHandler<TKey> StateChanged;
            event StateChangingEventHandler<TKey> StateChanging;
        }
    }
}

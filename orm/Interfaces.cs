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
            Original, // объект загружен из источника данных и ещё ни разу с тех пор не изменялся
            Changed,  // объект загружен из источника данных и с тех пор был уже изменен
            Deleted,  // объект удалён из источника данных, но пока ещё существует в памяти
            Loading   // объект в данный момент загружается из источника данных, это состояние
                      // необходимо исключительно для случаев когда data mapper загружает из базы
                      // данных объект, находящийсяв состоянии Virtual, чтобы иметь возможность
                      // загружать значения свойств объекта обращаясь к ним напрямую и косвенно
                      // вызывая метод Persistent.Set() - без этого состояния подобная стратегия
                      // вызывает циклический вызов методов Persistent.Set(), Persistent.LazyLoad(),
                      // IPersistent.Load(), IDataMapper.Select() и далее по кругу.
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

        public delegate void StateChangingEventHandler(object sender, StateEventArgs args);

        public delegate void StateChangedEventHandler(object sender, StateEventArgs args);

        public interface ISerializable
        {
            void Serialize(BinaryWriter stream);
            void Deserialize(BinaryReader stream);
        }

        public interface IPersistent<TKey> : ISerializable
        {
            TKey Key { get; }
            PersistenceState State { get; }
            event StateChangedEventHandler StateChanged;
            event StateChangingEventHandler StateChanging;
            void Save();
            void Kill();
            void Load();
        }

        public interface IDataMapper
        {
            void Insert(ISerializable entity);
            void Select(ISerializable entity);
            void Update(ISerializable entity);
            void Delete(ISerializable entity);
        }

        public interface IUserTypeFactory
        {
            object New(Type type);
            object New(Type type, object key);
            object New(Type type, PersistenceState state);
            object New(Type type, object key, PersistenceState state);
        }
    }
}

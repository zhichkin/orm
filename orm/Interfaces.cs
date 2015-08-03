using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        public interface ISerializable
        {
            void Serialize(BinaryWriter stream);
            void Deserialize(BinaryReader stream);
        }

        public interface IActiveRecord : ISerializable
        {
            void Save();
            void Kill();
            void Load();
        }

        public interface IPersistent<TKey> : IActiveRecord
        {
            TKey Key { get; }
            PersistenceState State { get; }
            event StateChangedEventHandler StateChanged;
            event StateChangingEventHandler StateChanging;
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
            object New();
            object New(object key);
            object New(PersistenceState state);
            object New(object key, PersistenceState state);
        }
    }
}

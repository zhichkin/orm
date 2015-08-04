using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Transactions;
using System.ComponentModel;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        public abstract partial class Entity : Persistent<Guid>
        {
            protected byte[] version = new byte[8];

            public event EntitySaveEventHandler OnSave;
            public event EntityKillEventHandler OnKill;
            public event EntityLoadEventHandler OnLoad;

            private void FireSaveEvent()
            {
                if (OnSave != null) OnSave(this);
            }
            private void FireKillEvent()
            {
                if (OnKill != null) OnKill(this);
            }
            private void FireLoadEvent()
            {
                if (OnLoad != null) OnLoad(this);
            }

            public override void Save()
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    base.Save();
                    FireSaveEvent();
                    scope.Complete();
                }
            }
            public override void Kill()
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    FireKillEvent();
                    base.Kill();
                    scope.Complete();
                }
            }
            public override void Load()
            {
                base.Load();
                FireLoadEvent();
            }

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
        }
    }
}
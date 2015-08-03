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

            public event EntitySavingEventHandler  Saving;
            public event EntitySavedEventHandler   Saved;
            public event EntityKillingEventHandler Killing;
            public event EntityKilledEventHandler  Killed;
            private void OnSaving()
            {
                if (Saving != null) Saving(this);
            }
            private void OnSaved()
            {
                if (Saved != null) Saved(this);
            }
            private void OnKilling()
            {
                if (Killing != null) Killing(this);
            }
            private void OnKilled()
            {
                if (Killed != null) Killed(this);
            }

            public override void Save()
            {
                OnSaving();
                using (TransactionScope scope = new TransactionScope())
                {
                    base.Save();
                    OnSaved();
                    scope.Complete();
                }
            }

            public override void Kill()
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    OnKilling();
                    base.Kill();
                    scope.Complete();
                }
                OnKilled();
            }
        }
    }
}
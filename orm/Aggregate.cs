using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

namespace zhichkin
{
    namespace orm
    {
        public sealed class Aggregate<TOwner, TItem> : IEnumerable<TItem>
            where TOwner : Entity, new()
            where TItem  : class, new()
        {
            private readonly TOwner owner;
            private readonly string fk_name;
            private static Action<TItem, TOwner> set_item_owner;

            public Aggregate(TOwner owner, string fk_name)
            {
                this.owner = owner;
                this.fk_name = fk_name;
                GenerateSetItemOwnerMethod();
            }

            private PersistenceState state = PersistenceState.Virtual;

            private List<TItem> insert = new List<TItem>();
            private List<TItem> select = new List<TItem>();
            private List<TItem> update = new List<TItem>();
            private List<TItem> delete = new List<TItem>();

            private void GenerateSetItemOwnerMethod()
            {
                if (set_item_owner != null) return;

                PropertyInfo info = typeof(TItem).GetProperty(fk_name);
                DynamicMethod setter = new DynamicMethod(
                        typeof(TOwner).Name + typeof(TItem).Name + "ItemOwnerSetter",
                        null,
                        new Type[] { typeof(TItem), typeof(TOwner) });
                ILGenerator il = setter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, info.GetSetMethod());
                il.Emit(OpCodes.Ret);
                set_item_owner = (Action<TItem, TOwner>)setter.CreateDelegate(typeof(Action<TItem, TOwner>));
            }

            private void LazyLoad()
            {
                if (state == PersistenceState.Virtual)
                {
                    // TODO: select dependent items
                }
            }

            public TItem Add()
            {
                LazyLoad();
                TItem item = Context.Current.New<TItem>();
                set_item_owner(item, owner);
                insert.Add(item);
                return item;
            }

            public void Remove(TItem item)
            {
                select.Remove(item);
                //if(item.State)
            }

            public IEnumerator<TItem> GetEnumerator()
            {
                LazyLoad();
                return insert.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

﻿using System;
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
        public partial class Persistent<TKey>
        {
            public sealed class Relation<TOwner, TItem> : IEnumerable<TItem>
                where TOwner : Entity, new()
                where TItem : IPersistent, new()
            {
                private readonly TOwner owner;
                private readonly string fk_name;
                private readonly Func<TOwner, List<TItem>> load_items;
                private static Action<TItem, TOwner> set_item_owner;

                public Relation(TOwner owner, string fk_name, Func<TOwner, List<TItem>> items_loader)
                {
                    this.owner = owner;
                    this.fk_name = fk_name;
                    this.load_items = items_loader;
                    if (!string.IsNullOrEmpty(fk_name))
                    {
                        GenerateSetItemOwnerMethod();
                    }
                    owner.OnSave += Save;
                    owner.OnKill += Kill;
                }

                private PersistenceState state = PersistenceState.Virtual;

                private List<TItem> items = new List<TItem>();
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
                        Load();
                        state = PersistenceState.Original;
                    }
                }

                public void Load()
                {
                     items = load_items(owner);
                }

                private void Save(Entity sender)
                {
                    if (state == PersistenceState.Virtual) return;
                    foreach (TItem item in items)
                    {
                        item.Save();
                    }
                    foreach (TItem item in delete)
                    {
                        item.Kill();
                    }
                }

                private void Kill(Entity sender)
                {
                    Clear();                   
                    foreach (TItem item in delete)
                    {
                        item.Kill();
                    }
                }

                public TItem Add()
                {
                    LazyLoad();
                    TItem item = Context.Current.New<TItem>();
                    if (!string.IsNullOrEmpty(fk_name))
                    {
                        set_item_owner(item, owner);
                    }
                    items.Add(item);
                    return item;
                }

                public void Remove(TItem item)
                {
                    LazyLoad();
                    items.Remove(item);
                    delete.Add(item);
                }

                public void Clear()
                {
                    LazyLoad();
                    delete.AddRange(items);
                    items.Clear();
                }

                public List<TItem> Deleted
                {
                    get
                    {
                        return delete;
                    }
                }

                public IEnumerator<TItem> GetEnumerator()
                {
                    LazyLoad();
                    return items.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq.Expressions;

namespace zhichkin
{
    namespace orm
    {
        public sealed class Aggregate<TOwner, TItem>
            where TOwner : class, new()
            where TItem  : class, new()
        {
            private readonly TOwner owner;
            private Action<TItem, TOwner> set_owner;

            public Aggregate(TOwner owner)
            {
                this.owner = owner;
                GenerateSetOwnerMethod();
            }

            private PersistenceState state = PersistenceState.Virtual;

            private List<TItem> insert = new List<TItem>();
            private List<TItem> select = new List<TItem>();
            private List<TItem> update = new List<TItem>();
            private List<TItem> delete = new List<TItem>();

            private void GenerateSetOwnerMethod()
            {
                PropertyInfo[] properties = typeof(TItem).GetProperties();
                foreach (PropertyInfo info in properties)
                {
                    if (info.GetCustomAttribute<AggregateAttribute>() != null && info.PropertyType == typeof(TOwner))
                    {
                        ParameterExpression parameter = Expression.Parameter(typeof(TOwner), "owner");
                        set_owner = Expression.Lambda<Action<TItem, TOwner>>(
                            Expression.Call(info.GetSetMethod(), parameter)).Compile();
                    }
                }
            }

            public TItem Add()
            {
                TItem item = Context.Current.New<TItem>();
                set_owner(item, owner);
                insert.Add(item);
                return item;
            }

            public void Remove(TItem item)
            {
                select.Remove(item);
                //if(item.State)
            }
        }
    }
}

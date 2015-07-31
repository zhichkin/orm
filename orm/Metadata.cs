using System;

namespace zhichkin
{
    namespace orm
    {
        [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
        public sealed class DomainModelAttribute : Attribute
        {
            private string name = string.Empty;

            public DomainModelAttribute(string name) { this.name = name; }

            public string Name { get { return name; } }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class DiscriminatorAttribute : Attribute
        {
            private int discriminator = 0;

            public DiscriminatorAttribute(int value) { discriminator = value; }

            public int Discriminator { get { return discriminator; } }
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        public sealed class AggregateAttribute : Attribute
        {
            public AggregateAttribute() { }
        }
    }
}
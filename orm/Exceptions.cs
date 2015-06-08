using System;

namespace zhichkin
{
    namespace orm
    {
        public sealed class UnknownTypeException : ApplicationException
        {
            public UnknownTypeException(string type_name) : base(type_name) { }
        }

        public class ReferenceIntegrityException : Exception { public ReferenceIntegrityException(string message) : base(message) { } }

        public class OptimisticConcurencyException : Exception { public OptimisticConcurencyException(string message) : base(message) { } }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class DynamicMember
    {
        // GetOrAdd может многократно вызвать valueFactory но вернется всегда один экземпляр.
        // Lazy гарантирует что TypeContract будет создан 1 раз для определенного Type.
        private static readonly ConcurrentDictionary<Type, DynamicMember> _dict = new ConcurrentDictionary<Type, DynamicMember>();
        private readonly Lazy<TypeContract> _lazy;
        private readonly Type _type;

        private DynamicMember(Type type)
        {
            _type = type;
            _lazy = new Lazy<TypeContract>(LazyValueFactory, true);
        }

        public static TypeContract GetTypeContract(Type type)
        {
            DynamicMember lazyContract = _dict.GetOrAdd(type, ValueFactory);
            return lazyContract._lazy.Value;
        }

        private static DynamicMember ValueFactory(Type type)
        {
            return new DynamicMember(type);
        }

        // гарантированно вызывается только 1 раз для определенного type.
        private TypeContract LazyValueFactory()
        {
            return new TypeContract(_type);
        }
    }
}

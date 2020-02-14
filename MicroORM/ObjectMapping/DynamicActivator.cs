using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class DynamicActivator
    {
        private static readonly ConcurrentDictionary<Type, DynamicActivator> _dict = new ConcurrentDictionary<Type, DynamicActivator>();
        private readonly Lazy<ContractActivator> _lazy;
        private readonly Type _type;
        private readonly bool _anonimousType;

        private DynamicActivator(Type type, bool anonimousType)
        {
            _type = type;
            _anonimousType = anonimousType;
            _lazy = new Lazy<ContractActivator>(LazyValueFactory, true);
        }

        public static ContractActivator GetActivator(Type type)
        {
            DynamicActivator lazy = _dict.GetOrAdd(type, ValueFactory);
            return lazy._lazy.Value;
        }

        public static ContractActivator GetAnonimousActivator(Type type)
        {
            DynamicActivator lazy = _dict.GetOrAdd(type, ValueFactoryAnonimous);
            return lazy._lazy.Value;
        }

        private static DynamicActivator ValueFactory(Type type)
        {
            return new DynamicActivator(type, false);
        }

        private static DynamicActivator ValueFactoryAnonimous(Type type)
        {
            return new DynamicActivator(type, true);
        }

        [DebuggerStepThrough]
        private ContractActivator LazyValueFactory()
        {
            return new ContractActivator(_type, _anonimousType);
        }
    }
}

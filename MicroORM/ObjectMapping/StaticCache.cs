using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal static class StaticCache
    {
        /// <summary>
        /// Хранит свойства и поля каждого типа.
        /// </summary>
        public static readonly ConcurrentDictionary<(Type, string), OrmLazyProperty> TypesProperties = new();
        public static readonly ConcurrentDictionary<Type, Lazy<ContractActivator>> LazyTypesContracts = new();
        public static readonly ConcurrentDictionary<Type, TypeConverter> TypeConverters = new();

        /// <summary>
        /// Инициализирует контракт для типа <paramref name="type"/> из ленивой фабрики.
        /// </summary>
        public static ContractActivator FromLazyActivator(Type type)
        {
            // GetOrAdd может одновременно создать 2 экземпляра но потоки всегда получат одинаковый экземпляр.
            var lazy = LazyTypesContracts.GetOrAdd(type, static type =>
            {
                return new Lazy<ContractActivator>(() => new ContractActivator(type, anonymousType: false), LazyThreadSafetyMode.ExecutionAndPublication);
            });

            return lazy.Value;
        }

        /// <summary>
        /// Инициализирует контракт для типа <paramref name="type"/> из ленивой фабрики.
        /// </summary>
        public static ContractActivator FromLazyAnonimousActivator(Type type)
        {
            var lazy = LazyTypesContracts.GetOrAdd(type, static type =>
            {
                return new Lazy<ContractActivator>(() => new ContractActivator(type, anonymousType: true), LazyThreadSafetyMode.ExecutionAndPublication);
            });

            return lazy.Value;
        }
    }
}

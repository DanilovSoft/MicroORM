using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    /// <summary>
    /// Создание данного экземпляра должно выполняться синхронизированно.
    /// </summary>
    [DebuggerDisplay(@"\{Контракт типа {Contract.ContractType.Name}\}")]
    internal sealed class ContractActivator
    {
        private readonly Func<object>? _activator;
        private readonly Func<object[], object>? _anonimousActivator;
        public readonly OnDeserializingDelegate? OnDeserializingHandle;
        public readonly OnDeserializedDelegate? OnDeserializedHandle;
        public readonly Dictionary<string, ConstructorArgument>? ConstructorArguments;
        public readonly bool IsReadonlyStruct;
        public TypeContract Contract { get; }

        // ctor.
        // Мы защищены от одновременного создания с помощью Lazy.ExecutionAndPublication.
        public ContractActivator(Type type, bool anonimousType)
        {
            if(!anonimousType)
            {
                // Тип может быть readonly структурой, определить можно только перебором всех свойст и полей.
                IsReadonlyStruct = GetIsReadonlyStruct(type);

                if (!IsReadonlyStruct)
                {
                    _activator = DynamicReflectionDelegateFactory.Instance.CreateDefaultConstructor<object>(type);
                    OnDeserializingHandle = null;

                    MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (method.IsDefined(typeof(OnDeserializingAttribute), false))
                        {
                            OnDeserializingHandle = DynamicReflectionDelegateFactory.Instance.CreateOnDeserializingMethodCall(method, type);
                        }

                        if (method.IsDefined(typeof(OnDeserializedAttribute), false))
                        {
                            OnDeserializedHandle = DynamicReflectionDelegateFactory.Instance.CreateOnDeserializedMethodCall(method, type);
                        }
                    }
                }
                else
                // readonly структура.
                {
                    ConstructorInfo[] ctors = type.GetConstructors();
                    Debug.Assert(ctors.Length > 0, "Не найден открытый конструктор");
                    if (ctors.Length == 0)
                        throw new MicroOrmException("Не найден открытый конструктор");

                    ConstructorInfo ctor = ctors[0];
                    _anonimousActivator = DynamicReflectionDelegateFactory.Instance.CreateConstructor(type, ctor);

                    ConstructorArguments = ctor.GetParameters()
                        .Select((x, Index) => new { ParameterInfo = x, Index })
                        .ToDictionary(x => x.ParameterInfo.Name, x => new ConstructorArgument(x.Index, x.ParameterInfo.ParameterType));
                }
            }
            else
            {
                _anonimousActivator = DynamicReflectionDelegateFactory.Instance.CreateAnonimousConstructor(type);

                // поля у анонимных типов не рассматриваются.
                // берем только свойства по умолчанию.
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // порядок полей такой же как у конструктора.
                ConstructorArguments = properties
                    .Select((x, Index) => new { PropertyInfo = x, Index })
                    .ToDictionary(x => x.PropertyInfo.Name, x => new ConstructorArgument(x.Index, x.PropertyInfo.PropertyType));
            }

            Contract = new TypeContract(type);
        }

        private static bool GetIsReadonlyStruct(Type type)
        {
            if (!type.IsValueType)
                return false;

            // Поля должны быть IsInitOnly а свойства должны быть без сеттера.
            // Проверяем и публичные и приватные поля/свойства.

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            var members = ReflectionUtils.GetFieldsAndProperties(type, flags).Where(x => !ReflectionUtils.IsIndexedProperty(x));
            foreach (var m in members)
            {
                if (!m.IsDefined(typeof(CompilerGeneratedAttribute)))
                {
                    switch (m)
                    {
                        case PropertyInfo p:
                            {
                                if (!p.IsSpecialName)
                                {
                                    if (p.GetSetMethod(nonPublic: true) != null)
                                    {
                                        return false;
                                    }
                                }
                            }
                            break;
                        case FieldInfo f:
                            {
                                if (!f.IsSpecialName)
                                {
                                    if (!f.IsInitOnly)
                                    {
                                        return false;
                                    }
                                }
                            }
                            break;
                        default: throw new InvalidOperationException();
                    }
                }
            }
            return true;
        }

        public object CreateInstance(object[] args)
        {
            return _anonimousActivator.Invoke(args);
        }

        public object CreateReadonlyInstance(object[] args)
        {
            return _anonimousActivator.Invoke(args);
        }

        public object CreateInstance()
        {
            return _activator.Invoke();
        }

        ///// <summary>
        ///// Инициализирует активатор из ленивой фабрики.
        ///// </summary>
        ///// <returns></returns>
        //public TypeContract FromLazyTypeContract()
        //{
        //    return LazyTypeContract.GetTypeContract(_type);
        //}
    }
}

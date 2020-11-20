using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        /// <summary>
        /// Создаёт объект через пустой конструктор.
        /// </summary>
        private readonly Func<object>? _emptyCtorActivator;
        /// <summary>
        /// Создаёт объект через параметризованный конструктор.
        /// </summary>
        private readonly Func<object?[], object>? _ctorActivator;
        public readonly OnDeserializingDelegate? OnDeserializingHandle;
        public readonly OnDeserializedDelegate? OnDeserializedHandle;
        /// <summary>
        /// Для маппинга значений для конструктора по именам параметров.
        /// </summary>
        public readonly Dictionary<string, ConstructorArgument>? ConstructorArguments;
        public readonly bool IsEmptyCtor;
        public TypeContract Contract { get; }

        // ctor.
        // Мы защищены от одновременного создания с помощью Lazy.ExecutionAndPublication.
        public ContractActivator(Type type, bool anonimousType)
        {
            if (!anonimousType)
            {
                // Тип может быть readonly структурой, определить можно только перебором всех свойств и полей.
                bool isReadonlyStruct = GetIsReadonlyStruct(type);

                if (isReadonlyStruct)
                {
                    if (SingleNonEmptyCtor(type, out var singleCtor))
                    {
                        IsEmptyCtor = false;

                        _ctorActivator = DynamicReflectionDelegateFactory.CreateConstructor(type, singleCtor);

                        ConstructorArguments = singleCtor.GetParameters()
                            .Select((x, Index) => new { ParameterInfo = x, Index })
                            .ToDictionary(x => x.ParameterInfo.Name!, x => new ConstructorArgument(x.Index, x.ParameterInfo.ParameterType));
                    }
                    else
                    {
                        IsEmptyCtor = true;

                        _emptyCtorActivator = DynamicReflectionDelegateFactory.CreateDefaultConstructor<object>(type);

                        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        for (int i = 0; i < methods.Length; i++)
                        {
                            MethodInfo method = methods[i];
                            if (method.IsDefined(typeof(OnDeserializingAttribute), false))
                            {
                                OnDeserializingHandle = DynamicReflectionDelegateFactory.CreateOnDeserializingMethodCall(method, type);
                            }

                            if (method.IsDefined(typeof(OnDeserializedAttribute), false))
                            {
                                OnDeserializedHandle = DynamicReflectionDelegateFactory.CreateOnDeserializedMethodCall(method, type);
                            }
                        }
                    }
                }
                else
                // readonly структура.
                {
                    // Хоть у структуры и есть пустой конструктор, нам он не подходит.
                    IsEmptyCtor = false;

                    ConstructorInfo[] ctors = type.GetConstructors();
                    Debug.Assert(ctors.Length > 0, "Не найден открытый конструктор");
                    if (ctors.Length != 0)
                    {
                        ConstructorInfo ctor = ctors[0];
                        _ctorActivator = DynamicReflectionDelegateFactory.CreateConstructor(type, ctor);

                        ConstructorArguments = ctor.GetParameters()
                            .Select((x, Index) => new { ParameterInfo = x, Index })
                            .ToDictionary(x => x.ParameterInfo.Name!, x => new ConstructorArgument(x.Index, x.ParameterInfo.ParameterType));
                    }
                    else
                        throw new MicroOrmException("Не найден открытый конструктор");
                }
            }
            else
            {
                _ctorActivator = DynamicReflectionDelegateFactory.CreateAnonimousConstructor(type);

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

        private static bool SingleNonEmptyCtor(Type type, [NotNullWhen(true)] out ConstructorInfo? ctor)
        {
            ConstructorInfo[] ctors = type.GetConstructors();
            if (ctors.Length == 1 && ctors[0].GetParameters().Length > 0)
            {
                ctor = ctors[0];
                return true;
            }
            else
            {
                ctor = null;
                return false;
            }
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
                        default: 
                            throw new InvalidOperationException();
                    }
                }
            }
            
            return true;
        }

        public object CreateInstance(object?[] args)
        {
            Debug.Assert(_ctorActivator != null);

            return _ctorActivator.Invoke(args);
        }

        //public object CreateReadonlyInstance(object?[] args)
        //{
        //    Debug.Assert(_ctorActivator != null);

        //    return _ctorActivator.Invoke(args);
        //}

        public object CreateInstance()
        {
            Debug.Assert(_emptyCtorActivator != null);

            return _emptyCtorActivator.Invoke();
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

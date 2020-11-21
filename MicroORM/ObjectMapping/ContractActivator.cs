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
        private readonly Func<object?[], object>? _anonymousCtorActivator;
        public readonly OnDeserializingDelegate? OnDeserializingHandle;
        public readonly OnDeserializedDelegate? OnDeserializedHandle;
        /// <summary>
        /// Для маппинга параметров конструктора по именам.
        /// </summary>
        public readonly Dictionary<string, ConstructorArgument>? ConstructorArguments;
        public readonly bool IsEmptyCtor;
        public TypeContract Contract { get; }

        // ctor.
        // Мы защищены от одновременного создания с помощью Lazy.ExecutionAndPublication.
        public ContractActivator(Type type, bool anonymousType)
        {
            if (!anonymousType)
            {
                // Тип может быть readonly структурой, определить можно только перебором всех свойств и полей.
                bool isReadonlyStruct = GetIsReadonlyStruct(type);

                if (isReadonlyStruct)
                {
                    // Хоть у структуры и есть пустой конструктор, нам он не подходит.
                    IsEmptyCtor = false;

                    ConstructorInfo[] ctors = type.GetConstructors();
                    Debug.Assert(ctors.Length > 0, "У типа должны быть открытые конструкторы");

                    if (ctors.Length != 0)
                    {
                        ConstructorInfo ctor = ctors[0];
                        _anonymousCtorActivator = DynamicReflectionDelegateFactory.CreateConstructor(type, ctor);

                        ConstructorArguments = CreateConstructorArguments(ctor);
                    }
                    else
                        throw new MicroOrmException("Не найден открытый конструктор");
                }
                else
                // Не анонимный class
                {
                    if (SingleNonEmptyCtor(type, out var singleCtor))
                    {
                        IsEmptyCtor = false;
                        _anonymousCtorActivator = DynamicReflectionDelegateFactory.CreateConstructor(type, singleCtor);

                        ConstructorArguments = CreateConstructorArguments(singleCtor);
                    }
                    else
                    {
                        // Хоть у структуры и есть пустой конструктор, нам он не подходит.
                        IsEmptyCtor = true;
                        _emptyCtorActivator = DynamicReflectionDelegateFactory.CreateDefaultConstructor<object>(type);
                    }

                    InitializeStreamingMethods(type, out OnDeserializingHandle, out OnDeserializedHandle);

                    //IsEmptyCtor = true;
                    //_emptyCtorActivator = DynamicReflectionDelegateFactory.CreateDefaultConstructor<object>(type);
                }
            }
            else
            {
                _anonymousCtorActivator = DynamicReflectionDelegateFactory.CreateAnonimousConstructor(type);

                // поля у анонимных типов не рассматриваются.
                // берем только свойства по умолчанию.
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // порядок полей такой же как у конструктора.
                ConstructorArguments = CreateConstructorArguments(properties);
            }
            Contract = new TypeContract(type);
        }

        private static Dictionary<string, ConstructorArgument> CreateConstructorArguments(ConstructorInfo ctor)
        {
            return ctor.GetParameters()
                .Select((x, Index) => new { ParameterInfo = x, Index })
                .ToDictionary(x => x.ParameterInfo.Name!, x => new ConstructorArgument(x.Index, x.ParameterInfo));
        }

        private static Dictionary<string, ConstructorArgument> CreateConstructorArguments(PropertyInfo[] properties)
        {
            return properties
                .Select((x, Index) => new { PropertyInfo = x, Index })
                .ToDictionary(x => x.PropertyInfo.Name, x => new ConstructorArgument(x.Index, x.PropertyInfo));
        }

        private static void InitializeStreamingMethods(Type type, out OnDeserializingDelegate? onDeserializingHandle, out OnDeserializedDelegate? onDeserializedHandle)
        {
            onDeserializingHandle = null;
            onDeserializedHandle = null;

            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.IsDefined(typeof(OnDeserializingAttribute), false))
                {
                    onDeserializingHandle = DynamicReflectionDelegateFactory.CreateOnDeserializingMethodCall(method, type);
                }

                if (method.IsDefined(typeof(OnDeserializedAttribute), false))
                {
                    onDeserializedHandle = DynamicReflectionDelegateFactory.CreateOnDeserializedMethodCall(method, type);
                }
            }
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

        /// <returns><see langword="true"/> если <paramref name="type"/> является <see langword="readonly struct"/></returns>
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
            Debug.Assert(_anonymousCtorActivator != null);

            return _anonymousCtorActivator.Invoke(args);
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

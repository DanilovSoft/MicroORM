using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class ContractActivator
    {
        private readonly Func<object> _activator;
        private readonly Func<object[], object> _anonimousActivator;
        //private readonly Func<object[], object> _readonlyStructActivator;
        public readonly OnDeserializingDelegate OnDeserializingHandle;
        public readonly OnDeserializedDelegate OnDeserializedHandle;
        public readonly Dictionary<string, AnonimousProperty> AnonimousProperties;
        private readonly Type _type;

        // ctor.
        public ContractActivator(Type type, bool anonimouseType)
        {
            _type = type;

            if(!anonimouseType)
            {
                //// Тип может быть readonly структурой, определить можно только перебором всех свойст и полей — они должны быть IsInitOnly.
                //if(type.IsValueType)
                //{
                //    //IsReadonly(type);
                //}

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
            {
                _anonimousActivator = DynamicReflectionDelegateFactory.Instance.CreateAnonimousConstructor(type);

                // поля у анонимных типов не рассматриваются.
                // берем только свойства по умолчанию.
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // порядок полей такой же как у конструктора.
                AnonimousProperties = properties
                    .Select((x, Index) => new { PropertyInfo = x, Index })
                    .ToDictionary(x => x.PropertyInfo.Name, x => new AnonimousProperty(x.Index, x.PropertyInfo.PropertyType));
            }
        }

        //private static void IsReadonly(Type type)
        //{
        //    var mem = type.GetMembers();
        //    var p = (PropertyInfo)mem[0];
        //    var f = (FieldInfo)mem[0];
        //    //f.IsInitOnly
        //}

        public object CreateAnonimousInstance(object[] args)
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

        public TypeContract GetTypeContract()
        {
            return DynamicMember.GetTypeContract(_type);
        }
    }
}

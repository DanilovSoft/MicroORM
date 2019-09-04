using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class TypeContract
    {
        private static readonly BindingFlags DefaultMembersSearchFlags = BindingFlags.Instance | BindingFlags.Public;
        private static readonly ConcurrentDictionary<TypeMember, OrmLazyProperty> _dict = new ConcurrentDictionary<TypeMember, OrmLazyProperty>();
        private readonly Type _type;

        // Конструктор определенного type синхронизирован. (Потокобезопасно для каждого Type).
        public TypeContract(Type type)
        {
            _type = type;

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            var allMembers = ReflectionUtils.GetFieldsAndProperties(type, bindingFlags).Where(x => !ReflectionUtils.IsIndexedProperty(x));
            var defaultMembers = ReflectionUtils.GetFieldsAndProperties(type, DefaultMembersSearchFlags).Where(x => !ReflectionUtils.IsIndexedProperty(x)).ToList();

            foreach (MemberInfo member in allMembers)
            {
                // пропустить свойства которые невозможно записать.
                if (member is PropertyInfo p)
                {
                    // CanWrite returns true if the property has a set accessor, even if the accessor is private, 
                    // internal (or Friend in Visual Basic), or protected. If the property does not have a set accessor, the method returns false.
                    if (!p.CanWrite)
                    {
                        continue;
                    }
                }

                if(member.IsDefined(typeof(CompilerGeneratedAttribute)) || member.IsDefined(typeof(SqlIgnoreAttribute)))
                {
                    continue;
                }

                // аттрибуты собственного типа сильнее DataMember атрибутов.

                bool canIgnoreMember = true;
                string propName = member.Name;

                var sqlPropAttr = member.GetCustomAttribute<SqlPropertyAttribute>();
                if (sqlPropAttr != null)
                {
                    // если есть атрибут SqlProperty то это свойство игнорировать нельзя.
                    canIgnoreMember = false;

                    if (sqlPropAttr.Name != null)
                        propName = sqlPropAttr.Name;
                }
                else
                {
                    if (member.IsDefined(typeof(IgnoreDataMemberAttribute)))
                    {
                        continue;
                    }

                    var dataMember = member.GetCustomAttribute<DataMemberAttribute>();
                    if (dataMember != null)
                    {
                        // если есть атрибут DataMember то это свойство игнорировать нельзя.
                        canIgnoreMember = false;

                        if (dataMember.Name != null)
                            propName = dataMember.Name;
                    }
                }

                if(canIgnoreMember)
                {
                    if (!defaultMembers.Contains(member))
                    {  // свойство скорее всего приватное.

                        continue;
                    }
                }

                if (!_dict.TryAdd(new TypeMember(type, propName), new OrmLazyProperty(member)))
                {
                    // если не удалось добавить, причина может быть только одна — в словаре уже есть такой ключ.
                    throw new MicroOrmSerializationException($"A member with the name '{propName}' already exists on '{type}'. Use the {nameof(SqlPropertyAttribute)} to specify another name.");
                }
            }
        }

        /// <summary>
        /// Этот метод должен быть потокобезопасен.
        /// </summary>
        public bool TryGetOrmProperty(string memberName, out OrmProperty ormProperty)
        {
            if(_dict.TryGetValue(new TypeMember(_type, memberName), out var lazyOrmProperty))
            {
                ormProperty = lazyOrmProperty.Value;
                return true;
            }
            else
            {
                ormProperty = null;
                return false;
            }
        }
    }
}

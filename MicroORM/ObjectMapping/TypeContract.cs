using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    [DebuggerDisplay(@"\{Контракт для типа {ContractType.Name}\}")]
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct TypeContract
    {
        public readonly Type ContractType;

        // Конструктор определенного Type синхронизирован. (Потокобезопасно для каждого Type).
        public TypeContract(Type type)
        {
            ContractType = type;

            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            IEnumerable<MemberInfo> allMembers = ReflectionUtils.GetFieldsAndProperties(type, bindingFlags).Where(x => !ReflectionUtils.IsIndexedProperty(x));
            const BindingFlags DefaultMembersSearchFlags = BindingFlags.Instance | BindingFlags.Public;
            var defaultMembers = ReflectionUtils.GetFieldsAndProperties(type, DefaultMembersSearchFlags).Where(x => !ReflectionUtils.IsIndexedProperty(x)).ToHashSet();

            // Свойства и поля.
            foreach (MemberInfo member in allMembers)
            {
                if (member.IsDefined(typeof(CompilerGeneratedAttribute)) || member.IsDefined(typeof(SqlIgnoreAttribute)))
                {
                    continue;
                }

                // Свои аттрибуты приоритетнее DataMember атрибутов.

                bool canIgnoreMember = true;
                string propName = member.Name;

                var sqlPropAttr = member.GetCustomAttribute<SqlPropertyAttribute>();
                if (sqlPropAttr != null)
                // Есть атрибут SqlProperty — это свойство игнорировать нельзя.
                {
                    canIgnoreMember = false;

                    if (sqlPropAttr.Name != null)
                        propName = sqlPropAttr.Name;
                }
                else
                {
                    // Проверять этот атрибут нужно после SqlPropertyAttribute.
                    if (!member.IsDefined(typeof(IgnoreDataMemberAttribute)))
                    {
                        if (member.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute columnAttrib)
                        {
                            canIgnoreMember = false;

                            if (columnAttrib.Name != null)
                                propName = columnAttrib.Name;
                        }
                        else
                        {
                            var dataMember = member.GetCustomAttribute<DataMemberAttribute>();
                            if (dataMember != null)
                            {
                                // если есть атрибут DataMember то это свойство игнорировать нельзя.
                                canIgnoreMember = false;

                                if (dataMember.Name != null)
                                    propName = dataMember.Name;
                            }
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                if (canIgnoreMember)
                {
                    if (!defaultMembers.Contains(member))
                    {  
                        // свойство скорее всего приватное.
                        continue;
                    }
                }

                if (!StaticCache.TypesProperties.TryAdd((type, propName), new OrmLazyProperty(member)))
                {
                    // если не удалось добавить, причина может быть только одна — в словаре уже есть такой ключ.
                    throw new MicroOrmSerializationException($"A member with the name '{propName}' already exists on '{type}'. Use the {nameof(SqlPropertyAttribute)} to specify another name.");
                }
            }
        }

        /// <summary>
        /// Инициирует ленивое свойство при первом обращении.
        /// Этот метод потокобезопасен.
        /// </summary>
        public bool TryGetOrmPropertyFromLazy(string memberName, [NotNullWhen(true)] out OrmProperty? ormProperty)
        {
            if (StaticCache.TypesProperties.TryGetValue((ContractType, memberName), out OrmLazyProperty? lazyOrmProperty))
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

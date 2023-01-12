using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace DanilovSoft.MicroORM.ObjectMapping;

[DebuggerDisplay(@"\{Контракт для типа {ContractType.Name}\}")]
[StructLayout(LayoutKind.Auto)]
internal readonly struct TypeContract
{
    public readonly Type ContractType;

    // Конструктор определенного Type синхронизирован. (Потокобезопасно для каждого Type).
    public TypeContract(Type dboType)
    {
        ContractType = dboType;

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var allMembers = ReflectionUtils
            .GetFieldsAndProperties(dboType, bindingFlags)
            .Where(x => !ReflectionUtils.IsIndexedProperty(x));

        const BindingFlags DefaultMembersSearchFlags = BindingFlags.Instance | BindingFlags.Public;

        var defaultMembers = ReflectionUtils
            .GetFieldsAndProperties(dboType, DefaultMembersSearchFlags)
            .Where(x => !ReflectionUtils.IsIndexedProperty(x))
            .ToHashSet();

        // Свойства и поля.
        foreach (var memberInfo in allMembers)
        {
            if (memberInfo.IsDefined(typeof(CompilerGeneratedAttribute))
                || memberInfo.IsDefined(typeof(SqlIgnoreAttribute))
                || memberInfo.IsDefined(typeof(NotMappedAttribute)))
            {
                continue;
            }

            // Свои аттрибуты приоритетнее DataMember атрибутов.

            var canIgnoreMember = true;
            var memberName = memberInfo.Name;

            if (memberInfo.GetCustomAttribute<SqlPropertyAttribute>() is SqlPropertyAttribute sqlPropAttr)
            // Есть атрибут SqlProperty — это свойство игнорировать нельзя.
            {
                // Только для своего атрибута мы должны выполнить такую проверку.
                if (memberInfo is PropertyInfo propertyInfo && propertyInfo.SetMethod == null)
                {
                    throw new MicroOrmException($"Property '{propertyInfo.Name}' is not writable.");
                }

                canIgnoreMember = false;

                if (sqlPropAttr.Name != null)
                {
                    memberName = sqlPropAttr.Name;
                }
            }
            else
            {
                // Проверять этот атрибут нужно после SqlPropertyAttribute.
                if (memberInfo.IsDefined(typeof(IgnoreDataMemberAttribute)))
                {
                    continue;
                }
                else
                {
                    if (memberInfo.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute columnAttrib)
                    {
                        canIgnoreMember = false;

                        if (columnAttrib.Name != null)
                        {
                            memberName = columnAttrib.Name;
                        }
                    }
                    else
                    {
                        if (memberInfo.GetCustomAttribute<DataMemberAttribute>() is DataMemberAttribute dataMember)
                        {
                            // если есть атрибут DataMember то это свойство игнорировать нельзя.
                            canIgnoreMember = false;

                            if (dataMember.Name != null)
                            {
                                memberName = dataMember.Name;
                            }
                        }
                    }
                }
            }

            if (canIgnoreMember)
            {
                if (!defaultMembers.Contains(memberInfo))
                {
                    // свойство скорее всего приватное.
                    continue;
                }
            }

            if (!StaticCache.TypesProperties.TryAdd((dboType, memberName), new OrmLazyProperty(memberInfo)))
            {
                // если не удалось добавить, причина может быть только одна — в словаре уже есть такой ключ.
                throw new MicroOrmSerializationException($"A member with the name '{memberName}' already exists on '{dboType}'." +
                    $" Use the {nameof(SqlPropertyAttribute)} to specify another name.");
            }
        }
    }

    /// <summary>
    /// Инициирует ленивое свойство при первом обращении.
    /// Этот метод потокобезопасен.
    /// </summary>
    public bool TryGetOrmProperty(string memberName, [NotNullWhen(true)] out OrmProperty? ormProperty)
    {
        if (StaticCache.TypesProperties.TryGetValue((ContractType, memberName), out var lazyOrmProperty))
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

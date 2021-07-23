using System;
using System.Collections.Generic;
using System.Reflection;

namespace DanilovSoft.MicroORM
{
    internal static class ReflectionUtils
    {
        /// <summary>
        /// Determines whether the member is an indexed property.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>
        /// 	<c>true</c> if the member is an indexed property; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsIndexedProperty(MemberInfo member)
        {
            //ValidationUtils.ArgumentNotNull(member, nameof(member));

            if (member is PropertyInfo propertyInfo)
            {
                return IsIndexedProperty(propertyInfo);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether the property is an indexed property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// 	<c>true</c> if the property is an indexed property; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsIndexedProperty(PropertyInfo property)
        {
            //ValidationUtils.ArgumentNotNull(property, nameof(property));

            return property.GetIndexParameters().Length > 0;
        }

        public static IEnumerable<MemberInfo> GetFieldsAndProperties(Type type, BindingFlags bindingFlags)
        {
            foreach (PropertyInfo propertyInfo in type.GetProperties(bindingFlags))
            {
                yield return propertyInfo;
            }

            foreach (FieldInfo fieldInfo in type.GetFields(bindingFlags))
            {
                yield return fieldInfo;
            }
        }
    }
}

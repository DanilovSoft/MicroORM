using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class OrmLazyProperty
    {
        private readonly Lazy<OrmProperty> _lazy;
        private readonly MemberInfo _memberInfo;
        public OrmProperty Value => _lazy.Value;

        // ctor.
        public OrmLazyProperty(MemberInfo memberInfo)
        {
            _memberInfo = memberInfo;
            _lazy = new Lazy<OrmProperty>(ValueFactory, true);
        }

        // Вызывается один раз при первом обращении к свойству/полю.
        private OrmProperty ValueFactory()
        {
            return new OrmProperty(_memberInfo);
        }
    }
}

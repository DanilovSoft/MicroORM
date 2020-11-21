using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    /// <summary>
    /// Хранит мета информацию о свойстве или поле.
    /// Хранится в словаре поэтому извлекается быстрее как класс, а не как структура.
    /// </summary>
    [DebuggerDisplay(@"\{MemberInfo = {_lazy.Metadata}\}")]
    internal sealed class OrmLazyProperty
    {
        private readonly Lazy<OrmProperty, MemberInfo> _lazy;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public OrmProperty Value => _lazy.Value;

        // ctor.
        public OrmLazyProperty(MemberInfo memberInfo)
        {
            _lazy = new Lazy<OrmProperty, MemberInfo>(() => new OrmProperty(_lazy.Metadata), memberInfo, LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}

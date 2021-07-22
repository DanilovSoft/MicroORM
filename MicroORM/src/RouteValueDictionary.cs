using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DanilovSoft.MicroORM
{
    internal sealed class RouteValueDictionary : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly Dictionary<string, object?> _dict;

        // ctor.
        public RouteValueDictionary(object values)
        {
            _dict = new();
            PropertyInfo[] prop = values.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < prop.Length; i++)
            {
                string name = prop[i].Name;
                object? value = prop[i].GetValue(values);
                _dict.Add(name, value);
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }
    }
}

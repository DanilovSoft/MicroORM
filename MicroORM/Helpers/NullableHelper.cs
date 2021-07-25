using System.Diagnostics.CodeAnalysis;

namespace DanilovSoft.MicroORM.Helpers
{
    internal static class NullableHelper
    {
        [return: NotNullIfNotNull("value")]
        public static T? SetNull<T>(ref T? value) where T : class
        {
            var itemRefCopy = value;
            value = null;
            return itemRefCopy;
        }

        //[return: NotNullIfNotNull("value")]
        //public static T? SetNull<T>(ref T? value) where T : struct
        //{
        //    var itemRefCopy = value;
        //    value = null;
        //    return itemRefCopy;
        //}
    }
}

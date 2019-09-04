#if NET45

namespace DanilovSoft.System
{
    internal static class Array
    {
        public static T[] Empty<T>()
        {
            return EmptyArray<T>.Value;
        }
    }

    internal static class EmptyArray<T>
    {
        public static readonly T[] Value = new T[0];
    }
}
#endif

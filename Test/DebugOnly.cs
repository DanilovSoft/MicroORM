using System.Diagnostics;

namespace Test
{
    internal class DebugOnly
    {
        [DebuggerHidden]
        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        public static void Break()
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }
}

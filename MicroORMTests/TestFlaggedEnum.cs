using System;

namespace UnitTests
{
    [Flags]
    internal enum TestFlaggedEnum
    {
        None = 0,
        One = 1,
        Two = 2,
        Three = 4,
        Four = 8
    }
}

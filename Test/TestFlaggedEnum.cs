using System;

namespace DebugTest;

[Flags]
public enum TestFlaggedEnum
{
    None = 0,
    One = 1,
    Two = 2,
    Three = 4,
    Four = 8
}

public enum TestNonFlaggedEnum
{
    None,
    One,
    Two,
    Three,
    Four
}

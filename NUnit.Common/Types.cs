namespace NUnit.Common;

using System.Diagnostics.CodeAnalysis;

#nullable disable
public class DefaultClassModel
{
    public string Name { get; set; }
}
#nullable restore

public class TestMe
{
    [MaybeNull] public string Name1 { get; set; }
    [AllowNull] public string Name2 { get; set; }

    public string? Name3;
    public string Name4;
    [MaybeNull] public string Name5;
    [AllowNull] public string Name6;
}

public class UserModel<T>
{
    public string Name { get; set; }
    public T Surname { get; set; }
}

public record UserModel(string Name, string? Surname);

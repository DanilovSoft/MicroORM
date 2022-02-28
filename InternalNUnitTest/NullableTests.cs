using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace InternalNUnitTest.Types
{
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
}

namespace InternalNUnitTest
{
    using DanilovSoft.MicroORM;
    using InternalNUnitTest.Types;

    public class NullableTests
    {
        [Test]
        public void NotNull_Property()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(UserModel).GetProperty(nameof(UserModel.Name))!);

            Assert.IsTrue(isNonNull, "��������� �������� �� ��������� Null �� ������ ���������");
        }

        [Test]
        public void CanBeNull_ByDefault()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(DefaultClassModel).GetProperty(nameof(DefaultClassModel.Name))!);

            Assert.IsFalse(isNonNull, "��������� �������� ��������� Null �� ���������");
        }

        [Test]
        public void CanBeNull_Property()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(UserModel).GetProperty(nameof(UserModel.Surname))!);

            Assert.IsFalse(isNonNull, "��������� �������� ���� ��������� Null (���� '?')");
        }

        [Test]
        public void NotNull_Generic()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
               memberInfo: typeof(UserModel<string>).GetProperty(nameof(UserModel<string>.Surname))!);

            Assert.IsFalse(isNonNull, "��������� �������� ��������� Null �� ��������� ��� Generic �������.");
        }

        [Test]
        public void CanBeNull_Generic()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(UserModel<string?>).GetProperty(nameof(UserModel<string?>.Surname))!);

            Assert.IsFalse(isNonNull, "��������� �������� ��������� Null �� ��������� ��� Generic �������.");
        }

        [Test]
        public void MaybeNull_Attribute_OnProperty()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetProperty(nameof(TestMe.Name1))!);

            Assert.IsFalse(isNonNull, "��������� �������� �� ��������� Null �� ������ ���������, �� ���� ����������� �������");
        }

        [Test]
        public void AllowNull_Attribute_OnProperty()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetProperty(nameof(TestMe.Name2))!);

            // TODO ��������� ��� AllowNull �� ������ ��������� ��������� Null.
            Assert.IsTrue(isNonNull, "��������� �������� �� ��������� Null �� ������ ���������, �� ������ �� ������� AllowNull");
        }

        [Test]
        public void Nullable_Field()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetField(nameof(TestMe.Name3))!);

            Assert.IsFalse(isNonNull, "��������� ���� ���� ��������� Null (���� '?')");
        }

        [Test]
        public void NotNull_Field()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetField(nameof(TestMe.Name4))!);

            Assert.IsTrue(isNonNull, "��������� ���� �� ��������� Null �� ������ ���������");
        }

        [Test]
        public void MaybeNull_Attribute_OnField()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetField(nameof(TestMe.Name5))!);

            Assert.IsFalse(isNonNull, "��������� ���� �� ��������� Null �� ������ ���������, �� ���� ����������� �������");
        }

        [Test]
        public void AllowNull_Attribute_OnField()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetField(nameof(TestMe.Name6))!);

            // TODO ��������� ��� AllowNull �� ������ ��������� ��������� Null.
            Assert.IsTrue(isNonNull, "��������� ���� �� ��������� Null �� ������ ���������, �� ������ �� ������� AllowNull");
        }
    }
}
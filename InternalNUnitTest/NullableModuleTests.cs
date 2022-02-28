using DanilovSoft.MicroORM;
using NUnit.Common;
using NUnit.Framework;

namespace InternalNUnitTest
{
    public class NullableModuleTests
    {
        //[Test]
        //public void NotNull_Type()
        //{
        //    bool isNonNull = NonNullableConvention.IsNonNullableReferenceType(memberType: typeof(string));

        //    Assert.IsTrue(isNonNull, "Ссылочное свойство не допускает Null на основе контекста");
        //}

        [Test]
        public void NotNull_Property()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(UserModel).GetProperty(nameof(UserModel.Name))!);

            Assert.IsTrue(isNonNull, "Ссылочное свойство не допускает Null на основе контекста");
        }

        [Test]
        public void CanBeNull_ByDefault()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(DefaultClassModel).GetProperty(nameof(DefaultClassModel.Name))!);

            Assert.IsFalse(isNonNull, "Ссылочное свойство допускает Null по умолчанию");
        }

        [Test]
        public void CanBeNull_Property()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(UserModel).GetProperty(nameof(UserModel.Surname))!);

            Assert.IsFalse(isNonNull, "Ссылочное свойство явно допускает Null (знак '?')");
        }

        [Test]
        public void NotNull_Generic()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
               memberInfo: typeof(UserModel<string>).GetProperty(nameof(UserModel<string>.Surname))!);

            Assert.IsFalse(isNonNull, "Ссылочное свойство допускает Null по умолчанию для Generic свойств.");
        }

        [Test]
        public void CanBeNull_Generic()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(UserModel<string?>).GetProperty(nameof(UserModel<string?>.Surname))!);

            Assert.IsFalse(isNonNull, "Ссылочное свойство допускает Null по умолчанию для Generic свойств.");
        }

        [Test]
        public void MaybeNull_Attribute_OnProperty()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetProperty(nameof(TestMe.Name1))!);

            Assert.IsFalse(isNonNull, "Ссылочное свойство не допускает Null на основе контекста, но есть разрешающий атрибут");
        }

        [Test]
        public void AllowNull_Attribute_OnProperty()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetProperty(nameof(TestMe.Name2))!);

            // TODO убедиться что AllowNull не должен допускать установку Null.
            Assert.IsTrue(isNonNull, "Ссылочное свойство не допускает Null на основе контекста, не смотря на атрибут AllowNull");
        }

        [Test]
        public void Nullable_Field()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetField(nameof(TestMe.Name3))!);

            Assert.IsFalse(isNonNull, "Ссылочное поле явно допускает Null (знак '?')");
        }

        [Test]
        public void NotNull_Field()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetField(nameof(TestMe.Name4))!);

            Assert.IsTrue(isNonNull, "Ссылочное поле не допускает Null на основе контекста");
        }

        [Test]
        public void MaybeNull_Attribute_OnField()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetField(nameof(TestMe.Name5))!);

            Assert.IsFalse(isNonNull, "Ссылочное поле не допускает Null на основе контекста, но есть разрешающий атрибут");
        }

        [Test]
        public void AllowNull_Attribute_OnField()
        {
            var isNonNull = NonNullableConvention.IsNonNullableReferenceType(
                memberInfo: typeof(TestMe).GetField(nameof(TestMe.Name6))!);

            // TODO убедиться что AllowNull не должен допускать установку Null.
            Assert.IsTrue(isNonNull, "Ссылочное поле не допускает Null на основе контекста, не смотря на атрибут AllowNull");
        }
    }
}

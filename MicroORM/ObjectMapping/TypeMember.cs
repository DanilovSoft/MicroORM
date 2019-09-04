using System;
using System.Collections.Generic;
using System.Text;

namespace DanilovSoft.MicroORM.ObjectMapping
{
    internal sealed class TypeMember : IEquatable<TypeMember>
    {
        public readonly Type Type;
        public readonly string MemberName;

        public TypeMember(Type type, string memberName)
        {
            Type = type;
            MemberName = memberName;
        }

        public bool Equals(TypeMember other)
        {
            if (other is null)
                return false;

            return Type == other.Type && MemberName == other.MemberName;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TypeMember);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 486187739 + Type.GetHashCode();
                hash = hash * 486187739 + MemberName.GetHashCode();
                return hash;

                //int value = (Type.GetHashCode() << 16) ^ (MemberName.GetHashCode() & 65535);
                //return value;
            }
        }

        public static bool operator ==(TypeMember left, TypeMember right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(TypeMember left, TypeMember right)
        {
            if (left is null)
                return !(right is null);

            return !left.Equals(right);
        }
    }
}

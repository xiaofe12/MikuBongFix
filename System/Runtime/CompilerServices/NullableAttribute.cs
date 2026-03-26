using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute(byte value)
        {
            NullableFlags = new byte[] { value };
        }

        public NullableAttribute(byte[] value)
        {
            NullableFlags = value;
        }

        public readonly byte[] NullableFlags;
    }
}

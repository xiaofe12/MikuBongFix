using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    public sealed class NullableContextAttribute : Attribute
    {
        public NullableContextAttribute(byte value)
        {
            Flag = value;
        }

        public readonly byte Flag;
    }
}

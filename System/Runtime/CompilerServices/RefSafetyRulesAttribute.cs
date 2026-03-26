using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class RefSafetyRulesAttribute : Attribute
    {
        public RefSafetyRulesAttribute(int version)
        {
            Version = version;
        }

        public readonly int Version;
    }
}

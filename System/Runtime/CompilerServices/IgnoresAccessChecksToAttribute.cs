using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        private readonly string _assemblyName;

        public string AssemblyName
        {
            get { return _assemblyName; }
        }

        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            _assemblyName = assemblyName;
        }
    }
}

using System;

namespace BepInEx.Preloader.Core.Patching
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PatcherAutoPluginAttribute : Attribute
    {
        private string _guid;
        private string _name;
        private string _version;

        public string GUID
        {
            get { return _guid; }
            set { _guid = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public PatcherAutoPluginAttribute(string guid = null, string name = null, string version = null)
        {
            this._guid = guid;
            this._name = name;
            this._version = version;
        }
    }
}

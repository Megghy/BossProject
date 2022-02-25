using System;

namespace BossFramework.BInterfaces
{
    public interface IScriptModule : IDisposable
    {
        public string Name { get; }
        public string Author { get; }
        public string Version { get; }
        public void Initialize();
    }
}

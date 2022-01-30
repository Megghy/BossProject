using BossPlugin.BAttributes;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BossPlugin.BInterfaces
{
    public interface ICommand
    {
        public string[] Names { get; }
    }
    public abstract class BaseCommand : ICommand
    {
        public abstract string[] Names { get; }
        public IReadOnlyList<SubCommandAttribute> SubCommands { get; set; }
        public bool HasDefaultCommand => SubCommands.Any(s => !s.Names?.Any() ?? true);
        public void RegisterAllSubCommands()
        {
            var t = GetType();
            var list = new List<SubCommandAttribute>();
            t.GetMethods()
            .ForEach(method =>
            {
                if (method.GetCustomAttribute<SubCommandAttribute>() is { } attr)
                {
                    attr.Method = method;
                    attr.Permission = method.GetCustomAttribute<NeedPermissionAttribute>()?.Perms?.FirstOrDefault();
                    list.Add(attr);
                }
                else if (method.IsStatic)
                {
                    attr = new SubCommandAttribute()
                    {
                        Names = new string[] { method.Name.ToLower() },
                        Method = method,
                    };
                    list.Add(attr);
                }
            });
            SubCommands = list;
        }
    }
}

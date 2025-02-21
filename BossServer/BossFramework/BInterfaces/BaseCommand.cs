using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BossFramework.BAttributes;
using BossFramework.BModels;
using TShockAPI;

namespace BossFramework.BInterfaces
{
    public interface ICommand
    {
        public string[] Names { get; }
    }
    public abstract class BaseCommand : ICommand
    {
        public abstract string[] Names { get; }

        public virtual string Description { get; } = "";
        public virtual void Init() { }
        public virtual void Dispose() { }
        [SubCommand("help")]
        public virtual void Help(SubCommandArgs args)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"无效输入. 可用命令: {string.Join(',', Names)}");
            SubCommands.ForEach(s => sb.AppendLine($"{string.Join(',', s.Names)} : {s.Description} {(args.TsPlayer.HasPermission("boss.admin") ? $"<{s.Permission}>" : "")} {s.Description}"));
            args.SendInfoMsg(sb.ToString());
        }
        public virtual void Default(SubCommandArgs args)
        {
            args.SendInfoMsg($"未实现此命令");
        }

        public IReadOnlyList<SubCommandAttribute> SubCommands { get; set; } = new List<SubCommandAttribute>();
        public bool HasDefaultCommand
            => !(GetType().GetMethod("Default").DeclaringType == typeof(BaseCommand));
        public void RegisterAllSubCommands()
        {
            var t = GetType();
            var list = new List<SubCommandAttribute>();
            t.GetMethods()
            .ForEach(method =>
            {
                var attrs = method.GetCustomAttributes(typeof(SubCommandAttribute), false);
                if (attrs.Any())
                    attrs.ForEach(a =>
                    {
                        var sub = ((SubCommandAttribute)a);
                        sub.Method = method;
                        sub.Permission ??= method.GetCustomAttribute<NeedPermissionAttribute>()?.Perms?.FirstOrDefault();
                        sub.Description ??= method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
                        list.Add(sub);
                    });
                else if (method.IsPublic)
                {
                    var sub = new SubCommandAttribute()
                    {
                        Names = new[] { method.Name.ToLower() },
                        Method = method,
                        Permission = method.GetCustomAttribute<NeedPermissionAttribute>()?.Perms?.FirstOrDefault()
                    };
                    sub.Description ??= method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
                    list.Add(sub);
                }
            });
            SubCommands = list;
        }
    }
}

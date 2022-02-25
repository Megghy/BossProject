using BossFramework.BAttributes;
using BossFramework.BModels;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BossFramework.BInterfaces
{
    public interface ICommand
    {
        public string[] Names { get; }
    }
    public abstract class BaseCommand : ICommand
    {
        public abstract string[] Names { get; }
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
                    attrs.ForEach(a => list.Add(new SubCommandAttribute()
                    {
                        Names = ((SubCommandAttribute)a).Names,
                        Method = method,
                        Permission = method.GetCustomAttribute<NeedPermissionAttribute>()?.Perms?.FirstOrDefault()
                    }));
                else if (method.IsStatic)
                {
                    var attr = new SubCommandAttribute()
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

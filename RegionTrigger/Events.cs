using System.ComponentModel;
using System.Text;

namespace RegionTrigger
{
    internal static class Events
    {
        public static Dictionary<Event, string> EventsDescriptions = new Dictionary<Event, string>();

        static Events()
        {
            var values = typeof(Event).GetEnumValues();

            for (var index = 0; index < values.Length; index++)
            {
                var val = (Event)values.GetValue(index);
                var enumName = val.ToString();
                var fieldInfo = typeof(Event).GetField(enumName);

                var descattr =
                    fieldInfo.GetCustomAttributes(false).FirstOrDefault(o => o is DescriptionAttribute) as DescriptionAttribute;
                var desc = !string.IsNullOrWhiteSpace(descattr?.Description) ? descattr.Description : "None";
                EventsDescriptions.Add(val, desc);
            }
        }

        internal static Event ParseEvents(string eventString)
        {
            if (string.IsNullOrWhiteSpace(eventString))
                return Event.None;

            var @event = Event.None;

            var splitedEvents = eventString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var e in splitedEvents.Select(s => s.Trim()))
            {
                if (!Enum.TryParse(e, true, out Event val))
                {
                    continue;
                }
                @event |= val;
            }
            return @event;
        }

        internal static Event ValidateEventWhenAdd(string eventString, out string invalids)
        {
            invalids = null;

            if (string.IsNullOrWhiteSpace(eventString))
                return Event.None;

            var @event = Event.None;

            var splitedEvents = eventString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var e in splitedEvents.Select(s => s.Trim()))
            {
                if (!Enum.TryParse(e, true, out Event val))
                {
                    sb.Append(e + ", ");
                    continue;
                }
                @event |= val;
            }
            if (sb.Length != 0)
                invalids = sb.Remove(sb.Length - 2, 2).ToString();
            return @event;
        }
    }

    [Flags]
    internal enum Event
    {
        [Description("代表区域无事件（该事件无法被添加）")]
        None = 0,

        [Description("进入区域时发送消息。")]
        EnterMsg = 1 << 0,

        [Description("离开区域时发送消息。")]
        LeaveMsg = 1 << 1,

        [Description("以特定间隔区域内玩家发送消息。")]
        Message = 1 << 2,

        [Description("进入区域后玩家获得临时组。")]
        TempGroup = 1 << 3,

        [Description("区域内禁用特定物品。")]
        Itemban = 1 << 4,

        [Description("区域内禁用特定弹幕。")]
        Projban = 1 << 5,

        [Description("区域内禁用特定物块。")]
        Tileban = 1 << 6,

        [Description("杀死进入区域的玩家。")]
        Kill = 1 << 7,

        [Description("区域内玩家获得无敌状态。")]
        Godmode = 1 << 8,

        [Description("区域内强制开启PvP模式。")]
        Pvp = 1 << 9,

        [Description("区域内强制关闭PvP模式。")]
        NoPvp = 1 << 10,

        [Description("区域内禁止改变PvP状态。")]
        InvariantPvp = 1 << 11,

        [Description("禁止进入区域。")]
        Private = 1 << 12,

        [Description("区域内玩家获得临时权限。")]
        TempPermission = 1 << 13,

        [Description("禁止玩家丢东西。")]
        NoItem = 1 << 14
    }
}

using BossFramework.BCore;
using BossFramework.DB;
using FreeSql.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using TShockAPI.DB;

namespace BossFramework.BModels
{
    public class BRegion : DBStructBase<BRegion>
    {
        public const string DefaultRegionName = "DefaultBRegion";
        public static BRegion Default { get; } = new(null);
        public BRegion() { Name = DefaultRegionName; WorldId = Terraria.Main.worldID; Init(); }
        public BRegion(Region region)
        {
            OriginRegion = region;
            Name = region is null ? DefaultRegionName : region.Name;
            WorldId = Terraria.Main.worldID;
            Init();
        }
        public override void Init()
        {
            if (Name != DefaultRegionName)
                OriginRegion ??= TShockAPI.TShock.Regions.Regions.FirstOrDefault(r => r.Name == Name && r.WorldID == WorldId.ToString());
            ProjContext = new(this);
        }

        #region 自身变量
        public string Name { get; private set; }
        [JsonMap]
        public List<string> Tags { get; private set; } = new();

        public long WorldId { get; private set; }
        public Region OriginRegion { get; private set; }
        public long ParentId { get; private set; } = -1;
        public BRegion Parent
            => BRegionSystem.AllBRegion.FirstOrDefault(r => r.Id == ParentId);
        [JsonMap]
        public List<long> ChildsId { get; private set; } = new();
        private List<BRegion> _childRegion;
        public List<BRegion> ChildRegion
        {
            get
            {
                if (_childRegion is null)
                {
                    _childRegion = new();
                    ChildsId?.ForEach(r =>
                        {
                            if (BRegionSystem.AllBRegion.FirstOrDefault(r => r.Id == r.Id) is { } child)
                                _childRegion.Add(child);
                        });
                }
                return _childRegion;
            }
        }

        public BPlayer[] GetPlayers(bool includeChild = true)
            => BInfo.OnlinePlayers.Where(p => IsPlayerInThis(p, includeChild) == true)
            .ToArray();
        #endregion

        #region 弹幕重定向
        public ProjRedirectContext ProjContext { get; private set; }
        #endregion

        #region 方法
        public void AddChild(BRegion region)
        {
            if (ChildRegion.Contains(region))
                return;
            _childRegion.Add(region);
            ChildsId.Add(region.Id);
            Update(r => r.ChildsId);

            region.ParentId = Id;
            region.Update(r => r.ParentId);
        }
        public void RemoveChild(BRegion region)
        {
            if (!ChildRegion.Contains(region))
                return;
            _childRegion.Remove(region);
            ChildsId.Remove(region.Id);
            Update(r => r.ChildsId);

            region.ParentId = Id;
            region.Update(r => r.ParentId);
        }
        public void SetParent(BRegion region)
        {
            if (region is null)
                Update(r => r.ParentId, -1);
            else
                region.RemoveChild(this);
        }

        public bool IsPlayerInThis(BPlayer plr, bool includeChild = true)
        {
            return plr.CurrentRegion == this || (includeChild && IsInChildRegion(plr, this));
            bool IsInChildRegion(BPlayer plr, BRegion parent)
            {
                if (parent.ChildRegion.Any())
                {
                    foreach (var child in parent.ChildRegion)
                    {
                        if (IsInChildRegion(plr, child))
                            return true;
                    }
                    return false;
                }
                else
                    return false;
            }
        }

        public bool AddTag(string tagName, bool createInstance = true)
        {
            if (Tags.Contains(tagName))
                return false;
            Tags.Add(tagName);
            if (Update(r => r.Tags) > 0)
                return true;
            else
            {
                Tags.Remove(tagName);
                return false;
            }
        }
        public void DelTag(string tagName)
        {
            if (!Tags.Contains(tagName))
                return;
            if (!Tags.Remove(tagName) || Update(r => r.Tags) == 0)
                Tags.Add(tagName);
        }
        #endregion

        public override bool Equals(object obj)
        {
            if (obj is BRegion region)
                return region.Id == Id;
            return false;
        }
        public override string ToString()
            => $"{Name}:{WorldId}";
        public override int GetHashCode()
            => base.GetHashCode();
        public static implicit operator Region(BRegion bregion)
            => bregion.OriginRegion;
        public static implicit operator BRegion(Region region)
            => BRegionSystem.FindBRegionForRegion(region);
    }
}

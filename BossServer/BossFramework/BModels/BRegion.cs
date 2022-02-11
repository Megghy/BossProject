using BossFramework.BCore;
using BossFramework.DB;
using FreeSql.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using TShockAPI.DB;

namespace BossFramework.BModels
{
    public class BRegion : UserConfigBase<BRegion>
    {
        public const string DefaultRegionName = "DefaultBRegion";
        public static BRegion Default { get; } = new(null);
        public BRegion() { Name = DefaultRegionName; Id = Terraria.Main.worldID; }
        public BRegion(Region region)
        {
            OriginRegion = region;
            Name = region is null ? DefaultRegionName : region.Name;
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
        public long WorldId { get; private set; }
        public Region OriginRegion { get; private set; }
        public int ParentId { get; private set; }
        private BRegion _parent;
        public BRegion Parent
        {
            get
            {
                _parent ??= BRegionSystem.AllBRegion.FirstOrDefault(r => r.Id == ParentId);
                return _parent;
            }
        }
        [JsonMap]
        public List<int> ChildsId { get; private set; }
        private List<BRegion> _childRegion;
        public List<BRegion> ChildRegion
        {
            get
            {
                if (_childRegion is null)
                {
                    ChildsId.ForEach(r =>
                        {
                            if (BRegionSystem.AllBRegion.FirstOrDefault(r => r.Id == r.Id) is { } child)
                                _childRegion.Add(child);
                        });
                }
                return _childRegion;
            }
        }
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
        }
        public void RemoveChild(BRegion region)
        {
            if (!ChildRegion.Contains(region))
                return;
            _childRegion.Remove(region);
            ChildsId.Remove(region.Id);
            Update(r => r.ChildsId);
        }
        public void SetParent(BRegion region)
        {
            if (region is null)
            {
                Update(r => r.ParentId, -1);
                _parent = null;
            }
            else
            {
                _parent = region;
                Update(r => r.ParentId, _parent.Id);
            }
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

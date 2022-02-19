using BossFramework.BCore;
using BossFramework.BInterfaces;
using BossFramework.DB;
using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI.DB;

namespace BossFramework.BModels
{
    public class BRegion : UserConfigBase<BRegion>
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

            Tags = BRegionSystem.CreateTags(this);
            Tags.ForEach(t =>
            {
                if (TagsName.Contains(t.Name))
                    TagsName.Remove(t.Name);
            });
            UpdateSingle(r => r.TagsName);
        }

        #region 自身变量
        public string Name { get; private set; }
        [JsonMap]
        public List<string> TagsName { get; private set; } = new();
        public List<BaseRegionTag> Tags { get; internal set; } = new();

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
        public List<long> ChildsId { get; private set; }
        private List<BRegion> _childRegion;
        public List<BRegion> ChildRegion
        {
            get
            {
                if (_childRegion is null)
                {
                    _childRegion = new();
                    ChildsId.ForEach(r =>
                        {
                            if (BRegionSystem.AllBRegion.FirstOrDefault(r => r.Id == r.Id) is { } child)
                                _childRegion.Add(child);
                        });
                }
                return _childRegion;
            }
        }

        public BPlayer[] GetPlayers(bool includeChild = true)
            => BInfo.OnlinePlayers.Where(p => p?.CurrentRegion?.IsPlayerInThis(p, includeChild) == true)
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
            UpdateSingle(r => r.ChildsId);
        }
        public void RemoveChild(BRegion region)
        {
            if (!ChildRegion.Contains(region))
                return;
            _childRegion.Remove(region);
            ChildsId.Remove(region.Id);
            UpdateSingle(r => r.ChildsId);
        }
        public void SetParent(BRegion region)
        {
            if (region is null)
            {
                UpdateSingle(r => r.ParentId, -1);
                _parent = null;
            }
            else
            {
                _parent = region;
                UpdateSingle(r => r.ParentId, _parent.Id);
            }
        }

        public bool IsPlayerInThis(BPlayer plr, bool includeChild = true)
        {
            return plr.CurrentRegion == this || (includeChild && IsInChildRegion(plr, this));
            bool IsInChildRegion(BPlayer plr, BRegion parent){
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

        public bool AddTag(BaseRegionTag tag, bool createInstance = true)
        {
            if (TagsName.Contains(tag.Name))
                return false;
            TagsName.Add(tag.Name);
            if (UpdateSingle(r => r.TagsName) > 0)
            {
                Tags.Add(createInstance
                    ? (BaseRegionTag)Activator.CreateInstance(tag.GetType(), new object[] { tag.Region })
                    : tag);
                return true;
            }
            else
            {
                TagsName.Remove(tag.Name);
                return false;
            }
        }
        public void DelTag(string tagName)
        {
            if (!TagsName.Contains(tagName))
                return;
            if (TagsName.Remove(tagName) && UpdateSingle(r => r.TagsName) > 0)
            {
                if(Tags.FirstOrDefault(t => t.Name == tagName) is { } tag)
                {
                    Tags.Remove(tag);
                    tag.Dispose();
                }    
            }
            else
                TagsName.Add(tagName);
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

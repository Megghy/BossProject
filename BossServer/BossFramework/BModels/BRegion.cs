using BossFramework.BCore;
using BossFramework.DB;
using System.Collections.Generic;
using System.Linq;
using TShockAPI.DB;

namespace BossFramework.BModels
{
    public class BRegion : UserConfigBase<BRegion>
    {
        public static BRegion Default { get; } = new(null);
        public BRegion() { ID = "DefaultBRegion"; }
        public BRegion(Region region)
        {
            OriginRegion = region;
            ID = $"{region.Name}_{region.WorldID}";
        }
        public override void Init()
        {
            OriginRegion ??= TShockAPI.TShock.Regions.Regions.FirstOrDefault(r => $"{r.Name}_{r.WorldID}" == ID);
        }

        public Region OriginRegion { get; private set; }
        public string ParentName { get; private set; }
        private BRegion _parent;
        public BRegion Parent
        {
            get
            {
                _parent ??= BRegionSystem.AllBRegion.FirstOrDefault(r => r.ID == ParentName);
                return _parent;
            }
        }

        public string ChildName { get; private set; } = "[]";
        private List<BRegion> _childRegion;
        public List<BRegion> ChildRegion
        {
            get
            {
                if(_childRegion is null)
                {
                    var regions = BUtils.DeserializeJson<string[]>(ChildName);
                    _childRegion = new();
                    BUtils.DeserializeJson<string[]>(ChildName)
                        .ForEach(r =>
                        {
                            if (BRegionSystem.AllBRegion.FirstOrDefault(r => r.ID == r.ID) is { } child)
                                _childRegion.Add(child);
                        });
                }
                return _childRegion;
            }
        }

        #region 方法
        public void AddChild(BRegion region)
        {
            if (ChildRegion.Contains(region))
                return;
            _childRegion.Add(region);
            var childs = ChildName.DeserializeJson<List<string>>();
            childs.Add(region.ID);
            Update(r => r.ChildName, childs.SerializeToJson());
        }
        public void RemoveChild(BRegion region)
        {
            if (!ChildRegion.Contains(region))
                return;
            _childRegion.Remove(region);
            var childs = ChildName.DeserializeJson<List<string>>();
            childs.Remove(region.ID);
            Update(r => r.ChildName, childs.SerializeToJson());
        }
        public void SetParent(BRegion region)
        {
            if(region is null)
            {
                Update(r => r.ParentName, "");
                _parent = null;
            }
            else
            {
                _parent = region;
                Update(r => r.ParentName, _parent.ID);
            }
        }
        #endregion
    }
}

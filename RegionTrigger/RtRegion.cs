using Extensions;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace RegionTrigger
{
    internal sealed class RtRegion
    {
        public int Id { get; set; }

        public string EnterMsg { get; set; }

        public string LeaveMsg { get; set; }

        public string Message { get; set; }

        public int MsgInterval { get; set; }

        public Group TempGroup { get; set; }

        public readonly Region Region;

        private Event _event = Event.None;

        public string Events => _event.ToString("F");

        private readonly List<string> _itembans = new List<string>();

        public string Itembans
        {
            get => string.Join(",", _itembans);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;
                _itembans.Clear();
                var items = value.Trim().Split(',');
                foreach (var item in items.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    _itembans.Add(item);
                }
            }
        }

        private readonly List<short> _projbans = new List<short>();
        public string Projbans
        {
            get => string.Join(",", _projbans);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;
                _projbans.Clear();
                var projids = value.Trim().ToLower().Split(',');
                foreach (var projid in projids.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    if (short.TryParse(projid, out short proj) && proj > 0 && proj < Main.maxProjectileTypes)
                        _projbans.Add(proj);
                }
            }
        }

        private readonly List<short> _tilebans = new List<short>();
        public string Tilebans
        {
            get => string.Join(",", _tilebans);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;
                _tilebans.Clear();
                var tileids = value.Trim().ToLower().Split(',');
                foreach (var tileid in tileids.Where(e => !string.IsNullOrWhiteSpace(e)))
                {
                    if (short.TryParse(tileid, out short tile) && tile > -1 && tile < Main.maxTileSets)
                        _tilebans.Add(tile);
                }
            }
        }

        private readonly List<string> _permissions = new List<string>();
        public string Permissions
        {
            get => string.Join(",", _permissions);
            set
            {
                _permissions.Clear();
                value?.Split(',').TForEach(AddPermission);
            }
        }

        public RtRegion(int id, int rid)
        {
            Id = id;
            Region = TShock.Regions.GetRegionByID(rid);
            if (Region == null)
                throw new Exception("无效区域ID");
        }

        public RtRegion(int id, int rid, Event ev) : this(id, rid)
        {
            _event = ev;
        }

        public bool HasEvent(Event ev)
        {
            return _event.Has(ev);
        }

        public void AddEvent(Event ev)
        {
            _event = _event.Include(ev);
        }

        public void RemoveEvent(Event ev)
        {
            _event = _event.Remove(ev);
        }

        public bool TileIsBanned(short tileId)
            => _tilebans.Contains(tileId);

        public bool RemoveBannedTile(short tileId)
            => _tilebans.Remove(tileId);

        public bool ProjectileIsBanned(short projId)
            => _projbans.Contains(projId);

        public bool RemoveBannedProjectile(short projId)
            => _projbans.Remove(projId);

        public bool ItemIsBanned(string itemName)
            => _itembans.Contains(itemName);

        public bool RemoveBannedItem(string itemName)
            => _itembans.Remove(itemName);

        public void AddPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return;
            var pLower = permission.ToLower();
            if (_permissions.Contains(pLower))
                return;
            _permissions.Add(pLower);
        }

        public void RemovePermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return;
            var pLower = permission.ToLower();
            _permissions.Remove(pLower);
        }

        public bool HasPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return false;
            var pLower = permission.ToLower();
            return _permissions.Contains(pLower);
        }

        internal static RtRegion FromReader(QueryResult reader)
        {
            var groupName = reader.Get<string>("TempGroup");

            var region = new RtRegion(reader.Get<int>("Id"), reader.Get<int>("RegionId"))
            {
                _event = global::RegionTrigger.Events.ParseEvents(reader.Get<string>("Events")),
                EnterMsg = reader.Get<string>("EnterMsg"),
                LeaveMsg = reader.Get<string>("LeaveMsg"),
                Message = reader.Get<string>("Message"),
                MsgInterval = reader.Get<int?>("MessageInterval") ?? 0,
                TempGroup = TShock.Groups.GetGroupByName(groupName),
                Itembans = reader.Get<string>("Itembans"),
                Projbans = reader.Get<string>("Projbans"),
                Tilebans = reader.Get<string>("Tilebans"),
                Permissions = reader.Get<string>("Permissions")
            };

            if (region.TempGroup == null && region.HasEvent(Event.TempGroup))
                TShock.Log.ConsoleError("[RegionTrigger] 临时组： '{0}'在 '{1}' 失效!", groupName, region.Region.Name);

            return region;
        }
    }
}

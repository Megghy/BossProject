using System.Collections.Generic;
using System.Linq;
using BossFramework.BAttributes;
using BossFramework.BModels;
using Microsoft.Xna.Framework;

namespace BossFramework.BCore
{
    public static class GridSystem
    {
        public class GridCell
        {
            public Dictionary<Type, List<(object obj, Point absolutePos)>> ObjectsInCell = [];
            /// <summary>
            /// 记录每个对象所属的网格
            /// </summary>
            public Dictionary<object, Point> ObjectsBelongToCell = [];
            public Rectangle Bounds; // 网格的矩形区域
        }
        private const int CELL_SIZE = 100; // 网格边长（按游戏单位设定）
        private static Dictionary<Point, GridCell> _grid = [];
        private static Dictionary<BPlayer, Point> _playerInGrid = [];

        /// <summary>
        /// 初始化时预生成所有静态的网格
        /// </summary>
        [AutoInit]
        public static void Init()
        {
        }

        [SimpleTimer(Time = 1)]
        public static void UpdatePlayerInGrid()
        {
            _playerInGrid = BInfo.OnlinePlayers.ToDictionary(p => p, p => GetGridPosition(p.TilePosition));
        }

        public static Point GetGridPosition(Vector2 worldPos)
        {
            return new Point(
                (int)(worldPos.X / CELL_SIZE),
                (int)(worldPos.Y / CELL_SIZE));
        }
        public static Point GetGridPosition(Point worldPos)
        {
            return new Point(
                (worldPos.X / CELL_SIZE),
                (worldPos.Y / CELL_SIZE));
        }
        public static void AddObject<T>(T target, Point absolutePos)
        {
            Point gridPos = GetGridPosition(absolutePos);

            // 获取或创建网格
            if (!_grid.TryGetValue(gridPos, out var cell))
            {
                cell = new GridCell
                {
                    Bounds = new Rectangle(
                        gridPos.X * CELL_SIZE,
                        gridPos.Y * CELL_SIZE,
                        CELL_SIZE, CELL_SIZE)
                };
                _grid[gridPos] = cell;
            }
            if (cell.ObjectsBelongToCell.ContainsKey(target))
            {
                return;
            }

            if (cell.ObjectsInCell.TryGetValue(typeof(T), out var list))
            {
                list.Add((target, absolutePos));
            }
            else
            {
                cell.ObjectsInCell.Add(typeof(T), [(target, absolutePos)]);
            }
            cell.ObjectsBelongToCell.TryAdd(target, gridPos);
            //_signToGrid[sign] = gridPos; // 记录位置
        }
        public static void RemoveObject<T>(T target)
        {
            foreach (var cell in _grid.Values)
            {
                if (cell.ObjectsBelongToCell.Remove(target))
                {
                    cell.ObjectsInCell[typeof(T)].RemoveAll(o => target.Equals(o.obj));
                }
            }
        }
        public static void RemoveObject<T>(Point pos, bool first)
        {
            foreach (var cell in _grid.Values)
            {
                if (cell.ObjectsInCell.TryGetValue(typeof(T), out var list) && list.Count > 0)
                {
                    for (var i = list.Count - 1; i >= 0; i--)
                    {
                        var p = list[i].absolutePos;
                        if (p == pos)
                        {
                            cell.ObjectsBelongToCell.Remove(list[i].obj);
                            list.RemoveAt(i);
                            if (first)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
        public static IEnumerable<T> GetAllObject<T>() where T : class
        {
            return _grid.Values.SelectMany(c => c.ObjectsInCell.TryGetValue(typeof(T), out var list) ? list : []).Select(o => o.obj as T);
        }
        public static List<T> GetNearbyObjects<T>(this BPlayer player, float? viewDistance = null) where T : class
        {
            List<T> result = [];
            Point centerGrid = GetGridPosition(player.TilePosition);
            var searchRange = viewDistance is null ? 1 : (int)Math.Ceiling(viewDistance.Value / CELL_SIZE);

            // 检查3x3区域（中心网格+8个相邻网格）
            for (int dx = -searchRange; dx <= searchRange; dx++)
            {
                for (int dy = -searchRange; dy <= searchRange; dy++)
                {
                    Point checkGrid = new(
                        centerGrid.X + dx,
                        centerGrid.Y + dy);

                    if (_grid.TryGetValue(checkGrid, out var cell))
                    {
                        if (cell.ObjectsInCell.TryGetValue(typeof(T), out var list))
                        {
                            // 精确距离检测
                            foreach (var obj in list)
                            {
                                if (viewDistance is null || BUtils.Distance(obj.absolutePos, player.TilePositionV) <= viewDistance)
                                {
                                    result.Add(obj.obj as T);
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}

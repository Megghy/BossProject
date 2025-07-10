using BossFramework;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using FakeProvider;
using Microsoft.Xna.Framework;
using Terraria.ID;
using TerrariaUI;
using TerrariaUI.Base;
using TerrariaUI.Base.Style;
using TerrariaUI.Widgets;
using TShockAPI;
using TUIPlugin;

public class drawboard : BaseMiniGame
{
    #region 常量与嵌套类

    public const int MaxPlayer = 5;
    public const int WIDTH = 150;
    public const int HEIGHT = 75;
    public const int DRAWBOARD_WIDTH = 120;
    private int DrawboardHeight => HEIGHT;
    private static readonly string DrawingsDir = Path.Combine(TShock.SavePath, "Drawings");

    /// <summary>
    /// 存储玩家游戏内的信息
    /// </summary>
    public class PlayerInfo
    {
        public int Score = 0;
        public byte SelectColor = PaintID.BlackPaint;
        public Point LastPoint = new(-1, -1);
        public int EraserSize = 0;
    }

    /// <summary>
    /// 用于序列化保存的笔迹点
    /// </summary>
    public class SerializablePoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte WallColor { get; set; }
        public byte Wall { get; set; }
    }

    #endregion

    #region 属性

    public override string[] Names { get; } = new[] { "db", "drawboard", "画板" };
    public override string Author { get; } = "Cai";
    public override string Description { get; } = "一个你画我猜游戏";

    public Panel MainPanel { get; set; }
    public VisualContainer Board { get; set; }
    public TileProvider FakePanel { get; set; }
    /// <summary>
    /// 画板上所有独立的像素点 (Label)
    /// </summary>
    public List<Label> Points { get; set; }
    /// <summary>
    /// 存储每一笔的绘画数据，用于撤销
    /// </summary>
    private readonly List<List<Label>> _strokes = new();
    public Dictionary<string, PlayerInfo> PlayerInfos { get; set; }
    public BPlayer NowPainting { get; set; }
    private Label _eraserPreviewLabel;

    #endregion

    #region 游戏主流程

    public drawboard(Guid id) : base(id)
    {
    }

    public override void Init(BPlayer creator)
    {
        PlayerInfos = new();
        Points = new();
        _strokes.Clear();
        CreatePanel(creator.TileX - (WIDTH / 2), creator.TileY - (HEIGHT / 2), GId);
        Directory.CreateDirectory(DrawingsDir);
    }

    public override void Dispose()
    {
        TUI.Destroy(MainPanel);
        FakeProviderAPI.Remove(FakePanel);
        Players.Clear();
        PlayerInfos.Clear();
        Points.Clear();
        _strokes.Clear();
    }

    public override bool Join(BPlayer plr)
    {
        if (State == MiniGameState.Playing)
        {
            plr.SendErrorMsg("此场游戏已开始");
            return false;
        }

        if (Players.Count >= MaxPlayer)
        {
            plr.SendInfoMsg("人数已满");
            return false;
        }

        if (Players.Contains(plr))
        {
            plr.SendInfoMsg("你已加入此场游戏");
            return false;
        }

        PlayerInfos.Add(plr.Name, new());
        NowPainting ??= plr; // 将第一个加入的玩家设置为绘画者
        plr.SendSuccessMsg($"已加入, 当前人数 {Players.Count} 人: {string.Join(", ", Players)}");
        return true;
    }

    public override void Start()
    {
        // TODO: 实现游戏开始逻辑，如轮换玩家、选择词语等
    }

    public override void Stop()
    {
        Dispose();
    }

    public override void Update(long gameTime)
    {
        // TODO: 实现游戏更新逻辑，如计时器
    }

    #endregion

    #region UI 创建

    private void CreatePanel(int x, int y, Guid id)
    {
        FakePanel = FakeProviderAPI.CreateTileProvider(id.ToString(), x, y, WIDTH, HEIGHT + 4);
        MainPanel = TUI.Create(new Panel(id.ToString(), x, y, WIDTH, HEIGHT + 4, new UIConfiguration() { Permission = "hy.game.move" }, null, FakePanel));

        // 1. 创建主要布局容器并应用样式
        var title = new VisualContainer(0, 0, WIDTH, 4, null, new ContainerStyle { Wall = (byte?)WallID.DiamondGemspark, WallColor = PaintID.BlackPaint });
        var control = new VisualContainer(0, 4, 10, HEIGHT, null, new ContainerStyle { Wall = (byte?)WallID.DiamondGemspark, WallColor = PaintID.LimePaint });
        Board = new VisualContainer(10, 4, DRAWBOARD_WIDTH, DrawboardHeight, new UIConfiguration(), new ContainerStyle { Wall = (byte?)WallID.DiamondGemspark, WallColor = PaintID.WhitePaint });
        var brush = new VisualContainer(WIDTH - 20, 4, 20, HEIGHT, null, new ContainerStyle { Wall = (byte?)WallID.DiamondGemspark, WallColor = PaintID.CyanPaint });

        // 2. 填充每个容器的内容
        PopulateTitleBar(title);
        PopulateControlBar(control);
        PopulateBrushBar(brush);

        // 3. 将所有部分添加到主面板
        MainPanel.Add(title);
        MainPanel.Add(control);
        MainPanel.Add(Board);
        MainPanel.Add(new Label(10, 4, DRAWBOARD_WIDTH, DrawboardHeight, "", new UIConfiguration() { UseBegin = true, UseMoving = true, UseEnd = true }, null, Draw)); // 绘画输入捕捉层
        MainPanel.Add(brush);

        MainPanel.UpdateSelf();
    }

    private void PopulateTitleBar(VisualContainer title)
    {
        var titleText = "你画我猜";
        title.Add(new Label((WIDTH / 2) - (titleText.Length / 2), 0, 30, 4, titleText, null, new LabelStyle()
        {
            TextAlignment = Alignment.Center,
            TextColor = PaintID.GrayPaint
        }));
    }

    private void PopulateControlBar(VisualContainer control)
    {
        control.Add(new Label(0, 0, 10, 4, "加入游戏", new UIConfiguration() { UseBegin = true }, new ButtonStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint,
            TextColor = PaintID.DeepGreenPaint,
            TextAlignment = Alignment.Center
        }, (self, touch) => Join(touch.Player().GetBPlayer())));
    }

    private void PopulateBrushBar(VisualContainer brush)
    {
        // 颜色标题
        brush.Add(new Label(0, 0, 20, 4, "画笔颜色", null, new LabelStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint, TextAlignment = Alignment.Center, TextColor = PaintID.GrayPaint }));
        var colors = brush.Add(new VisualContainer(0, 4, 20, 11, null, null, null));
        CreateColorPalette(colors);

        // 清空按钮
        var clear = brush.Add(new Button(0, 15, 20, 4, "清空", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint, TextAlignment = Alignment.Center, TextColor = PaintID.YellowPaint }, OnClearButtonClick));
        clear.Add(new Arrow(2, 1, new ArrowStyle() { TileColor = PaintID.YellowPaint, Direction = Direction.Left }));
        clear.Add(new Arrow(16, 1, new ArrowStyle() { TileColor = PaintID.YellowPaint, Direction = Direction.Right }));

        // 橡皮擦
        brush.Add(new Label(0, 20, 20, 4, "橡皮擦", null, new LabelStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint, TextAlignment = Alignment.Center, TextColor = PaintID.GrayPaint }));
        var erasers = brush.Add(new VisualContainer(0, 25, 20, 9, null, new ContainerStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint }));
        CreateEraserSelector(erasers);

        // 撤销按钮
        var undo = brush.Add(new Button(0, 35, 20, 4, "撤销", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint, TextAlignment = Alignment.Center, TextColor = PaintID.YellowPaint }, OnUndoButtonClick));
        undo.Add(new Arrow(2, 1, new ArrowStyle() { TileColor = PaintID.YellowPaint, Direction = Direction.Left }));
        undo.Add(new Arrow(16, 1, new ArrowStyle() { TileColor = PaintID.YellowPaint, Direction = Direction.Right }));

        // 保存按钮
        var save = brush.Add(new Button(0, 40, 20, 4, "保存", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint, TextAlignment = Alignment.Center, TextColor = PaintID.YellowPaint }, OnSaveButtonClick));
        save.Add(new Arrow(2, 1, new ArrowStyle() { TileColor = PaintID.YellowPaint, Direction = Direction.Left }));
        save.Add(new Arrow(16, 1, new ArrowStyle() { TileColor = PaintID.YellowPaint, Direction = Direction.Right }));
    }

    private void CreateColorPalette(VisualContainer colors)
    {
        byte[] palette = {
            PaintID.BlackPaint, PaintID.GrayPaint, PaintID.WhitePaint, PaintID.ShadowPaint, PaintID.NegativePaint, PaintID.BrownPaint,
            PaintID.RedPaint, PaintID.OrangePaint, PaintID.YellowPaint, PaintID.LimePaint, PaintID.GreenPaint, PaintID.TealPaint,
            PaintID.CyanPaint, PaintID.SkyBluePaint, PaintID.BluePaint, PaintID.PurplePaint, PaintID.VioletPaint, PaintID.PinkPaint
        };

        for (int i = 0; i < palette.Length; i++)
        {
            int row = i / 6;
            int col = i % 6;
            var color = palette[i];
            colors.Add(new Button(1 + col * 3, 1 + row * 3, 3, 3, "", null,
                new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = color },
                (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)(((Button)sender).LabelStyle.WallColor ?? color))));
        }
    }

    private void CreateEraserSelector(VisualContainer erasers)
    {
        var erasersContainer = erasers.Add(new VisualContainer(1, 1, 18, 7, null, new ContainerStyle() { Wall = (byte?)WallID.DiamondGemspark, WallColor = PaintID.WhitePaint }));
        erasersContainer.Add(new Button(2, 3, 1, 1, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.BlackPaint }, EraserSelect));
        erasersContainer.Add(new Button(6, 2, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.BlackPaint }, EraserSelect));
        erasersContainer.Add(new Button(12, 1, 5, 5, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.BlackPaint }, EraserSelect));
    }

    #endregion

    #region 事件处理器

    private void OnClearButtonClick(object sender, Touch touch)
    {
        MainPanel.Confirm("确定要清空整个画板吗?", value =>
        {
            if (value)
            {
                Points.ForEach(p => Board.Remove(p));
                Points.Clear();
                _strokes.Clear();
                Board.UpdateSelf();
            }
        });
    }

    private void OnUndoButtonClick(object sender, Touch touch)
    {
        MainPanel.Confirm("确定要撤销上一步操作吗?", value =>
        {
            if (value)
            {
                if (!_strokes.Any())
                {
                    touch.Player().SendInfoMessage("没有可以撤销的笔迹了。");
                    return;
                }
                var lastStroke = _strokes.Last();
                lastStroke.ForEach(p =>
                {
                    Points.Remove(p);
                    Board.Remove(p);
                });
                _strokes.Remove(lastStroke);
                Board.UpdateSelf();
            }
        });
    }

    private void OnSaveButtonClick(object sender, Touch touch)
    {
        var plr = touch.Player().GetBPlayer();
        if (!Points.Any())
        {
            plr.SendInfoMsg("画板是空的，无法保存。");
            return;
        }

        MainPanel.Confirm("确定要保存当前画作吗?", value =>
        {
            if (value)
            {
                try
                {
                    var filename = $"drawing_{DateTime.Now:yyyyMMddHHmmss}.json";
                    SaveDrawing(filename);
                    plr.SendSuccessMsg($"画作已保存为: {filename}");
                    plr.SendInfoMsg($"文件位于服务器的 {DrawingsDir} 目录下。");
                }
                catch (Exception ex)
                {
                    plr.SendErrorMsg("保存失败！详情请查看后台日志。");
                    TShock.Log.ConsoleError($"[Drawboard] 保存画作失败: {ex}");
                }
            }
        });
    }

    private void EraserSelect(object sender, Touch touch)
    {
        var data = touch.Player().GetBPlayer();
        if (!PlayerInfos.ContainsKey(data.Name))
        {
            data.SendInfoMsg("你尚未加入游戏。");
            return;
        }

        data.SendSuccessMsg("已选择橡皮擦。");
        var l = sender as Label;
        var info = PlayerInfos[data.Name];
        info.EraserSize = l.Width;
        info.SelectColor = PaintID.WhitePaint; // 橡皮擦本质是白色画笔
    }

    private void ColorSelect(BPlayer data, byte color)
    {
        if (!PlayerInfos.ContainsKey(data.Name))
        {
            data.SendInfoMsg("你尚未加入游戏。");
            return;
        }

        if (data != NowPainting)
        {
            data.SendInfoMsg($"现在是 {NowPainting.Name} 的回合。");
            return;
        }

        var info = PlayerInfos[data.Name];
        info.SelectColor = color;
        info.EraserSize = 0; // 选择颜色时，自动取消橡皮擦模式
        data.SendSuccessMsg($"已修改画笔颜色。");
    }

    private void Draw(object sender, Touch t)
    {
        try
        {
            var data = t.Player().GetBPlayer();
            if (t.X < 0 || t.X >= DRAWBOARD_WIDTH || t.Y < 0 || t.Y >= DrawboardHeight)
                return;

            if (!PlayerInfos.TryGetValue(data.Name, out var info))
            {
                if (t.State == TouchState.Begin)
                    data.SendInfoMsg("你尚未加入游戏, 请点击左侧加入。");
                return;
            }

            var point = new Point(t.X, t.Y);
            bool useEraser = info.EraserSize != 0 || t.Cutter;
            int size = t.Cutter ? 1 : useEraser ? info.EraserSize : 1;
            int offset = (size - 1) / 2;

            if (t.State == TouchState.Begin)
            {
                info.LastPoint = point;
                var currentStroke = new List<Label>();
                _strokes.Add(currentStroke);
                AddPointToBoard(point, size, offset, useEraser, info, currentStroke);
            }
            else if (t.Session.Used)
            {
                var currentStroke = _strokes.LastOrDefault();
                if (currentStroke != null)
                {
                    GetBresenhamLine(point, info.LastPoint).ForEach(p => AddPointToBoard(p, size, offset, useEraser, info, currentStroke));
                }
            }

            info.LastPoint = point;

            // 更新橡皮擦预览
            if (_eraserPreviewLabel != null)
            {
                Board.Remove(_eraserPreviewLabel);
                _eraserPreviewLabel = null;
            }
            if (useEraser && t.State != TouchState.End)
            {
                _eraserPreviewLabel = new Label(t.X - offset, t.Y - offset, size, size, "", new LabelStyle
                {
                    WallColor = PaintID.BlackPaint,
                    Wall = (byte?)WallID.DiamondGemspark
                });
                Board.Add(_eraserPreviewLabel);
            }

            Board.UpdateSelf();
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[Drawboard] 绘图时发生错误: {ex}");
        }
    }

    #endregion

    #region 核心逻辑

    /// <summary>
    /// 在画板上添加一个点（或橡皮擦块）
    /// </summary>
    private void AddPointToBoard(Point p, int size, int offset, bool useEraser, PlayerInfo info, List<Label> currentStroke)
    {
        var label = new Label(p.X - offset, p.Y - offset, size, size, "", new LabelStyle()
        {
            WallColor = useEraser ? PaintID.WhitePaint : info.SelectColor,
            Wall = (byte?)(useEraser ? WallID.DiamondGemspark : WallID.TopazGemspark)
        });
        Board.Add(label);
        Points.Add(label);
        currentStroke?.Add(label);
    }

    /// <summary>
    /// 保存当前画板内容到文件
    /// </summary>
    private void SaveDrawing(string filename)
    {
        var serializablePoints = Points.Select(l => new SerializablePoint
        {
            X = l.X,
            Y = l.Y,
            Width = l.Width,
            Height = l.Height,
            WallColor = (byte)(l.LabelStyle.WallColor ?? 0),
            Wall = (byte)(l.LabelStyle.Wall ?? 0)
        }).ToList();

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(serializablePoints, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Path.Combine(DrawingsDir, filename), json);
    }

    /// <summary>
    /// 使用Bresenham算法获取两点间的直线点集
    /// </summary>
    public static List<Point> GetBresenhamLine(Point p0, Point p1)
    {
        int x0 = p0.X;
        int y0 = p0.Y;
        int x1 = p1.X;
        int y1 = p1.Y;
        var points = new List<Point>();
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            points.Add(new Point(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
        return points;
    }

    #endregion
}

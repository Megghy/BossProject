using BossFramework;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using FakeProvider;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using TerrariaUI;
using TerrariaUI.Base;
using TerrariaUI.Base.Style;
using TerrariaUI.Widgets;
using TUIPlugin;
using static MonoMod.InlineRT.MonoModRule;

public class drawboard : BaseMiniGame
{
    public drawboard(Guid id) : base(id)
    {
    }

    public override string[] Names { get; } = new[] {"db", "drawboard", "画板" };
    public override string Author { get; }
    public override string Description { get; }

    public class Info
    {
        public int Score = 0;
        public byte SelectColor = 25;
        public Point LastPoint = new(-1, -1);
        public int EraserSize = 0;
    }

    public const int MaxPlayer = 5;
    public Panel MainPanel { get; set; }
    public VisualContainer Board { get; set; }
    public TileProvider FakePanel { get; set; }
    public List<Label> Points { get; set; }
    public Dictionary<int, List<Label>> UndoPoints { get; set; }
    public Dictionary<string, Info> GameInfo { get; set; }
    public BPlayer NowPainting { get; set; }

    public const int WIDTH = 150;
    public const int HEIGHT = 75;
    public const int DRAWBOARD_WIDTH = 120;
    int drawboardHeight => HEIGHT;

    public override void Dispose()
    {
        TUI.Destroy(MainPanel);
        FakeProviderAPI.Remove(FakePanel);
        Players.Clear();
        GameInfo.Clear();
        Points.Clear();
        UndoPoints.Clear();
    }

    public override void Init(BPlayer creator)
    {
        GameInfo = new();
        Points = new();
        UndoPoints = new();
        CreatePanel(creator.TileX - (WIDTH / 2), creator.TileY - (HEIGHT / 2), GId);
    }

    void CreatePanel(int x, int y, Guid id)
    {
        FakePanel = FakeProviderAPI.CreateTileProvider(id.ToString(), x, y, WIDTH, HEIGHT + 4);
        MainPanel = TUI.Create(new Panel(id.ToString(), x, y, WIDTH, HEIGHT + 4, new UIConfiguration() { Permission = "hy.game.move" }, null, FakePanel));

        var title = MainPanel.Add(new VisualContainer(0, 0, WIDTH, 4, null, new ContainerStyle
        {
            Wall = (byte?)WallID.DiamondGemspark,
            WallColor = PaintID.BlackPaint
        }, null), null); //最上部标题栏
        var control = MainPanel.Add(new VisualContainer(0, 4, 10, HEIGHT, null, new ContainerStyle
        {
            Wall = (byte?)WallID.DiamondGemspark,
            WallColor = PaintID.LimePaint
        }, null), null); //左侧按键栏
        Board = MainPanel.Add(new VisualContainer(10, 4, DRAWBOARD_WIDTH, drawboardHeight, new UIConfiguration() { }, new ContainerStyle
        {
            Wall = (byte?)WallID.DiamondGemspark,
            WallColor = PaintID.WhitePaint,

        }, null), null); //画板容器
        MainPanel.Add(new Label(10, 4, DRAWBOARD_WIDTH, drawboardHeight, "", new UIConfiguration() { UseBegin = true, UseMoving = true, UseEnd = true, }, null, Draw));//绘画板
        var brush = MainPanel.Add(new VisualContainer(WIDTH - 20, 4, 20, HEIGHT, null, new ContainerStyle
        {
            Wall = (byte?)WallID.DiamondGemspark,
            WallColor = PaintID.CyanPaint
        }, null), null);   //右侧控件容器
        var titleText = "draw and guess";
        title.Add(new Label((WIDTH / 2) - (titleText.Length / 2), 0, 30, 4, titleText, null, new LabelStyle()
        {
            TextAlignment = Alignment.Center,
            TextColor = PaintID.GrayPaint
        }));

        control.Add(new Label(0, 0, 10, 4, "Join", new UIConfiguration() { UseBegin = true }, new ButtonStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint,
            TextColor = PaintID.DeepGreenPaint,
            TextAlignment = Alignment.Center
        }, (self, touch) => Join(touch.Player().GetBPlayer())));

        brush.Add(new Label(0, 0, 20, 4, "color", null, new LabelStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint,
            TextAlignment = Alignment.Center,
            TextColor = PaintID.GrayPaint
        }));

        var colors = brush.Add(new VisualContainer(0, 4, 20, HEIGHT - 5, null, null, null), null);
        var clear = brush.Add(new Button(0, 15, 20, 4, "Clear", null, new ButtonStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint,
            TextAlignment = Alignment.Center,
            TextColor = PaintID.YellowPaint
        }, (self, touch) => MainPanel.Confirm("Are you sure you want to clear", value =>
        {
            if (value)
            {
                Points.ForEach(p => Board.Remove(p));
                Points.Clear();
                UndoPoints.Clear();
                Board.Update().Apply().Draw();
            }
        })));
        clear.Add(new Arrow(2, 1, new ArrowStyle()
        {
            TileColor = PaintID.YellowPaint,
            Direction = Direction.Left
        }));
        clear.Add(new Arrow(16, 1, new ArrowStyle()
        {
            TileColor = PaintID.YellowPaint,
            Direction = Direction.Right
        }));
        brush.Add(new Label(0, 20, 20, 4, "eraser", null, new LabelStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint,
            TextAlignment = Alignment.Center,
            TextColor = PaintID.GrayPaint
        }));
        var erasers = brush.Add(new VisualContainer(0, 25, 20, 9, null, new ContainerStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint
        }));
        var erasersContainer = erasers.Add(new VisualContainer(1, 1, 18, 7, null, new ContainerStyle()
        {
            Wall = (byte?)WallID.DiamondGemspark,
            WallColor = PaintID.WhitePaint
        }));
        erasersContainer.Add(new Button(2, 3, 1, 1, "", null, new ButtonStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.BlackPaint
        }, EraserSelect));
        erasersContainer.Add(new Button(6, 2, 3, 3, "", null, new ButtonStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.BlackPaint
        }, EraserSelect));
        erasersContainer.Add(new Button(12, 1, 5, 5, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.BlackPaint }, EraserSelect));
        var undo = brush.Add(new Button(0, 35, 20, 4, "undo", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint, TextAlignment = Alignment.Center, TextColor = PaintID.YellowPaint }, Undo));
        undo.Add(new Arrow(2, 1, new ArrowStyle()
        {
            TileColor = PaintID.YellowPaint,
            Direction = Direction.Left
        }));
        undo.Add(new Arrow(16, 1, new ArrowStyle()
        {
            TileColor = PaintID.YellowPaint,
            Direction = Direction.Right
        }));
        var save = brush.Add(new Button(0, 40, 20, 4, "save", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint, TextAlignment = Alignment.Center, TextColor = PaintID.YellowPaint }, (self, touch) => MainPanel.Confirm("Are you sure you want to save", value => { if (value) { Points.Clear(); Board.Update().Apply().Draw(); } }))) as Label;
        save.Add(new Arrow(2, 1, new ArrowStyle()
        {
            TileColor = PaintID.YellowPaint,
            Direction = Direction.Left
        }));
        save.Add(new Arrow(16, 1, new ArrowStyle()
        {
            TileColor = PaintID.YellowPaint,
            Direction = Direction.Right
        }));
        #region 颜色选择
        colors.Add(new Button(1, 1, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.BlackPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //黑色
        colors.Add(new Button(4, 1, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.GrayPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //灰色
        colors.Add(new Button(7, 1, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.WhitePaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //白色
        colors.Add(new Button(10, 1, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.ShadowPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //暗影
        colors.Add(new Button(13, 1, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.NegativePaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //反色
        colors.Add(new Button(16, 1, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.BrownPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //反色

        //第二排
        colors.Add(new Button(1, 4, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.RedPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //红色
        colors.Add(new Button(4, 4, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.OrangePaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //橙色
        colors.Add(new Button(7, 4, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.YellowPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //黄色
        colors.Add(new Button(10, 4, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.LimePaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //lime?
        colors.Add(new Button(13, 4, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.GreenPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //绿色
        colors.Add(new Button(16, 4, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.TealPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //teal

        //第三排
        colors.Add(new Button(1, 7, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.CyanPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //cyan
        colors.Add(new Button(4, 7, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.SkyBluePaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //天蓝
        colors.Add(new Button(7, 7, 3, 3, "", null, new ButtonStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.BluePaint,
        }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //蓝
        colors.Add(new Button(10, 7, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.PurplePaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //紫
        colors.Add(new Button(13, 7, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.VioletPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //紫罗兰
        colors.Add(new Button(16, 7, 3, 3, "", null, new ButtonStyle() { Wall = (byte?)WallID.TopazGemspark, WallColor = PaintID.PinkPaint }, (sender, data) => ColorSelect(data.Player().GetBPlayer(), (byte)((Label)sender).LabelStyle.WallColor))); //粉色
        #endregion


        MainPanel.UpdateSelf();
    }

    public override bool Join(BPlayer plr)
    {
        if (State == MiniGameState.Playing)
        {
            plr.SendErrorMsg("此场游戏已开始");
            return false;
        }
        else
        {
            if (Players.Count < MaxPlayer)
            {
                if (Players.Contains(plr))
                {
                    plr.SendInfoMsg("你已加入此场游戏");
                }
                else
                {
                    GameInfo.Add(plr.Name, new());
                    NowPainting = plr;
                    plr.SendSuccessMsg($"已加入, 当前人数 {Players.Count} 人: {string.Join(", ", Players)}");
                    return true;
                }
            }
            else
            {
                plr.SendInfoMsg("人数已满");
            }
        }
        return false;
    }

    public override void Start()
    {
    }

    public override void Stop()
    {
        Dispose();
    }

    public override void Update(long gameTime)
    {
    }

    void Undo(object sender, Touch touch) => MainPanel.Confirm("Are you sure you want to undo", value =>
    {
        if (value)
        {
            if (!UndoPoints.Any())
            {
                touch.Player().SendInfoMessage("未找到任何绘画记录");
                return;
            }
            var undoList = UndoPoints.Last();
            undoList.Value.ForEach(p =>
            {
                Points.Remove(p);
                Board.Remove(p);
            });
            UndoPoints.Remove(undoList.Key);
            Board.UpdateSelf();
        }
    });
    void EraserSelect(object sender, Touch touch)
    {
        var data = touch.Player().GetBPlayer();
        if (!Players.Contains(data))
        {
            data.SendInfoMsg("你尚未加入游戏");
            data.TsPlayer.Teleport(FakePanel.X * 16, FakePanel.Y * 16);
        }
        else
        {
            data.SendSuccessMsg("已选择橡皮擦");
            var l = sender as Label;
            var info = GameInfo[data.Name];
            info.EraserSize = l.Width;
        }

    }
    void ColorSelect(BPlayer data, byte color)
    {
        if (!Players.Contains(data))
        {
            data.SendInfoMsg("你尚未加入游戏");
            data.TsPlayer.Teleport(FakePanel.X * 16, FakePanel.Y * 16);
        }
        else
        {
            var info = GameInfo[data.Name];
            if (data == NowPainting)
            {
                info.SelectColor = color;
                info.EraserSize = 0;
                data.SendSuccessMsg($"已修改画笔颜色");
            }
            else
            {
                data.SendInfoMsg($"现在是 {NowPainting} 的回合");
            }
        }
    }
    public static List<Point> GetBresenhamLine(Point p0, Point p1)
    {
        int x0 = p0.X;
        int y0 = p0.Y;
        int x1 = p1.X;
        int y1 = p1.Y;
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        var points = new List<Point>();

        while (true)
        {
            points.Add(new Point(x0, y0));
            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err = err - dy;
                x0 = x0 + sx;
            }
            if (e2 < dx)
            {
                err = err + dx;
                y0 = y0 + sy;
            }
        }

        return points;
    }
    Label eraserLabel;
    void Draw(object sender, Touch t)
    {
        try
        {
            var data = t.Player().GetBPlayer();
            var state = t.State;
            var isCutter = t.Cutter;
            var x = t.X;
            var y = t.Y;
            if (x >= 0 && x < DRAWBOARD_WIDTH && y >= 0 && y < drawboardHeight)
            {
                if (!Players.Contains(data))
                {
                    if (state == TouchState.Begin)
                    {
                        data.SendInfoMsg("你尚未加入游戏");
                        data.TsPlayer.Teleport(FakePanel.X * 16, FakePanel.Y * 16);
                    }
                }
                else
                {
                    var info = GameInfo[data.Name];
                    var point = new Point(x, y);
                    bool useEraser = info.EraserSize != 0 || isCutter;
                    int eraserSize = isCutter ? 1 : useEraser ? info.EraserSize : 0;
                    int offset = eraserSize switch
                    {
                        0 => 0,
                        1 => 0,
                        3 => 1,
                        5 => 2
                    };

                    if (state == TouchState.Begin)
                    {
                        if (info.LastPoint.X == -1 && info.LastPoint.Y == -1) info.LastPoint = new Point(x, y);
                        var l = new Label(x - offset, y - offset, useEraser ? eraserSize : 1, useEraser ? eraserSize : 1, "", new LabelStyle()
                        {
                            WallColor = useEraser ? PaintID.WhitePaint : info.SelectColor,
                            Wall = (byte?)(useEraser ? WallID.DiamondGemspark : WallID.TopazGemspark)
                        });
                        UndoPoints.Add(UndoPoints.Any() ? UndoPoints.Last().Key + 1 : 0, new List<Label>() { l });
                        Board.Add(l);
                        Points.Add(l);
                    }
                    else if (t.Session.Used)
                    {
                        GetBresenhamLine(new(x, y), info.LastPoint).ForEach(p =>
                        {
                            var l = new Label(p.X - offset, p.Y - offset, useEraser ? eraserSize : 1, useEraser ? eraserSize : 1, "", new LabelStyle()
                            {
                                WallColor = useEraser ? PaintID.WhitePaint : info.SelectColor,
                                Wall = (byte?)(useEraser ? WallID.DiamondGemspark : WallID.TopazGemspark)
                            });
                            Board.Add(l);
                            Points.Add(l);
                            if (UndoPoints.Any()) UndoPoints.Last().Value.Add(l);
                        });
                    }
                    /*else
                    {
                        if (eraserLabel != null)
                        {
                            Board.Remove(eraserLabel);
                            eraserLabel = null;
                        }
                    }*/
                    if (eraserLabel != null)
                    {
                        Board.Remove(eraserLabel);
                        eraserLabel = null;
                    }
                    if (useEraser && state != TouchState.End)
                    {
                        eraserLabel = new Label(x - offset, y - offset, eraserSize, eraserSize, "", new LabelStyle()
                        {
                            WallColor = PaintID.BlackPaint,
                            Wall = (byte?)WallID.DiamondGemspark
                        });
                        Board.Add(eraserLabel);
                    }
                    info.LastPoint = point;
                    Board.UpdateSelf();
                    /*if (data == NowPainting)
                    {
                        if (state == TouchState.Begin)
                        {
                            if (info.LastPoint.X == -1 && info.LastPoint.Y == -1) info.LastPoint = new Point(x, y);
                            Board.Add(new Label(x, y, 1, 1, "", new LabelStyle() { WallColor = info.SelectColor }));
                            info.LastPoint = new Point(x, y);
                        }
                        else
                        {
                            GetBresenhamLine(new(x, y), info.LastPoint).ForEach(p => Board.Add(new Label(p.X, p.Y, 1, 1, "", new LabelStyle() { WallColor = info.SelectColor, Wall = (byte?)WallID.AmberGemspark })));
                            info.LastPoint = new Point(x, y);
                        }
                        Board.Update().Apply().Draw();
                    }
                    else
                    {
                        data.SendInfoMsg($"现在是 {NowPainting} 的回合");
                    }*/
                }
            }
        }
        catch (Exception ex) { TShockAPI.TShock.Log.ConsoleError(ex.Message); }
    }
}

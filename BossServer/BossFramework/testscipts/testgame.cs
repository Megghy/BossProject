using System.Collections.Generic;
using BossFramework;
using BossFramework.BInterfaces;
using BossFramework.BModels;
using FakeProvider;
using Terraria;
using Terraria.ID;
using TerrariaUI;
using TerrariaUI.Base;
using TerrariaUI.Base.Style;
using TerrariaUI.Widgets;
using TShockAPI;
using TUIPlugin;
using Label = TerrariaUI.Widgets.Label;

public class BackGammon : BaseMiniGame
{
    public class BG_Piece : VisualObject
    {
        public BG_Piece(int x, int y, int width, int height, UIConfiguration configuration = null, UIStyle style = null, System.Action<VisualObject, Touch> callback = null) : base(x, y, width, height, configuration, style, callback)
        {
        }

        protected override void ApplyThisNative()
        {
            base.ApplyThisNative();
        }
    }
    public enum Kind
    {
        None,
        Black,
        White
    };
    public BackGammon(System.Guid id) : base(id) { }
    public override string[] Names { get; } = new[] { "backgammon", "bg", "五子棋", "wzq" };
    public override string Author { get; } = "Megghy";
    public override string Description { get; } = "就是五子棋";
    public override int MaxCount => -1;

    #region 

    public Panel BGPanel { get; set; }
    public TileProvider FakePanel { get; set; }

    public BPlayer Black { get; set; }
    public BPlayer White { get; set; }

    public int BlackIgnoreCount { get; set; }
    public int WhiteIgnoreCount { get; set; }

    public Kind NowKind { get; set; }
    public void SetNowKind(Kind kind)
    {
        NowKind = kind;
        if (State == MiniGameState.Playing)
        {
            if (kind == Kind.White)
            {
                White.SendCombatMessage("现在是你的回合");
                NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(White.TRPlayer.position, 112, -1, 0.62f), White.Index);
            }
            else
            {
                Black.SendCombatMessage("现在是你的回合");
                NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(White.TRPlayer.position, 112, -1, 0.62f), White.Index);
            }
            NowKindLabel.UpdateTileColor(kind == Kind.Black ? PaintID.BlackPaint : PaintID.WhitePaint);
        }
    }

    public const int LENGTH = 20;
    public const int WAIT_SECOND = 30;

    public int[,] TileType { get; set; }
    public Kind[,] Maps { get; set; }

    public Label CountdownLabel { get; set; }
    public Label StatusLabel { get; set; }
    public Label NowKindLabel { get; set; }
    public Label StepCountLabel { get; set; }
    public VisualContainer CanHideContainer { get; set; }

    public List<VisualObject> Pieces { get; set; } = new();
    public VisualObject LastPiece = null;
    public int CD { get; set; }
    #endregion

    public override void Init(BPlayer creator)
    {
        Reset();
        CreatePanel(creator.TileX - 20, creator.TileY - 20);
        Join(creator);
    }

    public void Reset()
    {
        BlackIgnoreCount = 0;
        WhiteIgnoreCount = 0;
        State = MiniGameState.Waiting;
        NowKind = Kind.Black;
        Maps = new Kind[LENGTH, LENGTH];
        TileType = new int[LENGTH, LENGTH];
        CD = UPDATE_PER_SECOND * WAIT_SECOND;
        Pieces.Clear();
    }

    private void CreatePanel(int x, int y)
    {
        int width = LENGTH * 2;
        int height = LENGTH * 2;
        FakePanel = FakeProviderAPI.CreateTileProvider(GId.ToString(), x, y, width, height + 10);
        BGPanel = TUI.Create(new Panel(GId.ToString(), x, y, width, height + 10, new UIConfiguration() { Permission = MINIGAME_GUI_TOUCH_PERMISSION }, null, FakePanel));
        VisualContainer boardNode = BGPanel.Add(new VisualContainer(0, 10, LENGTH * 2, LENGTH * 2, null, new ContainerStyle
        {
            Wall = (byte?)WallID.DiamondGemspark,
            WallColor = PaintID.WhitePaint
        }, null));
        //创建棋盘
        ISize[] Checkerboard = new ISize[LENGTH];
        for (int i = 0; i < LENGTH; i++)
        {
            Checkerboard[i] = new Absolute(2);
        }
        boardNode.SetupGrid(Checkerboard, Checkerboard);
        //创建标题
        VisualContainer Title = BGPanel.Add(new VisualContainer(0, 0, 40, 10, null, new ContainerStyle
        {
            Wall = (byte?)WallID.RubyGemspark,
            WallColor = PaintID.WhitePaint
            //WallColor = PaintID.Brown
        }, null), null);
        Title.Add(new Label(0, 0, 40, 2, "backgammon", new LabelStyle()
        {
            TextAlignment = Alignment.Center,
            TextColor = PaintID.DeepOrangePaint
        }));
        CountdownLabel = Title.Add(new Label(1, 6, 4, 2, "00", new LabelStyle()//倒计时
        {
            TextAlignment = Alignment.Left
        }));
        CanHideContainer = Title.Add(new VisualContainer(0, 3, width, 2));
        CanHideContainer.Add(new Label(22, 0, 8, 2, "step", new LabelStyle()//步数
        {
            TextAlignment = Alignment.Left,
        }));
        CanHideContainer.Add(new Arrow(30, 0, new ArrowStyle()//步数
        {
            Direction = Direction.Right
        }));
        StepCountLabel = CanHideContainer.Add(new Label(33, 0, 6, 2, "000", new LabelStyle()//步数
        {
            TextAlignment = Alignment.Left,
            TextColor = PaintID.BluePaint
        }));
        //游戏状态
        NowKindLabel = CanHideContainer.Add(new Label(1, 0, 9, 2, "now ", new LabelStyle()
        {
            TextAlignment = Alignment.Left
        }));
        NowKindLabel.Add(new Label(7, 0, 2, 2, "", new LabelStyle()
        {
            Tile = 448
        }));
        CanHideContainer.Disable(false);
        Title.SetTop(CanHideContainer);
        StatusLabel = Title.Add(new Button(8, 6, 24, 2, "Join game", null, new ButtonStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint,
            TextColor = PaintID.DeepGreenPaint,
            TextAlignment = Alignment.Center,
            TriggerStyle = ButtonTriggerStyle.TouchEnd
        }, (VisualObject self, Touch touch) => Join(touch.Player().GetBPlayer())));
        for (int tempX = 0; tempX < LENGTH; tempX++)
        {
            for (int tempY = 0; tempY < LENGTH; tempY++)
            {
                //if ((tempX % 2 == 0 && tempY % 2 == 1) || (tempX % 2 == 1 && tempY % 2 == 0))
                if ((tempX + tempY) % 2 == 1)
                {
                    boardNode[tempX, tempY] = new(0, 0, 2, 2, null, new()
                    {
                        Wall = WallID.SapphireGemspark,
                        WallColor = PaintID.BrownPaint
                    });
                }
                else
                    boardNode[tempX, tempY] = new(0, 0, 2, 2, null, new()
                    {
                        Wall = WallID.DiamondGemspark,
                        WallColor = PaintID.BrownPaint
                    });
            }
        }
        //创建棋盘点击事件传递
        boardNode.Add(new VisualObject(0, 0, width, height, new UIConfiguration() { UseBegin = true }, null, OnCheckerboardClick));
        BGPanel.UpdateSelf();
    }

    public override bool Join(BPlayer plr)
    {
        if (plr.PlayingGame != null && plr.PlayingGame.GId != GId)
        {
            plr.SendErrorMsg("你已加入另一场比赛");
            return false;
        }
        if (Black is null)
        {
            Black = plr;
            plr.SendSuccessMsg($"已加入该五子棋对局, 你为 {"黑".Color("AAAAAA")} 方");

            CD = WAIT_SECOND * UPDATE_PER_SECOND;

            CountdownLabel.UpdateText(WAIT_SECOND);
            StatusLabel.UpdateTextColor(PaintID.DeepYellowPaint);
            return true;
        }
        else if (White is null && Black != plr)
        {
            White = plr;
            NowKind = Kind.Black;
            //修改对局状态标识
            State = MiniGameState.Playing;
            CountdownLabel.UpdateText(WAIT_SECOND);
            StatusLabel.UpdateTextColor(PaintID.DeepRedPaint);

            CD = WAIT_SECOND * UPDATE_PER_SECOND;

            White.SendSuccessMsg($"已加入该对局,你为 {"白".Color("FFFFFF")} 方, 黑方为 {Black.Name}");
            White.SendSuccessMsg("五子棋对局开始");
            Black.SendSuccessMsg("五子棋对局开始. 你为先手");
            //显示当前谁落子
            CanHideContainer.Enable(true);
            NowKindLabel.UpdateTileColor(PaintID.BlackPaint);
            BGPanel.UpdateSelf();
            return true;
        }
        else
        {
            plr.SendErrorMsg("该五子棋对局已开始");
            return false;
        }
    }
    public override void Leave(BPlayer plr)
    {
        if (!Players.Contains(plr))
            return;
        if (State == MiniGameState.Playing)
        {
            if (plr == White)
                Black.SendInfoMsg($"{plr} 离开了比赛");
            else
                White.SendInfoMsg($"{plr} 离开了比赛");
            GameOver(Kind.None);
        }
        else
        {
            plr.PlayingGame = default;
            if (plr == White)
                Black.SendInfoMsg($"{plr} 离开了比赛");
            else
                GameOver(Kind.None);
        }
    }
    public override void Start()
    {

    }

    public override void Stop()
    {
        Dispose();
    }
    public override void Dispose()
    {
        if (State == MiniGameState.Disposed)
            return;
        GameOver(Kind.None);
        State = MiniGameState.Disposed;
        TUI.Destroy(BGPanel);
        FakeProviderAPI.Remove(FakePanel);
        TileType = null;
        Maps = null;
        Pieces.Clear();
    }
    public override void Update(long gameTime)
    {
        CD -= 1;
        if (CD % (UPDATE_PER_SECOND / 2) == 0 && LastPiece != null)
        {
            if (LastPiece.Enabled)
                LastPiece.Disable(true);
            else
                LastPiece.Enable(true);
            BGPanel.UpdateSelf();
        }
        if (CD % UPDATE_PER_SECOND != 0)
            return;
        if (State == MiniGameState.Playing)
        {
            if (CD < 0)
            {
                if (BlackIgnoreCount >= 2)
                {
                    White.SendSuccessMsg($"{Black.Color("AAAAAA")} 3次未落子, 你取得了胜利");
                    Black.SendInfoMsg("你三次未落子, 对局失败");
                    GameOver(Kind.White);
                }
                else if (WhiteIgnoreCount >= 2)
                {
                    Black.SendSuccessMsg($"{White.Color("ffffff")} 3次未落子, 你取得了胜利");
                    White.SendInfoMsg("你三次未落子, 对局失败");
                    GameOver(Kind.Black);
                }
                else
                {
                    if (NowKind == Kind.Black)
                    {
                        BlackIgnoreCount++;
                        White.SendSuccessMsg($"{Black.Color("AAAAAA")} 未落子, 现在是你的回合");
                        Black.SendInfoMsg($"你已{BlackIgnoreCount}次未落子, 3次时将判定为失败");
                        SetNowKind(Kind.White);
                    }
                    else
                    {
                        WhiteIgnoreCount++;
                        Black.SendSuccessMsg($"{White.Color("ffffff")} 未落子, 现在是你的回合");
                        White.SendInfoMsg($"你已{WhiteIgnoreCount}次未落子, 3次时将判定为失败");
                        SetNowKind(Kind.Black);
                    }
                }
                CD = WAIT_SECOND * UPDATE_PER_SECOND;
            }

        }
        else if (CD < 0)
        {
            Black?.SendInfoMsg($"没有玩家加入, 对局取消");
            Stop();
            return;
        }

        if (CD % (2 * UPDATE_PER_SECOND) == 0)
            BUtils.SendCombatMessage($"状态: {(State == MiniGameState.Playing ? $"进行中. [黑: {Black}] VS [白: {White}]" : "等待中...")}", (BGPanel.X + (BGPanel.Width / 2)) * 16, BGPanel.Y * 16 + 45, default, false);
        CountdownLabel.SetText((CD / UPDATE_PER_SECOND).ToString("00"));
        BGPanel.UpdateSelf();
    }

    void GetColor(int x, int y, out int tile, out short tileColor)
    {
        if ((x + y) % 2 == 1)
            tile = 446;
        else
            tile = 447;
        tileColor = NowKind == Kind.White ? PaintID.WhitePaint : PaintID.BlackPaint;
    }
    void OnCheckerboardClick(VisualObject self, Touch touch)
    {
        var plr = touch.Player();
        var bplr = BUtils.GetBPlayer(plr);
        if (State == MiniGameState.Playing)
        {
            if (bplr != White && bplr != Black)
            {
                bplr.SendErrorMsg("该五子棋对局进行中");
                return;
            }
            if ((NowKind == Kind.Black && bplr == White) || (NowKind == Kind.White && bplr == Black))
            {
                (NowKind == Kind.Black ? White : Black).SendErrorMsg("现在不是你的回合");
                return;
            }
            int x = touch.X / 2;
            int y = touch.Y / 2;
            if (Maps[x, y] != 0)
                return;
            //因为传递麻烦,就懒得另外写个Draw了
            VisualObject node = BGPanel.GetChild(0);
            GetColor(x, y, out var tile, out var tileColor);

            VisualObject piece = node.Add(new VisualObject(x * 2, y * 2, 2, 2, null, new UIStyle()
            {
                Tile = (ushort)tile,
                TileColor = (byte?)tileColor
            }));
            StepCountLabel.UpdateText(Pieces.Count.ToString("000"));
            NowKindLabel.UpdateTileColor(NowKind == Kind.Black ? PaintID.BlackPaint : PaintID.WhitePaint);
            //检查棋盘是否已满,同时储存一下棋子
            TileType[x, y] = (short)tile;
            int piecenum = 0;
            foreach (int i1 in TileType)
            {
                if (i1 != 0)
                {
                    piecenum++;
                }
            }
            Pieces.Add(piece);
            Maps[x, y] = NowKind;
            if (Pieces.Count >= LENGTH * LENGTH)
            {
                White.SendInfoMsg($"棋盘已满,本局平局");
                Black.SendInfoMsg($"棋盘已满,本局平局");
                GameOver(Kind.None);
                return;
            }
            CD = WAIT_SECOND * UPDATE_PER_SECOND;
            //判断是否胜利
            Settlement(x, y);
            CountdownLabel.UpdateText(WAIT_SECOND.ToString());
            //切换下一位落子者
            if (State != MiniGameState.Playing)
                return;
            var lastPiece = LastPiece;
            LastPiece = piece;//设置最后一个输入的棋子
            lastPiece?.Enable(true);
            BGPanel.UpdateSelf();

            SetNowKind(NowKind == Kind.Black ? Kind.White : Kind.Black);
        }
        else
        {
            if (Black is null)
                bplr.SendInfoMsg($"该对局尚无玩家, 点击上方的 {"Join Game".Color("A8D9D0")} 来加入对局");
            else if (White is null)
            {
                if (bplr != Black)
                    bplr.SendInfoMsg($"点击上方的 {"Join Game".Color("A8D9D0")} 来加入对局");
                else
                    plr.SendErrorMessage("另一位玩家尚未加入");
            }
            else
                bplr.SendErrorMsg("对局已满");
        }
    }
    private void Settlement(int row, int col)
    {
        int n = 0;
        for (int j = col - 4; j <= col + 4; j++)
        {
            //如果超过索引则跳过
            if (j < 0 || j >= LENGTH)
                continue;
            //否则检查连子情况
            if (Maps[row, j] == NowKind)
            {
                n++;
            }
            else
            {
                n = 0;
            }
            if (n == 5) GameOver(NowKind);
        }

        //检查同一竖排（变行）
        n = 0;
        for (int i = row - 4; i <= row + 4; i++)
        {
            //如果超过索引则跳过
            if (i < 0 || i >= 20)
                continue;
            if (Maps[i, col] == NowKind)
            {
                n++;
            }
            else
            {
                n = 0;
            }
            if (n == 5) GameOver(NowKind);

        }

        //检查左上到右下斜
        n = 0;
        for (int i = row - 4, j = col - 4; i <= row + 4; i++, j++)
        {
            //如果超过索引则跳过
            if (i < 0 || i >= 20 || j < 0 || j >= 20)
                continue;
            if (Maps[i, j] == NowKind)
            {
                n++;
            }
            else
            {
                n = 0;
            }
            if (n == 5) GameOver(NowKind);
        }

        //检查左下到右上
        //检查左上到右下斜
        n = 0;
        for (int i = row + 4, j = col - 4; i >= row - 4; i--, j++)
        {
            //如果超过索引则跳过
            if (i < 0 || i >= 19 || j < 0 || j >= 19)
                continue;
            if (Maps[i, j] == NowKind)
            {
                n++;
            }
            else
            {
                n = 0;
            }
            if (n == 5) GameOver(NowKind);
        }
    }
    public void GameOver(Kind winer)
    {
        State = MiniGameState.End;
        var plr = winer == Kind.Black ? Black : White;
        if (White != null)
        {
            if (winer != Kind.None)
                White?.SendSuccessMsg($"对局结束,胜者为: [c/ff34fd:{plr.Name}]. 总步数: {Pieces.Count}");
            White.PlayingGame = null;
            White = null;
        }
        if (Black != null && Black != plr)
        {
            if (winer != Kind.None)
                Black.SendSuccessMsg($"对局结束,胜者为: [c/ff34fd:{plr.Name}]. 总步数: {Pieces.Count}");
            Black.PlayingGame = null;
            Black = null;
        }
        if (plr is not null)
        {
            int num = Projectile.NewProjectile(Projectile.GetNoneSource(), plr.TRPlayer.position.X, plr.TRPlayer.position.Y - 64f, 0f, -8f, 167, 0, 0f, 255, 0f, 0f);
            Main.projectile[num].Kill();
        }
        TileType = new int[LENGTH, LENGTH];
        Maps = new Kind[LENGTH, LENGTH];
        var panel = BGPanel.GetChild(0);
        Pieces.ForEach(p => panel.Remove(p));
        Pieces.Clear();
        NowKind = Kind.None;
        try
        {
            StatusLabel.UpdateTextColor(PaintID.DeepGreenPaint);
            CD = WAIT_SECOND * UPDATE_PER_SECOND;
            StepCountLabel.SetText("000");
            LastPiece = null;
            CanHideContainer.Disable(true);
        }
        catch { }
        BGPanel.UpdateSelf();
    }
}


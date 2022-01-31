using BossPlugin;
using BossPlugin.BCore;
using BossPlugin.BInterfaces;
using BossPlugin.BModels;
using FakeProvider;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using TerrariaUI;
using TerrariaUI.Base;
using TerrariaUI.Base.Style;
using TerrariaUI.Widgets;
using TShockAPI;

public class BackGammon : IMiniGame
{
    public BackGammon() { }
    /// <summary>
    /// 同时允许允许的小游戏数量
    /// </summary>
    public int MaxCount => 100;
    /// <summary>
    /// 小游戏名称
    /// </summary>
    public string[] Names { get; } = { "五子棋", "backgammon", "bg" };
    /// <summary>
    /// 状态
    /// </summary>
    public MiniGameState State { get; set; } = MiniGameState.Waiting;
    /// <summary>
    /// 游玩的玩家
    /// </summary>
    public List<BPlayer> Players { get; } = new List<BPlayer>();

    #region 局内变量
    public string PanelID = Guid.NewGuid().ToString();
    public readonly int BOARD_SIZE = 20; //每边棋子数量, 不是长度
    public readonly int WIDTH = 40; //面板宽度
    public readonly int HEIGHT = 40; //面板高度
    public readonly int TITLE_HEIGHT = 10; //标题高度

    public readonly int MAX_TIMEOUT_COUNT = 3; //最大超时次数
    public readonly int ROUND_TIME = 30; //每回合的等待时间, 单位秒

    public enum Kind { None, Black, White };

    public Panel BGPanel { get; set; }
    public TileProvider FakePanel { get; set; }
    public BPlayer Black { get; set; }
    public BPlayer White { get; set; }
    public int BlackTimeoutCount { get; set; }
    public int WhiteTimeoutCount { get; set; }
    public Kind NowKind { get; set; }
    public int[,] TileType { get; set; }
    public Kind[,] PiecesMaps { get; set; }
    public Label CountdownLabel { get; set; }
    public Label StatusLabel { get; set; }
    public Label NowKindLabel { get; set; }
    public List<VisualObject> Pieces { get; set; }
    public int CD { get; set; }
    #endregion

    #region 进程操作
    /// <summary>
    /// 初始化小游戏
    /// </summary>
    /// <param name="creator">创建者</param>
    public void Init(BPlayer creator = null)
    {
        var x = creator.TsPlayer.TileX - (WIDTH / 2);
        var y = creator.TsPlayer.TileY - (HEIGHT / 2);
        FakePanel = FakeProviderAPI.CreateTileProvider(PanelID, x, y, WIDTH, HEIGHT + TITLE_HEIGHT);
        BGPanel = TUI.Create(new Panel(PanelID, x, y, WIDTH, HEIGHT + TITLE_HEIGHT, new UIConfiguration() { Permission = "boss.panel.move" }, null, FakePanel));
        VisualContainer node = BGPanel.Add(new VisualContainer(0, 10, 40, 40, null, new ContainerStyle
        {
            Wall = (byte?)WallID.DiamondGemspark,
            WallColor = PaintID.WhitePaint
        }, null), null);
        //创建棋盘
        ISize[] Checkerboard = new ISize[BOARD_SIZE];
        BOARD_SIZE.ForEach(i => Checkerboard[i] = new Absolute(2)); //生成棋盘内网格
        node.SetupGrid(Checkerboard, Checkerboard);
        //创建棋盘点击事件传递
        node.Add(new VisualObject(0, 0, WIDTH, HEIGHT, new UIConfiguration() { UseBegin = true }, null, OnCheckerboardClick), null);
        //创建标题
        VisualContainer Title = BGPanel.Add(new VisualContainer(0, 0, 40, 10, null, new ContainerStyle
        {
            Wall = (byte?)WallID.RubyGemspark,
            WallColor = PaintID.WhitePaint
        }, null), null);
        Title.Add(new Label(0, 0, WIDTH, 2, "backgammon", new LabelStyle()
        {
            TextAlignment = Alignment.Center,
            TextColor = PaintID.DeepOrangePaint
        }));
        CountdownLabel = Title.Add(new Label(1, 6, 4, 2, "00", new LabelStyle()//倒计时
        {
            TextAlignment = Alignment.Left
        }));
        //游戏状态
        NowKindLabel = Title.Add(new Label(30, 3, 9, 2, "now ", new LabelStyle()
        {
            TextAlignment = Alignment.Left
        }));
        NowKindLabel.Add(new Label(7, 0, 2, 2, "", new LabelStyle()
        {
            Tile = 448
        }));
        NowKindLabel.Disable(true);
        Title.SetTop(NowKindLabel);
        StatusLabel = Title.Add(new Button(8, 6, 24, 2, "Join game", null, new ButtonStyle()
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint,
            TextColor = PaintID.DeepGreenPaint,
            TextAlignment = Alignment.Center,
            TriggerStyle = ButtonTriggerStyle.TouchEnd
        }, (VisualObject self, Touch touch) => Join(touch.Player().GetBPlayer())));
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            if (i % 2 == 0)
            {
                for (int ii = 0; ii < BOARD_SIZE; ii++)
                {
                    if (ii % 2 == 0)
                    {
                        for (int iii = 0; iii < BOARD_SIZE; iii += 2)
                        {
                            node[ii, iii] = new VisualObject(0, 0, 2, 2, null, new UIStyle() { Wall = WallID.SapphireGemspark, WallColor = PaintID.WhitePaint });
                            node[iii, ii] = new VisualObject(0, 0, 2, 2, null, new UIStyle() { Wall = WallID.SapphireGemspark, WallColor = PaintID.WhitePaint });
                        }
                    }
                    else
                    {
                        for (int iii = 1; iii < BOARD_SIZE; iii += 2)
                        {
                            node[ii, iii] = new VisualObject(0, 0, 2, 2, null, new UIStyle() { Wall = WallID.SapphireGemspark, WallColor = PaintID.WhitePaint });
                            node[iii, ii] = new VisualObject(0, 0, 2, 2, null, new UIStyle() { Wall = WallID.SapphireGemspark, WallColor = PaintID.WhitePaint });
                        }
                    }
                }
            }
        }
        BGPanel.UpdateSelf();
    }
    /// <summary>
    /// 开始
    /// </summary>
    public void Start()
    {
        //不是通过命令开始的
    }
    public void Update(long gameTime)
    {
        CD -= MiniGameManager.UPDATE_PRE_SECEND;
        if (State == MiniGameState.Playing)
        {
            if (CD < 0)
            {
                if (!CheckTimeOut())
                {
                    if (NowKind == Kind.Black)
                    {
                        BlackTimeoutCount++;
                        White.SendSuccessEX($"{Black.Color("AAAAAA")} 未落子, 现在是你的回合");
                        Black.SendInfoEX($"你已{BlackTimeoutCount}次未落子, 3次时将判定为失败");
                        NowKind = Kind.White;
                    }
                    else
                    {
                        WhiteTimeoutCount++;
                        Black.SendSuccessEX($"{White.Color("ffffff")} 未落子, 现在是你的回合");
                        White.SendInfoEX($"你已{WhiteTimeoutCount}次未落子, 3次时将判定为失败");
                        NowKind = Kind.Black;
                    }
                }
                CD = ROUND_TIME * MiniGameManager.UPDATE_PRE_SECEND;
            }

        }
        else if (CD < 0)
        {
            Black?.SendInfoEX($"没有玩家加入, 对局取消");
            Stop();
            return;
        }

        if (CD % MiniGameManager.UPDATE_PRE_SECEND == 0)
            BossPlugin.Utils.SendCombatMessage($"状态: {(State == MiniGameState.Playing ? $"进行中. [{Black}] VS [{White}]" : "等待中...")}", (BGPanel.X + (BGPanel.Width / 2)) * 16, BGPanel.Y * 16 + 45, Color.White, false);
        CountdownLabel.UpdateText(CD / MiniGameManager.UPDATE_PRE_SECEND);
    }
    /// <summary>
    /// 玩家加入游戏
    /// </summary>
    /// <param name="player"></param>
    public bool Join(BPlayer plr)
    {
        if (plr.IsInGame())
        {
            plr.SendErrorEX("你已加入一场比赛");
            return false;
        }
        if (Black == null)
        {
            Black = plr;
            plr.SendSuccessEX($"已加入该五子棋对局, 你为 {"黑".Color("AAAAAA")} 方");

            CD = ROUND_TIME * MiniGameManager.UPDATE_PRE_SECEND;

            CountdownLabel.UpdateText(CD / MiniGameManager.UPDATE_PRE_SECEND);
            StatusLabel.UpdateTextColor(PaintID.DeepYellowPaint);
            return true;
        }
        else if (White == null && plr != Black)
        {
            //修改对局状态标识
            State = MiniGameState.Playing;
            White = plr;
            NowKind = Kind.Black;
            CountdownLabel.UpdateText(CD / MiniGameManager.UPDATE_PRE_SECEND);
            StatusLabel.UpdateTextColor(PaintID.DeepRedPaint);

            CD = ROUND_TIME * MiniGameManager.UPDATE_PRE_SECEND;

            White.SendSuccessEX($"已加入该对局,你为 {"白".Color("FFFFFF")} 方");
            White.SendSuccessEX("五子棋对局开始");
            Black.SendSuccessEX("五子棋对局开始. 你为先手");
            //显示当前谁落子
            NowKindLabel.Enable(true);
            NowKindLabel.UpdateTileColor(PaintID.BlackPaint);
            BGPanel.Update().Apply().Draw();
            return true;
        }
        else
        {
            plr.SendInfoEX("该五子棋对局已开始");
            return false;
        }
    }
    /// <summary>
    /// 玩家离开游戏
    /// </summary>
    /// <param name="player"></param>
    public void Leave(BPlayer plr)
    {
        if (State == MiniGameState.Playing)
        {
            if (plr == White)
                Black.SendInfoEX($"{plr} 离开了比赛");
            else
                White.SendInfoEX($"{plr} 离开了比赛");
            GameOver(Kind.None);
        }
        else
        {
            if (plr == White)
                Black.SendInfoEX($"{plr} 离开了比赛");
            else
                GameOver(Kind.None);
        }
    }
    /// <summary>
    /// 尝试暂停
    /// </summary>
    public void Pause()
    {
        //也许哪天会加?
    }
    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        State = MiniGameState.End;
        TUI.Destroy(BGPanel);
        FakePanel.Dispose();
        TileType = null;
        PiecesMaps = null;
        Pieces.Clear();
    }
    #endregion

    #region 游戏方法
    public void ChangeKind(Kind k)
    {
        if (State == MiniGameState.Playing)
        {
            if (k == Kind.White)
            {
                White.SendCombatMessage("现在是你的回合");
                NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(White.TsPlayer.TPlayer.position, 112, -1, 0.62f), White.TsPlayer.Index);
            }
            else
            {
                Black.SendCombatMessage("现在是你的回合");
                NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(White.TsPlayer.TPlayer.position, 112, -1, 0.62f), White.TsPlayer.Index);
            }
            NowKindLabel.UpdateTileColor(k == Kind.Black ? PaintID.BlackPaint : PaintID.WhitePaint);
        }
    }
    private bool CheckTimeOut()
    {
        if (BlackTimeoutCount + 1 > 2)
        {
            White.SendSuccessEX($"{Black.Color("AAAAAA")} 3次未落子, 你取得了胜利");
            Black.SendInfoEX("你三次未落子, 对局失败");
            GameOver(Kind.White);
            return true;
        }
        else if (WhiteTimeoutCount + 1 > 2)
        {
            Black.SendSuccessEX($"{White.Color("ffffff")} 3次未落子, 你取得了胜利");
            White.SendInfoEX("你三次未落子, 对局失败");
            GameOver(Kind.Black);
            return true;
        }
        else
            return false;
    }
    int GetTile(int x, int y)
    {
        var tile = 0;
        //脑瘫判断 不过也懒得想别的写法
        int tempx = 1;
        int tempy = 1;
        int tempx1 = 1;
        int tempy1 = 1;
        if (x + 1 > 19)
        {
            tempx = 0;
        }
        if (x - 1 < 0)
        {
            tempx1 = 0;
        }
        if (y + 1 > 19)
        {
            tempy = 0;
        }
        if (y - 1 < 0)
        {
            tempy1 = 0;
        }
        if (TileType[x + tempx, y] == 448 || TileType[x - tempx1, y] == 448 || TileType[x, y + tempy] == 448 || TileType[x, y - tempy1] == 448)
        {
            if (TileType[x + tempx, y] == 446 || TileType[x - tempx1, y] == 446 || TileType[x, y + tempy] == 446 || TileType[x, y - tempy1] == 446)
            {
                tile = 447;
            }
            else
            {
                tile = 446;
            }
        }
        else
        {
            tile = 448;
        }
        return tile;
    }
    void OnCheckerboardClick(VisualObject self, Touch touch)
    {
        var plr = touch.Player().GetBPlayer();
        if (State == MiniGameState.Playing)
        {
            if (plr != Black && plr != White)
            {
                plr.SendInfoEX("该五子棋对局进行中");
                return;
            }
            if ((NowKind == Kind.Black && plr == White) || (NowKind == Kind.White && plr == Black))
            {
                (NowKind == Kind.Black ? White : Black).SendInfoEX("现在不是你的回合");
                return;
            }
            int x = touch.X / 2;
            int y = touch.Y / 2;
            //因为传递麻烦,就懒得另外写个Draw了
            VisualObject node = BGPanel.GetChild(0);
            var tile = GetTile(x, y);
            var tileColor = NowKind == Kind.White ? PaintID.WhitePaint : PaintID.BlackPaint;

            VisualObject piece = node.Add(new VisualObject(x * 2, y * 2, 2, 2, null, new UIStyle()
            {
                Tile = (ushort)tile,
                TileColor = (byte?)tileColor
            }));
            BGPanel.Update().Apply().Draw();
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
            PiecesMaps[x, y] = NowKind;
            if (Pieces.Count >= BOARD_SIZE * BOARD_SIZE)
            {
                White.SendSuccessEX($"棋盘已满,本局平局");
                Black.SendSuccessEX($"棋盘已满,本局平局");
                GameOver(Kind.None);
                return;
            }
            CD = ROUND_TIME * MiniGameManager.UPDATE_PRE_SECEND; //重置倒计时
            //判断是否胜利
            Settlement(x, y);
            CountdownLabel.UpdateText(CD / MiniGameManager.UPDATE_PRE_SECEND);
            //切换下一位落子者
            if (State != MiniGameState.Playing)
                return;
            ChangeKind(NowKind == Kind.Black ? Kind.White : Kind.Black);
        }
        else
        {
            if (Black == null)
            {
                plr.SendInfoEX($"该对局尚无玩家, 点击上方的 {"Join Game".Color("A8D9D0")} 来加入对局");
            }
            else if (White == null)
            {
                if (plr != Black)
                {
                    plr.SendInfoEX($"点击上方的 {"Join Game".Color("A8D9D0")} 来加入对局");
                }
                else plr.SendErrorEX("另一位玩家尚未加入");
            }
            else
            {
                plr.SendErrorEX("对局进行中");
            }
        }


    }
    private void Settlement(int row, int col)
    {
        int n = 0;
        for (int j = col - 4; j <= col + 4; j++)
        {
            //如果超过索引则跳过
            if (j < 0 || j >= BOARD_SIZE)
                continue;
            //否则检查连子情况
            if (PiecesMaps[row, j] == NowKind)
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
            if (PiecesMaps[i, col] == NowKind)
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
            if (PiecesMaps[i, j] == NowKind)
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
            if (PiecesMaps[i, j] == NowKind)
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
        var msg = $"对局结束,胜者为: [c/ff34fd:{plr.Name}]. 总步数: {Pieces.Count}";
        if (White != null)
        {
            if (winer != Kind.None)
                White?.SendSuccessEX(msg);
            White = null;
        }
        if (Black != null)
        {
            if (winer != Kind.None)
                Black.SendSuccessEX(msg);
            Black = null;
        }
        if (plr != null)
        {
            int num = Projectile.NewProjectile(Projectile.GetNoneSource(), plr.TsPlayer.TPlayer.position.X, plr.TsPlayer.TPlayer.position.Y - 64f, 0f, -8f, 167, 0, 0f, 255, 0f, 0f);
            Main.projectile[num].Kill();
        }
        ResetPanel();
    }
    private void ResetPanel()
    {
        TileType = new int[BOARD_SIZE, BOARD_SIZE];
        PiecesMaps = new Kind[BOARD_SIZE, BOARD_SIZE];
        var panel = BGPanel.GetChild(0);
        Pieces.ForEach(p => panel.Remove(p));
        Pieces.Clear();
        NowKind = Kind.None;
        StatusLabel.UpdateTextColor(PaintID.DeepGreenPaint);
        CD = ROUND_TIME * MiniGameManager.UPDATE_PRE_SECEND; ;
        NowKindLabel.Disable(true);
        BGPanel.UpdateSelf();
    }
    #endregion
}
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
using TUIPlugin;
using Label = TerrariaUI.Widgets.Label;

/// <summary>
/// 表示一个五子棋小游戏实例。
/// </summary>
public class FiveInARowGame : BaseMiniGame
{
    #region 常量与字段

    /// <summary>
    /// 棋盘大小 (N x N)。
    /// </summary>
    private const int BOARD_SIZE = 15;

    /// <summary>
    /// 每回合的思考时间（秒）。
    /// </summary>
    private const int TIMEOUT_SECONDS = 30;

    /// <summary>
    /// 超时多少次后判负。
    /// </summary>
    private const int MAX_TIMEOUT_COUNT = 3;

    /// <summary>
    /// 获胜需要的连子数量。
    /// </summary>
    private const int WIN_CONDITION = 5;

    private TileProvider _tileProvider;
    private Panel _gamePanel;

    private PlayerRole[,] _boardState;
    private PlayerRole _currentTurn;
    private int _turnCountdown;

    private int _blackTimeouts;
    private int _whiteTimeouts;

    // UI元素
    private Label _countdownLabel;
    private Label _statusLabel;
    private Label _nowKindLabel;
    private Label _stepCountLabel;
    private VisualContainer _turnInfoContainer;
    private readonly List<VisualObject> _pieces = new();
    private VisualObject _lastPlacedPiece;

    #endregion

    #region 游戏属性

    /// <summary>
    /// 玩家角色/棋子颜色。
    /// </summary>
    public enum PlayerRole
    {
        None,
        Black,
        White
    }

    /// <summary>
    /// 执黑方的玩家。
    /// </summary>
    public BPlayer BlackPlayer { get; private set; }

    /// <summary>
    /// 执白方的玩家。
    /// </summary>
    public BPlayer WhitePlayer { get; private set; }

    #endregion

    /// <summary>
    /// 初始化一个新的五子棋游戏实例。
    /// </summary>
    /// <param name="id">游戏 GUID。</param>
    public FiveInARowGame(Guid id) : base(id) { }

    #region 公共方法 (生命周期)

    public override string[] Names { get; } = { "fiveinarow", "fir", "五子棋", "wzq" };
    public override string Author { get; } = "Megghy (重构 by Gemini)";
    public override string Description { get; } = "经典的五子棋游戏。";
    public override int MaxCount => 2;

    public override void Init(BPlayer creator)
    {
        ResetGame();
        CreateGameBoard(creator.TileX - 20, creator.TileY - 20);
        Join(creator);
    }

    public override void Start() { }

    public override bool Join(BPlayer player)
    {
        if (player.PlayingGame != null && player.PlayingGame.GId != GId)
        {
            player.SendErrorMsg("你已经加入了另一场游戏。");
            return false;
        }

        if (BlackPlayer == null)
        {
            BlackPlayer = player;
            player.SendSuccessMsg("成功加入对局，你是【黑方】。");
            ResetTurnCountdown();
            return true;
        }

        if (WhitePlayer == null && BlackPlayer != player)
        {
            WhitePlayer = player;
            StartGame();
            return true;
        }

        player.SendErrorMsg(State == MiniGameState.Playing ? "该对局已开始。" : "该对局已满员。");
        return false;
    }

    public override void Leave(BPlayer player)
    {
        if (player != BlackPlayer && player != WhitePlayer)
            return;

        if (State == MiniGameState.Playing)
        {
            // 游戏中离开，判对方胜利
            var winner = (player == WhitePlayer) ? PlayerRole.Black : PlayerRole.White;
            var winnerPlayer = (winner == PlayerRole.Black) ? BlackPlayer : WhitePlayer;
            winnerPlayer?.SendInfoMsg($"{player.Name} 中途退出了游戏，你获得了胜利！");
            GameOver(winner);
        }
        else
        {
            // 等待阶段离开，直接结束游戏
            var otherPlayer = (player == BlackPlayer) ? WhitePlayer : BlackPlayer;
            otherPlayer?.SendInfoMsg($"{player.Name} 离开了游戏，对局已取消。");
            BlackPlayer?.SendInfoMsg("你离开了游戏，对局已取消。");
            GameOver(PlayerRole.None); // 无胜者
        }
    }

    public override void Update(long gameTime)
    {
        _turnCountdown--;

        // 处理最后一个棋子的闪烁效果
        if (_turnCountdown % (UPDATE_PER_SECOND / 2) == 0 && _lastPlacedPiece != null)
        {
            if (_lastPlacedPiece.Enabled)
                _lastPlacedPiece.Disable(true);
            else
                _lastPlacedPiece.Enable(true);
            _gamePanel.UpdateSelf();
        }

        if (_turnCountdown % UPDATE_PER_SECOND != 0)
            return;

        _countdownLabel.SetText((_turnCountdown / UPDATE_PER_SECOND).ToString("00"));
        _gamePanel.UpdateSelf();

        if (State == MiniGameState.Playing)
        {
            if (_turnCountdown < 0)
            {
                HandleTimeout();
            }
        }
        else if (State == MiniGameState.Waiting && _turnCountdown < 0)
        {
            BlackPlayer?.SendInfoMsg("由于长时间无人加入，对局已自动取消。");
            GameOver(PlayerRole.None);
        }
    }

    public override void Stop()
    {
        Dispose();
    }

    public override void Dispose()
    {
        if (State == MiniGameState.Disposed)
            return;

        GameOver(PlayerRole.None);
        State = MiniGameState.Disposed;

        if (_gamePanel != null)
        {
            TUI.Destroy(_gamePanel);
            _gamePanel = null;
        }
        if (_tileProvider != null)
        {
            FakeProviderAPI.Remove(_tileProvider);
            _tileProvider = null;
        }
    }

    #endregion

    #region 核心游戏逻辑

    /// <summary>
    /// 重置游戏状态到初始。
    /// </summary>
    private void ResetGame()
    {
        _blackTimeouts = 0;
        _whiteTimeouts = 0;
        State = MiniGameState.Waiting;
        _currentTurn = PlayerRole.Black;
        _boardState = new PlayerRole[BOARD_SIZE, BOARD_SIZE];
        _pieces.Clear();
        _lastPlacedPiece = null;
        ResetTurnCountdown();
    }

    /// <summary>
    /// 正式开始游戏。
    /// </summary>
    private void StartGame()
    {
        State = MiniGameState.Playing;
        _statusLabel.UpdateText("游戏中");
        _statusLabel.UpdateTextColor(PaintID.DeepRedPaint);
        _turnInfoContainer.Enable(true);

        WhitePlayer.SendSuccessMsg($"成功加入对局，你是【白方】，对手是 {BlackPlayer.Name}。");
        BlackPlayer.SendSuccessMsg($"玩家 {WhitePlayer.Name} 加入对局。");
        BlackPlayer.SendInfoMsg("对局开始，你执黑先行。");

        SwitchTurn(PlayerRole.Black); // 黑方先手
    }

    /// <summary>
    /// 切换当前回合。
    /// </summary>
    /// <param name="turn">要切换到的回合方。</param>
    private void SwitchTurn(PlayerRole turn)
    {
        _currentTurn = turn;
        ResetTurnCountdown();

        if (State == MiniGameState.Playing)
        {
            var player = (turn == PlayerRole.White) ? WhitePlayer : BlackPlayer;
            player?.SendCombatMessage("现在是你的回合");
            NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(player.TRPlayer.position, 112, -1, 0.62f), player.Index);

            _nowKindLabel.UpdateTileColor(turn == PlayerRole.Black ? PaintID.BlackPaint : PaintID.WhitePaint);
            _gamePanel.UpdateSelf();
        }
    }

    /// <summary>
    /// 处理玩家落子超时的逻辑。
    /// </summary>
    private void HandleTimeout()
    {
        if (_currentTurn == PlayerRole.Black)
        {
            _blackTimeouts++;
            if (_blackTimeouts >= MAX_TIMEOUT_COUNT)
            {
                WhitePlayer?.SendSuccessMsg($"{BlackPlayer.Name} 超时次数过多，你获得了胜利！");
                BlackPlayer?.SendErrorMsg($"你因 {MAX_TIMEOUT_COUNT} 次未落子超时，输掉了比赛。");
                GameOver(PlayerRole.White);
            }
            else
            {
                BlackPlayer?.SendInfoMsg($"你已超时 {_blackTimeouts} 次，达到 {MAX_TIMEOUT_COUNT} 次将判负。");
                WhitePlayer?.SendInfoMsg($"{BlackPlayer.Name} 未落子，回合跳过。");
                SwitchTurn(PlayerRole.White);
            }
        }
        else // 白方回合
        {
            _whiteTimeouts++;
            if (_whiteTimeouts >= MAX_TIMEOUT_COUNT)
            {
                BlackPlayer?.SendSuccessMsg($"{WhitePlayer.Name} 超时次数过多，你获得了胜利！");
                WhitePlayer?.SendErrorMsg($"你因 {MAX_TIMEOUT_COUNT} 次未落子超时，输掉了比赛。");
                GameOver(PlayerRole.Black);
            }
            else
            {
                WhitePlayer?.SendInfoMsg($"你已超时 {_whiteTimeouts} 次，达到 {MAX_TIMEOUT_COUNT} 次将判负。");
                BlackPlayer?.SendInfoMsg($"{WhitePlayer.Name} 未落子，回合跳过。");
                SwitchTurn(PlayerRole.Black);
            }
        }
    }

    /// <summary>
    /// 游戏结束处理。
    /// </summary>
    /// <param name="winner">胜利方，若为 None 则为平局或中止。</param>
    public void GameOver(PlayerRole winner)
    {
        if (State == MiniGameState.End || State == MiniGameState.Disposed)
            return;

        State = MiniGameState.End;

        var winnerPlayer = winner == PlayerRole.Black ? BlackPlayer : WhitePlayer;

        if (winner != PlayerRole.None)
        {
            var message = $"对局结束，胜者为: [c/ff34fd:{winnerPlayer.Name}]。总步数: {_pieces.Count}";
            BlackPlayer?.SendSuccessMsg(message);
            WhitePlayer?.SendSuccessMsg(message);

            // 胜利特效
            int num = Projectile.NewProjectile(Projectile.GetNoneSource(), winnerPlayer.TRPlayer.position.X, winnerPlayer.TRPlayer.position.Y - 64f, 0f, -8f, 167, 0, 0f, 255, 0f, 0f);
            Main.projectile[num].Kill();
        }
        else
        {
            BlackPlayer?.SendInfoMsg("对局已结束。");
            WhitePlayer?.SendInfoMsg("对局已结束。");
        }

        // 清理所有玩家状态
        if (BlackPlayer != null)
        {
            BlackPlayer.PlayingGame = null;
            BlackPlayer = null;
        }
        if (WhitePlayer != null)
        {
            WhitePlayer.PlayingGame = null;
            WhitePlayer = null;
        }

        // 重置UI和游戏数据
        _boardState = new PlayerRole[BOARD_SIZE, BOARD_SIZE];
        var boardContainer = _gamePanel?.GetChild(0);
        if (boardContainer is VisualContainer container)
        {
            _pieces.ForEach(p => container.Remove(p));
        }
        _pieces.Clear();
        _lastPlacedPiece = null;
        _currentTurn = PlayerRole.None;

        _statusLabel?.UpdateText("等待加入");
        _statusLabel?.UpdateTextColor(PaintID.DeepGreenPaint);
        _stepCountLabel?.SetText("000");
        _turnInfoContainer?.Disable(true);
        _gamePanel?.UpdateSelf();
    }

    /// <summary>
    /// 检查在指定位置落子后是否达成胜利条件。
    /// </summary>
    /// <param name="row">落子行。</param>
    /// <param name="col">落子列。</param>
    private void CheckForWin(int row, int col)
    {
        // 定义四个检查方向: 水平, 垂直, 左下到右上, 左上到右下
        var directions = new[]
        {
            (dr: 0, dc: 1),  // 水平
            (dr: 1, dc: 0),  // 垂直
            (dr: 1, dc: -1), // 左下到右上 (\)
            (dr: 1, dc: 1)   // 左上到右下 (/)
        };

        foreach (var (dr, dc) in directions)
        {
            // 计算当前方向上的连子数, 包括刚刚落下的棋子, 所以从1开始
            int count = 1;

            // 检查正方向
            for (int i = 1; i < WIN_CONDITION; i++)
            {
                int r = row + i * dr;
                int c = col + i * dc;
                if (r >= 0 && r < BOARD_SIZE && c >= 0 && c < BOARD_SIZE && _boardState[r, c] == _currentTurn)
                    count++;
                else
                    break;
            }

            // 检查反方向
            for (int i = 1; i < WIN_CONDITION; i++)
            {
                int r = row - i * dr;
                int c = col - i * dc;
                if (r >= 0 && r < BOARD_SIZE && c >= 0 && c < BOARD_SIZE && _boardState[r, c] == _currentTurn)
                    count++;
                else
                    break;
            }

            if (count >= WIN_CONDITION)
            {
                GameOver(_currentTurn);
                return;
            }
        }

        // 检查是否平局
        if (_pieces.Count >= BOARD_SIZE * BOARD_SIZE)
        {
            WhitePlayer?.SendInfoMsg("棋盘已满，本局为平局。");
            BlackPlayer?.SendInfoMsg("棋盘已满，本局为平局。");
            GameOver(PlayerRole.None);
        }
    }


    #endregion

    #region UI处理

    /// <summary>
    /// 创建游戏棋盘及UI面板。
    /// </summary>
    private void CreateGameBoard(int x, int y)
    {
        int width = BOARD_SIZE * 2;
        int height = BOARD_SIZE * 2;
        _tileProvider = FakeProviderAPI.CreateTileProvider(GId.ToString(), x, y, width, height + 10);
        _gamePanel = TUI.Create(new Panel(GId.ToString(), x, y, width, height + 10, new UIConfiguration() { Permission = MINIGAME_GUI_TOUCH_PERMISSION }, null, _tileProvider));

        // 棋盘区域
        VisualContainer boardNode = _gamePanel.Add(new VisualContainer(0, 10, width, height, null, new ContainerStyle
        {
            Wall = (byte?)WallID.DiamondGemspark,
            WallColor = PaintID.BrownPaint
        }));

        var gridTemplate = new ISize[BOARD_SIZE];
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            gridTemplate[i] = new Absolute(2);
        }
        boardNode.SetupGrid(gridTemplate, gridTemplate);

        // 绘制棋盘网格线
        for (int tempX = 0; tempX < BOARD_SIZE; tempX++)
        {
            for (int tempY = 0; tempY < BOARD_SIZE; tempY++)
            {
                boardNode[tempX, tempY] = new VisualObject(0, 0, 2, 2, null, new()
                {
                    Wall = WallID.Wood, // 使用木墙作为网格线
                    WallColor = PaintID.None
                });
            }
        }
        boardNode.Add(new VisualObject(0, 0, width, height, new UIConfiguration() { UseBegin = true }, null, OnBoardClicked));


        // 顶部信息栏
        VisualContainer titleBar = _gamePanel.Add(new VisualContainer(0, 0, width, 10, null, new ContainerStyle
        {
            Wall = (byte?)WallID.RubyGemspark,
            WallColor = PaintID.WhitePaint
        }));

        titleBar.Add(new Label(0, 0, width, 2, "五子棋 (Five in a Row)", new LabelStyle()
        {
            TextAlignment = Alignment.Center,
            TextColor = PaintID.DeepOrangePaint
        }));

        _countdownLabel = titleBar.Add(new Label(1, 6, 4, 2, "00", new LabelStyle { TextAlignment = Alignment.Left }));

        _statusLabel = titleBar.Add(new Button(8, 6, 24, 2, "等待加入", null, new ButtonStyle
        {
            Wall = (byte?)WallID.TopazGemspark,
            WallColor = PaintID.WhitePaint,
            TextColor = PaintID.DeepGreenPaint,
            TextAlignment = Alignment.Center,
            TriggerStyle = ButtonTriggerStyle.TouchEnd
        }, (self, touch) => Join(BUtils.GetBPlayer(touch.Player()))));

        // 回合信息（默认隐藏）
        _turnInfoContainer = titleBar.Add(new VisualContainer(0, 3, width, 2));
        _turnInfoContainer.Add(new Label(1, 0, 9, 2, "当前回合: ", new LabelStyle { TextAlignment = Alignment.Left }));
        _nowKindLabel = _turnInfoContainer.Add(new Label(11, 0, 2, 2, "", new LabelStyle { Tile = 448 })); // 用一个色块表示颜色

        _turnInfoContainer.Add(new Label(22, 0, 8, 2, "步数:", new LabelStyle { TextAlignment = Alignment.Left }));
        _stepCountLabel = _turnInfoContainer.Add(new Label(30, 0, 6, 2, "000", new LabelStyle
        {
            TextAlignment = Alignment.Left,
            TextColor = PaintID.BluePaint
        }));
        _turnInfoContainer.Disable(false);
        titleBar.SetTop(_turnInfoContainer);

        _gamePanel.UpdateSelf();
    }

    /// <summary>
    /// 重置回合倒计时。
    /// </summary>
    private void ResetTurnCountdown()
    {
        _turnCountdown = TIMEOUT_SECONDS * UPDATE_PER_SECOND;
        _countdownLabel?.UpdateText(TIMEOUT_SECONDS.ToString("00"));
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理棋盘点击事件。
    /// </summary>
    private void OnBoardClicked(VisualObject self, Touch touch)
    {
        var bplayer = BUtils.GetBPlayer(touch.Player());

        if (State != MiniGameState.Playing)
        {
            bplayer.SendInfoMsg($"游戏尚未开始，点击上方的 [{_statusLabel.Text}] 按钮加入。");
            return;
        }

        if (bplayer != BlackPlayer && bplayer != WhitePlayer)
        {
            bplayer.SendErrorMsg("你不是该对局的玩家。");
            return;
        }

        if ((_currentTurn == PlayerRole.Black && bplayer != BlackPlayer) ||
            (_currentTurn == PlayerRole.White && bplayer != WhitePlayer))
        {
            bplayer.SendErrorMsg("现在不是你的回合。");
            return;
        }

        int x = touch.X / 2;
        int y = touch.Y / 2;

        if (x < 0 || x >= BOARD_SIZE || y < 0 || y >= BOARD_SIZE || _boardState[x, y] != PlayerRole.None)
        {
            // 点击位置无效或已有棋子
            return;
        }

        // 放置棋子
        PlacePiece(x, y);

        // 检查胜利
        CheckForWin(x, y);

        // 如果游戏未结束，则切换回合
        if (State == MiniGameState.Playing)
        {
            SwitchTurn(_currentTurn == PlayerRole.Black ? PlayerRole.White : PlayerRole.Black);
        }
    }

    /// <summary>
    /// 在棋盘上放置一个棋子。
    /// </summary>
    private void PlacePiece(int x, int y)
    {
        if (_gamePanel.GetChild(0) is not VisualContainer boardNode)
            return;

        // 创建新的棋子UI
        var piece = boardNode.Add(new VisualObject(x * 2, y * 2, 2, 2, null, new UIStyle()
        {
            Tile = (ushort)TileID.GemLocks, // 使用一个简单的圆形瓦片
            TileColor = (byte?)(_currentTurn == PlayerRole.Black ? PaintID.BlackPaint : PaintID.WhitePaint)
        }));

        _boardState[x, y] = _currentTurn;
        _pieces.Add(piece);

        // 让上一个闪烁的棋子恢复正常显示
        _lastPlacedPiece?.Enable(true);
        _lastPlacedPiece = piece;

        // 更新UI
        _stepCountLabel.UpdateText(_pieces.Count.ToString("000"));
        _gamePanel.UpdateSelf();
    }
    #endregion
}


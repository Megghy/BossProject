#region Using
using BossFramework;
using BossFramework.BCore;
using BossFramework.BModels;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TrProtocol;
using TrProtocol.Packets;
#endregion

namespace FakeProvider.Handlers
{
  /// <summary>
  /// 处理假Provider中的箱子和牌子相关逻辑
  /// </summary>
  internal static class SignChestHandler
  {
    private static readonly object _providerLock = new object();

    #region 初始化和清理

    /// <summary>
    /// 注册箱子牌子相关的事件处理器
    /// </summary>
    public static void Initialize()
    {
      SignRedirector.SignRead += OnRequestSign;
      SignRedirector.SignUpdate += OnUpdateSign;
      ChestRedirector.ChestOpen += OnRequestChest;
      ChestRedirector.ChestSyncActive += OnCloseChest;
      ChestRedirector.ChestUpdateItem += OnUpdateChest;
    }

    /// <summary>
    /// 取消注册箱子牌子相关的事件处理器
    /// </summary>
    public static void Dispose()
    {
      SignRedirector.SignRead -= OnRequestSign;
      SignRedirector.SignUpdate -= OnUpdateSign;
      ChestRedirector.ChestOpen -= OnRequestChest;
      ChestRedirector.ChestSyncActive -= OnCloseChest;
      ChestRedirector.ChestUpdateItem -= OnUpdateChest;
    }

    #endregion

    #region 实体查找

    /// <summary>
    /// 获取指定位置上层级最高的指定类型实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="targetPlrIndex">目标玩家索引，用于个人Provider查找</param>
    /// <returns>找到的实体，如果没有则返回default(T)</returns>
    private static T GetTopEntity<T>(int x, int y, int targetPlrIndex = -1) where T : IFake
    {
      // 个人Provider拥有最高优先级，因为它们只对特定玩家可见
      lock (_providerLock)
      {
        if (targetPlrIndex != -1)
        {
          var personalProvider = FakeProviderAPI.Tile.Personal
              .Where(p => p.Enabled && p.Observers.Contains(targetPlrIndex) && p.HasCollision(x, y, 1, 1))
              .FirstOrDefault();

          if (personalProvider?._entityManager.Entities.FirstOrDefault(e => e is T result && result.X == x && result.Y == y) is { } personalEntity)
            return (T)personalEntity;
        }

        // 如果在个人Provider中没找到，则查找全局Provider
        ushort topProviderIndex = FakeProviderAPI.Tile.ProviderIndexes[x, y];
        var globalProvider = FakeProviderAPI.Tile.GetProviderByIndex(topProviderIndex);

        if (globalProvider != null && globalProvider.Name != FakeProviderAPI.WorldProviderName && globalProvider.Enabled)
        {
          if (globalProvider._entityManager.Entities.FirstOrDefault(e => e is T result && result.X == x && result.Y == y) is { } globalEntity)
            return (T)globalEntity;
        }

        return default;
      }
    }

    #endregion

    #region 牌子处理

    /// <summary>
    /// 处理牌子更新事件
    /// </summary>
    private static void OnUpdateSign(BEventArgs.SignUpdateEventArgs args)
    {
      if (GetTopEntity<FakeSign>(args.Position.X, args.Position.Y, args.Player.Index) is { })
        args.Handled = true;
    }

    /// <summary>
    /// 处理牌子读取事件
    /// </summary>
    private static void OnRequestSign(BEventArgs.SignReadEventArgs args)
    {
      if (GetTopEntity<FakeSign>(args.Position.X, args.Position.Y, args.Player.Index) is { } sign)
      {
        args.Handled = true;
        SignRedirector.SendSign(args.Player, (short)sign.x, (short)sign.y, sign.text);
      }
    }

    #endregion

    #region 箱子处理

    /// <summary>
    /// 处理箱子打开事件
    /// </summary>
    private static void OnRequestChest(BEventArgs.ChestOpenEventArgs args)
    {
      if (GetTopEntity<FakeChest>(args.Position.X, args.Position.Y, args.Player.Index) is { } chest)
      {
        args.Handled = true;

        List<Packet> list = new();
        40.For(i =>
        {
          if (i < chest.item.Length)
          {
            chest.item[i] ??= new();
            var c = chest.item[i];
            list.Add(new SyncChestItem()
            {
              ChestSlot = 7998,
              ChestItemSlot = (byte)i,
              ItemType = (short)c.type,
              Prefix = c.prefix,
              Stack = (short)c.stack,
            });
          }
          else
            list.Add(new SyncChestItem()
            {
              ChestSlot = 7998,
              ChestItemSlot = (byte)i,
              ItemType = 0,
              Prefix = 0,
              Stack = 0,
            });
        });

        args.Player.SendPacket(new SyncPlayerChest()
        {
          Chest = 7998,
          Name = chest.name,
          NameLength = (byte)(chest.name?.Length ?? 0),
          Position = new((short)chest.X, (short)chest.Y)
        });  //同步箱子信息

        args.Player.SendPackets(list); //同步物品

        args.Player.WatchingChest = null;
      }
    }

    /// <summary>
    /// 处理箱子关闭事件
    /// </summary>
    private static void OnCloseChest(BEventArgs.ChestSyncActiveEventArgs args)
    {
      if (GetTopEntity<FakeChest>(args.Position.X, args.Position.Y, args.Player.Index) is { } chest)
        args.Handled = true;
    }

    /// <summary>
    /// 处理箱子物品更新事件
    /// </summary>
    private static void OnUpdateChest(BEventArgs.ChestUpdateItemEventArgs args)
    {
      args.Handled = args.Player.WatchingChest is null;
    }

    #endregion
  }
}
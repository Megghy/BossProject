#region Using
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using TShockAPI;
#endregion

namespace FakeProvider.Handlers
{
  /// <summary>
  /// 处理假Provider的世界保存相关逻辑
  /// </summary>
  internal static class WorldSaveHandler
  {
    #region 初始化和清理

    /// <summary>
    /// 注册世界保存相关的钩子
    /// </summary>
    public static void Initialize()
    {
      On.Terraria.IO.WorldFile.SaveWorld += OnSaveWorld_Hook;
    }

    /// <summary>
    /// 取消注册世界保存相关的钩子
    /// </summary>
    public static void Dispose()
    {
      On.Terraria.IO.WorldFile.SaveWorld -= OnSaveWorld_Hook;
    }

    #endregion

    #region SaveWorld 钩子

    /// <summary>
    /// 世界保存钩子，确保只保存真实世界数据
    /// </summary>
    private static void OnSaveWorld_Hook(On.Terraria.IO.WorldFile.orig_SaveWorld orig)
    {
      if (FakeProviderAPI.World == null)
      {
        orig();
        return;
      }

      var originalTile = Main.tile;
      var originalMaxX = Main.maxTilesX;
      var originalMaxY = Main.maxTilesY;
      var originalWorldSurface = Main.worldSurface;
      var originalRockLayer = Main.rockLayer;

      Main.tile = FakeProviderAPI.World;
      Main.maxTilesX = FakeProviderAPI.World.Width;
      Main.maxTilesY = FakeProviderAPI.World.Height;
      // 考虑到 OffsetX/OffsetY 已被注释掉，此处暂不调整 worldSurface 和 rockLayer

      On.Terraria.IO.WorldFile.SaveChests += Hook_SaveChestsWithFilter;
      On.Terraria.IO.WorldFile.SaveSigns += Hook_SaveSignsWithFilter;
      On.Terraria.IO.WorldFile.SaveTileEntities += Hook_SaveTileEntitiesWithFilter;

      try
      {
        TShock.Log.Info("[FakeProvider] 正在通过健壮的钩子方法保存世界...");
        orig();
        TShock.Log.Info("[FakeProvider] 世界保存成功。");
      }
      catch (Exception e)
      {
        TShock.Log.Error($"[FakeProvider] 在挂钩保存世界期间发生错误: {e}");
        throw;
      }
      finally
      {
        Main.tile = originalTile;
        Main.maxTilesX = originalMaxX;
        Main.maxTilesY = originalMaxY;
        Main.worldSurface = originalWorldSurface;
        Main.rockLayer = originalRockLayer;

        On.Terraria.IO.WorldFile.SaveChests -= Hook_SaveChestsWithFilter;
        On.Terraria.IO.WorldFile.SaveSigns -= Hook_SaveSignsWithFilter;
        On.Terraria.IO.WorldFile.SaveTileEntities -= Hook_SaveTileEntitiesWithFilter;
      }
    }

    /// <summary>
    /// 带过滤器的箱子保存钩子，只保存真实世界Provider中的箱子
    /// </summary>
    private static int Hook_SaveChestsWithFilter(On.Terraria.IO.WorldFile.orig_SaveChests orig, BinaryWriter writer)
    {
      short num = 0;
      for (int i = 0; i < 8000; i++)
      {
        Chest chest = Main.chest[i];
        if (chest != null)
        {
          if (chest is IFake fchest && fchest.Provider != FakeProviderAPI.World)
            continue;
          bool flag = false;
          for (int j = chest.x; j <= chest.x + 1; j++)
          {
            for (int k = chest.y; k <= chest.y + 1; k++)
            {
              if (j < 0 || k < 0 || j >= FakeProviderAPI.World.Width || k >= FakeProviderAPI.World.Height)
              {
                flag = true;
                break;
              }
              ITile tile = FakeProviderAPI.World[j, k];
              if (!tile.active() || !Main.tileContainer[(int)tile.type])
              {
                flag = true;
                break;
              }
            }
          }
          if (flag)
          {
            Main.chest[i] = null;
          }
          else
          {
            num += 1;
          }
        }
      }
      writer.Write(num);
      writer.Write((short)40);
      for (int i = 0; i < 8000; i++)
      {
        Chest chest = Main.chest[i];

        if (chest != null)
        {
          if (chest is IFake fchest && fchest.Provider != FakeProviderAPI.World)
            continue;
          writer.Write(chest.x);
          writer.Write(chest.y);
          writer.Write(chest.name);
          for (int l = 0; l < 40; l++)
          {
            Item item = chest.item[l];
            if (item == null)
            {
              writer.Write((short)0);
            }
            else
            {
              if (item.stack > item.maxStack)
              {
                item.stack = item.maxStack;
              }
              if (item.stack < 0)
              {
                item.stack = 1;
              }
              writer.Write((short)item.stack);
              if (item.stack > 0)
              {
                writer.Write(item.netID);
                writer.Write(item.prefix);
              }
            }
          }
        }
      }
      return (int)writer.BaseStream.Position;
    }

    /// <summary>
    /// 带过滤器的牌子保存钩子，只保存真实世界Provider中的牌子
    /// </summary>
    private static int Hook_SaveSignsWithFilter(On.Terraria.IO.WorldFile.orig_SaveSigns orig, BinaryWriter writer)
    {
      short num = 0;
      for (int i = 0; i < 1000; i++)
      {
        Sign sign = Main.sign[i];
        if (sign != null && sign.text != null)
        {
          if (sign is IFake fsign && fsign.Provider != FakeProviderAPI.World)
            continue;
          num += 1;
        }
      }
      writer.Write(num);
      for (int j = 0; j < 1000; j++)
      {
        Sign sign = Main.sign[j];
        if (sign != null && sign.text != null)
        {
          if (sign is IFake fsign && fsign.Provider != FakeProviderAPI.World)
            continue;
          writer.Write(sign.text);
          writer.Write(sign.x);
          writer.Write(sign.y);
        }
      }
      return (int)writer.BaseStream.Position;
    }

    /// <summary>
    /// 带过滤器的物块实体保存钩子，只保存真实世界Provider中的物块实体
    /// </summary>
    private static int Hook_SaveTileEntitiesWithFilter(On.Terraria.IO.WorldFile.orig_SaveTileEntities orig, BinaryWriter writer)
    {
      object entityCreationLock = TileEntity.EntityCreationLock;
      lock (entityCreationLock)
      {
        writer.Write((int)TileEntity.ByID.Count(keyValuePair =>
            !(keyValuePair.Value is IFake fentity && fentity.Provider != FakeProviderAPI.World)));
        foreach (KeyValuePair<int, TileEntity> keyValuePair in TileEntity.ByID)
        {
          if (keyValuePair.Value is IFake fentity && fentity.Provider != FakeProviderAPI.World)
            continue;
          TileEntity.Write(writer, keyValuePair.Value, false);
        }
      }
      return (int)writer.BaseStream.Position;
    }

    #endregion
  }
}
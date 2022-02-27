using Terraria;
using TShockAPI;

namespace CustomWeaponAPI;

public static class CustomWeaponDropper
{
    private static byte GetAlterStartFlags(CustomWeapon weapon)
    {
        byte b = 0;
        if (weapon.Color.HasValue)
        {
            b = (byte)(b + 1);
        }
        if (weapon.Damage.HasValue)
        {
            b = (byte)(b + 2);
        }
        if (weapon.Knockback.HasValue)
        {
            b = (byte)(b + 4);
        }
        if (weapon.UseAnimation.HasValue)
        {
            b = (byte)(b + 8);
        }
        if (weapon.UseTime.HasValue)
        {
            b = (byte)(b + 16);
        }
        if (weapon.ShootProjectileId.HasValue)
        {
            b = (byte)(b + 32);
        }
        if (weapon.ShootSpeed.HasValue)
        {
            b = (byte)(b + 64);
        }
        if (weapon.DropAreaWidth.HasValue || weapon.DropAreaHeight.HasValue || weapon.Scale.HasValue || weapon.AmmoIdentifier.HasValue || weapon.UseAmmoIdentifier.HasValue || weapon.NotAmmo.HasValue)
        {
            b = (byte)(b + 128);
        }
        return b;
    }

    private static byte GetAlterEndFlags(CustomWeapon weapon)
    {
        byte b = 0;
        if (weapon.DropAreaWidth.HasValue)
        {
            b = (byte)(b + 1);
        }
        if (weapon.DropAreaHeight.HasValue)
        {
            b = (byte)(b + 2);
        }
        if (weapon.Scale.HasValue)
        {
            b = (byte)(b + 4);
        }
        if (weapon.AmmoIdentifier.HasValue)
        {
            b = (byte)(b + 8);
        }
        if (weapon.UseAmmoIdentifier.HasValue)
        {
            b = (byte)(b + 16);
        }
        if (weapon.NotAmmo.HasValue)
        {
            b = (byte)(b + 32);
        }
        return b;
    }

    private static void SetAlterItemDropNextFlags(PacketWriter alterItemDrop, CustomWeapon weapon)
    {
        alterItemDrop.PackByte(GetAlterEndFlags(weapon));
        if (weapon.DropAreaWidth.HasValue)
        {
            alterItemDrop.PackInt16(weapon.DropAreaWidth.Value);
        }
        if (weapon.DropAreaHeight.HasValue)
        {
            alterItemDrop.PackInt16(weapon.DropAreaHeight.Value);
        }
        if (weapon.Scale.HasValue)
        {
            alterItemDrop.PackSingle(weapon.Scale.Value);
        }
        if (weapon.AmmoIdentifier.HasValue)
        {
            alterItemDrop.PackInt16(weapon.AmmoIdentifier.Value);
        }
        if (weapon.UseAmmoIdentifier.HasValue)
        {
            alterItemDrop.PackInt16(weapon.UseAmmoIdentifier.Value);
        }
        if (weapon.NotAmmo.HasValue)
        {
            alterItemDrop.PackByte((byte)(weapon.NotAmmo.Value ? 1u : 0u));
        }
    }

    private static byte[] GetAlterItemDropPacket(CustomWeapon weapon, int itemIndex)
    {
        byte alterStartFlags = GetAlterStartFlags(weapon);
        PacketWriter packetWriter = new PacketWriter().SetType(88).PackInt16((short)itemIndex).PackByte(alterStartFlags);
        if (weapon.Color.HasValue)
        {
            packetWriter.PackUInt32((uint)(-16777216 + (weapon.Color?.R).Value + ((weapon.Color?.G).Value << 8) + ((weapon.Color?.B).Value << 16)));
        }
        if (weapon.Damage.HasValue)
        {
            packetWriter.PackUInt16(weapon.Damage.Value);
        }
        if (weapon.Knockback.HasValue)
        {
            packetWriter.PackSingle(weapon.Knockback.Value);
        }
        if (weapon.UseAnimation.HasValue)
        {
            packetWriter.PackUInt16(weapon.UseAnimation.Value);
        }
        if (weapon.UseTime.HasValue)
        {
            packetWriter.PackUInt16(weapon.UseTime.Value);
        }
        if (weapon.ShootProjectileId.HasValue)
        {
            packetWriter.PackInt16(weapon.ShootProjectileId.Value);
        }
        if (weapon.ShootSpeed.HasValue)
        {
            packetWriter.PackSingle(weapon.ShootSpeed.Value);
        }
        if ((alterStartFlags & 0x80u) != 0)
        {
            SetAlterItemDropNextFlags(packetWriter, weapon);
        }
        return packetWriter.GetByteData();
    }

    public static void DropItem(TSPlayer player, CustomWeapon weapon)
    {
        int num = 400;
        for (int i = 0; i < 400; i++)
        {
            if (!Main.item[i].active && Main.timeItemSlotCannotBeReusedFor[i] == 0)
            {
                num = i;
                break;
            }
        }
        Main.item[num] = new Item();
        Main.item[num].active = true;
        byte[] byteData = new PacketWriter().SetType(90).PackInt16((short)num).PackSingle(player.TPlayer.position.X - (float)weapon.DropAreaWidth.GetValueOrDefault() / 2f)
            .PackSingle(player.TPlayer.position.Y - (float)weapon.DropAreaHeight.GetValueOrDefault() / 2f)
            .PackSingle(0f)
            .PackSingle(0f)
            .PackInt16(weapon.Stack ?? 1)
            .PackByte(weapon.Prefix.GetValueOrDefault())
            .PackByte(0)
            .PackInt16(weapon.ItemNetId)
            .GetByteData();
        byte[] byteData2 = new PacketWriter().SetType(22).PackInt16((short)num).PackByte((byte)player.Index)
            .GetByteData();
        player.SendRawData(byteData);
        player.SendRawData(GetAlterItemDropPacket(weapon, num));
        player.SendRawData(byteData2);
    }

    public static void DropItemSafely(TSPlayer player, CustomWeapon weapon, int x, int y)
    {
        int freeIndex = 400;
        for (int i = 0; i < 400; i++)
        {
            if (!Main.item[i].active && Main.timeItemSlotCannotBeReusedFor[i] == 0)
            {
                freeIndex = i;
                break;
            }
        }
        Main.timeItemSlotCannotBeReusedFor[freeIndex] = 0;
        Main.item[freeIndex] = new Item();
        Main.item[freeIndex].active = true;
        byte[] byteData = new PacketWriter().SetType(90).PackInt16((short)freeIndex).PackSingle(player.TPlayer.position.X + (float)x)
            .PackSingle(player.TPlayer.position.Y + (float)y)
            .PackSingle(0f)
            .PackSingle(0f)
            .PackInt16(weapon.Stack ?? 1)
            .PackByte(weapon.Prefix.GetValueOrDefault())
            .PackByte(1)
            .PackInt16(weapon.ItemNetId)
            .GetByteData();
        byte[] byteData2 = new PacketWriter().SetType(22).PackInt16((short)freeIndex).PackByte((byte)player.Index)
            .GetByteData();
        player.SendRawData(byteData);
        for (int j = 0; j < 3; j++)
        {
            player.SendRawData(GetAlterItemDropPacket(weapon, freeIndex));
        }
        player.SendRawData(byteData2);
        Task task = Task.Run(async delegate
        {
            await Task.Delay(50);
            player.Teleport(player.TPlayer.position.X + (float)x, player.TPlayer.position.Y + (float)y, 1);
            await Task.Delay(500);
            player.Teleport(player.TPlayer.position.X, player.TPlayer.position.Y, 1);
            Main.item[freeIndex].active = false;
            TSPlayer.All.SendData((PacketTypes)21, "", freeIndex);
        });
    }
}

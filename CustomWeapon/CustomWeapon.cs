using Microsoft.Xna.Framework;

namespace CustomWeaponAPI;

public class CustomWeapon
{
    public byte? Prefix;

    public short ItemNetId;

    public Color? Color;

    public short? Stack;

    public ushort? Damage;

    public float? Knockback;

    public ushort? UseAnimation;

    public ushort? UseTime;

    public short? ShootProjectileId;

    public float? ShootSpeed;

    public float? Scale;

    public short? AmmoIdentifier;

    public short? UseAmmoIdentifier;

    public bool? NotAmmo;

    public short? DropAreaWidth;

    public short? DropAreaHeight;

    public CustomWeapon(CustomWeapon weapon)
    {
        Prefix = weapon.Prefix;
        ItemNetId = weapon.ItemNetId;
        if (weapon.Color.HasValue)
        {
            Color = new Color(weapon.Color.Value.R, weapon.Color.Value.G, weapon.Color.Value.B);
        }
        Stack = weapon.Stack;
        Damage = weapon.Damage;
        Knockback = weapon.Knockback;
        UseAnimation = weapon.UseAnimation;
        UseTime = weapon.UseTime;
        ShootProjectileId = weapon.ShootProjectileId;
        ShootSpeed = weapon.ShootSpeed;
        Scale = weapon.Scale;
        AmmoIdentifier = weapon.AmmoIdentifier;
        UseAmmoIdentifier = weapon.UseAmmoIdentifier;
        NotAmmo = weapon.NotAmmo;
        DropAreaHeight = weapon.DropAreaHeight;
        DropAreaWidth = weapon.DropAreaWidth;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is CustomWeapon customWeapon))
        {
            return false;
        }
        return ItemNetId == customWeapon.ItemNetId;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public CustomWeapon()
    {
    }
}

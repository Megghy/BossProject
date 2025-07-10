using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace FakeProvider
{
    [StructLayout(LayoutKind.Sequential, Size = 14, Pack = 1)]
    public struct StructTile : ITile
    {
        public static readonly StructTile Empty = new();
        public const int Type_Solid = 0;
        public const int Type_Halfbrick = 1;
        public const int Type_SlopeDownRight = 2;
        public const int Type_SlopeDownLeft = 3;
        public const int Type_SlopeUpRight = 4;
        public const int Type_SlopeUpLeft = 5;
        public const int Liquid_Water = 0;
        public const int Liquid_Lava = 1;
        public const int Liquid_Honey = 2;

        public byte liquid;
        public byte bTileHeader;
        public byte bTileHeader2;
        public byte bTileHeader3;
        public ushort type;
        public ushort wall;
        public ushort sTileHeader;
        public short frameX;
        public short frameY;

        ushort ITile.type { get => type; set => type = value; }
        ushort ITile.wall { get => wall; set => wall = value; }
        byte ITile.liquid { get => liquid; set => liquid = value; }
        ushort ITile.sTileHeader { get => sTileHeader; set => sTileHeader = value; }
        byte ITile.bTileHeader { get => bTileHeader; set => bTileHeader = value; }
        byte ITile.bTileHeader2 { get => bTileHeader2; set => bTileHeader2 = value; }
        byte ITile.bTileHeader3 { get => bTileHeader3; set => bTileHeader3 = value; }
        short ITile.frameX { get => frameX; set => frameX = value; }
        short ITile.frameY { get => frameY; set => frameY = value; }

        public StructTile(StructTile copy)
        {
            type = copy.type;
            wall = copy.wall;
            liquid = copy.liquid;
            sTileHeader = copy.sTileHeader;
            bTileHeader = copy.bTileHeader;
            bTileHeader2 = copy.bTileHeader2;
            bTileHeader3 = copy.bTileHeader3;
            frameX = copy.frameX;
            frameY = copy.frameY;
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }

        public void ClearEverything()
        {
            type = 0;
            wall = 0;
            liquid = 0;
            sTileHeader = 0;
            bTileHeader = 0;
            bTileHeader2 = 0;
            bTileHeader3 = 0;
            frameX = 0;
            frameY = 0;
        }

        public void ClearTile()
        {
            slope(0);
            halfBrick(false);
            active(false);
            inActive(false);
        }

        public void CopyFrom(ITile from)
        {
            type = from.type;
            wall = from.wall;
            liquid = from.liquid;
            sTileHeader = from.sTileHeader;
            bTileHeader = from.bTileHeader;
            bTileHeader2 = from.bTileHeader2;
            bTileHeader3 = from.bTileHeader3;
            frameX = from.frameX;
            frameY = from.frameY;
        }

        public int collisionType
        {
            get
            {
                if (!active())
                {
                    return 0;
                }
                if (halfBrick())
                {
                    return 2;
                }
                if (slope() > 0)
                {
                    return (int)(2 + slope());
                }
                if (Main.tileSolid[(int)type] && !Main.tileSolidTop[(int)type])
                {
                    return 1;
                }
                return -1;
            }
        }


        public bool isTheSameAs(ITile compTile)
        {
            if (sTileHeader != compTile?.sTileHeader)
            {
                return false;
            }
            if (active())
            {
                if (type != compTile.type)
                {
                    return false;
                }
                if (Main.tileFrameImportant[(int)type] && (frameX != compTile.frameX || frameY != compTile.frameY))
                {
                    return false;
                }
            }
            if (wall != compTile.wall || liquid != compTile.liquid)
            {
                return false;
            }
            if (compTile.liquid == 0)
            {
                if (wallColor() != compTile.wallColor())
                {
                    return false;
                }
                if (wire4() != compTile.wire4())
                {
                    return false;
                }
            }
            else if (bTileHeader != compTile.bTileHeader)
            {
                return false;
            }
            return true;
        }

        public int blockType()
        {
            if (halfBrick())
            {
                return 1;
            }
            int num = (int)slope();
            if (num > 0)
            {
                num++;
            }
            return num;
        }

        public void liquidType(int liquidType)
        {
            if (liquidType == 0)
            {
                bTileHeader &= 159;
                return;
            }
            if (liquidType == 1)
            {
                lava(true);
                return;
            }
            if (liquidType == 2)
            {
                honey(true);
            }
        }

        public byte liquidType()
        {
            return (byte)((bTileHeader & 96) >> 5);
        }

        public bool nactive()
        {
            return (sTileHeader & 96) == 32;
        }

        public void ResetToType(ushort type)
        {
            liquid = 0;
            sTileHeader = 32;
            bTileHeader = 0;
            bTileHeader2 = 0;
            bTileHeader3 = 0;
            frameX = 0;
            frameY = 0;
            type = type;
        }

        public void ClearMetadata()
        {
            liquid = 0;
            sTileHeader = 0;
            bTileHeader = 0;
            bTileHeader2 = 0;
            bTileHeader3 = 0;
            frameX = 0;
            frameY = 0;
        }

        public Color actColor(Color oldColor)
        {
            if (!inActive())
            {
                return oldColor;
            }
            double num = 0.4;
            return new Color((int)((byte)(num * (double)oldColor.R)), (int)((byte)(num * (double)oldColor.G)), (int)((byte)(num * (double)oldColor.B)), (int)oldColor.A);
        }

        public void actColor(ref Vector3 oldColor)
        {
            if (!inActive())
            {
                return;
            }
            oldColor *= 0.4f;
        }

        public bool topSlope()
        {
            byte b = slope();
            return b == 1 || b == 2;
        }

        public bool bottomSlope()
        {
            byte b = slope();
            return b == 3 || b == 4;
        }

        public bool leftSlope()
        {
            byte b = slope();
            return b == 2 || b == 4;
        }

        public bool rightSlope()
        {
            byte b = slope();
            return b == 1 || b == 3;
        }

        public bool HasSameSlope(ITile tile)
        {
            return (sTileHeader & 29696) == (tile.sTileHeader & 29696);
        }

        public byte wallColor()
        {
            return (byte)(bTileHeader & 31);
        }

        public void wallColor(byte wallColor)
        {
            bTileHeader = (byte)((bTileHeader & 224) | wallColor);
        }

        public bool lava()
        {
            return (bTileHeader & 32) == 32;
        }

        public void lava(bool lava)
        {
            if (lava)
            {
                bTileHeader = (byte)((bTileHeader & 159) | 32);
                return;
            }
            bTileHeader &= 223;
        }

        public bool honey()
        {
            return (bTileHeader & 64) == 64;
        }

        public void honey(bool honey)
        {
            if (honey)
            {
                bTileHeader = (byte)((bTileHeader & 159) | 64);
                return;
            }
            bTileHeader &= 191;
        }

        public bool wire4()
        {
            return (bTileHeader & 128) == 128;
        }

        public void wire4(bool wire4)
        {
            if (wire4)
            {
                bTileHeader |= 128;
                return;
            }
            bTileHeader &= 127;
        }

        public int wallFrameX()
        {
            return (int)((bTileHeader2 & 15) * 36);
        }

        public void wallFrameX(int wallFrameX)
        {
            bTileHeader2 = (byte)((int)(bTileHeader2 & 240) | (wallFrameX / 36 & 15));
        }

        public byte frameNumber()
        {
            return (byte)((bTileHeader2 & 48) >> 4);
        }

        public void frameNumber(byte frameNumber)
        {
            bTileHeader2 = (byte)((int)(bTileHeader2 & 207) | (int)(frameNumber & 3) << 4);
        }

        public byte wallFrameNumber()
        {
            return (byte)((bTileHeader2 & 192) >> 6);
        }

        public void wallFrameNumber(byte wallFrameNumber)
        {
            bTileHeader2 = (byte)((int)(bTileHeader2 & 63) | (int)(wallFrameNumber & 3) << 6);
        }

        public int wallFrameY()
        {
            return (int)((bTileHeader3 & 7) * 36);
        }

        public void wallFrameY(int wallFrameY)
        {
            bTileHeader3 = (byte)((int)(bTileHeader3 & 248) | (wallFrameY / 36 & 7));
        }

        public bool checkingLiquid()
        {
            return (bTileHeader3 & 8) == 8;
        }

        public void checkingLiquid(bool checkingLiquid)
        {
            if (checkingLiquid)
            {
                bTileHeader3 |= 8;
                return;
            }
            bTileHeader3 &= 247;
        }

        public bool skipLiquid()
        {
            return (bTileHeader3 & 16) == 16;
        }

        public void skipLiquid(bool skipLiquid)
        {
            if (skipLiquid)
            {
                bTileHeader3 |= 16;
                return;
            }
            bTileHeader3 &= 239;
        }

        public byte color()
        {
            return (byte)(sTileHeader & 31);
        }

        public void color(byte color)
        {
            sTileHeader = (ushort)((sTileHeader & 65504) | (int)color);
        }

        public bool active()
        {
            return (sTileHeader & 32) == 32;
        }

        public void active(bool active)
        {
            if (active)
            {
                sTileHeader |= 32;
                return;
            }
            sTileHeader = (ushort)(sTileHeader & 65503);
        }

        public bool inActive()
        {
            return (sTileHeader & 64) == 64;
        }

        public void inActive(bool inActive)
        {
            if (inActive)
            {
                sTileHeader |= 64;
                return;
            }
            sTileHeader = (ushort)(sTileHeader & 65471);
        }

        public bool wire()
        {
            return (sTileHeader & 128) == 128;
        }

        public void wire(bool wire)
        {
            if (wire)
            {
                sTileHeader |= 128;
                return;
            }
            sTileHeader = (ushort)(sTileHeader & 65407);
        }

        public bool wire2()
        {
            return (sTileHeader & 256) == 256;
        }

        public void wire2(bool wire2)
        {
            if (wire2)
            {
                sTileHeader |= 256;
                return;
            }
            sTileHeader = (ushort)(sTileHeader & 65279);
        }

        public bool wire3()
        {
            return (sTileHeader & 512) == 512;
        }

        public void wire3(bool wire3)
        {
            if (wire3)
            {
                sTileHeader |= 512;
                return;
            }
            sTileHeader = (ushort)(sTileHeader & 65023);
        }

        public bool halfBrick()
        {
            return (sTileHeader & 1024) == 1024;
        }

        public void halfBrick(bool halfBrick)
        {
            if (halfBrick)
            {
                sTileHeader |= 1024;
                return;
            }
            sTileHeader = (ushort)(sTileHeader & 64511);
        }

        public bool actuator()
        {
            return (sTileHeader & 2048) == 2048;
        }

        public void actuator(bool actuator)
        {
            if (actuator)
            {
                sTileHeader |= 2048;
                return;
            }
            sTileHeader = (ushort)(sTileHeader & 63487);
        }

        public byte slope()
        {
            return (byte)((sTileHeader & 28672) >> 12);
        }

        public void slope(byte slope)
        {
            sTileHeader = (ushort)((sTileHeader & 36863) | (int)(slope & 7) << 12);
        }

        public void Clear(TileDataType types)
        {
            if ((types & TileDataType.Tile) != (TileDataType)0)
            {
                type = 0;
                active(false);
                frameX = 0;
                frameY = 0;
            }
            if ((types & TileDataType.Wall) != (TileDataType)0)
            {
                wall = 0;
                wallFrameX(0);
                wallFrameY(0);
            }
            if ((types & TileDataType.TilePaint) != (TileDataType)0)
            {
                color(0);
            }
            if ((types & TileDataType.WallPaint) != (TileDataType)0)
            {
                wallColor(0);
            }
            if ((types & TileDataType.Liquid) != (TileDataType)0)
            {
                liquid = 0;
                liquidType(0);
                checkingLiquid(false);
            }
            if ((types & TileDataType.Slope) != (TileDataType)0)
            {
                slope(0);
                halfBrick(false);
            }
            if ((types & TileDataType.Wiring) != (TileDataType)0)
            {
                wire(false);
                wire2(false);
                wire3(false);
                wire4(false);
            }
            if ((types & TileDataType.Actuator) != (TileDataType)0)
            {
                actuator(false);
                inActive(false);
            }
        }

        public bool shimmer()
        {
            return (bTileHeader & 0x60) == 96;
        }

        public void shimmer(bool shimmer)
        {
            if (shimmer)
            {
                bTileHeader = (byte)((bTileHeader & 0x9F) | 0x60);
            }
            else
            {
                bTileHeader &= 159;
            }
        }

        public readonly bool invisibleBlock()
        {
            return (bTileHeader3 & 0x20) == 32;
        }

        public void invisibleBlock(bool invisibleBlock)
        {
            if (invisibleBlock)
            {
                bTileHeader3 |= 32;
            }
            else
            {
                bTileHeader3 = (byte)(bTileHeader3 & -33);
            }
        }

        public readonly bool invisibleWall()
        {
            return (bTileHeader3 & 0x40) == 64;
        }

        public void invisibleWall(bool invisibleWall)
        {
            if (invisibleWall)
            {
                bTileHeader3 |= 64;
            }
            else
            {
                bTileHeader3 = (byte)(bTileHeader3 & -65);
            }
        }

        public bool fullbrightBlock()
        {
            return (bTileHeader3 & 0x80) == 128;
        }

        public void fullbrightBlock(bool fullbrightBlock)
        {
            if (fullbrightBlock)
            {
                bTileHeader3 |= 128;
            }
            else
            {
                bTileHeader3 = (byte)(bTileHeader3 & -129);
            }
        }

        public readonly bool fullbrightWall()
        {
            return (sTileHeader & 0x8000) == 32768;
        }

        public void fullbrightWall(bool fullbrightWall)
        {
            if (fullbrightWall)
            {
                sTileHeader |= 32768;
            }
            else
            {
                sTileHeader = (ushort)(sTileHeader & -32769);
            }
        }

        public void CopyPaintAndCoating(ITile other)
        {
            color(other.color());
            invisibleBlock(other.invisibleBlock());
            fullbrightBlock(other.fullbrightBlock());
        }

        public TileColorCache BlockColorAndCoating()
        {
            TileColorCache result = default;
            result.Color = color();
            result.FullBright = fullbrightBlock();
            result.Invisible = invisibleBlock();
            return result;
        }

        public TileColorCache WallColorAndCoating()
        {
            TileColorCache result = default;
            result.Color = wallColor();
            result.FullBright = fullbrightWall();
            result.Invisible = invisibleWall();
            return result;
        }

        public readonly void UseBlockColors(TileColorCache cache)
        {
            cache.ApplyToBlock(this);
        }

        public readonly void UseWallColors(TileColorCache cache)
        {
            cache.ApplyToWall(this);
        }

        public void ClearBlockPaintAndCoating()
        {
            color(0);
            fullbrightBlock(fullbrightBlock: false);
            invisibleBlock(invisibleBlock: false);
        }

        public void ClearWallPaintAndCoating()
        {
            wallColor(0);
            fullbrightWall(fullbrightWall: false);
            invisibleWall(invisibleWall: false);
        }
    }
}

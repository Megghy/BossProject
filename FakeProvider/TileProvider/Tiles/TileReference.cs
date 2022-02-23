#region Using
using Microsoft.Xna.Framework;
using System.Diagnostics.CodeAnalysis;
using Terraria;
using Terraria.DataStructures;
#endregion
namespace FakeProvider
{
    public struct TileReference : ITile
    {
        #region Constants

        public const int Type_Solid = 0;
        public const int Type_Halfbrick = 1;
        public const int Type_SlopeDownRight = 2;
        public const int Type_SlopeDownLeft = 3;
        public const int Type_SlopeUpRight = 4;
        public const int Type_SlopeUpLeft = 5;
        public const int Liquid_Water = 0;
        public const int Liquid_Lava = 1;
        public const int Liquid_Honey = 2;

        #endregion

        #region Data

        private readonly StructTile[,] Data;
        public readonly int X;
        public readonly int Y;

        #endregion
        #region Constructor

        public TileReference(StructTile[,] Data, int X, int Y)
        {
            this.Data = Data;
            this.X = X;
            this.Y = Y;
        }

        #endregion

        #region Initialise

        public void Initialise()
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

        #endregion

        #region type

        public ushort type
        {
            get => Data[X, Y].type;
            set => Data[X, Y].type = value;
        }

        #endregion
        #region wall

        public ushort wall
        {
            get => Data[X, Y].wall;
            set => Data[X, Y].wall = value;
        }

        #endregion
        #region liquid

        public byte liquid
        {
            get => Data[X, Y].liquid;
            set => Data[X, Y].liquid = value;
        }

        #endregion
        #region frameX

        public short frameX
        {
            get => Data[X, Y].frameX;
            set => Data[X, Y].frameX = value;
        }

        #endregion
        #region frameY

        public short frameY
        {
            get => Data[X, Y].frameY;
            set => Data[X, Y].frameY = value;
        }

        #endregion

        #region sTileHeader

        public short sTileHeader
        {
            get => Data[X, Y].sTileHeader;
            set => Data[X, Y].sTileHeader = value;
        }

        #endregion
        #region bTileHeader

        public byte bTileHeader
        {
            get => Data[X, Y].bTileHeader;
            set => Data[X, Y].bTileHeader = value;
        }

        #endregion
        #region bTileHeader2

        public byte bTileHeader2
        {
            get => Data[X, Y].bTileHeader2;
            set => Data[X, Y].bTileHeader2 = value;
        }

        #endregion
        #region bTileHeader3

        public byte bTileHeader3
        {
            get => Data[X, Y].bTileHeader3;
            set => Data[X, Y].bTileHeader3 = value;
        }

        #endregion
        #region collisionType

        public int collisionType
        {
            get
            {
                if (!active())
                    return 0;
                if (halfBrick())
                    return 2;
                if (slope() > 0)
                    return (2 + slope());
                if (Main.tileSolid[type] && !Main.tileSolidTop[type])
                    return 1;
                return -1;
            }
        }

        #endregion

        #region Clear

        public void Clear(TileDataType types)
        {
            if ((types & TileDataType.Tile) != (TileDataType)0)
            {
                this.type = 0;
                this.active(false);
                this.frameX = 0;
                this.frameY = 0;
            }
            if ((types & TileDataType.Wall) != (TileDataType)0)
            {
                this.wall = 0;
                this.wallFrameX(0);
                this.wallFrameY(0);
            }
            if ((types & TileDataType.TilePaint) != (TileDataType)0)
            {
                this.color(0);
            }
            if ((types & TileDataType.WallPaint) != (TileDataType)0)
            {
                this.wallColor(0);
            }
            if ((types & TileDataType.Liquid) != (TileDataType)0)
            {
                this.liquid = 0;
                this.liquidType(0);
                this.checkingLiquid(false);
            }
            if ((types & TileDataType.Slope) != (TileDataType)0)
            {
                this.slope(0);
                this.halfBrick(false);
            }
            if ((types & TileDataType.Wiring) != (TileDataType)0)
            {
                this.wire(false);
                this.wire2(false);
                this.wire3(false);
                this.wire4(false);
            }
            if ((types & TileDataType.Actuator) != (TileDataType)0)
            {
                this.actuator(false);
                this.inActive(false);
            }
        }

        #endregion
        #region ClearEverything

        public void ClearEverything()
        {
            type = 0;
            wall = 0;
            ClearMetadata();
        }

        #endregion
        #region ClearTile

        public void ClearTile()
        {
            slope(0);
            halfBrick(false);
            active(false);
        }

        #endregion
        #region ClearMetadata

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

        #endregion
        #region ResetToType

        public void ResetToType(ushort Type)
        {
            liquid = 0;
            sTileHeader = 32;
            bTileHeader = 0;
            bTileHeader2 = 0;
            bTileHeader3 = 0;
            frameX = 0;
            frameY = 0;
            type = Type;
        }

        #endregion

        #region CopyFrom

        public void CopyFrom(ITile From)
        {
            type = From.type;
            wall = From.wall;
            liquid = From.liquid;
            sTileHeader = From.sTileHeader;
            bTileHeader = From.bTileHeader;
            bTileHeader2 = From.bTileHeader2;
            bTileHeader3 = From.bTileHeader3;
            frameX = From.frameX;
            frameY = From.frameY;
        }

        #endregion
        #region isTheSameAs

        public bool isTheSameAs(ITile Tile)
        {
            if ((Tile == null) || (sTileHeader != Tile.sTileHeader))
                return false;
            if (active())
            {
                if (type != Tile.type)
                    return false;
                if (Main.tileFrameImportant[type]
                        && ((frameX != Tile.frameX)
                        || (frameY != Tile.frameY)))
                    return false;
            }
            if ((wall != Tile.wall) || (liquid != Tile.liquid))
                return false;
            if (Tile.liquid == 0)
            {
                if (wallColor() != Tile.wallColor())
                    return false;
                if (wire4() != Tile.wire4())
                    return false;
            }
            else if (bTileHeader != Tile.bTileHeader)
                return false;
            return true;
        }

        #endregion
        #region actColor

        const double ActNum = 0.4;
        public Color actColor(Color oldColor)
        {
            if (!inActive())
                return oldColor;

            return new Color
            (
                ((byte)(ActNum * oldColor.R)),
                ((byte)(ActNum * oldColor.G)),
                ((byte)(ActNum * oldColor.B)),
                oldColor.A
            );
        }

        public void actColor(ref Vector3 oldColor)
        {
            if (!inActive())
                return;

            oldColor *= (float)ActNum;
        }

        #endregion

        #region lava

        public bool lava() => ((bTileHeader & 32) == 32);
        public void lava(bool Lava)
        {
            if (Lava)
                bTileHeader = (byte)((bTileHeader & 159) | 32);
            else
                bTileHeader &= 223;
        }

        #endregion
        #region honey

        public bool honey() => ((bTileHeader & 64) == 64);
        public void honey(bool Honey)
        {
            if (Honey)
                bTileHeader = (byte)((bTileHeader & 159) | 64);
            else
                bTileHeader &= 191;
        }

        #endregion
        #region liquidType

        public byte liquidType() => (byte)((bTileHeader & 96) >> 5);
        public void liquidType(int LiquidType)
        {
            if (LiquidType == 0)
                bTileHeader &= 159;
            else if (LiquidType == 1)
                lava(true);
            else if (LiquidType == 2)
                honey(true);
        }

        #endregion
        #region checkingLiquid

        public bool checkingLiquid() => ((bTileHeader3 & 8) == 8);
        public void checkingLiquid(bool CheckingLiquid)
        {
            if (CheckingLiquid)
                bTileHeader3 |= 8;
            else
                bTileHeader3 &= 247;
        }

        #endregion
        #region skipLiquid

        public bool skipLiquid() => ((bTileHeader3 & 16) == 16);
        public void skipLiquid(bool SkipLiquid)
        {
            if (SkipLiquid)
                bTileHeader3 |= 16;
            else
                bTileHeader3 &= 239;
        }

        #endregion

        #region frame

        public byte frameNumber() => (byte)((bTileHeader2 & 48) >> 4);
        public void frameNumber(byte FrameNumber) =>
            bTileHeader2 = (byte)((bTileHeader2 & 207) | ((FrameNumber & 3) << 4));

        public byte wallFrameNumber() => (byte)((bTileHeader2 & 192) >> 6);
        public void wallFrameNumber(byte WallFrameNumber) =>
            bTileHeader2 = (byte)((bTileHeader2 & 63) | ((WallFrameNumber & 3) << 6));

        public int wallFrameX() => ((bTileHeader2 & 15) * 36);
        public void wallFrameX(int WallFrameX) =>
            bTileHeader2 = (byte)((bTileHeader2 & 240) | ((WallFrameX / 36) & 15));

        public int wallFrameY() => ((bTileHeader3 & 7) * 36);
        public void wallFrameY(int WallFrameY) =>
            bTileHeader3 = (byte)((bTileHeader3 & 248) | ((WallFrameY / 36) & 7));

        #endregion

        #region color

        public byte color() => (byte)(sTileHeader & 31);
        public void color(byte Color)
        {
            if (Color > 31)
                Color = 31;
            sTileHeader = (short)((sTileHeader & 65504) | Color);
        }

        #endregion
        #region wallColor

        public byte wallColor() => (byte)(bTileHeader & 31);
        public void wallColor(byte WallColor)
        {
            if (WallColor > 31)
                WallColor = 31;
            bTileHeader = (byte)((bTileHeader & 224) | WallColor);
        }

        #endregion

        #region active

        public bool active() => ((sTileHeader & 32) == 32);
        public void active(bool Active)
        {
            if (Active)
                sTileHeader |= 32;
            else
                sTileHeader = (short)(sTileHeader & 65503);
        }

        #endregion
        #region inActive

        public bool inActive() => ((sTileHeader & 64) == 64);
        public void inActive(bool InActive)
        {
            if (InActive)
                sTileHeader |= 64;
            else
                sTileHeader = (short)(sTileHeader & 65471);
        }

        #endregion
        public bool nactive() => ((sTileHeader & 96) == 32);

        #region wire

        public bool wire() => ((sTileHeader & 128) == 128);
        public void wire(bool Wire)
        {
            if (Wire)
                sTileHeader |= 128;
            else
                sTileHeader = (short)(sTileHeader & 65407);
        }

        #endregion
        #region wire2

        public bool wire2() => ((sTileHeader & 256) == 256);
        public void wire2(bool Wire2)
        {
            if (Wire2)
                sTileHeader |= 256;
            else
                sTileHeader = (short)(sTileHeader & 65279);
        }

        #endregion
        #region wire3

        public bool wire3() => ((sTileHeader & 512) == 512);
        public void wire3(bool Wire3)
        {
            if (Wire3)
                sTileHeader |= 512;
            else
                sTileHeader = (short)(sTileHeader & 65023);
        }

        #endregion
        #region wire4

        public bool wire4() => ((bTileHeader & 128) == 128);

        public void wire4(bool Wire4)
        {
            if (Wire4)
                bTileHeader |= 128;
            else
                bTileHeader &= 127;
        }

        #endregion
        #region actuator

        public bool actuator() => ((sTileHeader & 2048) == 2048);
        public void actuator(bool Actuator)
        {
            if (Actuator)
                sTileHeader |= 2048;
            else
                sTileHeader = (short)(sTileHeader & 63487);
        }

        #endregion

        #region halfBrick

        public bool halfBrick() => ((sTileHeader & 1024) == 1024);
        public void halfBrick(bool HalfBrick)
        {
            if (HalfBrick)
                sTileHeader |= 1024;
            else
                sTileHeader = (short)(sTileHeader & 64511);
        }

        #endregion
        #region slope

        public byte slope() => (byte)((sTileHeader & 28672) >> 12);
        public void slope(byte Slope) =>
            sTileHeader = (short)((sTileHeader & 36863) | ((Slope & 7) << 12));

        #endregion
        #region topSlope

        public bool topSlope()
        {
            byte b = slope();
            return ((b == 1) || (b == 2));
        }

        #endregion
        #region bottomSlope

        public bool bottomSlope()
        {
            byte b = slope();
            return ((b == 3) || (b == 4));
        }

        #endregion
        #region leftSlope

        public bool leftSlope()
        {
            byte b = slope();
            return ((b == 2) || (b == 4));
        }

        #endregion
        #region rightSlope

        public bool rightSlope()
        {
            byte b = slope();
            return ((b == 1) || (b == 3));
        }

        #endregion
        public bool HasSameSlope(ITile Tile) =>
            ((sTileHeader & 29696) == (Tile.sTileHeader & 29696));
        #region blockType

        public int blockType()
        {
            if (halfBrick())
                return 1;
            int num = slope();
            if (num > 0)
                num++;
            return num;
        }

        #endregion

        public object Clone() => MemberwiseClone();
        #region ToString

        public new string ToString() =>
            $"Tile Type:{type} Active:{active()} " +
            $"Wall:{wall} Slope:{slope()} fX:{frameX} fY:{frameY}";

        #endregion
        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj is ITile t)
                return t.isTheSameAs(this);
            return false;
        }
    }
}
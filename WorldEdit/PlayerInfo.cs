using System;
using System.Linq;
using Terraria;
using WorldEdit.Expressions;

namespace WorldEdit
{
	public class PlayerInfo
	{
		private int _x = -1;
		private int _x2 = -1;
		private int _y = -1;
		private int _y2 = -1;

        public const string Key = "WorldEdit_Data";
        
        public int Point = 0;
		public Selection Select = null;
        private MagicWand _magicWand = null;
        public Expression SavedExpression = null;
        public int X
		{
			get => _x;
            set
            {
                _x = Math.Max(0, value);
                _magicWand = null;
            }
		}
		public int X2
		{
			get => _x2;
            set
            {
                _x2 = Math.Min(value, Main.maxTilesX - 1);
                _magicWand = null;
            }
		}
		public int Y
		{
			get => _y;
            set
            {
                _y = Math.Max(0, value);
                _magicWand = null;
            }
		}
		public int Y2
		{
			get => _y2;
            set
            {
                _y2 = Math.Min(value, Main.maxTilesY - 1);
                _magicWand = null;
            }
		}
        public MagicWand MagicWand
        {
            get => _magicWand;
            set
            {
                _magicWand = value ?? new MagicWand();
                if (value == null)
                { _x = _x2 = _y = _y2 = -1; }
                else
                {
                    var _1 = _magicWand.Points.OrderBy(p => p.X);
                    var _2 = _magicWand.Points.OrderBy(p => p.Y);
                    _x = _1.First().X;
                    _x2 = _1.Last().X;
                    _y = _2.First().Y;
                    _y2 = _2.Last().Y;
                }
            }
        }
    }
}
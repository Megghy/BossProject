using System;
using System.Reflection;
using System.Reflection.Emit;

namespace FakeProvider
{
    static class Helper
    {
        #region Inside

        public static bool Inside(int PointX, int PointY, int X, int Y, int Width, int Height) =>
            PointX >= X && PointY >= Y && PointX < X + Width && PointY < Y + Height;

        #endregion
        #region Clamp

        public static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

        #endregion
    }
}

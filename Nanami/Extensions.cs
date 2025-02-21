namespace Nanami
{
    internal static class Extensions
    {
        public static T NotNull<T>(this T obj) where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            return obj;
        }

        public static string ToChineseCharacterDigit(this int num)
        {
            if (num == 0)
            {
                return ChineseCharacters[0].ToString();
            }

            var lessThanZero = num < 0;
            var result = "";
            num = Math.Abs(num);

            while (num > 0)
            {
                var digit = num % 10;
                result += ChineseCharacters[digit];
                num /= 10;
            }

            return (lessThanZero ? "负" : string.Empty) + string.Join(string.Empty, result.Reverse());
        }

        private static readonly char[] ChineseCharacters = { '零', '一', '二', '三', '四', '五', '六', '七', '八', '九' };
    }
}

using System.Text;

namespace PreSearingOfficialGames.Extensions.GamesExtensions
{
    public static partial class GamesExtensions
    {
        /// <summary>
        /// Removes white spaces from the specified string
        /// </summary>
        /// <param name="str"></param>
        /// <returns><see langword="string"/> without spaces</returns>
        public static string RemoveSpaces(this string str) => str.Replace(" ", "");

        /// <summary>
        /// Add white spaces before each capital letter, excluding the one at the beginning of <paramref name="input"/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns><see langword="string"/> with added white spaces</returns>
        public static string AddSpaceBeforeCapitals(this string input)
        {
            var result = new StringBuilder();
            result.Append(input[0]);

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }

            return result.ToString();
        }

        public static bool IsEqual(this string str1, string str2) 
            => string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
    }
}

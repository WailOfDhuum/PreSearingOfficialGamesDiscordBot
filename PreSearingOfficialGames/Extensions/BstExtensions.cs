using System.Text.RegularExpressions;

namespace PreSearingOfficialGames.Extensions
{
    public static class BstExtensions
    {
        public static bool ContainsAnyOfBstDigits(this string currentNumber) => Regex.IsMatch(currentNumber, "[369]");
    }
}

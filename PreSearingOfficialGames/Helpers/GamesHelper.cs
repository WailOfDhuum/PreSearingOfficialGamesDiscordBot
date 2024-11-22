using Discord;
using PreSearingOfficialGames.Extensions.GamesExtensions;
using PreSearingOfficialGames.Games;

namespace PreSearingOfficialGames.Helpers
{
    public static class GamesHelper
    {
        public const string CheckmarkEmoteUnicode = "\u2705";
        public const string CrossEmoteUnicode = "\u274C";
        public static char[] AllowedSpecialCharacters { get; } = new[]
        {
            '!',
            ' ',
            ':',
            '_',
            '-',
            '[',
            ']'
        };

        public static bool IsMessageValid(IMessage message, ulong botId, ulong channelId)
        {
            if (message.IsNullOrEmpty()
                || message.IsMadKingBotMessage(botId)
                || !message.Channel.IsFromCorrectChannel(channelId)) return false;

            return true;
        }

        public static ValidationResult<GameTimer?> GetTimerFromMessage(IUserMessage message, string command)
        {
            var timerMessageValidation = ValidateTimerMessage(message, command);
            if (timerMessageValidation.IsError)

                return ValidationResult<GameTimer?>.Failure(timerMessageValidation.ErrorMessage);

            var newTimerValueResults = GetTimerValueIfValid(message, command);
            if (newTimerValueResults.IsError)
                return ValidationResult<GameTimer?>.Failure(newTimerValueResults.ErrorMessage);

            var newTimerUnitsResults = GetTimerUnitsIfValid(message, command);
            if (newTimerUnitsResults.IsError)
                return ValidationResult<GameTimer?>.Failure(newTimerUnitsResults.ErrorMessage);

            var newTimerValue = newTimerValueResults.Value.Value;
            var newTimerUnits = newTimerUnitsResults.Value.Value;

            var timerResult = GameTimer.GetGameTimerIfValid(newTimerValue, newTimerUnits);
            if (timerResult.IsError)
            {
                return ValidationResult<GameTimer?>.Failure(timerResult.ErrorMessage!);
            }

            return timerResult;
        }

        public static ValidationResult ValidateTimerMessage(IUserMessage message, string command)
        {
            var validations = new Func<ValidationResult>[]
           {
                () => message.IsValid(command),
                message.IsUserModerator,
                () => AreSquareBracketsValid(message, command),
           };

            foreach (var validator in validations)
            {
                var validationResult = validator();
                if (validationResult.IsError)
                {
                    return ValidationResult.Failure(validationResult.ErrorMessage);
                }
            }

            return ValidationResult.Success();
        }

        private static ValidationResult AreSquareBracketsValid(IUserMessage message, string command)
        {
            var content = message.GetAnswer(command);

            var openBracketCount = content.Count(c => c == '[');
            var closeBracketCount = content.Count(c => c == ']');
            if ((openBracketCount == 0 && closeBracketCount == 0)
                || (openBracketCount == 1 && closeBracketCount == 1))
            {
                return ValidationResult.Success();
            }

            return ValidationResult.Failure("Use the appropriate number of square brackets you moron!");
        }

        public static ValidationResult<int?> GetTimerValueIfValid(IUserMessage message, string command)
        {
            var content = message.GetAnswer(command);
            string val = string.Empty;

            //units are in [], so everything before '[' is the new timer val
            if (!content.Any(c => c == '['))
            {
                val = content;
            }
            else
            {
                var endIndex = content.LastIndexOf('[');
                val = content[0..endIndex];
            }

            if (!int.TryParse(val, out var newTimerValue))
                return ValidationResult<int?>.Failure("Incorrect value for new timer! Use numeric values.");

            return ValidationResult<int?>.Success(newTimerValue);
        }

        public static ValidationResult<TimerUnits?> GetTimerUnitsIfValid(IUserMessage message, string command)
        {
            var content = message.GetAnswer(command);
            TimerUnits newTimerUnits;

            // not having units is allowed
            // the presence of brackets and their number is checked in the method AreSquareBracketsValid
            if (!content.Any(c => c == '[' || c == ']'))
            {
                newTimerUnits = TimerUnits.none;
                return ValidationResult<TimerUnits?>.Success(newTimerUnits);
            }

            var startIndex = content.IndexOf('[') + 1;
            var endIndex = content.LastIndexOf(']');
            var units = content[startIndex..endIndex];

            if (!Enum.TryParse(units, true, out newTimerUnits))
            {
                return ValidationResult<TimerUnits?>.Failure(
                    "Incorrect units for new timer! " +
                    $"Use {string.Join(", ", Enum.GetNames(typeof(TimerUnits)))} in square brackets.");
            }

            return ValidationResult<TimerUnits?>.Success(newTimerUnits);
        }
    }
}
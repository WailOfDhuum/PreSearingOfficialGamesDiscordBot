using Discord;
using Discord.WebSocket;
using PreSearingOfficialGames.Helpers;
using System.Text.RegularExpressions;

namespace PreSearingOfficialGames.Extensions.GamesExtensions
{
    public static partial class GamesExtensions
    {
        /// <summary>
        /// Validates the user's message
        /// </summary>
        /// <param name="message">The user's message</param>
        /// <param name="command">The game command</param>
        /// <returns><see langword="ValidationResult"/> with <see langword="IsError"/> property set 
        /// as <see langword="true"/> and an error message if <paramref name="message"/> is invalid 
        /// or <see langword="false"/> and an empty error message if the message valid </returns>
        public static ValidationResult IsValid(this IMessage message, string command)
        {
            var validators = new Func<ValidationResult>[]
            {
                () => message.IsNullOrEmpty(command),
                () => message.StartsWithCommand(command),
                () => message.IsValidLength(command),
                () => message.AreValidChars(),
                () => message.IsSpecialCharacterAbuse(),
                () => message.IsAnswerSeparatedByWhiteSpace(command)
            };

            foreach (var validator in validators)
            {
                var result = validator();
                if (result.IsError)
                    return result;
            }

            return ValidationResult.Success();
        }

        private static ValidationResult IsNullOrEmpty(this IMessage message, string command)
        {
            if (string.IsNullOrEmpty(message?.Content))
            {
                return ValidationResult.Failure(
                    $"Hmmm your message is empty, try to use `{command} <x>`"
                    );
            }

            return ValidationResult.Success();
        }

        private static ValidationResult StartsWithCommand(this IMessage message, string command)
        {
            if (message.Content.StartsWith(command, StringComparison.OrdinalIgnoreCase))
                return ValidationResult.Success();

            return ValidationResult.Failure(
                isError: true, 
                errorMessage: string.Empty
                );
        }

        private static ValidationResult IsValidLength(this IMessage message, string command)
        {
            if (message.Content.Length > 40)
            {
                return ValidationResult.Failure(
                    $"Are you writing a thesis or what? To play the game use `{command} <x>`, " + 
                    "to write your thesis use a private chat with Meir"
                    );
            }

            return ValidationResult.Success();
        }

        private static ValidationResult IsSpecialCharacterAbuse(this IMessage message)
        {
            if (message.Content.Count(c => GamesHelper.AllowedSpecialCharacters.Contains(c)) > 10)
            {
                return ValidationResult.Failure(
                    "If you are bored, why not play tag with charrs outside the northern walls?"
                    );
            }

            return ValidationResult.Success();
        }

        private static ValidationResult AreValidChars(this IMessage message)
        {
            if (Regex.IsMatch(message.Content, @"[^a-zA-Z0-9! \[\]:_-]"))
            {
                return ValidationResult.Failure(
                    "What are you cooking? Use english alphabet smartass or your answer will be ignored!"
                    );
            }

            return ValidationResult.Success();
        }

        private static ValidationResult IsAnswerSeparatedByWhiteSpace(this IMessage message, string command)
        {
            if (message.Content.Length == command.Length || !char.IsWhiteSpace(message.Content[command.Length]))
            {
                return ValidationResult.Failure(
                    $"You fool, your command is incorrect! Use `{command} <x>` " +
                    "and don't waste my time checking such nonsense."
                    );
            }

            return ValidationResult.Success();
        }

        public static ValidationResult IsUserModerator(this IMessage message)
        {
            var user = message.GetUser()
                ?? throw new ArgumentNullException($"Message author is empty. Message content: {message.Content}");

            if (!user.IsModerator())
            {
                var fool = user.GetUserName();
                return ValidationResult.Failure(
                    $@"{(fool is not null 
                        ? $"Haha, nice try {fool}! Unfortunately, you are just a mere mortal." 
                        : "Haha, nice try! Unfortunately, you are just a mere mortal.")}");
            }

            return ValidationResult.Success();
        }

        public static bool IsNullOrEmpty(this IMessage message) => string.IsNullOrEmpty(message?.Content);

        public static bool IsMadKingBotMessage(this IMessage message, ulong madKingBotId) => message.Author.Id == madKingBotId;
        public static SocketGuildUser GetUser(this IMessage message) => message?.Author as SocketGuildUser ?? null;

        /// <summary>
        /// Extracts content from the user's message ignoring <paramref name="command"/>
        /// </summary>
        /// <param name="message">The user's message</param>
        /// <param name="command">The game command</param>
        /// <returns>Answer as <see langword="string"/></returns>
        public static string GetAnswer(this IMessage message, string command)
            => message.Content.Substring(command.Length).RemoveSpaces();
    }
}
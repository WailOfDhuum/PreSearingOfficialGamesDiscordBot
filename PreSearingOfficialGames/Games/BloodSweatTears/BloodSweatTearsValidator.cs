using Discord;
using PreSearingOfficialGames.Extensions.GamesExtensions;
using PreSearingOfficialGames.Games.Interfaces;
using PreSearingOfficialGames.Helpers;

namespace PreSearingOfficialGames.Games.BloodSweatTears
{
    public partial class BloodSweatTears : Game, IGame
    {
        private ValidationResult IsAnswerIncorrect(IUserMessage message)
        {
            var userMessage = message.GetAnswer(_bstAnswerCommand);
            CorrectAnswer = GetCorrectAnswer(CurrentNumber.ToString());

            if (string.IsNullOrEmpty(CorrectAnswer))
                throw new ArgumentNullException($"Correct answer is null or empty. CurrentNumber: {CurrentNumber}");

            if (!string.Equals(userMessage.RemoveSpaces(), CorrectAnswer.RemoveSpaces(),
                StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(
                    $"Incorrect answer. The correct answer is {CorrectAnswer}.\n" +
                    "You must be a charr spy. Guards! Banish this fool beyond the northern walls."
                    );
            }

            return ValidationResult.Success();
        }

        private ValidationResult HasUserAlreadyPosted(IUserMessage message)
        {
            var author = message.Author.Username;

            if (_playersQueue.Contains(author))
            {
                return ValidationResult.Failure(
                    $"Can't you count to {_maxQueueSize}?\n" +
                    "Ascalonian peasants are becoming more and more stupid..."
                    );
            }

            return ValidationResult.Success();
        }

        private ValidationResult IsRecordAlreadyBeaten()
        {
            if (_isRecordBeaten)
            {
                return ValidationResult.Failure(
                    "You cannot set a new record value "
                    + "if the current record has been beaten during this game!"
                    );
            }

            return ValidationResult.Success();
        }

        private ValidationResult GetValueToSet(IUserMessage message, out int validRecord)
        {
            var newRecord = message.GetAnswer(_bstSetNewRecord);
            if (!int.TryParse(newRecord, out validRecord))
            {
                return ValidationResult.Failure("*sigh* Do I really need to explain why your command did not work?");
            }

            return ValidationResult.Success();
        }

        private ValidationResult IsNewValueValid(int value)
        {
            if (value <= CurrentNumber)
            {
                return ValidationResult.Failure(
                    "You clown, you are trying to set the current record to a value, " +
                    $"which is lower or the same as the next expected answer. Are you trying to cheat me? " +
                    "I will not allow that!"
                    );
            }

            return ValidationResult.Success();
        }

        private ValidationResult IsNewBstNumberValid(IUserMessage message, out int value)
        {
            var content = message.GetAnswer(_bstStartCountingFrom);
            if (!int.TryParse(content, out value))
            {
                return ValidationResult.Failure("Use NUMBERS, clown!");
            }

            if (value <= 0)
            {
                return ValidationResult.Failure("What are you doing you idiot? Counting in bst starts from 1!");
            }

            return ValidationResult.Success();
        }
    }
}
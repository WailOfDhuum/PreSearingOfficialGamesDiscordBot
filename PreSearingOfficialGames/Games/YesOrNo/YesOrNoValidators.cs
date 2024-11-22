using Discord;
using PreSearingOfficialGames.Extensions.GamesExtensions;
using PreSearingOfficialGames.Helpers;

namespace PreSearingOfficialGames.Games.YesOrNo
{
    public partial class YesOrNo
    {
        private ValidationResult ValidateAnswer(IUserMessage message, out string answer)
        {
            answer = message.GetAnswer(_yonAnswerCommand);
            if (!_yesOrNoAnswers.Contains(answer, StringComparer.OrdinalIgnoreCase))
                return ValidationResult.Failure("All you have to do is write `Yes` or `No`!");

            return ValidationResult.Success();
        }

        private ValidationResult GetParticipantsNumber(IUserMessage message, out int newParticipantsNumber)
        {
            var content = message.GetAnswer(_yonChangeParticipantsNumber);

            if (!int.TryParse(content, out newParticipantsNumber))
                return ValidationResult.Failure("Incorrect value for new max participants number! Use numeric values.");

            return ValidationResult.Success();
        }

        private ValidationResult IsMaxParticipantsNumberValid(int participantsNumber)
        {
            if (participantsNumber < 1)
            {
                return ValidationResult.Failure("... What are you trying to do?");
            }
            var currentParticipantsNumber = _usersCorrectAnswers.Keys.Count;
            if (participantsNumber <= currentParticipantsNumber)
            {
                return ValidationResult.Failure("You cannot set the number of participants to be less or the same " +
                    $"as the current number. Current Participants number: {currentParticipantsNumber}.");
            }

            if (participantsNumber > _maxParticipantsExt)
            {
                return ValidationResult.Failure("I don't allow this value. "
                    + "Does this server even have that many active users?");
            }

            return ValidationResult.Success();
        }
    }
}
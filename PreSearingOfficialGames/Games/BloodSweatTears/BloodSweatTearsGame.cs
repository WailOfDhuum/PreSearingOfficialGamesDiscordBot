using Discord;
using PreSearingOfficialGames.Extensions;
using PreSearingOfficialGames.Extensions.GamesExtensions;
using PreSearingOfficialGames.Games.Interfaces;
using PreSearingOfficialGames.Helpers;

namespace PreSearingOfficialGames.Games.BloodSweatTears
{
    public partial class BloodSweatTears : Game, IGame
    {
        public int CurrentNumber { get; set; } = 1;
        public int CurrentRecord { get; set; } = 691;

        //game starts from 1
        public string CorrectAnswer { get; set; } = "1";
        public string GameName => nameof(BloodSweatTears).AddSpaceBeforeCapitals();

        public event Func<Task>? GameStarted;
        public event Func<Task>? GameEnded;

        private const string _bstAnswerCommand = "!bst";
        private const string _bstSpecialCommand = "!bst_sc";
        private const string _bstSetNewRecord = $"{_bstSpecialCommand} set_new_record";
        private const string _bstStartCountingFrom = $"{_bstSpecialCommand} start_counting_from";

        private bool _isRecordBeaten = false;
        private readonly int _maxQueueSize = 3;
        private readonly Queue<string> _playersQueue = new();


        public BloodSweatTears(ulong botId, IMessageChannel channel) : base(botId, channel)
        {
            _specialCommands = new Dictionary<string, Func<IUserMessage, Task>>()
            {
                { _bstSetNewRecord, SetBstRecord },
                { _bstStartCountingFrom, SetBstCurrentNumber }
            };
        }

        public async Task Run()
        {
            var initialMessage =
                "If you don't know this game, then you need to play more Pre.\n" +
                "Count as high as you can replacing the number 3, 6 and 9 with 'Blood', 'Sweat', 'Tears' respectively.\n" +
                "eg: ...21, 22, Blood, 24, 25...\n\n" +
                "You may only count one number per post.\n" +
                "You can only re-post after 3 other players have posted (relates to the game only).\n" +
                "Double posting the same number results in a game loss.\n\n" +
                "Participants will receive 20 coins for each correct response collectively.\n" +
                "If participants beat the current record, the rewards are doubled.\n" +
                $"Current record is: {CurrentRecord}\n" +
                "Blood Sweat Tears has started!\n" +
                "Enter the next number using the format `!bst <NUMBER>`";

            await Channel.SendMessageAsync(initialMessage);
            await FireEvent(GameStarted);
        }

        public async Task End()
        {
            if (_isRecordBeaten)
            {
                await Channel.SendMessageAsync($"{nameof(BloodSweatTears).AddSpaceBeforeCapitals()} is over, " +
                    "but rejoice people of the peaceful Ascalon city! " +
                    "The spy will be punished and you have beaten the record, what happens very rarely.");
            }
            else
            {
                await Channel.SendMessageAsync($"{nameof(BloodSweatTears).AddSpaceBeforeCapitals()} is over.");
            }
            await FireEvent(GameEnded);
        }

        public async Task ListenForAnswers(IUserMessage message)
        {
            if (!GamesHelper.IsMessageValid(message, BotId, Channel.Id)) return;

            if (IsSpecialCommand(message.Content))
            {
                await TryRunCommand(message);
                return;
            }
            var messageValidationResult = message.IsValid(_bstAnswerCommand);
            if (messageValidationResult.IsError)
            {
                if (!string.IsNullOrEmpty(messageValidationResult.ErrorMessage))
                    await Channel.SendMessageAsync(messageValidationResult.ErrorMessage);

                return;
            }

            var bstAnswerValidations = new Func<ValidationResult>[]
            {
                () => IsAnswerIncorrect(message),
                () => HasUserAlreadyPosted(message)
            };

            foreach (var validator in bstAnswerValidations)
            {
                var result = validator();
                if (result.IsError)
                {
                    await message.AddReactionAsync(new Emoji(GamesHelper.CrossEmoteUnicode));
                    await Channel.SendMessageAsync(result.ErrorMessage);
                    await End();
                    return;
                }
            }

            await message.AddReactionAsync(new Emoji(GamesHelper.CheckmarkEmoteUnicode));
            await CheckIfRecordIsBeaten();
            AddUserToQueue(message.Author.Username);
            CurrentNumber++;
        }

        /// <summary>
        /// Only for mods - sets new bst record
        /// </summary>
        private async Task SetBstRecord(IUserMessage message)
        {
            int validRecord = 0;
            var bstCommandValidations = new Func<ValidationResult>[]
            {
                () => message.IsValid(_bstSetNewRecord),
                message.IsUserModerator,
                IsRecordAlreadyBeaten,
                () => GetValueToSet(message, out validRecord),
                () => IsNewValueValid(validRecord)
            };

            foreach (var validator in bstCommandValidations)
            {
                var validationResult = validator();
                if (validationResult.IsError)
                {
                    await Channel.SendMessageAsync(validationResult.ErrorMessage);
                    return;
                }
            }

            CurrentRecord = validRecord;
            await Channel.SendMessageAsync("Done!");
        }

        private async Task SetBstCurrentNumber(IUserMessage message)
        {
            var newValue = -1;
            var bstCommandValidations = new Func<ValidationResult>[]
            {
                () => message.IsValid(_bstStartCountingFrom),
                message.IsUserModerator,
                () => IsNewBstNumberValid(message, out newValue)
            };

            foreach (var validator in bstCommandValidations)
            {
                var validationResult = validator();
                if (validationResult.IsError)
                {
                    await Channel.SendMessageAsync(validationResult.ErrorMessage);
                    return;
                }
            }

            CurrentNumber = newValue;
            _playersQueue.Clear();
            await Channel.SendMessageAsync($"From now on, counting starts from {newValue}.");
        }

        private static string GetCorrectAnswer(string currentNumber)
        {
            if (!currentNumber.ContainsAnyOfBstDigits())
                return currentNumber;

            var bstAnswer = string.Empty;

            for (int i = 0; i < currentNumber.Length; i++)
            {
                var result = Bst(currentNumber[i]);
                if (string.IsNullOrEmpty(result)) continue;

                bstAnswer = string.Concat(bstAnswer, result, " ");
            }

            return bstAnswer.TrimEnd();
        }

        private static string Bst(char digit) => digit switch
        {
            '3' => "blood",
            '6' => "sweat",
            '9' => "tears",
            _ => string.Empty
        };

        private async Task CheckIfRecordIsBeaten()
        {
            if (!_isRecordBeaten && CurrentNumber > CurrentRecord)
            {
                _isRecordBeaten = true;
                await Channel.SendMessageAsync("You are smarter than I thought, good job! " +
                    "You have just beaten the current record! The rewards will be doubled after completing the game.");
            }

            if (_isRecordBeaten)
            {
                CurrentRecord = CurrentNumber;
            }
        }

        private void AddUserToQueue(string user)
        {
            _playersQueue.Enqueue(user);

            if (_playersQueue.Count > 3)
                _playersQueue.Dequeue();
        }
    }
}
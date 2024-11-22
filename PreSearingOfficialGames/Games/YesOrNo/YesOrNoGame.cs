using Discord;
using PreSearingOfficialGames.Extensions.GamesExtensions;
using PreSearingOfficialGames.Extensions.EnumsExtensions;
using PreSearingOfficialGames.Games.Interfaces;
using PreSearingOfficialGames.Helpers;

namespace PreSearingOfficialGames.Games.YesOrNo
{
    public partial class YesOrNo : Game, IGame
    {
        public event Func<Task>? GameStarted;
        public event Func<Task>? GameEnded;

        private bool _addingEmojis = false;
        private bool _endingGame = false;

        private int _maxParticipantsNumber = 500; //should never be reached

        private const int _maxParticipantsExt = 1500;

        private const string _emoteN_Unicode = "\U0001F1F3";
        private const string _emoteY_Unicode = "\U0001F1FE";

        private const string _yonAnswerCommand = "!yon";
        private const string _yonSpecialCommand = "!yon_sc";
        private const string _yonFinishGameIn = $"{_yonSpecialCommand} finish_game_in";
        private const string _yonChangeParticipantsNumber = $"{_yonSpecialCommand} change_participants_number";

        private readonly TimeSpan _waitBeforeAddingNextReaction = TimeSpan.FromMicroseconds(100);
        private GameTimer _timer = GameTimer.GetDefaultGameTimer();

        private DateTime _endTime;

        private readonly CancellationTokenSource _mainCancellationTokenSource;
        private CancellationTokenSource _timerCancellationTokenSource;
        private CancellationTokenSource _reactionsCancellationTokenSource;
        private Dictionary<ulong, YesOrNoAnswer> _usersCorrectAnswers = new();

        private Task _startTimer;
        private Task _addReactions;

        private readonly string[] _yesOrNoAnswers =
        [
            "No",
            "Yes"
        ];

        public YesOrNo(ulong botId, IMessageChannel channel) : base(botId, channel)
        {
            _specialCommands = new Dictionary<string, Func<IUserMessage, Task>>()
            {
                { _yonFinishGameIn, FinishGameIn },
                { _yonChangeParticipantsNumber, ChangeParticipantsNumber }
            };

            _mainCancellationTokenSource = new CancellationTokenSource();
            _timerCancellationTokenSource = CreateNewLinkedTokenSourceToMainToken();
            _reactionsCancellationTokenSource = CreateNewLinkedTokenSourceToMainToken();
        }

        private CancellationTokenSource CreateNewLinkedTokenSourceToMainToken()
            => CancellationTokenSource .CreateLinkedTokenSource(_mainCancellationTokenSource.Token);

        public async Task Run()
        {
            var initialMessage =
                "Yes or No? The rules are simple! Choose your answer by using `!yon yes` or `!yon no` command.\n" +
                "The result will be announced after 24 hours.";

            await Channel.SendMessageAsync(initialMessage);
            await FireEvent(GameStarted);
            SetEndTime();
            _startTimer = StartTimer(_timerCancellationTokenSource.Token);
        }

        private void SetEndTime() => _endTime = DateTime.UtcNow.Add(_timer.Value);
        
        private async Task StartTimer(CancellationToken token)
        {
            try
            {
                await Task.Delay(_timer.Value, token);
                await Channel.SendMessageAsync("Time has passed!");
                await FinishGame();
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Stopping the timer earlier.");
            }
        }

        public async Task ListenForAnswers(IUserMessage message)
        {
            if (IsGameEnding()) return;
            if (!GamesHelper.IsMessageValid(message, BotId, Channel.Id)) return;

            if (IsSpecialCommand(message.Content))
            {
                await TryRunCommand(message);
                return;
            }

            var answer = string.Empty;
            var yonValidations = new Func<ValidationResult>[]
            {
                () => message.IsValid(_yonAnswerCommand),
                () => ValidateAnswer(message, out answer)
            };

            foreach (var validator in yonValidations)
            {
                var result = validator();
                if (result.IsError)
                {
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                        await Channel.SendMessageAsync(result.ErrorMessage);

                    return;
                }
            }

            if (IsMaxPlayersCount(message.Author.Id))
            {
                await Channel.SendMessageAsync("Haha! You are unlucky, I decided to finish this game " +
                    "earlier due to too much interest.");

                await FinishGame();
                return;

            }
            await AddAnswerToDictionary(message, answer);
        }

        private async Task AddAnswerToDictionary(IUserMessage message, string answer)
        {
            var userId = message.Author.Id;
            if (_usersCorrectAnswers.TryGetValue(userId, out var currentValue))
            {
                if (IsUserAnswerSame(answer, currentValue))
                {
                    await Channel.SendMessageAsync("Why do you answer the same way twice in a row? " +
                        "Don't waste my time you fool!");

                    return;
                };

                _usersCorrectAnswers[userId] = new YesOrNoAnswer(message.Id, answer.ToLower());
            }
            else
            {
                _usersCorrectAnswers.Add(userId, new YesOrNoAnswer(message.Id, answer.ToLower()));
            }

            await AddToMessageCorrespondingYesOrNoEmote(message, answer);

            bool IsUserAnswerSame(string answer, YesOrNoAnswer currentValue) => answer.IsEqual(currentValue.Answer);
        }

        private bool IsMaxPlayersCount(ulong id)
            => _usersCorrectAnswers.Count >= _maxParticipantsNumber && !_usersCorrectAnswers.ContainsKey(id);

        private async Task AddToMessageCorrespondingYesOrNoEmote(IUserMessage message, string answer)
        {
            switch (answer)
            {
                case var ans when ans.IsEqual(_yesOrNoAnswers[0]):
                    await message.AddReactionAsync(new Emoji(_emoteN_Unicode));
                    break;
                case var ans when ans.IsEqual(_yesOrNoAnswers[1]):
                    await message.AddReactionAsync(new Emoji(_emoteY_Unicode));
                    break;
                default:
                    throw new NotImplementedException("Emoji for the given answer was not found.");
            }
        }

        private async Task FinishGame()
        {
            _endingGame = true;
            _timerCancellationTokenSource.Cancel();

            var answer = await ChooseAnswer();
            _addReactions = AddEmotesToAllParticipatingMessages(answer, _reactionsCancellationTokenSource.Token);
            _ = EndYesOrNo();
        }

        private async Task<string> ChooseAnswer()
        {
            var random = new Random();
            var answer = random.Next(2) == 0 ? _yesOrNoAnswers[0] : _yesOrNoAnswers[1];

            await Channel.SendMessageAsync($"My answer is... {answer.ToUpper()!}");
            return answer;
        }

        private async Task AddEmotesToAllParticipatingMessages(string answer, CancellationToken token)
        {
            try
            {
                _addingEmojis = true;
                foreach (var userAnswer in _usersCorrectAnswers)
                {
                    var message = await Channel.GetMessageAsync(userAnswer.Value.MessageId);
                    if (answer.IsEqual(userAnswer.Value.Answer))
                    {
                        await message.AddReactionAsync(new Emoji(GamesHelper.CheckmarkEmoteUnicode));
                    }
                    else
                    {
                        await message.AddReactionAsync(new Emoji(GamesHelper.CrossEmoteUnicode));
                    }

                    await Task.Delay(_waitBeforeAddingNextReaction, token);
                }

                _addingEmojis = false;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Stopped adding reactions to participating messages.");
            }
        }

        private async Task FinishGameIn(IUserMessage message)
        {
            var timerValidationResults = GamesHelper.GetTimerFromMessage(message, _yonFinishGameIn);
            if (timerValidationResults.IsError)
            {
                if (!string.IsNullOrEmpty(timerValidationResults.ErrorMessage))
                    await Channel.SendMessageAsync(timerValidationResults.ErrorMessage);

                return;
            }

            var timer = timerValidationResults.Value ?? throw new ArgumentNullException("Timer is null");

            await SetNewTimer(timer);
            await Channel.SendMessageAsync($"Timer set to {timer.RawValue}[{timer.Units.NameToString()}]");
        }

        private async Task SetNewTimer(GameTimer timer)
        {
            if (timer.Value == TimeSpan.Zero)
            {
                await FinishGame();
                return;
            }

            _timerCancellationTokenSource.Cancel();

            //a small lag might occur
            await _startTimer;

            _timerCancellationTokenSource = CreateNewLinkedTokenSourceToMainToken();
            _timer = timer;
            SetEndTime();

            _startTimer = StartTimer(_timerCancellationTokenSource.Token);
        }

        private async Task ChangeParticipantsNumber(IUserMessage message)
        {
            int newParticipantsNumber = -1;
            var validations = new Func<ValidationResult>[]
            {
                () => message.IsValid(_yonChangeParticipantsNumber),
                message.IsUserModerator,
                () => GetParticipantsNumber(message, out newParticipantsNumber),
                () => IsMaxParticipantsNumberValid(newParticipantsNumber),
            };

            foreach (var validator in validations)
            {
                var validationResult = validator();
                if (validationResult.IsError)
                {
                    await Channel.SendMessageAsync(validationResult.ErrorMessage);
                    return;
                }
            }

            await Channel.SendMessageAsync("Max participants number will be changed from "
                + $"{_maxParticipantsNumber} to {newParticipantsNumber}.");

            _maxParticipantsNumber = newParticipantsNumber;
        }

        private bool IsGameEnding() => _addingEmojis || _endingGame;

        public async Task End()
        {
            _endingGame = true;
            _mainCancellationTokenSource.Cancel();
            _ = EndYesOrNo();
        }

        private async Task EndYesOrNo()
        {
            var tasksToWait = new[] { _startTimer, _addReactions };
            await Task.WhenAll(tasksToWait.Where(t => t is not null && !t.IsCompleted));
            await Channel.SendMessageAsync($"{nameof(YesOrNo).AddSpaceBeforeCapitals()} is over.");
            _endingGame = false;
            await FireEvent(GameEnded);
        }
    }
}
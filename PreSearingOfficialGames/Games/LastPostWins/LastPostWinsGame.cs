using Discord;
using PreSearingOfficialGames.Extensions.EnumsExtensions;
using PreSearingOfficialGames.Extensions.GamesExtensions;
using PreSearingOfficialGames.Games.Interfaces;
using PreSearingOfficialGames.Helpers;

namespace PreSearingOfficialGames.Games.LastPostWins
{
    public class LastPostWins : Game, IGame
    {
        public event Func<Task>? GameStarted;
        public event Func<Task>? GameEnded;

        private bool _isGameEnding;
        private const int _maxParticipantsNumber = 2000; //should never be reached
        private const string _lpwSpecialCommand = "!lpw_sc";
        private const string _lpwFinishGameIn = $"{_lpwSpecialCommand} finish_game_in";

        private ulong _lastMessageId;

        private GameTimer _timer;

        private DateTime _endTime;

        private CancellationTokenSource _timerCancellationTokenSource;
        private Task _startTimer;

        public LastPostWins(ulong botId, Discord.IMessageChannel channel) : base(botId, channel)
        {
            _specialCommands = new Dictionary<string, Func<IUserMessage, Task>>()
            {
                { _lpwFinishGameIn, FinishGameIn }
            };

            _timerCancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Run()
        {
            var initialMessage =
                "I am disappointed with your choice, people of Ascalon, " +
                "probably not the first and not the last time... This game was made for fools and yet you chose it. " + 
                "The rules are very simple: the last post before the timer goes off wins... Yes, very interesting. " + 
                "Let the spam begin!";

            SetRandomTimerValue();
            SetEndTime();

            await Channel.SendMessageAsync(initialMessage);
            await FireEvent(GameStarted);
            _startTimer = StartTimer(_timerCancellationTokenSource.Token);
        }

        private void SetRandomTimerValue()
        {
            var random = new Random();

            var minMinutes = (int)TimeSpan.FromMinutes(1).TotalMinutes;
            var maxMinutes = (int)TimeSpan.FromHours(12).TotalMinutes;

            var endTimeInMins = random.Next(minMinutes, maxMinutes);

            var units = TimerUnits.min;
            var timerValidation = GameTimer.GetGameTimerIfValid(endTimeInMins, units);
            if (timerValidation.IsError || timerValidation.Value is null)
                throw new InvalidOperationException(
                    $"Timer could not be set for values: rawTimer {endTimeInMins}, units: {units}.");

            _timer = timerValidation.Value;
        }

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
            if (_isGameEnding) return;
            if (!GamesHelper.IsMessageValid(message, BotId, Channel.Id)) return;

            if (IsSpecialCommand(message.Content))
            {
                await TryRunCommand(message);
                return;
            }

            _lastMessageId = message.Id;
        }

        private async Task FinishGame()
        {
            _isGameEnding = true;
            _timerCancellationTokenSource?.Cancel();

            await MarkLastMessage();
            _ = EndLastPostWins();
        }

        private async Task MarkLastMessage()
        {
            var message = await Channel.GetMessageAsync(_lastMessageId);
            await message.AddReactionAsync(new Emoji(GamesHelper.CheckmarkEmoteUnicode));
        }

        private async Task FinishGameIn(IUserMessage message)
        {
            var timerValidationResults = GamesHelper.GetTimerFromMessage(message, _lpwFinishGameIn);
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

            await _startTimer;

            _timerCancellationTokenSource = new CancellationTokenSource();
            _timer = timer;

            SetEndTime();

            _startTimer = StartTimer(_timerCancellationTokenSource.Token);
        }

        private void SetEndTime() => _endTime = DateTime.UtcNow.Add(_timer.Value);

        public async Task End()
        {
            _isGameEnding = true;
            _timerCancellationTokenSource.Cancel();
            _ = EndLastPostWins();
        }

        private async Task EndLastPostWins()
        {
            await _startTimer;
            await Channel.SendMessageAsync($"{nameof(LastPostWins).AddSpaceBeforeCapitals()} is over.");
            _isGameEnding = false;
            await FireEvent(GameEnded);
        }
    }
}
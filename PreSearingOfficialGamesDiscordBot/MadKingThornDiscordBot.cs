using Discord.WebSocket;
using Discord;
using PreSearingOfficialGames.Games.Interfaces;
using System.Text;
using PreSearingOfficialGames.Extensions.GamesExtensions;
using PreSearingOfficialGames.Helpers;
using Microsoft.Extensions.Configuration;

namespace PreSearingGamesDiscordBot
{
    public class MadKingThornDiscordBot
    {
        private DiscordSocketClient _client;
        private IUserMessage _gameMessage;
        private IGame _runningGame;

        private readonly string _token;
        private readonly ulong _channelId;

        private bool _isAnyGameOn => _runningGame is not null ? true : false;
        private bool _isStartMessageSent = false;

        private const string _startGameCommand = "!game";
        private const string _emergencyStopGameCommand = "!emergency_stop";
        private const int _neededReactionsCount = 2; //TODO: set to 6
        private const int _maxGamesNumber = 9; // we want to use only number emotes from 1 to 9

        private readonly Dictionary<string, string> _gameReactions = [];
        //{
        //    { "\u0031\uFE0F\u20E3", "Blood Sweat Tears" }, //1
        //    { "\u0032\uFE0F\u20E3", "Last Post Wins" },   //2
        //    { "\u0033\uFE0F\u20E3", "Skill Master" },   //3
        //    { "\u0034\uFE0F\u20E3", "Yes or No" },   //4
        //};

        public MadKingThornDiscordBot()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _token = GetTokenFromConfigFile(configuration);
            _channelId = GetChannelIdFromConfigFile(configuration);
            _client = GetDiscordClient();

            LoadGames();
        }

        private static DiscordSocketClient GetDiscordClient()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            return new DiscordSocketClient(config);
        }

        private static string GetTokenFromConfigFile(IConfigurationRoot configuration)
        {
            var botToken = configuration["BotConfiguration:Token"];
            if (string.IsNullOrEmpty(botToken))
            {
                throw new ArgumentNullException("Token not found in the configuration file");
            }

            return botToken;
        }

        private static ulong GetChannelIdFromConfigFile(IConfigurationRoot configuration)
        {
            var channelIdStr = configuration["BotConfiguration:ChannelId"];
            if (string.IsNullOrEmpty(channelIdStr))
            {
                throw new ArgumentNullException("ChannelId not found in the configuration file.");
            }

            if(!ulong.TryParse(channelIdStr, out var channelId))
            {
                throw new ArgumentNullException("ChannelId could not be parsed.");
            }

            return channelId;
        }

        public void LoadGames()
        {
            var interfaceType = typeof(IGame);

            var gamesNames = interfaceType.Assembly.GetTypes()
                .Where(t => t is not null && interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .Select(t => t.Name)
                .Order()
                .ToList();

            var i = 1;
            foreach (var game in gamesNames)
            {
                if (i > _maxGamesNumber) break;

                _gameReactions.Add($"{(char)(0x30 + i)}\uFE0F\u20E3", game);
                i++;
            }
        }

        public async Task RunBotAsync()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, ex) =>
            {
                Console.WriteLine("Unhandled exception: " + ex.ToString());
                Environment.Exit(1); // Optionally terminate the app
            };

            //_client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.ReactionAdded += ReactionAddedAsync;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        //private Task LogAsync(LogMessage log)
        //{
        //    Console.WriteLine(log.ToString());
        //    return Task.CompletedTask;
        //}

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            await TryToSendGameStartMessage(message);
            await ListenToSelectedGame(message);
        }

        //TODO: fix these ifs?
        private async Task TryToSendGameStartMessage(SocketMessage message)
        {
            if (!GamesHelper.IsMessageValid(message, _client.CurrentUser.Id, _channelId)) return;

            var user = message.GetUser()
                ?? throw new ArgumentNullException($"Message author is empty. Message content: {message.Content}");

            if (CanGameStartMessageBeSent(message, user))
            {
                await SendGameStartMessage(message);
                return;
            }

            if (StartingGameWhenGameStartMessageAlreadySent(message))
            {
                var fool = user.GetUserName();
                await message.Channel.SendMessageAsync($@"
                    {(fool is not null
                        ? $"{fool} you fool, voting for a game has already started!"
                        : "You fool, voting for a game has already started!")}");

                return;
            }

            if (StartingGameWhenGameAlreadyOn(message))
            {
                var fool = user.GetUserName();
                await message.Channel.SendMessageAsync($@"
                    {(fool is not null
                        ? $"{fool} you fool, the game is already running!"
                        : "You fool, the game is already running!")}");

                return;
            }

            if (CanGameBeStopped(message, user))
            {
                await message.Channel.SendMessageAsync("Stopping the game immediately!");
                await EmergencyStop();
                return;
            }
        }

        private async Task SendGameStartMessage(SocketMessage message)
        {
            var botMessage = new StringBuilder("Pick a game by reacting with one of the numbers:\n");
            foreach (var emote in _gameReactions)
            {
                botMessage.AppendLine($@"{emote.Key} - {emote.Value.AddSpaceBeforeCapitals()}");
            }

            botMessage.AppendLine();
            botMessage.AppendLine("After 6 votes the chosen game will be started.");

            var gameMessage = await message.Channel.SendMessageAsync(botMessage.ToString());

            foreach (var emote in _gameReactions)
            {
                await gameMessage.AddReactionAsync(new Emoji(emote.Key));
            }

            _gameMessage = gameMessage;
            _isStartMessageSent = true;
        }

        private bool CanGameStartMessageBeSent(SocketMessage message, SocketGuildUser user)
            => !_isStartMessageSent && !_isAnyGameOn && user.IsModerator()
                    && IsCorrectStartGameCommand(message);

        private bool StartingGameWhenGameStartMessageAlreadySent(SocketMessage message)
            => IsCorrectStartGameCommand(message) && _isStartMessageSent && !_isAnyGameOn;

        private bool StartingGameWhenGameAlreadyOn(SocketMessage message)
            => IsCorrectStartGameCommand(message) && (_isAnyGameOn || _isStartMessageSent);

        private bool CanGameBeStopped(SocketMessage message, SocketGuildUser user)
            => _isAnyGameOn && IsCorrectEmergencyStopGameCommand(message) 
                && user.IsModerator() && !message.IsMadKingBotMessage(_client.CurrentUser.Id);

        private async Task ReactionAddedAsync(
            Cacheable<IUserMessage, ulong> cache,
            Cacheable<IMessageChannel, ulong> channel,
            SocketReaction reaction
            )
        {
            if (UnsuccessfulBasicValidation(reaction)) return;

            var message = await cache.GetOrDownloadAsync();
            if (message is null) return;

            if (!IsCorrectReactionOnStartGameMessage(reaction, message))
                return;

            if (!IsReactionCountReached(message, reaction)) return;

            await RunGame(reaction, message);

            bool UnsuccessfulBasicValidation(SocketReaction reaction)
            {
                return _gameMessage is null || _isAnyGameOn || reaction.UserId == _client.CurrentUser.Id;
            }
        }

        private bool IsCorrectReactionOnStartGameMessage(SocketReaction reaction, IUserMessage message)
            => message.Id == _gameMessage.Id && _gameReactions.ContainsKey(reaction.Emote.Name);

        private static bool IsReactionCountReached(IUserMessage message, SocketReaction reaction)
        {
            var reactionCount = message.Reactions[new Emoji(reaction.Emote.Name)].ReactionCount;
            return reactionCount >= _neededReactionsCount;
        }

        private async Task RunGame(SocketReaction reaction, IUserMessage message)
        {
            var gameName = GetGameName(reaction);
            await message.Channel.SendMessageAsync($"Game {gameName} selected!");

            _runningGame = CreateSelectedGame(gameName, _client.CurrentUser.Id, message.Channel);
            _runningGame.GameStarted += OnGameStarted;
            _runningGame.GameEnded += OnGameEnded;
            await _runningGame.Run();
        }

        private string GetGameName(SocketReaction reaction)
        {
            var gameName = MapEmojiToGameName(reaction.Emote);
            if (string.IsNullOrEmpty(gameName))
                throw new ArgumentNullException("Game for the selected emote was not found.");

            return gameName;
        }


        private string MapEmojiToGameName(IEmote emote)
        {
            if (_gameReactions.TryGetValue(emote.Name, out var name))
                return name;

            return null;
        }

        private static IGame CreateSelectedGame(string gameName, ulong botId, IMessageChannel channel)
        {
            var interfaceType = typeof(IGame);
            var type = interfaceType.Assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == gameName && interfaceType.IsAssignableFrom(t) && !t.IsAbstract)
                        ?? throw new NotImplementedException("Game type not found!");

            return (IGame)Activator.CreateInstance(type, botId, channel);
        }

        private async Task ListenToSelectedGame(SocketMessage message)
        {
            if (message is null || message.IsMadKingBotMessage(_client.CurrentUser.Id)) return;
            if (!_isAnyGameOn) return;
            if (_runningGame is null)
                throw new ArgumentNullException("Game was expected to listen to the channel but was not found.");

            var userMessage = message as IUserMessage;
            if (userMessage is null) return;

            await _runningGame.ListenForAnswers(userMessage);
        }

        private async Task OnGameStarted()
        {
            //message some1 about a game start?
            await Task.CompletedTask;
        }

        private async Task EmergencyStop() => await _runningGame.End();
        
        private async Task OnGameEnded()
        {
            _runningGame.GameStarted -= OnGameStarted;
            _runningGame.GameEnded -= OnGameEnded;
            StopRunningGame();

            await Task.CompletedTask;
        }

        private void StopRunningGame()
        {
            _isStartMessageSent = false;
            _runningGame = null;
        }

        private static bool IsCorrectStartGameCommand(SocketMessage message) => message.Content == _startGameCommand;
        private static bool IsCorrectEmergencyStopGameCommand(SocketMessage message) => message.Content == _emergencyStopGameCommand;
    }
}
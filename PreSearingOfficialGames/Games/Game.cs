using Discord;
using System.Runtime.CompilerServices;

namespace PreSearingOfficialGames.Games
{
    public abstract class Game(ulong botId, IMessageChannel channel)
    {
        protected ulong BotId { get; } = botId;
        protected IMessageChannel Channel { get; } = channel;
        protected Dictionary<string, Func<IUserMessage, Task>> _specialCommands;

        protected static async Task FireEvent(Func<Task>? task, [CallerArgumentExpression(nameof(task))] string name = null)
        {
            //TODO: Find a solution to the problem below
            //Problem: in Run method 1st the message is sent, then an exception is thrown and it "disappears"
            //because dc channel listener is triggered, 
            if (task is null) return;
            //throw new ArgumentException($"No subscribers to {name}");

            await task.Invoke();
        }

        //TODO: not many commands on the list but this method might need optimisation in the future
        protected bool IsSpecialCommand(string content)
        {
            if (_specialCommands is null) throw new ArgumentNullException("No special commands available!");
            foreach (var command in _specialCommands.Keys)
            {
                var commandSplit = command.Split(' ');
                foreach (var part in commandSplit)
                {
                    var words = part
                        .Replace("_", " ")
                        .Split(' ');

                    if (ContainsAnySpecialCommandWords(content, words))
                        return true;
                }
            }

            return false;

            static bool ContainsAnySpecialCommandWords(string content, string[] words)
                => words.All(w => content.Contains(w, StringComparison.OrdinalIgnoreCase));

        }

        protected async Task TryRunCommand(IUserMessage message)
        {
            var command = _specialCommands.FirstOrDefault(sc => message.Content.StartsWith(sc.Key));
            if (command.Value is null)
            {
                await Channel.SendMessageAsync("This server will fall if you guys write like this...");
                return;
            }

            await command.Value.Invoke(message);
        }
    }
}

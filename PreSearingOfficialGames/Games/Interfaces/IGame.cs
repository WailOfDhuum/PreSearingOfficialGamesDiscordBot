using Discord;

namespace PreSearingOfficialGames.Games.Interfaces
{
    public interface IGame
    {
        public event Func<Task> GameStarted;
        public event Func<Task> GameEnded;
        Task Run();
        Task ListenForAnswers(IUserMessage message);
        Task End();
    }
}

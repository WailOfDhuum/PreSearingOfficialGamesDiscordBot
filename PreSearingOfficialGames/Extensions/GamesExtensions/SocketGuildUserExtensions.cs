using Discord.WebSocket;

namespace PreSearingOfficialGames.Extensions.GamesExtensions
{
    public static partial class GamesExtensions
    {
        public static bool IsModerator(this SocketGuildUser? user) => user?.GuildPermissions.ModerateMembers ?? false;
        public static string? GetUserName(this SocketGuildUser user) => user.DisplayName ?? user.Username;
    }
}

using Discord;

namespace PreSearingOfficialGames.Extensions.GamesExtensions
{
    public static partial class GamesExtensions
    {
        public static bool IsFromCorrectChannel(this IMessageChannel channel, ulong correctChannelId)
            => channel.Id == correctChannelId;
    }
}

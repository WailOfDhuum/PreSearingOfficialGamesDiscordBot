namespace PreSearingGamesDiscordBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Mad King Thorn bot is on!");

            try
            {
                var bot = new MadKingThornDiscordBot();
                bot.RunBotAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex: {ex}");
            }
        }
    }
}

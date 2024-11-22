namespace PreSearingOfficialGames.Games.YesOrNo
{
    public readonly struct YesOrNoAnswer(ulong messageId, string answer)
    {
        public ulong MessageId { get; } = messageId;
        public string Answer { get; } = answer;
    }
}
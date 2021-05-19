
namespace teams_sso_sample.Options
{
    public record ThrottlingOptions
    {
        public const string ThrottelingSettings = nameof(ThrottelingSettings);

        public int TeamsTeamAndChannelConcurrencyLimit { get; init; }
        public int TeamsQueueLength { get; init; }
        public int TeamsAny { get; init; }
        public int TeamsAnyTimeSpanInSeconds { get; init; }
        public int TeamsGet { get; init; }
        public int TeamsPostPut { get; init; }
        public int TeamsPatch { get; init; }
        public int TeamsDelete { get; init; }
        public int TeamsIdGet { get; init; }
        public int TeamsIdPostPut { get; init; }
        public int TeamsChannelMessageGet { get; init; }
        public int TeamsChannelMessagePost { get; init; }
        public int TeamsGroupChatMessageGet { get; init; }
        public int TeamsGroupChatMessagePost { get; init; }
        public int TeamsIdScheduleGet { get; init; }
        public int TeamsIdSchedulePostPatchPut { get; init; }
        public int TeamsIdScheduleDelete { get; init; }
    }
}

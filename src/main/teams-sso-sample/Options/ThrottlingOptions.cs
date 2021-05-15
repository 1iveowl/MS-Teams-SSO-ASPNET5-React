
namespace teams_sso_sample.Options
{
    public record ThrottlingOptions
    {
        public const string ThrottelingSettings = nameof(ThrottelingSettings);

        public int TeamsConcurrency;
        public int TeamsQueueLength;
    }
}

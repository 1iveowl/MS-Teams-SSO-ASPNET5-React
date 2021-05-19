using System;

namespace teams_sso_sample.Options
{
    public record ApiAppOptions
    {
        public const string ApiAppRegistration = nameof(ApiAppRegistration);

        public string Instance { get; init; }
        public Guid TenantId { get; init; }
        public Guid ClientId { get; init; }
        public string ClientSecret {get; init;}
        public string Audience { get; init; }
        public string Scopes { get; init; }
        public string ValidIssuers { get; init; }
    }
}

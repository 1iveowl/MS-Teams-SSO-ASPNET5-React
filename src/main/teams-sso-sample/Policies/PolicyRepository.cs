using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using teams_sso_sample.Options;

namespace teams_sso_sample.Policies
{
    public static class PolicyRepository
    {
        public static IReadOnlyPolicyRegistry<string> AddPollyPolicyRegistry(
            this IServiceCollection services, 
            ThrottlingOptions throttelingSettings)
        {
            var registry = services.AddPolicyRegistry();
                        
            registry.Add(
                nameof(PolicyName.TeamsBulkheadIsolationPolicy),
                Policy
                .BulkheadAsync<HttpResponseMessage>(
                    maxParallelization: throttelingSettings.TeamsTeamAndChannelConcurrencyLimit,
                    maxQueuingActions: throttelingSettings.TeamsQueueLength,
                    onBulkheadRejectedAsync: async context =>
                    {                        
                        await Task.CompletedTask;
                    }));

            registry.Add(
                nameof(PolicyName.GetTeams),
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)));

            return registry;
        }

        public static IAsyncPolicy<HttpResponseMessage> Selector(IReadOnlyPolicyRegistry<string> policyRegistry,
            HttpRequestMessage httpRequestMessage)
        {
            var pathParts = httpRequestMessage.RequestUri.AbsolutePath.Split('/').Select(p => p.ToLower());

            var isGraphBeta = pathParts.FirstOrDefault() == "beta";
            var serviceLimitKind = GetServiceLimitKind(pathParts);

            if (httpRequestMessage.Method == HttpMethod.Get)
            {
                var policy = Policy.WrapAsync(
                    policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(nameof(PolicyName.TeamsBulkheadIsolationPolicy)),
                    policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("SimpleWaitAndRetryPolicy"));

                return policy;
            }

            return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("SimpleWaitAndRetryPolicy");
        }

        private static GraphApiLimitKind GetServiceLimitKind(IEnumerable<string> pathParts) 
            => pathParts.Skip(1) switch
            {
                var parts when parts.FirstOrDefault() == "teams" => GraphApiLimitKind.TeamsServiceLimit,
                //var parts when parts.FirstOrDefault() == "me" 
                //    && parts.Skip(1).FirstOrDefault() == "joinedTeams" => GraphApiLimitKind.TeamsServiceLimit,

                _ => GraphApiLimitKind.Unknown
            };
    }
}

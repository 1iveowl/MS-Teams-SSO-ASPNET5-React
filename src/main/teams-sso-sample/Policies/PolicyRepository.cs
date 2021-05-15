using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using teams_sso_sample.Options;

namespace teams_sso_sample.Policies
{
    public static class PolicyRepository
    {
        public static IReadOnlyPolicyRegistry<string> AddPollyPolicyRegistry(this IServiceCollection services, IConfiguration configuration)
        {
            var registry = services.AddPolicyRegistry();

            var throttelingSettings = configuration.GetSection(ThrottlingOptions.ThrottelingSettings).Get<ThrottlingOptions>();            

            registry.Add(
                nameof(PolicyName.TeamsBulkheadIsolationPolicy),
                Policy
                .BulkheadAsync<HttpResponseMessage>(
                    maxParallelization: throttelingSettings.TeamsConcurrency,
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
            if (httpRequestMessage.Method == HttpMethod.Get)
            {
                var policy = Policy.WrapAsync(
                    policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(nameof(PolicyName.TeamsBulkheadIsolationPolicy)),
                    policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("SimpleWaitAndRetryPolicy"));

                return policy;
            }

            return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("SimpleWaitAndRetryPolicy");
        }


    }
}

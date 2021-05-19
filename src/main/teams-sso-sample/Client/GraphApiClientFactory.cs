using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace teams_sso_sample.Client
{
    public class GraphApiClientFactory : IGraphApiClientFactory
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly HttpClient _httpClient;

        public GraphApiClientFactory(ITokenAcquisition tokenAcquisition, HttpClient httpClient)
        {
            _tokenAcquisition = tokenAcquisition;
            _httpClient = httpClient;
        }

        public async Task<GraphServiceClient> Create(
            string[] scopes, 
            MediaTypeWithQualityHeaderValue mediaTypeHeader = null)
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes).ConfigureAwait(false);

            return new GraphServiceClient(_httpClient)
            {
                AuthenticationProvider = new DelegateAuthenticationProvider(requestMessage =>
                {
                    requestMessage.Headers.Accept.Clear();
                    requestMessage.Headers.Accept.Add(
                        mediaTypeHeader is null 
                        ? new MediaTypeWithQualityHeaderValue("application/json")
                        : mediaTypeHeader);

                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);

                    return Task.CompletedTask;
                })
            };
        }

        public async Task<GraphServiceClient> Create(MediaTypeWithQualityHeaderValue mediaTypeHeader = null)
        {
            return await Create(new string[] { ".default" }, mediaTypeHeader);
        }
    }
}

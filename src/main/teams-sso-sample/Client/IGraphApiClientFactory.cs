using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace teams_sso_sample.Client
{
    public interface IGraphApiClientFactory
    {
        Task<GraphServiceClient> Create(MediaTypeWithQualityHeaderValue mediaTypeHeader = null);

        Task<GraphServiceClient> Create(string[] scopes, MediaTypeWithQualityHeaderValue mediaTypeHeader = null);
    }
}

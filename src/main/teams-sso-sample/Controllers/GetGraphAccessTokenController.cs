using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Threading.Tasks;

namespace teams_sso_sample.Controllers
{
    [ApiController]
    public class GetGraphAccessTokenController : ControllerBase
    {
        private IDownstreamWebApi _downstreamWebApi;

        public GetGraphAccessTokenController(IDownstreamWebApi downstreamWebApi)
        {
            _downstreamWebApi = downstreamWebApi;
        }

        [Route("[controller]")]
        [Authorize]        
        //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        public async Task<IActionResult> OnGet()
        {
            var value = await _downstreamWebApi.CallWebApiForUserAsync(
             "MyApi",
             options =>
             {
                 options.RelativePath = $"me";
             });
            

            return new OkResult();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System;
using System.Threading.Tasks;
using teams_sso_sample.Client;
using teams_sso_sample.Options;

namespace teams_sso_sample.Controllers
{
    [ApiController]
    public class GraphController : ControllerBase
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IGraphApiClientFactory _graphApiClientFactory;
        private readonly IOptionsSnapshot<MSGraphOptions> _graphOptions;

        public GraphController(
            ITokenAcquisition tokenAcquisition,
            IGraphApiClientFactory graphApiClientFactory,
            IOptionsSnapshot<MSGraphOptions> graphOptions)
        {
            _tokenAcquisition = tokenAcquisition;
            _graphApiClientFactory = graphApiClientFactory;
            _graphOptions = graphOptions;
        }

        [Route("userInfo")]
        [Authorize]
        [RequiredScope(new string[] { "access_as_user" })]
        public async Task<IActionResult> OnGetUserInfo()
            => await SendGraphRequest(async graphClient 
                => await graphClient.Me.Request().GetAsync());

        [Route("userPhoto")]
        [Authorize]
        [RequiredScope(new string[] { "access_as_user" })]
        public async Task<IActionResult> OnGetUserPhoto()
        => await SendGraphRequest(async graphClient
            => await graphClient.Me.Request().GetAsync());

        [Route("checkConsent")]
        [Authorize]
        [RequiredScope(new string[] { "access_as_user" })]
        public async Task<IActionResult> OnCheckConsent() 
            => await GetAccessTokenForUser(async accessToken 
                => await Task.FromResult(new OkResult()));


        [Route("exchangeAccessToken")]
        [Authorize]        
        [RequiredScope(new string[] { "access_as_user" })]
        public async Task<IActionResult> OnGetAccessToken()
            => await GetAccessTokenForUser(async accessToken 
                => await Task.FromResult(new JsonResult(new { data = accessToken }) { StatusCode = 200 }));

        private async Task<IActionResult> GetAccessTokenForUser(Func<string, Task<IActionResult>> accessTokenFunc) 
            => await SendRequest(async () 
                => await accessTokenFunc(await _tokenAcquisition.GetAccessTokenForUserAsync(_graphOptions.Value.Scopes.Split(' '))));

        private async Task<IActionResult> SendGraphRequest<T>(Func<GraphServiceClient, Task<T>> graphFunc) 
            => await SendRequest(async () 
                => await graphFunc(await _graphApiClientFactory.Create()));

        private static async Task<IActionResult> SendRequest<T>(Func<Task<T>> sendFunc)
        {
            try
            {
                return new JsonResult(await sendFunc().ConfigureAwait(false));
            }
            catch (ServiceException serviceException)
            {
                return new JsonResult(new { error = $"{serviceException.Message}" }) { StatusCode = 500 };
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"{ex.Message}" }) { StatusCode = 500 };
            }
        }
    }    
}


//try
//{
//    string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(_graphOptions.Value.Scopes.Split(' '));

//    return await accessTokenFunc(accessToken);
//}
//catch (Exception ex)
//{
//    if (ex.InnerException.Message.Contains("AADSTS65001"))
//    {
//        return new JsonResult(new { error = "consent_required" }) { StatusCode = 403 };
//    }

//    return new JsonResult(new { error = $"Exception: {ex.Message}" }) { StatusCode = 500 };
//}

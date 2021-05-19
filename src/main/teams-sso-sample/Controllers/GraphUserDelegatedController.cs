using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using System.Threading.Tasks;
using teams_sso_sample.Client;

namespace teams_sso_sample.Controllers
{
    [ApiController]
    [Authorize]
    [RequiredScope(new string[] { "access_as_user" })]
    public class GraphUserDelegatedController : ControllerBase
    {
        private readonly IGraphRequestHandler _graphRequestHandler;

        public GraphUserDelegatedController(
            IGraphRequestHandler graphRequestHandler)
        {
            _graphRequestHandler = graphRequestHandler;
        }

        [Route("checkConsent")]
        public async Task<IActionResult> OnCheckConsent() => await _graphRequestHandler.CheckConsent();

        [Route("exchangeAccessToken")]
        public async Task<IActionResult> OnGetAccessToken() => await _graphRequestHandler.GetAccessToken();

        [Route("userInfo")]
        public async Task<IActionResult> OnGetUserInfo() =>
            await _graphRequestHandler.SendGraphRequest(async graphClient =>
                await graphClient.Me.Request().GetAsync());

        [Route("userPhoto")]
        public async Task<IActionResult> OnGetUserPhoto() => 
            await _graphRequestHandler.SendGraphImageRequest(async graphClient => 
                await graphClient.Me.Photo.Content.Request().GetAsync());
    }    
}

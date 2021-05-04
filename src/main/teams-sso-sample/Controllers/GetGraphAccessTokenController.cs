using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.JSInterop.Implementation;
using Microsoft.VisualBasic;
using System;
using System.Threading.Tasks;

namespace teams_sso_sample.Controllers
{
    [ApiController]
    public class GetGraphAccessTokenController : ControllerBase
    {
        private IDownstreamWebApi _downstreamWebApi;
        private readonly ITokenAcquisition _tokenAcquisition;

        static readonly string[] _scopeRequiredByAPI = new string[] { "access_as_user" };

        static readonly string[] _scopes = new string[] { "User.Read profile email offline_access openid" };

        public GetGraphAccessTokenController(
            IDownstreamWebApi downstreamWebApi,
            ITokenAcquisition tokenAcquisition)
        {
            _downstreamWebApi = downstreamWebApi;
            _tokenAcquisition = tokenAcquisition;
        }

        [Route("[controller]")]
        [Authorize]        
        //[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
        public async Task<IActionResult> OnGet()
        {
            HttpContext.VerifyUserHasAnyAcceptedScope(_scopeRequiredByAPI);

            try
            {
                string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
                return new JsonResult(new { access_token = accessToken });
                // call the downstream API with the bearer token in the Authorize header
            }
            //catch (MsalUiRequiredException ex)
            //{
            //    _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader(HttpContext, scopes, ex);
            //}

            //try
            //{
            //    var value = await _downstreamWebApi
            //        .CallWebApiForUserAsync("MSGraphAPI",
            //            options =>
            //            {                            
            //                options.RelativePath = $"me";
            //            });

            //    return new OkResult();
            //}
            catch (Exception ex)
            {
                if (ex.InnerException.Message.Contains("AADSTS65001"))
                {
                    return new JsonResult(new { error = "consent_required" }) { StatusCode = 403 };
                }

                return new JsonResult(new { error = $"Exception: {ex.Message}" }) { StatusCode = 500 };
            }         

            
        }
    }    
}
